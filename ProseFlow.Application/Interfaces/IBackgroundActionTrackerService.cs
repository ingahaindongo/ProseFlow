using ProseFlow.Application.DTOs;
using ProseFlow.Core.Enums;

namespace ProseFlow.Application.Interfaces;

/// <summary>
/// Defines the contract for a centralized service that tracks all active background actions.
/// </summary>
public interface IBackgroundActionTrackerService
{
    /// <summary>
    /// Fired when a new action is added to the tracker.
    /// </summary>
    event Action<TrackedAction>? ActionAdded;

    /// <summary>
    /// Fired when an action is removed from the tracker.
    /// </summary>
    event Action<TrackedAction>? ActionRemoved;

    /// <summary>
    /// Gets a snapshot of all currently active actions.
    /// </summary>
    /// <returns>An enumerable collection of the active actions.</returns>
    IEnumerable<TrackedAction> GetActiveActions();

    /// <summary>
    /// Registers a new action to be tracked by the system.
    /// </summary>
    /// <param name="name">A user-friendly name for the action (e.g., "Summarizing Document").</param>
    /// <param name="icon">The icon representing the action.</param>
    /// <returns>The newly created TrackedAction object, which includes its unique ID and CancellationTokenSource.</returns>
    TrackedAction AddAction(string name, string icon);

    /// <summary>
    /// Updates the status of a specific action.
    /// </summary>
    /// <param name="id">The unique ID of the action to update.</param>
    /// <param name="newStatus">The new status to set for the action.</param>
    void UpdateStatus(Guid id, ActionStatus newStatus);

    /// <summary>
    /// Requests the cancellation of an ongoing action.
    /// </summary>
    /// <param name="id">The unique ID of the action to cancel.</param>
    void RequestCancellation(Guid id);

    /// <summary>
    /// Marks an action as completed with a final status, displays it for a short duration, and then removes it.
    /// </summary>
    /// <param name="id">The unique ID of the action to complete.</param>
    /// <param name="finalStatus">The final status (Success or Error).</param>
    /// <param name="displayDuration">The amount of time to keep the action in the list before removing it.</param>
    void CompleteAction(Guid id, ActionStatus finalStatus, TimeSpan displayDuration);
}