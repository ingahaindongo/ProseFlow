namespace ProseFlow.Core.Enums;

/// <summary>
/// Specifies the desired output mode for an AI action.
/// </summary>
public enum OutputMode
{
    /// <summary>
    /// The application decides whether to open a new window or replace in-place based on the action's configuration.
    /// </summary>
    Default = -1,
    
    /// <summary>
    /// The result should be pasted directly, replacing the selected text.
    /// </summary>
    InPlace = 0,
    
    /// <summary>
    /// The result should be displayed in a new, interactive window.
    /// </summary>
    Windowed = 1,
    
    /// <summary>
    /// The result should be displayed in a diff view window for comparison and approval.
    /// </summary>
    Diff = 2
}