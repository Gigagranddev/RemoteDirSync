using RemoteDirSync.Bot.Controllers.DTOs;
using System.Collections.Concurrent;

namespace RemoteDirSync.Bot.Services;

// Shared scanning logic used by background worker
public sealed class DirScannerService
{
  public async Task<DirScanResultDTO[]> ScanAsync(string path, ConcurrentBag<DirScanResultDTO> resultsBag, CancellationToken ct)
  {
    var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);

    await Parallel.ForEachAsync(files, ct, async (file, token) =>
    {
      var hash = await GetFileHashAsync(file, token);
      resultsBag.Add(new DirScanResultDTO
      {
        FullPath = file,
        Sha256Hash = hash
      });
    });

    return resultsBag.ToArray();
  }

  private static async Task<string> GetFileHashAsync(string filePath, CancellationToken ct)
  {
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    await using var stream = File.OpenRead(filePath);
    var hash = await sha256.ComputeHashAsync(stream, ct);
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
  }
}