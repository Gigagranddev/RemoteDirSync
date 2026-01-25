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
      if (existingJobs.Any(j => j.Status == JobStatus.Running || j.Status == JobStatus.Pending))
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
      var existingJobs = _queue.GetJobsOfType(typeof(DirScanJobStartInfo)).OrderByDescending(j => j.CreatedAt);
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
    public async Task<ActionResult> SendFiles([FromBody] SendFileRequestDTO request)
    {
      var sendFileJobInfo = request.FilesToSend.Select(f => new SendFileJobStartInfo()
      {
        DestinationAddress = f.DestinationAddress,
        DestinationPort = f.DestinationPort,
        DestinationPath = f.DestinationPath,
        FilePath = f.FilePath
      });
      foreach(var job in sendFileJobInfo)
      {
        await _queue.EnqueueAsync(job);
      }
      return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveFile([FromForm]IFormFile file, [FromForm]string destinationPath)
    {
      if (file == null || file.Length == 0)
        return BadRequest("Empty file.");

      Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
      await using var fs = System.IO.File.Create(destinationPath);
      await file.CopyToAsync(fs);

      return Ok();
    }
  }
}