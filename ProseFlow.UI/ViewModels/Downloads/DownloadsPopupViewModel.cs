using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.DTOs.Models;
using ProseFlow.Application.Interfaces;
using ProseFlow.UI.Services;

namespace ProseFlow.UI.ViewModels.Downloads;

public partial class DownloadsPopupViewModel : ViewModelBase
{
    private readonly IDownloadManager _downloadManager;
    private readonly IDialogService _dialogService;
    
    public ReadOnlyObservableCollection<DownloadTaskViewModel> Downloads { get; }

    public DownloadsPopupViewModel(IDownloadManager downloadManager, IDialogService dialogService)
    {
        _downloadManager = downloadManager;
        _dialogService = dialogService;
        
        var collection = new ObservableCollection<DownloadTaskViewModel>(
            _downloadManager.AllDownloads.Select(dto => new DownloadTaskViewModel(dto, _downloadManager))
        );
        Downloads = new ReadOnlyObservableCollection<DownloadTaskViewModel>(collection);


        ((INotifyCollectionChanged)_downloadManager.AllDownloads).CollectionChanged += (_, e) =>
        {
            if (e is { Action: NotifyCollectionChangedAction.Add, NewItems: not null })
                foreach (var item in e.NewItems.Cast<DownloadTask>())
                    collection.Add(new DownloadTaskViewModel(item, _downloadManager));
            else if (e is { Action: NotifyCollectionChangedAction.Remove, OldItems: not null })
                foreach (var item in e.OldItems.Cast<DownloadTask>())
                {
                    var vmToRemove = collection.FirstOrDefault(vm => vm.Task == item);
                    if (vmToRemove is not null) collection.Remove(vmToRemove);
                }
        };
    }

    [RelayCommand]
    private void ClearAllCompleted()
    {
        _downloadManager.ClearAllCompleted();
    }
    
    [RelayCommand]
    private async Task OpenDownloadsFolder(DownloadTask task)
    {
        var folderPath = Path.GetDirectoryName(task.DestinationPath);
        if (!string.IsNullOrEmpty(folderPath)) await _dialogService.OpenUrlAsync(folderPath);
    }
}