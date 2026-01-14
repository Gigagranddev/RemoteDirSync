using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RemoteDirSync.Models;
using RemoteDirSync.Util;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RemoteDirSync.ViewModels
{
  public class StartPageViewModel : ObservableObject
  {
    private Session _currentSession = new Session(true);
    private SessionStorage _sessionStorage = new SessionStorage();
    public event EventHandler<Session>? StartRequested;

    public ICommand StartCommand { get; set; }
    public RelayCommand DeleteSessionCommand { get; set; }
    public ObservableCollection<Session> SavedSessions { get; set; } = new ObservableCollection<Session>();

    public Session CurrentSession
    {
      get => _currentSession;
      set
      {
        if (_currentSession != value)
        {
          _currentSession = value;
          OnPropertyChanged(nameof(CurrentSession));
          DeleteSessionCommand.NotifyCanExecuteChanged();
        }
      }
    }

    public StartPageViewModel()
    {
      StartCommand = new RelayCommand(async () => await StartSession());
      DeleteSessionCommand = new RelayCommand(async () => await DeletedCurrentSession(), () => CurrentSession != null && !CurrentSession.IsNew);
      SavedSessions.Add(_currentSession);
      _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
      var otherSessions = await _sessionStorage.LoadAsync();
      foreach (var session in otherSessions)
      {
        SavedSessions.Add(session);
      }
    }

    private async Task StartSession()
    {
      bool isValid = Validate();
      if (!isValid)
      {
        return;
      }
      if (CurrentSession.IsNew)
      {
        await _sessionStorage.SaveAsync(SavedSessions.ToList());
      }
      else
      {
        await _sessionStorage.SaveAsync(SavedSessions.Where(s => !s.IsNew).ToList());
      }
      StartRequested?.Invoke(this, CurrentSession);
    }

    private bool Validate()
    {
      return CurrentSession.Validate();
    }

    private async Task DeletedCurrentSession()
    {
      if (CurrentSession == null || CurrentSession.IsNew)
      {
        return;
      }
      SavedSessions.Remove(CurrentSession);
      await _sessionStorage.SaveAsync(SavedSessions.Where(s => !s.IsNew).ToList());
    }
  }
}
