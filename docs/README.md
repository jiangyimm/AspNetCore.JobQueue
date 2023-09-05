### Install Package
dotnet add package AspNetCore.JobQueue

### Create Job
创建你的Job，继承于IJob
```CSharp
public class OrderJob : IJob
{
    public string OrderId { get; set; }
    public string OrderName { get; set; }
}
```

### Create JobHandler
创建你的job handler，用来执行job，在ExecuteAsync方法里面写执行的代码
```CSharp
public class OrderJobHandler : IJobHandler<OrderJob>
{
    public async Task ExecuteAsync(OrderJob job, CancellationToken ct)
    {
        await Task.Delay(2000);
        Console.WriteLine("exec job: {0} {1}", job.OrderId, job.OrderName);
    }
}
```

### Create JobRecord
JobRecord用来做队列的持久化，根据项目的特点自行调整。
此处用entity framework code first做示例
```CSharp
[Table("job_record", Schema = "myjob")]
public class JobRecord : IJobStorageRecord
{
    [Key]
    [Column("id")]
    public Guid ID { get; set; }
    [Column("queue_id")]
    public string QueueID { get; set; }
    [NotMapped]
    public object JobData { get; set; }
    [Column("j_command")]
    public JsonElement JCommand { get; set; }
    [Column("execute_after")]
    public DateTime ExecuteAfter { get; set; }
    [Column("expire_on")]
    public DateTime ExpireOn { get; set; }
    [Column("execute_at")]
    public DateTime ExecuteAt { get; set; }
    [Column("is_complete")]
    public bool IsComplete { get; set; }
}
```

### Create JobRecordStorage
创建你的JobRecordStorage，用来实现JobRecord的持久化操作
```CSharp
public class JobRecordStorage : IJobStorageProvider<JobRecord>
{
    private readonly IDbContextFactory<JobDbContext> _dbContextFactory;

    public JobRecordStorage(IDbContextFactory<JobDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task StoreJobAsync(JobRecord r, CancellationToken ct)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        r.JCommand = JsonSerializer.SerializeToElement(r.JobData);
        await dbContext.JobRecords.AddAsync(r, ct);
        await dbContext.SaveChangesAsync(ct);
    }
    public async Task<IEnumerable<JobRecord>> GetNextBatchAsync(PendingJobSearchParams<JobRecord> parameters)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(parameters.CancellationToken);

        var result = await dbContext.JobRecords.AsNoTracking()
                        .Where(parameters.Match)
                        .Take(parameters.Limit)
                        .ToListAsync(parameters.CancellationToken);
        result.ForEach(p => p.JobData = JsonSerializer.Deserialize(p.JCommand, parameters.TypeOfJob));
        return result;
    }

    public async Task MarkJobAsCompleteAsync(JobRecord r, CancellationToken ct)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.JobRecords.Where(p => p.ID == r.ID)
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.IsComplete, true), ct);
    }

    public async Task OnHandlerExecutionFailureAsync(JobRecord r, Exception exception, CancellationToken ct)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.JobRecords.Where(p => p.ID == r.ID)
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.ExecuteAfter, DateTime.Now.AddMinutes(1)), ct);
    }

    public async Task PurgeStaleJobsAsync(StaleJobSearchParams<JobRecord> parameters)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync(parameters.CancellationToken);

        await dbContext.JobRecords.Where(parameters.Match)
            .ExecuteDeleteAsync(parameters.CancellationToken);
    }
}
```

### Inject JobQueue
```CSharp
builder.Services.AddJobQueues<JobRecord, JobRecordStorage>();
...
app.UseJobQueues();
```