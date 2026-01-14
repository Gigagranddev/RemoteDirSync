using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RemoteDirSync.Models
{
  public class Session : ObservableObject
  {
    private string _name = string.Empty;
    private string _nameErrorMessage = string.Empty;

    [JsonIgnore]
    public bool IsNew { get; } = false;

    public string Name
    {
      get => _name;
      set
      {
        if (_name == value) return;
        _name = value;
        OnPropertyChanged(nameof(Name));
      }
    }

    public string NameErrorMessage
    {
      get => _nameErrorMessage;
      set
      {
        if (_nameErrorMessage == value) return;
        _nameErrorMessage = value;
        OnPropertyChanged(nameof(NameErrorMessage));
      }
    }

    public ObservableCollection<Connection> Connections { get; set; } = new ObservableCollection<Connection>() {
      new Connection() { Name = "Connection 1" },
      new Connection() { Name = "Connection 2" }
    };

    public Session(bool isNew = false)
    {
      IsNew = isNew;
    }

    public bool Validate()
    {
      bool returnValue = true;
      NameErrorMessage = string.Empty;
      if (string.IsNullOrWhiteSpace(Name))
      {
        NameErrorMessage = "Session name is required.";
        returnValue = false;
      }
      foreach (var connection in Connections)
      {
        returnValue &= connection.Validate();
      }

      return returnValue;
    }
  }

 
}
