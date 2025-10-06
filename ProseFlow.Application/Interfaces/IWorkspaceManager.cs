using ProseFlow.Core.Models;
using Action = System.Action;

namespace ProseFlow.Application.Interfaces;


/// <summary>
/// Defines the contract for a service that manages the application's user data location and state.
/// </summary>
public interface IWorkspaceManager
{
    /// <summary>
    /// Gets the current state of the workspace connection.
    /// </summary>
    WorkspaceState CurrentState { get; }

    /// <summary>
    /// Gets a value indicating whether the application is currently connected to a shared workspace.
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Gets a value indicating whether remote changes are available to be pulled.
    /// </summary>
    bool AreRemoteChangesAvailable { get; }

    /// <summary>
    /// An event that is raised whenever the workspace state changes.
    /// </summary>
    event Action? StateChanged;

    /// <summary>
    /// Loads the workspace state from the local configuration file. This should be called at application startup.
    /// </summary>
    Task LoadStateAsync();

    /// <summary>
    /// Saves the provided workspace state to the local configuration file and notifies subscribers of the change.
    /// </summary>
    /// <param name="state">The new state to save.</param>
    Task SaveStateAsync(WorkspaceState state);

    /// <summary>
    /// Checks if the files in the shared workspace have been modified since the last sync.
    /// </summary>
    /// <returns>True if remote changes are detected; otherwise, false.</returns>
    Task<bool> CheckForRemoteChangesAsync();
    
    /// <summary>
    /// Sets the workspace password for the current application session.
    /// </summary>
    /// <param name="password">The workspace password.</param>
    void SetSessionPassword(string password);

    /// <summary>
    /// Gets the in-memory workspace password for the current session.
    /// </summary>
    /// <returns>The password if it has been set; otherwise, null.</returns>
    string? GetSessionPassword();
}