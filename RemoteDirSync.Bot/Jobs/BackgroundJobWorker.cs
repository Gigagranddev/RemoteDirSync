using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteDirSync.Bot.Controllers.DTOs;
using RemoteDirSync.Bot.Services;

namespace RemoteDirSync.Bot.Jobs;

public sealed class BackgroundJobWorker : BackgroundService
{
  private readonly IBackgroundJobQueue _queue;
  private readonly ILogger<BackgroundJobWorker> _logger;
  private readonly IServiceProvider _services;

  public BackgroundJobWorker(
      IBackgroundJobQueue queue,
      ILogger<BackgroundJobWorker> logger,
      IServiceProvider services)
  {
    _queue = queue;
    _logger = logger;
    _services = services;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      JobInfo job;
      try
      {
        job = await _queue.DequeueAsync(stoppingToken);
      }
      catch (OperationCanceledException)
      {
        break;
      }

      job.Status = JobStatus.Running;

      try
      {
        // Resolve the controller or a dedicated service that does the scan
        using var scope = _services.CreateScope();
        var jobRunner = JobFactory.CreateJobRunner(job.StartInfo);
        await jobRunner.RunAsync(scope, job, stoppingToken);

        job.Status = JobStatus.Succeeded;
        job.CompletedAt = DateTimeOffset.UtcNow;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Dir scan job {JobId} failed", job.Id);
        job.Status = JobStatus.Failed;
        job.Error = ex.Message;
        job.CompletedAt = DateTimeOffset.UtcNow;
      }
    }
  }
}