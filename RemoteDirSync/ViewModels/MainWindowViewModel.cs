using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RemoteDirSync.Models;

namespace RemoteDirSync.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
  private ObservableObject _currentPage = new StartPageViewModel();

  public ObservableObject CurrentPage
  {
    get => _currentPage;
    set => SetProperty(ref _currentPage, value);
  }

  public MainWindowViewModel()
  {
    // Start with the start page
    if(CurrentPage is StartPageViewModel startVm)
    {
      startVm.StartRequested += OnStartRequested;
    }
  }

  private void OnStartRequested(object? sender, Session session)
  {
    var runPageVm = new RunPageViewModel(session);
    runPageVm.OnBackRequested += RunPageVm_OnBackRequested; 
    CurrentPage = runPageVm;
  }

  private void RunPageVm_OnBackRequested(object? sender, EventArgs e)
  {
    var startVm = new StartPageViewModel();
    startVm.StartRequested += OnStartRequested;
    CurrentPage = startVm;
  }
}
