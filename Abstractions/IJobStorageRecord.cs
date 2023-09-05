namespace AspNetCore.JobQueue.Abstractions;

public interface IJobStorageRecord
{
    /// <summary>
    /// 队列唯一标识，每个job type对应一个
    /// </summary>
    string QueueID { get; set; }
    /// <summary>
    /// job数据
    /// </summary>
    object JobData { get; set; }
    /// <summary>
    /// 在此时间后执行
    /// </summary>
    DateTime ExecuteAfter { get; set; }
    /// <summary>
    /// 在此时间过期
    /// </summary>
    DateTime ExpireOn { get; set; }
    /// <summary>
    /// 实际在此时间执行
    /// </summary>
    DateTime ExecuteAt { get; set; }
    /// <summary>
    /// 是否完成
    /// </summary>
    bool IsComplete { get; set; }
}