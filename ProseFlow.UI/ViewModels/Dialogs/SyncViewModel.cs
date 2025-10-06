using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.UI.Services;
using ShadUI;

namespace ProseFlow.UI.ViewModels.Dialogs;

public partial class SyncViewModel : ViewModelBase
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly WorkspaceSyncService _syncService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SyncViewModel> _logger;
    
    internal readonly TaskCompletionSource<bool> CompletionSource = new();
    internal bool WasSuccess { get; private set; }
    
    [ObservableProperty]
    private string _lastSyncText;

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isPullRecommended;

    public SyncViewModel(IWorkspaceManager workspaceManager, WorkspaceSyncService syncService, IDialogService dialogService, ILogger<SyncViewModel> logger)
    {
        _workspaceManager = workspaceManager;
        _syncService = syncService;
        _dialogService = dialogService;
        _logger = logger;

        _lastSyncText = _workspaceManager.CurrentState.LastSyncTimestamp?.ToLocalTime().ToString("f") ?? "Never";
        
        IsPullRecommended = _workspaceManager.AreRemoteChangesAvailable;
        _statusText = IsPullRecommended
            ? "New changes are available from the workspace."
            : "You are up to date with the workspace.";
    }

    [RelayCommand]
    private async Task PullChangesAsync(Window window)
    {
        if (!_workspaceManager.IsConnected || _workspaceManager.CurrentState.SharedPath is null) return;
        
        IsBusy = true;
        try
        {
            var password = _workspaceManager.GetSessionPassword();
            if (string.IsNullOrEmpty(password))
            {
                var passwordResult = await _dialogService.ShowWorkspacePasswordDialogAsync(WorkspacePasswordMode.Enter);
                if (!passwordResult.Success || string.IsNullOrEmpty(passwordResult.Password))
                {
                    IsBusy = false;
                    return;
                }
                password = passwordResult.Password;
            }
            
            await _syncService.PullFromWorkspaceAsync(_workspaceManager.CurrentState.SharedPath, password);

            // Only set the session password and close on success.
            AppEvents.RequestNotification("Successfully pulled changes from workspace.", NotificationType.Success);
            _workspaceManager.SetSessionPassword(password);
            WasSuccess = true;
            window.Close();
        }
        catch(CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during workspace pull.");
            AppEvents.RequestNotification(ex.Message, NotificationType.Error);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to pull changes from workspace.");
            AppEvents.RequestNotification($"Pull failed: {ex.Message}", NotificationType.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PushChangesAsync(Window window)
    {
        if (!_workspaceManager.IsConnected || _workspaceManager.CurrentState.SharedPath is null) return;

        IsBusy = true;
        try
        {
            var confirmed = await _dialogService.ShowOwnedConfirmationDialogAsync(window, "Confirm Push",
                "This will overwrite the shared workspace with your local settings. This action will affect all other users and cannot be undone. Are you sure you want to continue?");
            
            if (!confirmed)
            {
                IsBusy = false;
                return;
            }

            var password = _workspaceManager.GetSessionPassword();
            if (string.IsNullOrEmpty(password))
            {
                var passwordResult = await _dialogService.ShowWorkspacePasswordDialogAsync(WorkspacePasswordMode.Enter);
                if (!passwordResult.Success || string.IsNullOrEmpty(passwordResult.Password))
                {
                    IsBusy = false;
                    return;
                }
                password = passwordResult.Password;
            }
            
            await _syncService.PushToWorkspaceAsync(_workspaceManager.CurrentState.SharedPath, password);
            
            // Only set the session password and close on success.
            AppEvents.RequestNotification("Successfully pushed changes to workspace.", NotificationType.Success);
            _workspaceManager.SetSessionPassword(password);
            WasSuccess = true;
            window.Close();
        }
        catch(CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during workspace push.");
            AppEvents.RequestNotification(ex.Message, NotificationType.Error);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to push changes to workspace.");
            AppEvents.RequestNotification($"Push failed: {ex.Message}", NotificationType.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Close(Window window)
    {
        WasSuccess = false;
        window.Close();
    }
}