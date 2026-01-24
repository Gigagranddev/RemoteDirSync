using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RemoteDirSync.Bot.Controllers.DTOs;
using RemoteDirSync.Bot.Jobs;
using RemoteDirSync.Bot.Jobs.Implementations;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace RemoteDirSync.Bot.Controllers
{
  [ApiController]
  [Route("[controller]/[action]")]
  public class DirScanController : ControllerBase
  {
    private readonly IBackgroundJobQueue _queue;

    public DirScanController(
        IBackgroundJobQueue queue)
    {
      _queue = queue;
    }

    [HttpGet]
    public async Task<ActionResult> ScanDir([FromQuery][BindRequired] string path)
    {
      var existingJobs = _queue.GetJobsOfType(typeof(DirScanJobStartInfo));
      if (existingJobs.Any())
      {
        return Ok();
      }

      var jobId = await _queue.EnqueueAsync(new DirScanJobStartInfo()
      {
        Path = path
      });
      return Ok();
    }


    [HttpGet]
    public ActionResult<DirScanStatusDTO> GetScanDirStatus()
    {
      var existingJobs = _queue.GetJobsOfType(typeof(DirScanJobStartInfo));
      if (!existingJobs.Any())
      {
        return Ok(new DirScanStatusDTO()
        {
          Status = JobStatus.NotFound,
        });
      }
      var jobInfo = existingJobs.First();
      var status = new DirScanStatusDTO()
      {
        Status = jobInfo.Status,
        Results = (jobInfo.CurrentResult as ConcurrentBag<DirScanResultDTO>)?.ToList() ?? new List<DirScanResultDTO>(),
      };

      return Ok(status);
    }

    // You can keep SendFile here unchanged for now
    [HttpPost]
    public async Task<ActionResult> SendFile([FromBody] SendFileRequestDTO request)
    {
      return Ok(new { ReceivedCount = 1 });
    }
  }
}