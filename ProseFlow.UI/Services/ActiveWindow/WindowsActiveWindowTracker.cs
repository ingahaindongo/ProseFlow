using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProseFlow.Core.Interfaces;

namespace ProseFlow.UI.Services.ActiveWindow;

/// <summary>
/// Tracks the active window process on Microsoft Windows using Win32 API calls.
/// </summary>
public class WindowsActiveWindowTracker(ILogger<WindowsActiveWindowTracker> logger) : IActiveWindowTracker
{
    private const string UnknownProcess = "unknown.exe";

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    /// <inheritdoc />
    public Task<string> GetActiveWindowProcessNameAsync()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            _ = GetWindowThreadProcessId(hwnd, out var pid);

            if (pid == 0) return Task.FromResult(UnknownProcess);

            var process = Process.GetProcessById((int)pid);

            // Prefer the module name (e.g., "Code.exe") for the full executable name.
            return Task.FromResult(process.MainModule?.ModuleName ?? process.ProcessName);
        }
        catch (ArgumentException)
        {
            // Process likely exited between getting the PID and getting the process info.
            return Task.FromResult(UnknownProcess);
        }
        catch (Win32Exception ex)
        {
            // Commonly occurs due to insufficient permissions to query process information.
            logger.LogWarning(ex, "Access was denied when getting active window process details.");
            return Task.FromResult(UnknownProcess);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while getting the active window process name.");
            return Task.FromResult(UnknownProcess);
        }
    }
}