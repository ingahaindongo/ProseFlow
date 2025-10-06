namespace ProseFlow.Core.Enums;

/// <summary>
/// Defines the behavior for handling remote workspace updates.
/// </summary>
public enum WorkspaceSyncMode
{
    /// <summary>
    /// The application will notify the user of available updates but will not download them automatically.
    /// </summary>
    Manual,
    
    /// <summary>
    /// The application will automatically download and apply remote changes in the background.
    /// </summary>
    Automatic
}