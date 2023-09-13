using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.JobQueue.Abstractions;
using AspNetCore.JobQueue.JobParams;

namespace AspNetCore.JobQueue.JobStorages
{
    internal class JobStorage<TStorageRecord, TStorageProvider>
        where TStorageRecord : IJobStorageRecord, new()
        where TStorageProvider : IJobStorageProvider<TStorageRecord>
    {
        internal static TStorageProvider StorageProvider { private get; set; }
        internal static CancellationToken AppCancellation { private get; set; }
        internal static TimeSpan StaleJobPurgingTimeCycle { private set; get; } = TimeSpan.FromHours(1);

        static JobStorage()
        {
            //_ = StaleJobPurgingTask();

            //use longRunning task
            Task.Factory.StartNew(StaleJobPurgingTask, TaskCreationOptions.LongRunning);
        }

        private static async Task StaleJobPurgingTask()
        {
            while (true)
            {
                await Task.Delay(StaleJobPurgingTimeCycle);

                try
                {
                    await StorageProvider.PurgeStaleJobsAsync(new StaleJobSearchParams<TStorageRecord>()
                    {
                        Match = r => r.IsComplete || r.ExpireOn <= DateTime.Now,
                        CancellationToken = AppCancellation
                    });
                }
                catch { }
            }
        }
    }
}