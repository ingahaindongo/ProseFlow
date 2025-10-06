using System.Timers;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Interfaces;
using Timer = System.Timers.Timer;

namespace ProseFlow.Infrastructure.Services.Monitoring;

/// <summary>
/// A long-running singleton service that monitors a shared workspace using a hybrid approach:
/// a FileSystemWatcher for real-time events and a periodic timer for fallback reliability.
/// </summary>
public class WorkspaceWatcherService(ILogger<WorkspaceWatcherService> logger)
    : IWorkspaceWatcherService
{
    private FileSystemWatcher? _watcher;
    private Timer? _pollingTimer;

    public event Action? RemoteChangesDetected;

    /// <inheritdoc />
    public void StartWatching(string workspacePath)
    {
        StopWatching();

        logger.LogInformation("Starting to watch workspace at: {Path}", workspacePath);

        try
        {
            // Initialize FileSystemWatcher for instant notifications
            _watcher = new FileSystemWatcher(workspacePath)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = "*.json",
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Renamed += OnFileChanged;
            _watcher.Error += OnWatcherError;

            // Initialize polling timer as a reliable fallback
            _pollingTimer = new Timer(TimeSpan.FromSeconds(45).TotalMilliseconds)
            {
                AutoReset = true
            };
            _pollingTimer.Elapsed += OnTimerElapsed;
            _pollingTimer.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize workspace watcher for path: {Path}", workspacePath);
            StopWatching(); // Clean up partial initializations.
        }
    }

    /// <inheritdoc />
    public void StopWatching()
    {
        if (_watcher is not null)
        {
            logger.LogInformation("Stopping workspace watcher.");
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Created -= OnFileChanged;
            _watcher.Renamed -= OnFileChanged;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
        }

        if (_pollingTimer is not null)
        {
            _pollingTimer.Stop();
            _pollingTimer.Elapsed -= OnTimerElapsed;
            _pollingTimer.Dispose();
            _pollingTimer = null;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        logger.LogDebug("FileSystemWatcher detected a change: {ChangeType} on {FullPath}", e.ChangeType, e.FullPath);
        RemoteChangesDetected?.Invoke();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        RemoteChangesDetected?.Invoke();
    }
    
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        logger.LogError(e.GetException(), "An error occurred in the FileSystemWatcher. Monitoring may be unreliable.");
    }

    public void Dispose()
    {
        StopWatching();
        GC.SuppressFinalize(this);
    }
}