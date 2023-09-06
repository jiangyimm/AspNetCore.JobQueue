using AspNetCore.JobQueue.Abstractions;
using AspNetCore.JobQueue.Extensions;
using AspNetCore.JobQueue.JobStorages;
using AspNetCore.JobQueue.JobParams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.JobQueue.JobQueues
{
    // created by DI as singleton
    internal sealed class JobQueue<TJob, TStorageRecord, TStorageProvider> : JobQueueBase
        where TJob : IJob
        where TStorageRecord : IJobStorageRecord, new()
        where TStorageProvider : IJobStorageProvider<TStorageRecord>
    {
        private static readonly Type _tJob = typeof(TJob);
        private static readonly string _tJobName = _tJob.FullName;

        //public due to: https://github.com/FastEndpoints/FastEndpoints/issues/468
        public static readonly string _queueID = _tJobName.ToHash();

        private readonly ParallelOptions _parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
        private readonly CancellationToken _appCancellation;
        private readonly TStorageProvider _storage;
        private readonly SemaphoreSlim _sem = new SemaphoreSlim(0);
        private readonly ILogger _log;
        private readonly IServiceProvider _serviceProvider;
        private TimeSpan _executionTimeLimit = Timeout.InfiniteTimeSpan;
        private bool _isInUse;

        public JobQueue(TStorageProvider storageProvider,
            IHostApplicationLifetime appLife,
            IServiceProvider serviceProvider,
            ILogger<JobQueue<TJob, TStorageRecord, TStorageProvider>> logger)
        {
            _allQueues[_tJob] = this;
            _storage = storageProvider;
            _appCancellation = appLife.ApplicationStopping;
            _serviceProvider = serviceProvider;
            _parallelOptions.CancellationToken = _appCancellation;
            _log = logger;
            JobStorage<TStorageRecord, TStorageProvider>.StorageProvider = _storage;
            JobStorage<TStorageRecord, TStorageProvider>.AppCancellation = _appCancellation;
        }

        internal override void SetExecutionLimits(int concurrencyLimit, TimeSpan executionTimeLimit)
        {
            _parallelOptions.MaxDegreeOfParallelism = concurrencyLimit;
            _executionTimeLimit = executionTimeLimit;
            _ = CommandExecutorTask();
        }

        protected override async Task StoreJobAsync(object job, DateTime? executeAfter, DateTime? expireOn, CancellationToken ct)
        {
            _isInUse = true;
            await _storage.StoreJobAsync(new TStorageRecord()
            {
                QueueID = _queueID,
                JobData = job,
                ExecuteAfter = executeAfter ?? DateTime.Now,
                ExpireOn = expireOn ?? DateTime.Now.AddDays(1)
            }, ct);
            _sem.Release();
        }

        private async Task CommandExecutorTask()
        {
            var records = Enumerable.Empty<TStorageRecord>();
            var batchSize = _parallelOptions.MaxDegreeOfParallelism * 2;

            while (!_appCancellation.IsCancellationRequested)
            {
                try
                {
                    records = await _storage.GetNextBatchAsync(new PendingJobSearchParams<TStorageRecord>()
                    {
                        Limit = batchSize,
                        QueueID = _queueID,
                        CancellationToken = _appCancellation,
                        Match = r => r.QueueID == _queueID &&
                                     !r.IsComplete &&
                                     DateTime.Now >= r.ExecuteAfter &&
                                     DateTime.Now <= r.ExpireOn,
                        TypeOfJob = typeof(TJob)
                    });
                }
                catch (Exception x)
                {
                    _log.StorageRetrieveError(_queueID, _tJobName, x.Message);
                    await Task.Delay(5000);
                    continue;
                }

                if (!records.Any())
                {
                    // if _isInUse is false, a job has never been queued and there's no need for another iteration of the while loop -
                    // until the semaphore is released when the first job is queued.
                    // if _isInUse if true, we need to block until the next job is queued or until 1 min has elapsed.
                    // we need to re-check the storage every minute to see if the user has re-scheduled old jobs while there's no new jobs being queued.
                    // without the 1 minute check, rescheduled jobs will only execute when there's a new job being queued.
                    // which could lead to the rescheduled job being already expired by the time it's executed.
                    await (
                        _isInUse
                            ? Task.WhenAny(_sem.WaitAsync(_appCancellation), Task.Delay(60000))
                            : Task.WhenAny(_sem.WaitAsync(_appCancellation)));
                }
                else
                {
#if NET6_0_OR_GREATER
                    await Parallel.ForEachAsync(records, _parallelOptions, ExecuteCommand);
#else
                    Parallel.ForEach(records, _parallelOptions, async (p) => await ExecuteCommand(p, _appCancellation));
#endif
                }
            }

            async Task ExecuteCommand(TStorageRecord record, CancellationToken _)
            {
                try
                {
                    var job = (TJob)record.JobData;
                    using (var serviceScope = _serviceProvider.CreateScope())
                    {
                        var handler = serviceScope.ServiceProvider.GetService<IJobHandler<TJob>>();
                        await handler.ExecuteAsync(job, new CancellationTokenSource(_executionTimeLimit).Token);
                    }
                }
                catch (Exception x)
                {
                    _log.CommandExecutionCritical(_tJobName, x.Message);

                    while (!_appCancellation.IsCancellationRequested)
                    {
                        try
                        {
                            await _storage.OnHandlerExecutionFailureAsync(record, x, _appCancellation);
                            break;
                        }
                        catch (Exception xx)
                        {
                            _log.StorageOnExecutionFailureError(_queueID, _tJobName, xx.Message);

#pragma warning disable CA2016
                            await Task.Delay(5000);
#pragma warning restore CA2016
                        }
                    }

                    return; //abort execution here
                }

                while (!_appCancellation.IsCancellationRequested)
                {
                    try
                    {
                        record.IsComplete = true;
                        record.ExecuteAt = DateTime.Now;
                        await _storage.MarkJobAsCompleteAsync(record, _appCancellation);
                        break;
                    }
                    catch (Exception x)
                    {
                        _log.StorageMarkAsCompleteError(_queueID, _tJobName, x.Message);

#pragma warning disable CA2016
                        await Task.Delay(5000);
#pragma warning restore CA2016
                    }
                }
            }
        }
    }
}