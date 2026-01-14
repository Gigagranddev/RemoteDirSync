using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RemoteDirSync.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RemoteDirSync.ViewModels
{
  public class RunPageViewModel : ObservableObject
  {
    public string SessionName { get; set; } = string.Empty;
    public ObservableCollection<RemoteState> RemoteStates { get; } = new ObservableCollection<RemoteState>();
    public ICommand BackCommand { get; set; }
    public event EventHandler? OnBackRequested;

    public RunPageViewModel(Session session)
    {
      SessionName = session.Name;
      foreach (var conn in session.Connections)
      {
        RemoteStates.Add(new RemoteState()
        {
          Name = conn.Name,
          Address = conn.Address
        });
      }
     
      BackCommand = new RelayCommand(() => OnBackRequested?.Invoke(this, EventArgs.Empty));
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
        Address = "192.169.1.1"
      });
      session.Connections.Add(new Connection()
        {
        Name = "Connection 2",
        Address = "serverb.example.com"
      });
      return session;
    }

    public DesignRunPageViewModel() : base(CreateDesignSession())
    {
      RemoteStates[0].FileSystemItems.Add(new FileSystemItem()
      {
        Name = "Folder",
        ItemType = FileSystemItemType.Directory,
        SizeInBytes = 0,
        LastModified = DateTime.Now.AddDays(-1)
      });
      RemoteStates[0].FileSystemItems[0].Children.Add(new FileSystemItem()
      {
        Name = "File1.txt",
        ItemType = FileSystemItemType.File,
        SizeInBytes = 1024,
        LastModified = DateTime.Now.AddDays(-1)
      });
      RemoteStates[0].FileSystemItems[0].Children.Add(new FileSystemItem()
      {
        Name = "File2.txt",
        ItemType = FileSystemItemType.File,
        SizeInBytes = 1024,
        LastModified = DateTime.Now.AddDays(-1)
      });

      RemoteStates[1].FileSystemItems.Add(new FileSystemItem()
      {
        Name = "Folder",
        ItemType = FileSystemItemType.Directory,
        SizeInBytes = 0,
        LastModified = DateTime.Now.AddDays(-1)
      });
      RemoteStates[1].FileSystemItems[0].Children.Add(new FileSystemItem()
      {
        Name = "File1.txt",
        ItemType = FileSystemItemType.File,
        SizeInBytes = 1024,
        LastModified = DateTime.Now.AddDays(-1)
      });
      RemoteStates[1].FileSystemItems[0].Children.Add(new FileSystemItem()
      {
        Name = "File2.txt",
        ItemType = FileSystemItemType.File,
        SizeInBytes = 1024,
        LastModified = DateTime.Now.AddDays(-1)
      });
    }
  }
}
