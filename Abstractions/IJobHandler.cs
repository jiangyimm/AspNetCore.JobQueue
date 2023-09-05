namespace AspNetCore.JobQueue.Abstractions;

/// <summary>
/// Represents a job handler.
/// </summary>
/// <typeparam name="TJob"></typeparam>
public interface IJobHandler<TJob> where TJob : IJob
{
    Task ExecuteAsync(TJob job, CancellationToken ct);
}