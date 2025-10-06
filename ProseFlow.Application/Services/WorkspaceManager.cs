using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Models;
using Action = System.Action;

namespace ProseFlow.Application.Services;

/// <summary>
/// Manages the application's user data location (local vs. shared) and the in-memory session password.
/// </summary>
public class WorkspaceManager : IWorkspaceManager, IDisposable
{
    private readonly ILogger<WorkspaceManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWorkspaceWatcherService _watcherService;
    private readonly string _workspaceStateFilePath;
    private string? _inMemoryPassword;
    private volatile bool _isChangeNotificationPending;

    public WorkspaceState CurrentState { get; private set; } = new(null, null);
    public bool IsConnected => !string.IsNullOrWhiteSpace(CurrentState.SharedPath);
    public bool AreRemoteChangesAvailable { get; private set; }

    public event Action? StateChanged;

    public WorkspaceManager(ILogger<WorkspaceManager> logger, IServiceScopeFactory scopeFactory, IWorkspaceWatcherService watcherService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _watcherService = watcherService;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var proseFlowDataPath = Path.Combine(appDataPath, "ProseFlow");
        _workspaceStateFilePath = Path.Combine(proseFlowDataPath, "workspace.json");
        
        _watcherService.RemoteChangesDetected += OnRemoteChangesDetected;
    }

    /// <inheritdoc />
    public async Task LoadStateAsync()
    {
        if (!File.Exists(_workspaceStateFilePath))
        {
            CurrentState = new WorkspaceState(null, null);
            return;
        }

        try
        {
            await using var fileStream = new FileStream(_workspaceStateFilePath, FileMode.Open, FileAccess.Read);
            CurrentState = await JsonSerializer.DeserializeAsync<WorkspaceState>(fileStream) ??
                           new WorkspaceState(null, null);
            _logger.LogInformation("Workspace state loaded. Path: {Path}", CurrentState.SharedPath ?? "Local");

            if (IsConnected && CurrentState.SharedPath is not null)
            {
                AreRemoteChangesAvailable = await CheckForRemoteChangesAsync();
                _watcherService.StartWatching(CurrentState.SharedPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workspace state from {Path}. Defaulting to local mode.",
                _workspaceStateFilePath);
            CurrentState = new WorkspaceState(null, null);
        }

        StateChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task SaveStateAsync(WorkspaceState state)
    {
        var previousState = CurrentState;
        CurrentState = state;
        
        // Handle watcher lifecycle
        if (string.IsNullOrWhiteSpace(previousState.SharedPath) && !string.IsNullOrWhiteSpace(CurrentState.SharedPath))
        {
            // Connected
            _watcherService.StartWatching(CurrentState.SharedPath);
        }
        else if (!string.IsNullOrWhiteSpace(previousState.SharedPath) && string.IsNullOrWhiteSpace(CurrentState.SharedPath))
        {
            // Disconnected
            _watcherService.StopWatching();
            AreRemoteChangesAvailable = false;
        }
        else if (previousState.SharedPath != CurrentState.SharedPath && !string.IsNullOrWhiteSpace(CurrentState.SharedPath))
        {
            // Switched path
            _watcherService.StartWatching(CurrentState.SharedPath);
        }
        
        try
        {
            await using var fileStream = new FileStream(_workspaceStateFilePath, FileMode.Create, FileAccess.Write);
            await JsonSerializer.SerializeAsync(fileStream, CurrentState,
                new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Workspace state saved. Path: {Path}", CurrentState.SharedPath ?? "Local");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workspace state to {Path}.", _workspaceStateFilePath);
        }

        StateChanged?.Invoke();
    }

    /// <inheritdoc />
    public Task<bool> CheckForRemoteChangesAsync()
    {
        if (!IsConnected || CurrentState.SharedPath is null || !Directory.Exists(CurrentState.SharedPath))
            return Task.FromResult(false);

        var lastSyncTime = CurrentState.LastSyncTimestamp ?? DateTime.MinValue;

        var actionsFile = Path.Combine(CurrentState.SharedPath, "actions.json");
        var providersFile = Path.Combine(CurrentState.SharedPath, "providers.json");

        return Task.FromResult(File.Exists(actionsFile) && File.GetLastWriteTimeUtc(actionsFile) > lastSyncTime ||
                               File.Exists(providersFile) && File.GetLastWriteTimeUtc(providersFile) > lastSyncTime);
    }

    /// <inheritdoc />
    public void SetSessionPassword(string password)
    {
        _inMemoryPassword = password;
    }

    /// <inheritdoc />
    public string? GetSessionPassword()
    {
        return _inMemoryPassword;
    }
    
    private void OnRemoteChangesDetected()
    {
        if (_isChangeNotificationPending) return;
        _isChangeNotificationPending = true;

        Task.Run(async () =>
        {
            await Task.Delay(3000); // Wait for file syncs to settle.

            try
            {
                if (!await CheckForRemoteChangesAsync()) return;
                
                _logger.LogInformation("Verified remote changes found. Processing...");

                using var scope = _scopeFactory.CreateScope();
                var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
                var settings = await settingsService.GetGeneralSettingsAsync();

                if (settings.WorkspaceSyncMode == WorkspaceSyncMode.Automatic)
                {
                    _logger.LogInformation("Automatic sync mode is enabled. Attempting to pull changes.");
                    var password = GetSessionPassword();
                    if (string.IsNullOrEmpty(password))
                    {
                        _logger.LogWarning(
                            "Cannot auto-sync: workspace password not available in current session. Falling back to manual notification.");
                        SetManualUpdatePending();
                        return;
                    }

                    try
                    {
                        var syncService = scope.ServiceProvider.GetRequiredService<WorkspaceSyncService>();
                        await syncService.PullFromWorkspaceAsync(CurrentState.SharedPath!, password);

                        AreRemoteChangesAvailable = false; // Sync successful, clear the flag.
                        AppEvents.RequestNotification("Workspace was automatically updated with new changes.",
                            NotificationType.Success);
                    }
                    catch (CryptographicException)
                    {
                        _logger.LogError("Automatic sync failed: incorrect password.");
                        AppEvents.RequestNotification(
                            "Automatic sync failed: incorrect password. Please sync manually.", NotificationType.Error);
                        SetManualUpdatePending();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Automatic sync failed due to an unexpected error.");
                        AppEvents.RequestNotification($"Automatic sync failed: {ex.Message}", NotificationType.Error);
                        SetManualUpdatePending();
                    }
                }
                else // Manual Mode
                {
                    _logger.LogInformation("Manual sync mode is enabled. Notifying user.");
                    AppEvents.RequestNotification("New changes are available in your workspace.", NotificationType.Info);
                    SetManualUpdatePending();
                }

                StateChanged?.Invoke();
            }
            finally
            {
                _isChangeNotificationPending = false;
            }
        });
    }

    private void SetManualUpdatePending()
    {
        AreRemoteChangesAvailable = true;
        StateChanged?.Invoke();
    }
    
    public void Dispose()
    {
        _watcherService.RemoteChangesDetected -= OnRemoteChangesDetected;
        _watcherService.Dispose();
        GC.SuppressFinalize(this);
    }
}