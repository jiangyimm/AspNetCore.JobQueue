﻿using System.Linq.Expressions;
using AspNetCore.JobQueue.Abstractions;

namespace AspNetCore.JobQueue.JobParams;

/// <summary>
/// a dto representing search parameters for matching stale job storage records
/// </summary>
/// <typeparam name="TStorageRecord">the type of storage record</typeparam>
public struct StaleJobSearchParams<TStorageRecord> where TStorageRecord : IJobStorageRecord
{
    /// <summary>
    /// a boolean lambda expression to match stale job records
    /// <code>
    ///     r => r.IsComplete || r.ExpireOn &lt;= DateTime.Now
    /// </code>
    /// </summary>
    public Expression<Func<TStorageRecord, bool>> Match { get; internal set; }

    /// <summary>
    /// cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; internal set; }
}