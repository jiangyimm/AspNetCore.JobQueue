using Microsoft.Extensions.Logging;

namespace AspNetCore.JobQueue.Extensions;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Error, "Job storage 'get-next-batch' error for [queue-id:{queueID}]({tJob}): {msg}. Retrying in 5 seconds...")]
    public static partial void StorageRetrieveError(this ILogger logger, string queueID, string tJob, string msg);

    [LoggerMessage(2, LogLevel.Critical, "Job [{tJob}] 'execution' error: [{msg}]")]
    public static partial void CommandExecutionCritical(this ILogger logger, string tJob, string msg);

    [LoggerMessage(3, LogLevel.Error, "Job storage 'on-execution-failure' error for [queue-id:{queueID}]({tJob}): {msg}. Retrying in 5 seconds...")]
    public static partial void StorageOnExecutionFailureError(this ILogger logger, string queueID, string tJob, string msg);

    [LoggerMessage(4, LogLevel.Error, "Job storage 'mark-as-complete' error for [queue-id:{queueID}]({tJob}): {msg}. Retrying in 5 seconds...")]
    public static partial void StorageMarkAsCompleteError(this ILogger l, string queueID, string tJob, string msg);
}