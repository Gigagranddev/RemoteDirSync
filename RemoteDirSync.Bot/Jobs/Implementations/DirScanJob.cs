
using RemoteDirSync.Bot.Controllers.DTOs;
using RemoteDirSync.Bot.Services;
using System.Collections.Concurrent;

namespace RemoteDirSync.Bot.Jobs.Implementations
{
  public class DirScanJobStartInfo 
  {
    public string Path { get; set; } = string.Empty;

    
  }

  public class DirScanJob : IJobRunner
  {
    private DirScanJobStartInfo _dirScanJobStartInfo;

    public DirScanJob(DirScanJobStartInfo dirScanJobStartInfo)
    {
      _dirScanJobStartInfo = dirScanJobStartInfo;
    }

    public async Task RunAsync(IServiceScope scope, JobInfo job, CancellationToken cancellationToken = default)
    {
      var resultBag = new ConcurrentBag<DirScanResultDTO>();
      job.CurrentResult = resultBag;

      var dirScanner = scope.ServiceProvider.GetRequiredService<DirScannerService>();
      await dirScanner.ScanAsync(_dirScanJobStartInfo.Path, resultBag, cancellationToken);
    }
  }
}
