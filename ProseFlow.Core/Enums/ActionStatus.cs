namespace ProseFlow.Core.Enums;

/// <summary>
/// Represents the lifecycle status of a background action being tracked.
/// </summary>
public enum ActionStatus
{
    /// <summary>
    /// The action has been created but is waiting for resources to begin processing.
    /// </summary>
    Queued,

    /// <summary>
    /// The action is currently being executed.
    /// </summary>
    Processing,

    /// <summary>
    /// The action completed successfully. This is a transient state for feedback.
    /// </summary>
    Success,

    /// <summary>
    /// The action failed to complete. This is a transient state for feedback.
    /// </summary>
    Error
}