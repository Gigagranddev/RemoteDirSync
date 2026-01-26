
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

  public class SendFileJobStatus : IJobStatus
  {
    public long BytesSent { get; set; }
    public long TotalBytes { get; set; }
    public required string FileName { get; set; }

    public string JobType => nameof(SendFileJob);

    public object Clone()
    {
      return new SendFileJobStatus()
      {
        BytesSent = BytesSent,
        TotalBytes = TotalBytes,
        FileName = FileName
      };
    }

    public string GetStatusString()
    {
      var ratio = TotalBytes == 0 ? 0d : (double)BytesSent / TotalBytes;
      return $"{FileName}: Sent {BytesSent} of {TotalBytes} bytes. {ratio:P0}";
    }
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
      var currentStatus = new SendFileJobStatus() { FileName = Path.GetFileName(_sendFileJobInfo.FilePath) };
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

      using var progressStream = new ProgressStream(fileStream, bytesSent =>
      {
        currentStatus.BytesSent = bytesSent;
      });

      using var content = new MultipartFormDataContent();
      var fileContent = new StreamContent(progressStream);
      // optional: let server know about content type
      fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

      content.Add(fileContent, "file", fileInfo.Name);
      content.Add(new StringContent(_sendFileJobInfo.DestinationPath), "destinationPath");

      var response = await httpClient.PostAsync("DirScan/ReceiveFile", content, cancellationToken);
      response.EnsureSuccessStatusCode();
    }

    private class ProgressStream : Stream
    {
      private readonly Stream _inner;
      private readonly Action<long> _progressCallback;
      private long _bytesRead;

      public ProgressStream(Stream inner, Action<long> progressCallback)
      {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _progressCallback = progressCallback ?? throw new ArgumentNullException(nameof(progressCallback));
      }

      public override bool CanRead => _inner.CanRead;
      public override bool CanSeek => _inner.CanSeek;
      public override bool CanWrite => _inner.CanWrite;
      public override long Length => _inner.Length;

      public override long Position
      {
        get => _inner.Position;
        set => _inner.Position = value;
      }

      public override void Flush() => _inner.Flush();

      public override int Read(byte[] buffer, int offset, int count)
      {
        var read = _inner.Read(buffer, offset, count);
        if (read > 0)
        {
          _bytesRead += read;
          _progressCallback(_bytesRead);
        }
        return read;
      }

      public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
      {
        var read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        if (read > 0)
        {
          _bytesRead += read;
          _progressCallback(_bytesRead);
        }
        return read;
      }

      public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
      public override void SetLength(long value) => _inner.SetLength(value);
      public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    }
  }
}
