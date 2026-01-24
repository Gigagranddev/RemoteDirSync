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
    Identical
  }

  public class FileSystemItem
  {
    public required string Name { get; set; }
    public required string PathRelativeToRoot { get; set; }
    public required FileSystemItemType ItemType { get; set; }
    public CounterpartStatus Status { get; set; } = CounterpartStatus.Unknown;
    public ObservableCollection<FileSystemItem> Children { get; set; } = new ObservableCollection<FileSystemItem>();
    public string Sha256Hash { get; set; } = string.Empty;
  }
}
