using System.Collections.Concurrent;
using AspNetCore.JobQueue.Abstractions;

namespace AspNetCore.JobQueue.JobQueues;

internal abstract class JobQueueBase
{
    internal static readonly ConcurrentDictionary<Type, JobQueueBase> _allQueues = new();

    protected abstract Task StoreJobAsync(object job, DateTime? executeAfter, DateTime? expireOn, CancellationToken ct);

    internal abstract void SetExecutionLimits(int ConcurrencyLimit, TimeSpan executionTimeLimit);

    internal static Task AddToQueueAsync(object job, DateTime? executeAfter, DateTime? expireOn, CancellationToken ct)
    {
        var typeofjob = job.GetType();

        if (!_allQueues.TryGetValue(typeofjob, out var queue))
        {
            throw new InvalidOperationException($"A job queue has not been registered for [{typeofjob.FullName}]");
        }

        return queue.StoreJobAsync(job, executeAfter, expireOn, ct);
    }
}