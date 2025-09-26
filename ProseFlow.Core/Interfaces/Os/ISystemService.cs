namespace ProseFlow.Core.Interfaces.Os;

/// <summary>
/// Defines the contract for a service that handles general OS-level interactions.
/// </summary>
public interface ISystemService
{
    /// <summary>
    /// Configures the application to launch automatically at login.
    /// </summary>
    /// <param name="isEnabled">True to enable launch at login, false to disable.</param>
    void SetLaunchAtLogin(bool isEnabled);
}