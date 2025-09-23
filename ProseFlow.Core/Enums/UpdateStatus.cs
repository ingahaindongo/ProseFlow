namespace ProseFlow.Core.Enums;

/// <summary>
/// Represents the different states of the application update process.
/// </summary>
public enum UpdateStatus
{
    Idle,
    Checking,
    UpdateAvailable,
    Downloading,
    ReadyToInstall,
    Error
}