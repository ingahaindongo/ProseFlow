using Microsoft.Extensions.Logging;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Enums;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;
using Action = System.Action;

namespace ProseFlow.Infrastructure.Services.Updates;

/// <summary>
/// Implements the application update logic using the Velopack library.
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly UpdateManager? _updateManager;
    private CancellationTokenSource? _cts;

    public event Action? StateChanged;
    public event Action? UpdateAvailable;

    private UpdateStatus _currentStatus = UpdateStatus.Idle;
    public UpdateStatus CurrentStatus
    {
        get => _currentStatus;
        private set
        {
            if (_currentStatus == value) return;
            _currentStatus = value;
            StateChanged?.Invoke();
        }
    }

    private double _downloadProgress;
    public double DownloadProgress
    {
        get => _downloadProgress;
        private set
        {
            _downloadProgress = value;
            StateChanged?.Invoke();
        }
    }

    private UpdateInfo? _availableUpdateInfo;
    public UpdateInfo? AvailableUpdateInfo
    {
        get => _availableUpdateInfo;
        private set
        {
            _availableUpdateInfo = value;
            StateChanged?.Invoke();
        }
    }

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        try
        {
            var source = new GithubSource("https://github.com/LSXPrime/ProseFlow", null, false);
            _updateManager = new UpdateManager(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize UpdateManager. Updates will be disabled.");
        }
    }

    public async Task CheckForUpdateAsync()
    {
        if (CurrentStatus is UpdateStatus.Checking or UpdateStatus.Downloading || _updateManager is null) return;

        CurrentStatus = UpdateStatus.Checking;
        _logger.LogInformation("Checking for updates...");

        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (updateInfo is not null)
            {
                AvailableUpdateInfo = updateInfo;
                CurrentStatus = UpdateStatus.UpdateAvailable;
                UpdateAvailable?.Invoke();
                _logger.LogInformation("Update available: Version {Version}", updateInfo.TargetFullRelease.Version);
            }
            else
            {
                AvailableUpdateInfo = null;
                CurrentStatus = UpdateStatus.Idle;
                _logger.LogInformation("No updates found. You are running the latest version.");
            }
        }
        catch (NotInstalledException)
        {
            CurrentStatus = UpdateStatus.Error;
            _logger.LogWarning("ProseFlow is not installed, likely it's a development build. Updates will be disabled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates.");
            CurrentStatus = UpdateStatus.Error;
        }
    }

    public async Task DownloadUpdateAsync()
    {
        if (AvailableUpdateInfo is null || CurrentStatus == UpdateStatus.Downloading || _updateManager is null) return;

        CurrentStatus = UpdateStatus.Downloading;
        DownloadProgress = 0;
        _cts = new CancellationTokenSource();
        _logger.LogInformation("Downloading update: Version {Version}", AvailableUpdateInfo.TargetFullRelease.Version);

        try
        {
            await _updateManager.DownloadUpdatesAsync(AvailableUpdateInfo, p => DownloadProgress = p, _cts.Token);

            // Check if cancellation was requested during download
            if (_cts.Token.IsCancellationRequested)
            {
                CurrentStatus = UpdateStatus.UpdateAvailable; // Revert to previous state
                _logger.LogInformation("Update download was canceled.");
            }
            else
            {
                CurrentStatus = UpdateStatus.ReadyToInstall;
                _logger.LogInformation("Update downloaded successfully.");
            }
        }
        catch (OperationCanceledException)
        {
            CurrentStatus = UpdateStatus.UpdateAvailable;
            _logger.LogInformation("Update download was canceled via exception.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading update.");
            CurrentStatus = UpdateStatus.Error;
        }
        finally
        {
            DownloadProgress = 0;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void ApplyUpdateAndRestart()
    {
        if (AvailableUpdateInfo is null || _updateManager is null) return;
        _logger.LogInformation("Applying update and restarting application...");
        _updateManager.ApplyUpdatesAndRestart(AvailableUpdateInfo);
    }

    public void CancelDownload()
    {
        _cts?.Cancel();
    }
}