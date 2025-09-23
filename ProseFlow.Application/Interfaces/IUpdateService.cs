using ProseFlow.Core.Enums;
using Velopack;

namespace ProseFlow.Application.Interfaces;

/// <summary>
/// Defines the contract for a service that manages application updates.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Gets the current status of the update process.
    /// </summary>
    UpdateStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the current download progress percentage (0-100).
    /// </summary>
    double DownloadProgress { get; }

    /// <summary>
    /// Gets information about the available update, if any.
    /// </summary>
    UpdateInfo? AvailableUpdateInfo { get; }

    /// <summary>
    /// An event that is raised whenever the update status or progress changes.
    /// </summary>
    event Action? StateChanged;

    /// <summary>
    /// An event that is raised specifically when an update is found.
    /// Used for non-intrusive notifications.
    /// </summary>
    event Action? UpdateAvailable;

    /// <summary>
    /// Asynchronously checks the remote source for a new application update.
    /// </summary>
    Task CheckForUpdateAsync();

    /// <summary>
    /// Asynchronously downloads the available update.
    /// </summary>
    Task DownloadUpdateAsync();

    /// <summary>
    /// Applies the downloaded update and restarts the application.
    /// </summary>
    void ApplyUpdateAndRestart();

    /// <summary>
    /// Cancels the ongoing download.
    /// </summary>
    void CancelDownload();
}