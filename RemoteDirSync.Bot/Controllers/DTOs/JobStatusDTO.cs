namespace RemoteDirSync.Bot.Controllers.DTOs
{
  public class JobStatusDTO
  {
    public required Guid JobId { get; set; }
    public required string Status { get; set; }
    public required string JobType { get; set; }
    public string CurrentResult { get; set; } = string.Empty;
  }
}
