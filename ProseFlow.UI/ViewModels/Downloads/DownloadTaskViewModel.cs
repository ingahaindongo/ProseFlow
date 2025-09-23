using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.DTOs.Models;
using ProseFlow.Application.Interfaces;

namespace ProseFlow.UI.ViewModels.Downloads;

public partial class DownloadTaskViewModel : ViewModelBase
{
    private readonly IDownloadManager _downloadManager;

    [ObservableProperty]
    private DownloadTask _task;

    public DownloadTaskViewModel(DownloadTask task, IDownloadManager downloadManager)
    {
        _task = task;
        _downloadManager = downloadManager;
        _task.PropertyChanged += OnTaskPropertyChanged;
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Task));
    }
    
    [RelayCommand]
    private void Cancel()
    {
        _downloadManager.CancelDownload(Task);
    }

    [RelayCommand]
    private void Retry()
    {
        _downloadManager.RetryDownload(Task);
    }

    [RelayCommand]
    private void Clear()
    {
        _downloadManager.ClearDownload(Task);
    }

    ~DownloadTaskViewModel()
    {
        Task.PropertyChanged -= OnTaskPropertyChanged;
    }
}