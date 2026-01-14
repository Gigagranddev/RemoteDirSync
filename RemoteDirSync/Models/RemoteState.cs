using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDirSync.Models
{
  public class RemoteState
  {
    public required string Name { get; set; }
    public required string Address { get; set; }
    public ObservableCollection<FileSystemItem> FileSystemItems { get; set; } = new ObservableCollection<FileSystemItem>();
  }
}
