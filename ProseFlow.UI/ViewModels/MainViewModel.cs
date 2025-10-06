using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProseFlow.Application.Interfaces;
using ProseFlow.UI.Services;
using ProseFlow.UI.ViewModels.About;
using ProseFlow.UI.ViewModels.Actions;
using ProseFlow.UI.ViewModels.Dashboard;
using ProseFlow.UI.ViewModels.Downloads;
using ProseFlow.UI.ViewModels.History;
using ProseFlow.UI.ViewModels.Providers;
using ProseFlow.UI.ViewModels.Settings;
using ShadUI;

namespace ProseFlow.UI.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDownloadManager _downloadManager;
    private readonly IUpdateService _updateService;
    private readonly IWorkspaceManager _workspaceManager;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private DialogManager _dialogManager;
    [ObservableProperty] private ToastManager _toastManager;
    [ObservableProperty] private IPageViewModel? _currentPage;
    [ObservableProperty] private bool _hasActiveDownloads;
    [ObservableProperty] private int _activeDownloadCount;
    [ObservableProperty] private DownloadsPopupViewModel _downloadsPopup;
    [ObservableProperty] private bool _isWorkspaceConnected;
    [ObservableProperty] private bool _areWorkspaceUpdatesAvailable;

    public ObservableCollection<IPageViewModel> PageViewModels { get; } = [];

    public MainViewModel(IServiceProvider serviceProvider, DialogManager dialogManager, ToastManager toastManager,
        IDownloadManager downloadManager, IUpdateService updateService, IWorkspaceManager workspaceManager, IDialogService dialogService)
    {
        _serviceProvider = serviceProvider;
        _downloadManager = downloadManager;
        _updateService = updateService;
        _workspaceManager = workspaceManager;
        _dialogService = dialogService;
        _dialogManager = dialogManager;
        _toastManager = toastManager;
        _downloadsPopup = _serviceProvider.GetRequiredService<DownloadsPopupViewModel>();

        // Add instances of all page ViewModels
        PageViewModels.Add(serviceProvider.GetRequiredService<DashboardViewModel>());
        PageViewModels.Add(serviceProvider.GetRequiredService<ActionsViewModel>());
        PageViewModels.Add(serviceProvider.GetRequiredService<ProvidersViewModel>());
        PageViewModels.Add(serviceProvider.GetRequiredService<HistoryViewModel>());
        PageViewModels.Add(serviceProvider.GetRequiredService<SettingsViewModel>());
        PageViewModels.Add(serviceProvider.GetRequiredService<AboutViewModel>());

        // Set the initial page
        Navigate(PageViewModels.FirstOrDefault());

        // Subscribe to events
        _downloadManager.DownloadsChanged += OnDownloadsChanged;
        _updateService.UpdateAvailable += OnUpdateAvailable;
        _workspaceManager.StateChanged += OnWorkspaceStateChanged;
        
        OnDownloadsChanged(); // Set initial state
        OnWorkspaceStateChanged(); // Set initial state
    }

    private void OnDownloadsChanged()
    {
        ActiveDownloadCount = _downloadManager.ActiveDownloadCount;
        HasActiveDownloads = ActiveDownloadCount > 0;
    }
    
    private void OnWorkspaceStateChanged()
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            IsWorkspaceConnected = _workspaceManager.IsConnected;
            AreWorkspaceUpdatesAvailable = await _workspaceManager.CheckForRemoteChangesAsync();
        });
    }

    private void OnUpdateAvailable()
    {
        Dispatcher.UIThread.Post(() => ToastManager.CreateToast($"ProseFlow {_updateService.AvailableUpdateInfo?.TargetFullRelease.Version} is available")
                .WithDelay(5).WithAction("Show Update", () => Navigate(_serviceProvider.GetRequiredService<AboutViewModel>())).DismissOnClick().Show(Notification.Basic));
    }

    [RelayCommand]
    public void Navigate(IPageViewModel? page)
    {
        if (page is not null) CurrentPage = page;
    }

    [RelayCommand]
    private void ShowDownloadsPopup()
    {
        _dialogService.ShowDownloadsDialog();
    }

    [RelayCommand]
    private async Task SyncWorkspaceAsync()
    {
        var synced = await _dialogService.ShowSyncDialogAsync();
        if (synced && CurrentPage is not null) await CurrentPage.OnNavigatedToAsync(); // If a sync happened, force reload the current page's data
    }

    partial void OnCurrentPageChanged(IPageViewModel? oldValue, IPageViewModel? newValue)
    {
        if (newValue is null) return;
        
        // Unload data or dispose objects in old pag
        oldValue?.OnNavigatedFromAsync();
        
        // Deselect all pages
        foreach (var page in PageViewModels) page.IsSelected = false;

        // Select the new current page
        newValue.IsSelected = true;

        // Load data for the new page
        newValue.OnNavigatedToAsync();
    }

    [RelayCommand]
    private void SwitchTheme()
    {
        if (Avalonia.Application.Current is null) return;

        var currentTheme = Avalonia.Application.Current.RequestedThemeVariant;
        Avalonia.Application.Current.RequestedThemeVariant = currentTheme == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }

    public void Dispose()
    {
        _downloadManager.DownloadsChanged -= OnDownloadsChanged;
        _updateService.UpdateAvailable -= OnUpdateAvailable;
        _workspaceManager.StateChanged -= OnWorkspaceStateChanged;
        foreach (var page in PageViewModels)
            if (page is IDisposable disposablePage)
                disposablePage.Dispose();

        GC.SuppressFinalize(this);
    }
}