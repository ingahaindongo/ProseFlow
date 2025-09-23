using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Models;
using ProseFlow.UI.Services;
using ProseFlow.UI.Utils;
using Velopack;

namespace ProseFlow.UI.ViewModels.About;

/// <summary>
/// ViewModel for the About page, which includes application info, update management, and support links.
/// </summary>
public partial class AboutViewModel : ViewModelBase
{
    private readonly IUpdateService _updateService;
    private readonly IDialogService _dialogService;

    public override string Title => "About";
    public override IconSymbol Icon => IconSymbol.Info;

    [ObservableProperty] private UpdateStatus _updateStatus;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private UpdateInfo? _availableUpdate;
    
    public string AppVersion => Constants.AppVersion;
    public string AppDescription => Constants.AppDescription;
    public string AppAuthor => Constants.AppAuthor;
    
    public AboutViewModel(IUpdateService updateService, IDialogService dialogService)
    {
        _updateService = updateService;
        _dialogService = dialogService;

        _updateService.StateChanged += OnUpdateServiceStateChanged;
        OnUpdateServiceStateChanged(); // Set initial state
    }

    private void OnUpdateServiceStateChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateStatus = _updateService.CurrentStatus;
            DownloadProgress = _updateService.DownloadProgress;
            AvailableUpdate = _updateService.AvailableUpdateInfo;
        });
    }

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        await _updateService.CheckForUpdateAsync();
    }

    [RelayCommand]
    private async Task DownloadUpdateAsync()
    {
        await _updateService.DownloadUpdateAsync();
    }
    
    [RelayCommand]
    private void CancelDownload()
    {
        _updateService.CancelDownload();
    }

    [RelayCommand]
    private void InstallUpdate()
    {
        _updateService.ApplyUpdateAndRestart();
    }

    [RelayCommand]
    private async Task OpenLinkAsync(string url)
    {
        await _dialogService.OpenUrlAsync(url);
    }
}