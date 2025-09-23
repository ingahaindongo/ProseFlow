using ProseFlow.Application.DTOs;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.Application.Events;

/// <summary>
/// A request to execute a specific action with potential overrides.
/// </summary>
/// <param name="ActionToExecute">The action chosen by the user.</param>
/// <param name="ForceOpenInWindow">Whether the user overrode the default behavior to force opening a new window.</param>
/// <param name="ProviderOverride">Optional provider name to override the default for this single execution.</param>
public record ActionExecutionRequest(Action ActionToExecute, bool ForceOpenInWindow, string? ProviderOverride);

public enum NotificationType { Info, Success, Warning, Error }

public static class AppEvents
{
    /// <summary>
    /// Raised when the Action Orchestration Service needs the UI to display the Floating Action Menu.
    /// The UI layer subscribes to this, shows the menu, and returns the user's selection.
    /// The Func returns a task that resolves to the user's choice, or null if cancelled.
    /// </summary>
    public static event Func<IEnumerable<Action>, string, Task<ActionExecutionRequest?>>? ShowFloatingMenuRequested;

    /// <summary>
    /// Invokes the ShowFloatingMenuRequested event.
    /// </summary>
    public static async Task<ActionExecutionRequest?> RequestFloatingMenuAsync(IEnumerable<Action> availableActions, string activeAppContext)
    {
        return ShowFloatingMenuRequested is not null
            ? await ShowFloatingMenuRequested.Invoke(availableActions, activeAppContext)
            : await Task.FromResult<ActionExecutionRequest?>(null);
    }


    /// <summary>
    /// Raised when a result needs to be displayed in a window.
    /// The UI subscribes and is responsible for showing the window and then returning a Task
    /// that completes with the user's next action (a refinement instruction or null if closed).
    /// </summary>
    public static event Func<ResultWindowData, Task<RefinementRequest?>>? ShowResultWindowAndAwaitRefinement;

    /// <summary>
    /// Invokes the event to show the result window and waits for the user's interaction.
    /// </summary>
    /// <returns>A RefinementRequest if the user wants to refine, otherwise null.</returns>
    public static async Task<RefinementRequest?> RequestResultWindowAsync(ResultWindowData data)
    {
        return ShowResultWindowAndAwaitRefinement is not null
            ? await ShowResultWindowAndAwaitRefinement.Invoke(data)
            : await Task.FromResult<RefinementRequest?>(null);
        // Graceful failure
    }


    /// <summary>
    /// Raised to show a system notification (toast).
    /// The UI layer subscribes to this to display feedback.
    /// </summary>
    public static event Action<string, NotificationType>? ShowNotificationRequested;

    /// <summary>
    /// Invokes the ShowNotificationRequested event.
    /// </summary>
    public static void RequestNotification(string message, NotificationType type)
    {
        ShowNotificationRequested?.Invoke(message, type);
    }
    
    /// <summary>
    /// Raised when the Action Management Service detects conflicts during an import.
    /// The UI layer subscribes to this, shows a resolution dialog, and returns the user's choices.
    /// </summary>
    public static event Func<List<ActionConflict>, Task<List<ActionConflict>?>>? ResolveConflictsRequested;

    /// <summary>
    /// Invokes the ResolveConflictsRequested event to get user input for import conflicts.
    /// </summary>
    /// <returns>A list of resolved conflicts, or null if the user cancelled the operation.</returns>
    public static async Task<List<ActionConflict>?> RequestConflictResolutionAsync(List<ActionConflict> conflicts)
    {
        return ResolveConflictsRequested is not null
            ? await ResolveConflictsRequested.Invoke(conflicts)
            : await Task.FromResult<List<ActionConflict>?>(null);
    }
}