using RemoteDirSync.Bot.Jobs.Implementations;

namespace RemoteDirSync.Bot.Jobs
{
  public static class JobFactory
  {
    public static IJobRunner CreateJobRunner(object startInfo)
    {
      switch (startInfo)
      {
        case DirScanJobStartInfo dirScanJobStartInfo:
          return new DirScanJob(dirScanJobStartInfo);
        case SendFileJobStartInfo sendFileJobStartInfo:
          return new SendFileJob(sendFileJobStartInfo);
        default:
          throw new NotSupportedException($"Job with start info of type {startInfo.GetType().FullName} is not supported.");
        }
      }
  }
}
