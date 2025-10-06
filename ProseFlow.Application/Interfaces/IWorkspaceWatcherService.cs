namespace ProseFlow.Application.Interfaces;

/// <summary>
/// Defines a contract for a service that monitors a workspace for remote changes.
/// </summary>
public interface IWorkspaceWatcherService : IDisposable
{
    /// <summary>
    /// An event that is raised when remote changes are detected in the workspace.
    /// </summary>
    event Action? RemoteChangesDetected;

    /// <summary>
    /// Starts monitoring the specified workspace path.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace folder.</param>
    void StartWatching(string workspacePath);

    /// <summary>
    /// Stops all monitoring activities.
    /// </summary>
    void StopWatching();
}