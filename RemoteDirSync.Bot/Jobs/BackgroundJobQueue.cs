using RemoteDirSync.Bot.Jobs.Implementations;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace RemoteDirSync.Bot.Jobs;

public interface IBackgroundJobQueue
{
  Task<Guid> EnqueueAsync(object startInfo, CancellationToken cancellationToken = default);
  ValueTask<JobInfo> DequeueAsync(CancellationToken cancellationToken);
  public bool TryGet(Guid id, out JobInfo? job);
  List<JobInfo> GetJobsOfType(Type type);
  List<JobInfo> GetAllJobs();
}

// Channel-based in-memory queue
public sealed class BackgroundJobQueue : IBackgroundJobQueue
{
  private readonly Channel<JobInfo> _channel;
  private readonly ConcurrentDictionary<Guid, JobInfo> _jobs = new();

  public BackgroundJobQueue()
  {
    var options = new BoundedChannelOptions(100)
    {
      SingleReader = true,
      SingleWriter = false,
      FullMode = BoundedChannelFullMode.Wait
    };
    _channel = Channel.CreateBounded<JobInfo>(options);
  }

  public async Task<Guid> EnqueueAsync(object startInfo, CancellationToken cancellationToken = default)
  {
    var job = CreateJob(startInfo);
    await _channel.Writer.WriteAsync(job, cancellationToken);
    return job.Id;
  }

  public ValueTask<JobInfo> DequeueAsync(CancellationToken cancellationToken) =>
      _channel.Reader.ReadAsync(cancellationToken);

  public bool TryGet(Guid id, out JobInfo? job)
  {
    if (_jobs.TryGetValue(id, out JobInfo? realJob))
    {
      job = realJob.Clone() as JobInfo;
      return true;
    }
    job = null;
    return false;
  }

  public List<JobInfo> GetJobsOfType(Type type)
  {
    return _jobs.Values.Where(j => j.StartInfo.GetType() == type).Select(j => j.Clone()).Cast<JobInfo>().ToList();
  }

  public List<JobInfo> GetAllJobs()
  {
    return _jobs.Values.Select(j => j.Clone()).Cast<JobInfo>().ToList();
  }
  private JobInfo CreateJob(object startInfo)
  {
    string jobType = "";
    switch(startInfo)
    {
      case DirScanJobStartInfo:
        jobType = "Dir Scan";
        break;
      case SendFileJobStartInfo:
        jobType = "Send File";
        break;
      default:
        throw new ArgumentException("Unsupported job start info type", nameof(startInfo));
    }
    var job = new JobInfo()
    {
      Id = Guid.NewGuid(),
      Status = JobStatus.Pending,
      StartInfo = startInfo,
      JobType = jobType
    };
    _jobs[job.Id] = job;
    return job;
  }

}