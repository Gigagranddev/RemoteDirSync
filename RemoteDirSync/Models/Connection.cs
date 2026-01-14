using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDirSync.Models
{
  public class Connection : ObservableObject
  {
    private string _address = string.Empty;
    private string _addressErrorMessage = string.Empty;
    private string _remotePath = string.Empty;
    private string _remotePathErrorMessage = string.Empty;
    private bool _isReadonly = false;

    public required string Name { get; set; }

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

    public bool IsReadonly
    {
      get => _isReadonly;
      set
      {
        if (_isReadonly == value) return;
        _isReadonly = value;
        OnPropertyChanged(nameof(IsReadonly));
      }
    }

    public bool Validate()
    {
      bool returnValue = true;
      AddressErrorMessage = string.Empty;
      RemotePathErrorMessage = string.Empty;
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

      return returnValue;
    }
  }
}
