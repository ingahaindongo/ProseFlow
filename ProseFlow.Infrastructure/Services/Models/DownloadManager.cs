using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.DTOs.Models;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Models;
using Action = System.Action;

namespace ProseFlow.Infrastructure.Services.Models;

public class DownloadManager(
    ILogger<DownloadManager> logger,
    ILocalModelManagementService localModelService) : IDownloadManager
{
    private readonly HttpClient _httpClient = new();
    public ObservableCollection<DownloadTask> AllDownloads { get; } = [];

    public int ActiveDownloadCount => AllDownloads.Count(d => d.Status is DownloadStatus.Downloading or DownloadStatus.Queued);
    
    public event Action? DownloadsChanged;

    public async Task StartDownloadAsync(ModelCatalogEntry model, ModelQuantization quantization)
    {
        var destinationPath = Path.Combine(localModelService.GetManagedModelsDirectory(), quantization.FileName);

        if (File.Exists(destinationPath) || AllDownloads.Any(d => d.DestinationPath == destinationPath && d.Status is DownloadStatus.Downloading or DownloadStatus.Queued or DownloadStatus.Paused))
        {
            logger.LogInformation("Download for {FileName} already exists or is in progress.", quantization.FileName);
            return;
        }

        var task = new DownloadTask
        {
            Model = model,
            Quantization = quantization,
            DestinationPath = destinationPath,
            Status = DownloadStatus.Queued
        };

        AllDownloads.Add(task);
        DownloadsChanged?.Invoke();

        try
        {
            task.Status = DownloadStatus.Downloading;
            logger.LogInformation("Starting download for {FileName} from {Url}", task.Quantization.FileName, task.Quantization.Url);

            var stopwatch = Stopwatch.StartNew();
            long lastBytesDownloaded = 0;
            var lastUpdateTime = stopwatch.Elapsed;
            const int speedUpdateIntervalMs = 500;

            using var response = await _httpClient.GetAsync(task.Quantization.Url, HttpCompletionOption.ResponseHeadersRead, task.Cts.Token);
            response.EnsureSuccessStatusCode();

            task.TotalBytes = response.Content.Headers.ContentLength ?? 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync(task.Cts.Token);
            await using var fileStream = new FileStream(task.DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, task.Cts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), task.Cts.Token);
                task.BytesDownloaded += bytesRead;

                // Speed Calculation
                var currentTime = stopwatch.Elapsed;
                var timeSinceLastUpdate = currentTime - lastUpdateTime;

                if (timeSinceLastUpdate.TotalMilliseconds > speedUpdateIntervalMs)
                {
                    var bytesSinceLastUpdate = task.BytesDownloaded - lastBytesDownloaded;
                    // Calculate speed in MB/s and round to 2 decimal places
                    var speed = Math.Round(bytesSinceLastUpdate / timeSinceLastUpdate.TotalSeconds / (1024 * 1024), 2);

                    task.Speed = speed;

                    // Reset for the next interval
                    lastBytesDownloaded = task.BytesDownloaded;
                    lastUpdateTime = currentTime;
                    task.ProgressPercentage = task.TotalBytes > 0 ? (double)task.BytesDownloaded / task.TotalBytes * 100 : 0;
                }
            }
            
            stopwatch.Stop();
            task.Speed = 0;

            task.Status = DownloadStatus.Completed;
            logger.LogInformation("Successfully downloaded {FileName}", task.Quantization.FileName);
            
            // Create a record in the database for the new managed model.
            await localModelService.CreateManagedModelFromDownloadAsync(model, task.DestinationPath);
        }
        catch (OperationCanceledException)
        {
            task.Status = DownloadStatus.Canceled;
            logger.LogInformation("Download canceled for {FileName}", task.Quantization.FileName);
            CleanupFailedDownload(task);
        }
        catch (Exception ex)
        {
            task.Status = DownloadStatus.Failed;
            task.ErrorMessage = ex.Message;
            logger.LogError(ex, "Download failed for {FileName}", task.Quantization.FileName);
            CleanupFailedDownload(task);
        }
        finally
        {
            DownloadsChanged?.Invoke();
        }
    }

    public void CancelDownload(DownloadTask task)
    {
        task.Cts.Cancel();
    }

    public void RetryDownload(DownloadTask task)
    {
        var originalModel = task.Model;
        var originalQuant = task.Quantization;

        AllDownloads.Remove(task);
        _ = StartDownloadAsync(originalModel, originalQuant);
    }
    
    public void ClearDownload(DownloadTask task)
    {
        if (task.Status is DownloadStatus.Downloading or DownloadStatus.Queued) return;
        AllDownloads.Remove(task);
        DownloadsChanged?.Invoke();
    }

    public void ClearAllCompleted()
    {
        var completedTasks = AllDownloads.Where(d => d.Status is DownloadStatus.Completed or DownloadStatus.Canceled or DownloadStatus.Failed).ToList();
        foreach (var task in completedTasks) AllDownloads.Remove(task);
        DownloadsChanged?.Invoke();
    }

    private void CleanupFailedDownload(DownloadTask task)
    {
        if (File.Exists(task.DestinationPath))
            try
            {
                File.Delete(task.DestinationPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete incomplete download file: {Path}", task.DestinationPath);
            }
    }
}