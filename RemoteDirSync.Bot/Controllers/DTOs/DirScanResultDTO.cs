using RemoteDirSync.Bot.Jobs;

namespace RemoteDirSync.Bot.Controllers.DTOs
{
  public class DirScanStatusDTO
  {
    public JobStatus Status { get; set; }
    public List<DirScanResultDTO> Results { get; set; } = new List<DirScanResultDTO>();
  }

  public class DirScanResultDTO : ICloneable
  {
    public required string FullPath { get; set; }
    public required string Sha256Hash { get; set; }

    public object Clone()
    {
      return new DirScanResultDTO()
      {
        FullPath = FullPath,
        Sha256Hash = Sha256Hash
      };
    }
  }
}
