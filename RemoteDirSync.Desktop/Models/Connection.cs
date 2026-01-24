using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDirSync.Desktop.Models
{
  public class Connection : ObservableObject
  {
    private string _address = string.Empty;
    private string _addressErrorMessage = string.Empty;
    private string _remotePath = string.Empty;
    private string _remotePathErrorMessage = string.Empty;
    private int _port = 5000;
    private string _portErrorMessage = string.Empty;
    private string _name = string.Empty;

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

    public string Address
    {
      get => _address;
      set
      {
        if (_address == value) return;
        _address = value;
        OnPropertyChanged(nameof(Address));
      }
    }

    public string AddressErrorMessage
    {
      get => _addressErrorMessage;
      set
      {
        if (_addressErrorMessage == value) return;
        _addressErrorMessage = value;
        OnPropertyChanged(nameof(AddressErrorMessage));
      }
    }

    public string RemotePath
    {
      get => _remotePath;
      set
      {
        if (_remotePath == value) return;
        _remotePath = value;
        OnPropertyChanged(nameof(RemotePath));
      }
    }

    public string RemotePathErrorMessage
    {
      get => _remotePathErrorMessage;
      set
      {
        if (_remotePathErrorMessage == value) return;
        _remotePathErrorMessage = value;
        OnPropertyChanged(nameof(RemotePathErrorMessage));
      }
    }

    public int Port
    {
      get => _port;
      set
      {
        if (_port == value) return;
        _port = value;
        OnPropertyChanged(nameof(Port));
      }
    }

    public string PortErrorMessage
    {
      get => _portErrorMessage;
      set
      {
        if (_portErrorMessage == value) return;
        _portErrorMessage = value;
        OnPropertyChanged(nameof(PortErrorMessage));
      }
    }

    public bool Validate()
    {
      bool returnValue = true;
      AddressErrorMessage = string.Empty;
      RemotePathErrorMessage = string.Empty;
      PortErrorMessage = string.Empty;

      if (string.IsNullOrWhiteSpace(Address))
      {
        AddressErrorMessage = "Address is required.";
        returnValue = false;
      }
      if (string.IsNullOrWhiteSpace(RemotePath))
      {
        RemotePathErrorMessage = "Remote path is required.";
        returnValue = false;
      }
      if(Port <= 0)
      {
        PortErrorMessage = "Port must be a positive integer.";
        returnValue = false;
      }

      return returnValue;
    }
  }
}
