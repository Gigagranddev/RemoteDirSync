using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDirSync.Desktop.Models
{
  public enum FileSystemItemType
  {
    File,
    Directory
  }

  public enum CounterpartStatus
  {
    Unknown,
    Missing,
    Different,
    Identical,
    Transferred
  }

  public class FileSystemItem : ObservableObject
  {
    private CounterpartStatus _status = CounterpartStatus.Unknown;
    private string _name = string.Empty;
    private string _pathRelativeToRoot = string.Empty;
    private FileSystemItemType _itemType;
    private string _sha256Hash = string.Empty;

    public required string Name
    {
      get => _name;
      set
      {
        if (_name == value) return;
        _name = value;
        OnPropertyChanged(nameof(Name));
      }
    }

    public required string PathRelativeToRoot
    {
      get => _pathRelativeToRoot;
      set
      {
        if (_pathRelativeToRoot == value) return;
        _pathRelativeToRoot = value;
        OnPropertyChanged(nameof(PathRelativeToRoot));
      }
    }

    public required FileSystemItemType ItemType
    {
      get => _itemType;
      set
      {
        if (_itemType == value) return;
        _itemType = value;
        OnPropertyChanged(nameof(ItemType));
      }
    }

    public CounterpartStatus Status
    {
      get => _status;
      set
      {
        if (_status == value) return;
        _status = value;
        OnPropertyChanged(nameof(Status));
      }
    }

    public ObservableCollection<FileSystemItem> Children { get; set; } = new ObservableCollection<FileSystemItem>();

    public string Sha256Hash
    {
      get => _sha256Hash;
      set
      {
        if (_sha256Hash == value) return;
        _sha256Hash = value;
        OnPropertyChanged(nameof(Sha256Hash));
      }
    }
  }
}
