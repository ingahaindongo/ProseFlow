using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.Core.Models;
using ProseFlow.UI.Services;
using ShadUI;

namespace ProseFlow.UI.ViewModels.Dialogs;

public enum ConnectionMode { Local, Shared }
public enum WorkspacePasswordMode { Create, Enter }

public partial class ManageConnectionViewModel : ViewModelBase
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly IDialogService _dialogService;
    private readonly WorkspaceSyncService _syncService;
    private readonly ILogger<ManageConnectionViewModel> _logger;
    
    internal readonly TaskCompletionSource<bool> CompletionSource = new();
    internal bool WasSuccess { get; private set; }

    [ObservableProperty]
    private ConnectionMode _selectedMode;

    [ObservableProperty]
    private string _workspacePath = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ManageConnectionViewModel(IWorkspaceManager workspaceManager, IDialogService dialogService, WorkspaceSyncService syncService, ILogger<ManageConnectionViewModel> logger)
    {
        _workspaceManager = workspaceManager;
        _dialogService = dialogService;
        _syncService = syncService;
        _logger = logger;

        IsConnected = _workspaceManager.IsConnected;
        SelectedMode = IsConnected ? ConnectionMode.Shared : ConnectionMode.Local;
        WorkspacePath = _workspaceManager.CurrentState.SharedPath ?? string.Empty;
        UpdateStatusMessage();
    }
    
    partial void OnSelectedModeChanged(ConnectionMode value) => UpdateStatusMessage();
    partial void OnWorkspacePathChanged(string value) => UpdateStatusMessage();

    private void UpdateStatusMessage()
    {
        if (IsConnected)
        {
            StatusMessage = "Connected. To change paths, please disconnect first.";
            return;
        }

        if (SelectedMode == ConnectionMode.Local)
        {
            StatusMessage = "Actions and Providers are stored privately in your user profile.";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(WorkspacePath))
        {
            StatusMessage = "Select a folder to be used as the shared workspace.";
            return;
        }

        if (!Directory.Exists(WorkspacePath))
        {
            StatusMessage = "⚠️ The specified path does not exist.";
            return;
        }
        
        var filesExist = File.Exists(Path.Combine(WorkspacePath, "actions.json")) ||
                         File.Exists(Path.Combine(WorkspacePath, "providers.json"));
        
        StatusMessage = filesExist
            ? "ℹ️ Workspace found. You will be asked for a password to connect."
            : "ℹ️ Folder is empty. A new workspace will be created here.";
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        var folder = await _dialogService.ShowOpenFolderDialogAsync("Select Workspace Folder");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            WorkspacePath = folder;
        }
    }

    [RelayCommand]
    private async Task ApplyAsync(Window window)
    {
        if (SelectedMode == ConnectionMode.Local)
        {
            WasSuccess = true;
            window.Close();
            return;
        }
        
        // Connect to a shared workspace
        if (string.IsNullOrWhiteSpace(WorkspacePath) || !Directory.Exists(WorkspacePath))
        {
            AppEvents.RequestNotification("Please select a valid folder path.", NotificationType.Warning);
            return;
        }
        
        var filesExist = File.Exists(Path.Combine(WorkspacePath, "actions.json")) ||
                         File.Exists(Path.Combine(WorkspacePath, "providers.json"));

        var passwordMode = filesExist ? WorkspacePasswordMode.Enter : WorkspacePasswordMode.Create;
        var passwordResult = await _dialogService.ShowWorkspacePasswordDialogAsync(passwordMode);
        
        if (!passwordResult.Success || string.IsNullOrWhiteSpace(passwordResult.Password)) return;
        
        try
        {
            if (passwordMode == WorkspacePasswordMode.Create)
            {
                // Push current settings to the new workspace location and connect
                await _syncService.PushToWorkspaceAsync(WorkspacePath, passwordResult.Password);
                await _workspaceManager.SaveStateAsync(new WorkspaceState(WorkspacePath, DateTime.UtcNow));
                AppEvents.RequestNotification("New workspace created and initial settings pushed.", NotificationType.Success);
            }
            else // Enter mode
            {
                // Test password by trying to pull
                await _syncService.PullFromWorkspaceAsync(WorkspacePath, passwordResult.Password);
                await _workspaceManager.SaveStateAsync(new WorkspaceState(WorkspacePath, DateTime.UtcNow));
                AppEvents.RequestNotification("Successfully connected to workspace and synced settings.", NotificationType.Success);
            }
            
            _workspaceManager.SetSessionPassword(passwordResult.Password);
            WasSuccess = true;
            window.Close();
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error during workspace connection.");
            AppEvents.RequestNotification(ex.Message, NotificationType.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to workspace.");
            AppEvents.RequestNotification($"Failed to connect: {ex.Message}", NotificationType.Error);
            await _workspaceManager.SaveStateAsync(new WorkspaceState(null, null)); // Revert to local on failure
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync(Window window)
    {
        await _workspaceManager.SaveStateAsync(new WorkspaceState(null, null));
        _workspaceManager.SetSessionPassword(string.Empty);
        AppEvents.RequestNotification("Disconnected from workspace.", NotificationType.Info);
        WasSuccess = true;
        window.Close();
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        WasSuccess = false;
        window.Close();
    }
}