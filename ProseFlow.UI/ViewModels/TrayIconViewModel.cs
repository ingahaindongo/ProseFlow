using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.Infrastructure.Services.AiProviders.Local;

namespace ProseFlow.UI.ViewModels;

/// <summary>
/// ViewModel to manage the state and commands for the System Tray Icon.
/// </summary>
public partial class TrayIconViewModel : ViewModelBase, IDisposable
{
    private readonly LocalModelManagerService _modelManager;
    private readonly SettingsService _settingsService;
    private readonly IDownloadManager _downloadManager;

    // Event to signal the UI layer (App.axaml.cs) to show the main window.
    public event Action? ShowMainWindowRequested;
    public event Action? ShowDownloadsRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModelLoaded))]
    [NotifyPropertyChangedFor(nameof(ModelStatusText))]
    private ModelStatus _managerStatus;
    
    [ObservableProperty]
    private string _currentProviderType = "Cloud";
    
    [ObservableProperty]
    private bool _hasActiveDownloads;
    
    [ObservableProperty]
    private int _activeDownloadCount;

    public bool IsModelLoaded => ManagerStatus == ModelStatus.Loaded;

    public string ModelStatusText => ManagerStatus switch
    {
        ModelStatus.Unloaded => "Local model is not loaded.",
        ModelStatus.Loading => "Loading local model...",
        ModelStatus.Loaded => "Local model is loaded.",
        ModelStatus.Error => $"Error: {_modelManager.ErrorMessage}",
        _ => "Unknown status."
    };

    public TrayIconViewModel(LocalModelManagerService modelManager, SettingsService settingsService, IDownloadManager downloadManager)
    {
        _modelManager = modelManager;
        _settingsService = settingsService;
        _downloadManager = downloadManager;
        
        // Subscribe to state changes from the infrastructure service
        _modelManager.StateChanged += OnManagerStateChanged;
        _downloadManager.DownloadsChanged += OnDownloadsChanged;
        
        // Load initial state
        Dispatcher.UIThread.Post(async void () => await LoadInitialStateAsync());
        OnManagerStateChanged();
        OnDownloadsChanged();
    }
    
    private async Task LoadInitialStateAsync()
    {
        var settings = await _settingsService.GetProviderSettingsAsync();
        CurrentProviderType = settings.PrimaryServiceType;
    }

    private void OnManagerStateChanged()
    {
        // UI updates must be dispatched to the UI thread.
        Dispatcher.UIThread.Post(() =>
        {
            ManagerStatus = _modelManager.Status;
        });
    }

    private void OnDownloadsChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            ActiveDownloadCount = _downloadManager.ActiveDownloadCount;
            HasActiveDownloads = ActiveDownloadCount > 0;
        });
    }

    [RelayCommand]
    private void OpenSettings()
    {
        ShowMainWindowRequested?.Invoke();
    }
    
    [RelayCommand]
    private void ShowDownloads()
    {
        ShowDownloadsRequested?.Invoke();
    }
    
    [RelayCommand(CanExecute = nameof(CanToggleModel))]
    private async Task ToggleLocalModel()
    {
        if (IsModelLoaded)
        {
            _modelManager.UnloadModel();
        }
        else
        {
            var settings = await _settingsService.GetProviderSettingsAsync();
            if (string.IsNullOrWhiteSpace(settings.LocalModelPath))
            {
                AppEvents.RequestNotification("No local model selected in settings.", NotificationType.Warning);
                return;
            }
            await _modelManager.LoadModelAsync(settings);
        }
    }
    
    private bool CanToggleModel()
    {
        return ManagerStatus is not ModelStatus.Loading;
    }

    [RelayCommand]
    private async Task SetProviderType(string type)
    {
        if (CurrentProviderType == type) return;

        var settings = await _settingsService.GetProviderSettingsAsync();
        settings.PrimaryServiceType = type;
        await _settingsService.SaveProviderSettingsAsync(settings);
        
        CurrentProviderType = type;
        AppEvents.RequestNotification($"Primary provider set to {type}.", NotificationType.Info);
    }
    
    [RelayCommand]
    private void QuitApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime) lifetime.Shutdown();
    }

    public void Dispose()
    {
        _modelManager.StateChanged -= OnManagerStateChanged;
        _downloadManager.DownloadsChanged -= OnDownloadsChanged;
        GC.SuppressFinalize(this);
    }
}