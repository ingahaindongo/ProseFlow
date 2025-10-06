namespace ProseFlow.Core.Enums;

/// <summary>
/// Defines the strategy for handling conflicts during an action import.
/// </summary>
public enum ActionConflictResolutionStrategy
{
    /// <summary>
    /// If conflicts are found, automatically overwrite existing actions with the imported ones.
    /// </summary>
    Overwrite,
    
    /// <summary>
    /// If conflicts are found, prompt the user with a UI to resolve each one.
    /// </summary>
    Prompt,

    /// <summary>
    /// If conflicts are found, automatically skip the conflicting imported actions, keeping the existing ones.
    /// </summary>
    Skip
}