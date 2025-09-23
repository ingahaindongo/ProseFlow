using System;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.DTOs.Models;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Models;
using Action = System.Action;

namespace ProseFlow.UI.ViewModels.Downloads;

public enum DownloadCardState { NotDownloaded, Queued, Downloading, Failed, Installed }

public partial class AvailableModelViewModel(
    ModelCatalogEntry model,
    IDownloadManager downloadManager,
    Action goToMyModelsAction)
    : ViewModelBase, IDisposable
{
    [ObservableProperty] private ModelCatalogEntry _model = model;
    [ObservableProperty] private ModelQuantization _selectedQuantization = model.Quantizations.FirstOrDefault() ??
                                                                           throw new InvalidOperationException("Model must have at least one quantization.");
    [ObservableProperty] private DownloadCardState _currentState = DownloadCardState.NotDownloaded;
    [ObservableProperty] private DownloadTask? _activeDownloadTask;

    partial void OnActiveDownloadTaskChanged(DownloadTask? oldValue, DownloadTask? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= OnTaskPropertyChanged;
        if (newValue is not null) newValue.PropertyChanged += OnTaskPropertyChanged;
        UpdateStateFromTask();
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ActiveDownloadTask));
        UpdateStateFromTask();
    }

    private void UpdateStateFromTask()
    {
        if (ActiveDownloadTask is null) return;

        CurrentState = ActiveDownloadTask.Status switch
        {
            DownloadStatus.Queued => DownloadCardState.Queued,
            DownloadStatus.Downloading => DownloadCardState.Downloading,
            DownloadStatus.Failed => DownloadCardState.Failed,
            DownloadStatus.Completed => DownloadCardState.Installed, // Treat completed as installed
            _ => CurrentState
        };
    }

    [RelayCommand]
    private void StartDownload()
    {
        if (CurrentState != DownloadCardState.NotDownloaded) return;
        _ = downloadManager.StartDownloadAsync(Model, SelectedQuantization);
    }

    [RelayCommand]
    private void CancelDownload()
    {
        if (ActiveDownloadTask is not null) downloadManager.CancelDownload(ActiveDownloadTask);
    }

    [RelayCommand]
    private void RetryDownload()
    {
        if (ActiveDownloadTask is not null) downloadManager.RetryDownload(ActiveDownloadTask);
    }

    [RelayCommand]
    private void GoToMyModels()
    {
        goToMyModelsAction.Invoke();
    }

    public void Dispose()
    {
        if (ActiveDownloadTask is not null) ActiveDownloadTask.PropertyChanged -= OnTaskPropertyChanged;
        GC.SuppressFinalize(this);
    }
}