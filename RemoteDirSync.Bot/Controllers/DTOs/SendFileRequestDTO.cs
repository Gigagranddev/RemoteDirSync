namespace RemoteDirSync.Bot.Controllers.DTOs
{
  public class SendFileRequestDTO
  {
    public SendFileDataDTO[] FilesToSend { get; set; } = Array.Empty<SendFileDataDTO>();
  }

  public class SendFileDataDTO
  {
    public string FilePath { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public int DestinationPort { get; set; }
    public string DestinationPath { get; set; } = string.Empty;
  }
}
