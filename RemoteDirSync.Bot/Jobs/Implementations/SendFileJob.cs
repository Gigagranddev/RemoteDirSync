
using System.IO;

namespace RemoteDirSync.Bot.Jobs.Implementations
{
  public class SendFileJobStartInfo
  {
    public required string FilePath { get; set; }
    public required string DestinationAddress { get; set; }
    public required int DestinationPort { get; set; }
    public required string DestinationPath { get; set; }
  }

  public class SendFileJobStatus
  {
    public long BytesSent { get; set; }
    public long TotalBytes { get; set; }
  }

  public class SendFileJob : IJobRunner
  {
    private SendFileJobStartInfo _sendFileJobInfo;
    public SendFileJob(SendFileJobStartInfo sendFileJobInfo)
    {
      _sendFileJobInfo = sendFileJobInfo;
    }

    public async Task RunAsync(IServiceScope serviceScope, JobInfo jobInfo, CancellationToken cancellationToken = default)
    {
      var currentStatus = new SendFileJobStatus();
      jobInfo.CurrentResult = currentStatus;
      var fileInfo = new FileInfo(_sendFileJobInfo.FilePath);
      currentStatus.TotalBytes = fileInfo.Length;
      IHttpClientFactory httpFactory = serviceScope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
      var httpClient = httpFactory.CreateClient("fileTransferClient");
      httpClient.BaseAddress = new Uri($"http://{_sendFileJobInfo.DestinationAddress}:{_sendFileJobInfo.DestinationPort}/");

      await using var fileStream = new FileStream(
      _sendFileJobInfo.FilePath,
      FileMode.Open,
      FileAccess.Read,
      FileShare.Read,
      bufferSize: 64 * 1024,
      useAsync: true);

      using var content = new MultipartFormDataContent();
      var fileContent = new StreamContent(fileStream);
      // optional: let server know about content type
      fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

      content.Add(fileContent, "file", fileInfo.Name);
      content.Add(new StringContent(_sendFileJobInfo.DestinationPath), "destinationPath");

      var response = await httpClient.PostAsync("DirScan/ReceiveFile", content, cancellationToken);
      response.EnsureSuccessStatusCode();
    }
  }
}
