namespace ProseFlow.Core.Enums;

/// <summary>
/// Represents the state of an action processing operation.
/// </summary>
public enum ActionProcessingState
{
    /// <summary>
    /// The application is idle, awaiting user input.
    /// </summary>
    Idle,

    /// <summary>
    /// An AI request is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// The action completed successfully. This is a transient state for feedback.
    /// </summary>
    Success,

    /// <summary>
    /// The action failed. This is a transient state for feedback.
    /// </summary>
    Error
}