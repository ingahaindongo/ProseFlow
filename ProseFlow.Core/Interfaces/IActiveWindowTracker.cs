using System.Threading.Tasks;

namespace ProseFlow.Core.Interfaces;

/// <summary>
/// Defines a contract for services that can identify the process name of the active foreground window.
/// </summary>
public interface IActiveWindowTracker
{
    /// <summary>
    /// Asynchronously gets the process name of the currently active foreground window.
    /// </summary>
    /// <returns>A task that resolves to the process name (e.g., "Code.exe", "chrome") or "unknown".</returns>
    Task<string> GetActiveWindowProcessNameAsync();
}