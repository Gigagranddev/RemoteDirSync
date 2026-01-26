namespace RemoteDirSync.Bot.Jobs;

public enum JobStatus
{
  Pending,
  Running,
  Succeeded,
  Failed,
  NotFound
}

public sealed class JobInfo : ICloneable
{
  public Guid Id { get; init; } = Guid.NewGuid();
  public JobStatus Status { get; set; }
  public string? Error { get; set; }
  public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
  public DateTimeOffset? CompletedAt { get; set; }
  public required object StartInfo { get; set; }
  public IJobStatus? CurrentResult { get; set; }
  public required string JobType { get; set; }

  public object Clone()
  {
    return new JobInfo()
    {
      Id = Id,
      Status = Status,
      Error = Error,
      CreatedAt = CreatedAt,
      CompletedAt = CompletedAt,
      StartInfo = StartInfo,
      CurrentResult = CurrentResult?.Clone() as IJobStatus,
      JobType = JobType
    };
  }
}

public interface IJobStatus : ICloneable
{
  public string GetStatusString();
}

public interface IJobRunner
{
  Task RunAsync(IServiceScope serviceScope, JobInfo jobInfo, CancellationToken cancellationToken = default);
}