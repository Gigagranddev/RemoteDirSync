namespace RemoteDirSync.Bot.Jobs;

public enum JobStatus
{
  Pending,
  Running,
  Succeeded,
  Failed,
  NotFound
}

public sealed class JobInfo
{
  public Guid Id { get; init; } = Guid.NewGuid();
  public JobStatus Status { get; set; }
  public string? Error { get; set; }
  public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
  public DateTimeOffset? CompletedAt { get; set; }
  public required object StartInfo { get; set; }
  public object? CurrentResult { get; set; }
}

public interface IJobRunner
{
  Task RunAsync(IServiceScope serviceScope, JobInfo jobInfo, CancellationToken cancellationToken = default);
}