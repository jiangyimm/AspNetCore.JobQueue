using Microsoft.Extensions.Logging;

namespace AspNetCore.JobQueue.Extensions
{
    internal static partial class LoggingExtensions
    {
        //[LoggerMessage(1, LogLevel.Error, "Job storage 'get-next-batch' error for [queue-id:{queueID}]({tJob}): {msg}. Retrying in 5 seconds...")]
        internal static void StorageRetrieveError(this ILogger logger, string queueID, string tJob, string msg)
        {
            logger.LogError(1, "Job storage 'get-next-batch' error for [queue-id:{queueID}]({tJob}): {msg}. Retrying in 5 seconds...", queueID, tJob, msg);
        }

        //[LoggerMessage(2, LogLevel.Critical, "Job [{tJob}] 'execution' error: [{msg}]")]
        public static void CommandExecutionCritical(this ILogger logger, string tJob, string msg)
        {
            logger.LogCritical(2, "Job [{tJob}] 'execution' error: [{msg}]", tJob, msg);
        }

        //[LoggerMessage(3, LogLevel.Error, "Job storage 'on-execution-failure' error for [queue-id:{queueID}]({tJob}): {msg}. Retrying in 5 seconds...")]
        public static void StorageOnExecutionFailureError(this ILogger logger, string queueID, string tJob, string msg)
        {
            logger.LogError(3, "Job storage 'on-execution-failure' error for [queue-id:{queueID}]({tJob}): {msg}. Retrying in 5 seconds...", queueID, tJob, msg);
        }

        //[LoggerMessage(4, LogLevel.Error, "Job storage 'mark-as-complete' error for [queue-id:{queueID}]({tJob}): {msg}. Retrying in 5 seconds...")]
        public static void StorageMarkAsCompleteError(this ILogger logger, string queueID, string tJob, string msg)
        {
            logger.LogError(4, "Job storage 'mark-as-complete' error for [queue-id:{queueID}]({tJob}): {msg}. Retrying in 5 seconds...", queueID, tJob, msg);
        }
    }
}