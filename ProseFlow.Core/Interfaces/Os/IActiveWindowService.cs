namespace ProseFlow.Core.Interfaces.Os;

/// <summary>
/// Defines the contract for a service that tracks the active window.
/// </summary>
public interface IActiveWindowService
{
    /// <summary>
    /// Gets the process name of the currently active window.
    /// </summary>
    /// <returns>The active window's process name.</returns>
    Task<string> GetActiveWindowProcessNameAsync();
}