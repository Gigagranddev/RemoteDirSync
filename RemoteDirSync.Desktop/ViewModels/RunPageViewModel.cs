using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RemoteDirSync.Bot;
using RemoteDirSync.Bot.Controllers.DTOs;
using RemoteDirSync.Bot.Jobs;
using RemoteDirSync.Desktop.Models;
using RemoteDirSync.Desktop.Util;
using Semi.Avalonia.Tokens.Palette;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RemoteDirSync.Desktop.ViewModels
{
  public class RunPageViewModel : ObservableObject, IAsyncDisposable
  {
    public string SessionName { get; set; } = string.Empty;
    public ObservableCollection<RemoteState> RemoteStates { get; } = new ObservableCollection<RemoteState>();
    public ICommand BackCommand { get; set; }
    public ICommand RefreshAllCommand { get; set; }
    public event EventHandler? OnBackRequested;
    private bool _syncing = false;
    private CancellationTokenSource _jobPollCancelTokenSource = new CancellationTokenSource();

    private List<IHost> _hosts = new List<IHost>();

    public RunPageViewModel(Session session)
    {
      SessionName = session.Name;
      int index = 0;
      foreach (var conn in session.Connections)
      {
        var newState = new RemoteState()
        {
          Name = conn.Name,
          Address = conn.Address.Trim().ToLower(),
          Port = conn.Port,
          RemoteFilePath = conn.RemotePath.Trim().Replace("\\", "/"), //Standardize to forward slashes
          RemoteStateIndex = index++,
        };
        newState.OnFileMoveRequested += (s, e) => HandleFileMoveRequested(s, e);

        RemoteStates.Add(newState);
      }

      RemoteStates[0].SelectedNodes.CollectionChanged += (_, __) => SyncSelection(RemoteStates[0], RemoteStates[1]);
      RemoteStates[1].SelectedNodes.CollectionChanged += (_, __) => SyncSelection(RemoteStates[1], RemoteStates[0]);

      BackCommand = new RelayCommand(() => OnBackRequested?.Invoke(this, EventArgs.Empty));
      RefreshAllCommand = new RelayCommand(async () => await RefreshAll());

      _ = StartLocalHosts();
      StartPollingJobInfos();
    }

    private void StartPollingJobInfos()
    {
      CancellationToken ct = _jobPollCancelTokenSource.Token;
      Task.Run(async () =>
      {
        while (!ct.IsCancellationRequested)
        {
          foreach (var remoteState in RemoteStates)
          {
            try
            {
              var http = remoteState.HttpClient;
              var response = await http.GetAsync("DirScan/GetAllJobs");
              if (response.IsSuccessStatusCode)
              {
                remoteState.CurrentJobs.Clear();
                var content = await response.Content.ReadAsStringAsync();
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                  PropertyNameCaseInsensitive = true,
                };
                var jobs = JsonSerializer.Deserialize<List<JobStatusDTO>>(content, options);
                if (jobs != null)
                {
                  foreach (var jobSummary in jobs)
                  {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                      remoteState.CurrentJobs.Add(jobSummary);
                    });
                  }
                }
              }
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Error polling jobs for {remoteState.Address}:{remoteState.Port} - {ex.Message}");
            }
          }
          await Task.Delay(5000, ct);
        }
      }, ct);
    }

    private void HandleFileMoveRequested(object? sender, EventArgs e)
    {
      if (sender is not RemoteState sourceState || sourceState.SelectedNodes.Count == 0)
      {
        return;
      }
      var otherRemoteStates = RemoteStates.Where(rs => rs != sourceState).ToList();
      HttpClient client = sourceState.HttpClient;
      string encodedPath = Uri.EscapeDataString(sourceState.RemoteFilePath);

      var nodesToSend = sourceState.SelectedNodes.Where(n=> n.ItemType == FileSystemItemType.File).ToList();
      if(nodesToSend.Count != 1)
      {
        nodesToSend = nodesToSend.Where(n => n.Status != CounterpartStatus.Identical && n.Status != CounterpartStatus.Transferred).ToList();
      }

      foreach (var destState in otherRemoteStates)
      {
        var sendRequest = new SendFileRequestDTO()
        {
          FilesToSend = nodesToSend.Select(node => new SendFileDataDTO()
          {
            FilePath = sourceState.RemoteFilePath + "/" + node.PathRelativeToRoot,
            DestinationPort = destState.Port,
            DestinationAddress = destState.Address,
            DestinationPath = destState.RemoteFilePath + "/" + node.PathRelativeToRoot
          }).ToArray()
        };
        
        var jsonContent = JsonSerializer.Serialize(sendRequest);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        var response = client.PostAsync("DirScan/SendFiles", httpContent).Result;
        if (response.IsSuccessStatusCode)
        {
          foreach(var node in destState.SelectedNodes)
          {
            node.Status = CounterpartStatus.Transferred;
          }
        }
      }
      foreach (var node in sourceState.SelectedNodes)
      {
        node.Status = CounterpartStatus.Transferred;
      }


    }

    private void SyncSelection(RemoteState source, RemoteState dest)
    {
      if (_syncing) return;


      _syncing = true;
      try
      {
        dest.SelectedNodes.Clear();
        foreach (var sourceNode in source.SelectedNodes)
        {
          if (dest.ById.TryGetValue(sourceNode.PathRelativeToRoot, out var destNode))
          {
            if (destNode.ItemType == sourceNode.ItemType)
            {
              dest.SelectedNodes.Add(destNode);
            }
          }
        }
      }
      finally
      {
        _syncing = false;
      }
    }

    private async Task StartLocalHosts()
    {
      foreach (var remoteState in RemoteStates)
      {
        if (remoteState.Address == "localhost" || remoteState.Address == "127.0.0.1")
        {
          try
          {
            var localHost = await WebApiHost.StartAsync(Array.Empty<string>(), remoteState.Port);
            _hosts.Add(localHost);
          }
          catch (Exception ex)
          {
            // Log and ignore
            Console.WriteLine($"Failed to start local host for {remoteState.Address}:{remoteState.Port} - {ex.Message}");
          }
        }
      }
    }

    private async Task RefreshAll()
    {
      var tasks = RemoteStates.Select(rs => RefreshRemoteState(rs));
      await Task.WhenAll(tasks);

      // After all remotes are refreshed, compare files pairwise by SHA
      if (RemoteStates.Count == 2)
      {
        var left = RemoteStates[0];
        var right = RemoteStates[1];

        var leftFiles = left.GetAllFileItems().ToDictionary(f => f.PathRelativeToRoot, StringComparer.OrdinalIgnoreCase);
        var rightFiles = right.GetAllFileItems().ToDictionary(f => f.PathRelativeToRoot, StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in leftFiles)
        {
          var path = kvp.Key;
          var leftItem = kvp.Value;

          if (rightFiles.TryGetValue(path, out var rightItem))
          {
            // Both sides have the file; compare SHA
            if (!string.IsNullOrEmpty(leftItem.Sha256Hash) && !string.IsNullOrEmpty(rightItem.Sha256Hash))
            {
              if (string.Equals(leftItem.Sha256Hash, rightItem.Sha256Hash, StringComparison.OrdinalIgnoreCase))
              {
                leftItem.Status = CounterpartStatus.Identical;
                rightItem.Status = CounterpartStatus.Identical;
              }
              else
              {
                leftItem.Status = CounterpartStatus.Different;
                rightItem.Status = CounterpartStatus.Different;
              }
            }
            else
            {
              leftItem.Status = CounterpartStatus.Unknown;
              rightItem.Status = CounterpartStatus.Unknown;
            }
          }
          else
          {
            // Exists only on left
            leftItem.Status = CounterpartStatus.Missing;
          }
        }

        // Files that exist only on right
        foreach (var kvp in rightFiles)
        {
          var path = kvp.Key;
          var rightItem = kvp.Value;
          if (!leftFiles.ContainsKey(path))
          {
            rightItem.Status = CounterpartStatus.Missing;
          }
        }

        PropagateFolderStatus(left, right);
        PropagateFolderStatus(right, left);
      }
    }

    private static void PropagateFolderStatus(RemoteState primary, RemoteState other)
    {
      var primaryDirs = primary.GetAllDirectoryItems()
        .OrderByDescending(d => d.PathRelativeToRoot?.Length ?? 0) // deepest first
        .ToList();

      var otherDirsByPath = other.GetAllDirectoryItems()
        .ToDictionary(d => d.PathRelativeToRoot, StringComparer.OrdinalIgnoreCase);

      foreach (var dir in primaryDirs)
      {
        if (string.IsNullOrEmpty(dir.PathRelativeToRoot))
          continue;

        if (!otherDirsByPath.TryGetValue(dir.PathRelativeToRoot, out var counterpart))
        {
          // counterpart folder missing
          dir.Status = CounterpartStatus.Missing;
          continue;
        }

        if (dir.Children == null || dir.Children.Count == 0)
        {
          // No children -> treat as Unknown (or Identical if you prefer)
          dir.Status = CounterpartStatus.Unknown;
          continue;
        }

        // Look at children statuses (files + subfolders)
        var childStatuses = dir.Children.Select(c => c.Status).ToArray();

        if (childStatuses.All(s => s == CounterpartStatus.Identical))
        {
          dir.Status = CounterpartStatus.Identical;
        }
        else if (childStatuses.Any(s => s == CounterpartStatus.Different ||
                                        s == CounterpartStatus.Missing))
        {
          dir.Status = CounterpartStatus.Different;
        }
        else
        {
          // e.g. all Unknown, or mix of Unknown/Identical but no clear difference
          dir.Status = CounterpartStatus.Unknown;
        }
      }
    }

    private async Task RefreshRemoteState(RemoteState remoteState)
    {
      try
      {
        remoteState.IsScanning = true;
        HttpClient client = remoteState.HttpClient;
        
        string encodedPath = Uri.EscapeDataString(remoteState.RemoteFilePath);

        var response = await client.GetAsync($"DirScan/ScanDir?path={encodedPath}");
        if (!response.IsSuccessStatusCode)
        {
          throw new Exception($"Failed to scan directory {remoteState.RemoteFilePath}, Status: {response.StatusCode}");
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
          remoteState.FileSystemItems.Clear();
        });
        bool hasFinished = false;
        JsonSerializerOptions options = new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true,
        };
        while (!hasFinished)
        {
          var statusResponse = await client.GetAsync($"DirScan/GetScanDirStatus");
          if (statusResponse.IsSuccessStatusCode)
          {
            var statusContent = await statusResponse.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<DirScanStatusDTO>(statusContent, options);
            if (status != null)
            {
              hasFinished = status.Status == JobStatus.Succeeded || status.Status == JobStatus.Failed;
              var result = status.Results.OrderBy(r => r.FullPath).ToArray();
              await Dispatcher.UIThread.InvokeAsync(() =>
              {
                foreach (var fileItem in result)
                {
                  remoteState.UpdateOrCreateFileItem(fileItem, remoteState.RemoteFilePath);
                }
                remoteState.SortFileItems();
              });
              if (!hasFinished)
              {
                await Task.Delay(3000);
              }
            }
          }
          else
          {
            throw new Exception($"Failed to get scan status for directory {remoteState.RemoteFilePath}, Status: {statusResponse.StatusCode}");
          }
        }
      }
      catch (Exception ex)
      {
        // Log and ignore
        Console.WriteLine($"Failed to refresh remote state for {remoteState.Address}:{remoteState.Port} - {ex.Message}");
      }
      finally
      {
        remoteState.IsScanning = false;
      }
    }

    public async ValueTask DisposeAsync()
    {
      foreach (var host in _hosts)
      {
        await host.StopAsync();
        host.Dispose();
      }
    }
  }


  public class DesignRunPageViewModel : RunPageViewModel
  {
    private static Session CreateDesignSession()
    {
      var session = new Session();
      session.Name = "Design Session";
      session.Connections.Add(new Connection()
      {
        Name = "Connection 1",
        Address = "192.169.1.1",
        RemotePath = "/home/user/Folder"
      });
      session.Connections.Add(new Connection()
      {
        Name = "Connection 2",
        Address = "serverb.example.com",
        RemotePath = "/home/user/Folder"
      });
      return session;
    }

    public DesignRunPageViewModel() : base(CreateDesignSession())
    {
      RemoteStates[0].FileSystemItems.Add(new FileSystemItem()
      {
        Name = "Folder",
        ItemType = FileSystemItemType.Directory,
        PathRelativeToRoot = "/home/user/Folder"
      });
      RemoteStates[0].FileSystemItems.Last().Children.Add(new FileSystemItem()
      {
        Name = "File1.txt",
        ItemType = FileSystemItemType.File,
        PathRelativeToRoot = "/home/user/Folder/File1.txt"
      });
      RemoteStates[0].FileSystemItems.Last().Children.Add(new FileSystemItem()
      {
        Name = "File2.txt",
        ItemType = FileSystemItemType.File,
        PathRelativeToRoot = "/home/user/Folder/File2.txt"
      });

      RemoteStates[1].FileSystemItems.Add(new FileSystemItem()
      {
        Name = "Folder",
        ItemType = FileSystemItemType.Directory,
        PathRelativeToRoot = "/home/user/Folder"
      });
      RemoteStates[1].FileSystemItems.Last().Children.Add(new FileSystemItem()
      {
        Name = "File1.txt",
        ItemType = FileSystemItemType.File,
        PathRelativeToRoot = "/home/user/Folder/File1.txt"
      });
      RemoteStates[1].FileSystemItems.Last().Children.Add(new FileSystemItem()
      {
        Name = "File2.txt",
        ItemType = FileSystemItemType.File,
        PathRelativeToRoot = "/home/user/Folder/File2.txt"
      });

      RemoteStates[0].CurrentJobs.Add(new JobStatusDTO() { JobId = Guid.NewGuid(), JobType = "Scan", Status = "Scanning..." });
      RemoteStates[0].CurrentJobs.Add(new JobStatusDTO() { JobId = Guid.NewGuid(), JobType = "SendFile", Status = "Scanning..." });
      RemoteStates[0].CurrentJobs.Add(new JobStatusDTO() { JobId = Guid.NewGuid(), JobType = "SendFile", Status = "Scanning..." });
      RemoteStates[0].CurrentJobs.Add(new JobStatusDTO() { JobId = Guid.NewGuid(), JobType = "SendFile", Status = "Scanning..." });
      RemoteStates[0].CurrentJobs.Add(new JobStatusDTO() { JobId = Guid.NewGuid(), JobType = "SendFile", Status = "Scanning..." });
      RemoteStates[0].CurrentJobs.Add(new JobStatusDTO() { JobId = Guid.NewGuid(), JobType = "SendFile", Status = "Scanning..." });
    }
  }
}
