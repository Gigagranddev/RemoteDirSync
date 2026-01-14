using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDirSync.Models
{
  public enum FileSystemItemType
  {
    File,
    Directory
  }

  public class FileSystemItem
  {
    public required string Name { get; set; }
    public required FileSystemItemType ItemType { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime LastModified { get; set; }
    public ObservableCollection<FileSystemItem> Children { get; set; } = new ObservableCollection<FileSystemItem>();
  }
}
