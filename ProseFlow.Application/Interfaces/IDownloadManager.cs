using System.Collections.ObjectModel;
using ProseFlow.Application.DTOs.Models;
using ProseFlow.Core.Models;
using Action = System.Action;

namespace ProseFlow.Application.Interfaces;

public interface IDownloadManager
{
    ObservableCollection<DownloadTask> AllDownloads { get; }
    int ActiveDownloadCount { get; }

    Task StartDownloadAsync(ModelCatalogEntry model, ModelQuantization quantization);
    void CancelDownload(DownloadTask task);
    void RetryDownload(DownloadTask task);
    void ClearDownload(DownloadTask task);
    void ClearAllCompleted();
    
    event Action? DownloadsChanged;
}