using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RemoteDirSync.Bot.Controllers.DTOs;
using RemoteDirSync.Desktop.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RemoteDirSync.Desktop.Models
{
  public class RemoteState : ObservableObject
  {
    private string _remoteFilePath = string.Empty;
    private bool _isScanning;
    private string _name = string.Empty;
    private string _address = string.Empty;
    private int _port = 5000;
    private int _remoteStateIndex = -1;
    private HttpClient? _httpClient;
    public ICommand MoveSelectedFilesCommand { get; }
    public event EventHandler? OnFileMoveRequested;

    public required int RemoteStateIndex
    {
      get => _remoteStateIndex;
      set
      {
        _remoteStateIndex = value;
        OnPropertyChanged(nameof(RemoteStateIndex));
      }
    }

    public required string Name
    {
      get => _name;
      set
      {
        _name = value;
        OnPropertyChanged(nameof(Name));
      }
    }
    public required string Address
    {
      get => _address;
      set
      {
        _address = value;
        OnPropertyChanged(nameof(Address));
      }
    }
    public required int Port
    {
      get => _port;
      set
      {
        _port = value;
        OnPropertyChanged(nameof(Port));
      }
    }
    public bool IsScanning
    {
      get => _isScanning;
      set
      {
        _isScanning = value;
        OnPropertyChanged(nameof(IsScanning));
      }
    }
    public ObservableCollection<FileSystemItem> FileSystemItems { get; set; } = new();
    public ObservableCollection<FileSystemItem> SelectedNodes { get; } = new();
    public ObservableCollection<JobStatusDTO> CurrentJobs { get; } = new();
    public Dictionary<string, FileSystemItem> ById { get; } = new();

    public HttpClient HttpClient
    {
      get {
        if (_httpClient != null) return _httpClient;
        var handler = new HttpClientHandler
        {
          ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _httpClient = new HttpClient(handler)
        {
          BaseAddress = new Uri($"http://{Address}:{Port}/")
        };
        return _httpClient;
      }
    }

    public required string RemoteFilePath
    {
      get => _remoteFilePath;
      set
      {
        _remoteFilePath = value;
        OnPropertyChanged(nameof(RemoteFilePath));
      }
    }

    public RemoteState()
    {
      MoveSelectedFilesCommand = new RelayCommand(() =>
      {
        OnFileMoveRequested?.Invoke(this, EventArgs.Empty);
      }, () => SelectedNodes.Count > 0);

      SelectedNodes.CollectionChanged += (s, e) =>
      {
        (MoveSelectedFilesCommand as RelayCommand)?.NotifyCanExecuteChanged();
      };
    }

    public IEnumerable<FileSystemItem> GetAllFileItems()
    {
      return ById.Values.Where(item => item.ItemType == FileSystemItemType.File).ToList();
    }

    public IEnumerable<FileSystemItem> GetAllDirectoryItems()
    {
      return ById.Values.Where(item => item.ItemType == FileSystemItemType.Directory);
    }

   

    public void UpdateOrCreateFileItem(DirScanResultDTO dirScanResult, string rootFolder)
    {
      if (dirScanResult == null) throw new ArgumentNullException(nameof(dirScanResult));
      if (string.IsNullOrWhiteSpace(dirScanResult.FullPath)) return;

      // Normalize separators and split into path segments
      var normalizedPath = dirScanResult.FullPath.Substring(rootFolder.Length + 1).Replace("\\", "/");
      var pathParts = normalizedPath
        .Split('/', StringSplitOptions.RemoveEmptyEntries)
        .ToArray();

      if (pathParts.Length == 0) return;

      // Traverse or create down the tree until the last segment (file) is reached
      FileSystemItem? currentParent = null;
      var collection = FileSystemItems;

      for (int i = 0; i < pathParts.Length; i++)
      {
        bool isLast = i == pathParts.Length - 1;
        string segment = pathParts[i];

        // For all but the last segment we treat them as directories
        bool isDirectory = !isLast;

        var existing = FindChild(collection, segment, isDirectory);
        if (existing == null)
        {
          existing = CreateChild(segment, isDirectory, currentParent);
          collection.Add(existing);
          ById[existing.PathRelativeToRoot] = existing;
        }

        currentParent = existing;

        // For the next iteration, descend into the child collection
        collection = existing.Children;
      }

      // At this point currentParent represents the last segment (file)
      if (currentParent != null)
      {
        ApplyDirScanResult(currentParent, pathParts.Last(), dirScanResult.Sha256Hash);
      }
    }

    private static FileSystemItem? FindChild(ObservableCollection<FileSystemItem> collection, string name, bool isDirectory)
    {
      return collection.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)
                                            && x.ItemType == (isDirectory ? FileSystemItemType.Directory : FileSystemItemType.File));
    }

    private static FileSystemItem CreateChild(string path, bool isDirectory, FileSystemItem? parent)
    {
      int lastSeparatorIndex = path.LastIndexOf('/');
      string name = lastSeparatorIndex > 0 ? path[(lastSeparatorIndex + 1)..] : path;
      var item = new FileSystemItem
      {
        ItemType = isDirectory ? FileSystemItemType.Directory : FileSystemItemType.File,
        Name = name,
        PathRelativeToRoot = parent != null ? parent.PathRelativeToRoot + "/" + name : name,
      };

      return item;
    }

    private static void ApplyDirScanResult(FileSystemItem target, string name, string sha)
    {
      target.Name = name;
      target.Sha256Hash = sha;
    }

    internal void SortFileItems()
    {
      FileSystemItems.Sort(
        item => item.ItemType == FileSystemItemType.Directory ? -1 : 1,
        item => item.Name.ToLowerInvariant()
      );
      foreach (var item in FileSystemItems)
      {
        SortChildrenRecursively(item);
      }
    }

    private void SortChildrenRecursively(FileSystemItem item)
    {
      if (item.Children != null)
      {
        item.Children.Sort(
          child => child.ItemType == FileSystemItemType.Directory ? -1 : 1,
          child => child.Name.ToLowerInvariant()
        );

        foreach (var child in item.Children)
        {
          SortChildrenRecursively(child);
        }
      }
    }
  }
}
