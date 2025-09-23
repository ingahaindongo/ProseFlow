using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProseFlow.Core.Interfaces;

namespace ProseFlow.UI.Services.ActiveWindow;

/// <summary>
/// Tracks the active window process on Linux systems using the X11 windowing system.
/// This implementation relies on the 'xprop' command-line utility.
/// Note: This will not work on systems using Wayland by default due to its security architecture.
/// </summary>
public class LinuxActiveWindowTracker(ILogger<LinuxActiveWindowTracker> logger) : IActiveWindowTracker
{
    private const string UnknownProcess = "unknown.exe";

    // Pre-compile Regex for performance.
    private static readonly Regex ActiveWindowIdRegex = new(@"_NET_ACTIVE_WINDOW\(\w+\):.*?(0x[0-9a-fA-F]+)", RegexOptions.Compiled);
    private static readonly Regex WindowPidRegex = new(@"_NET_WM_PID\(\w+\)\s*=\s*(\d+)", RegexOptions.Compiled);

    /// <inheritdoc />
    public async Task<string> GetActiveWindowProcessNameAsync()
    {
        logger.LogDebug("Attempting to get active window process name on Linux.");

        // Wayland's security model prevents applications from accessing information about other windows.
        if (Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.Equals("wayland", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            logger.LogWarning("Wayland session detected. Active window tracking is not supported.");
            return UnknownProcess;
        }

        try
        {
            var windowId = await GetActiveWindowIdAsync();
            if (string.IsNullOrEmpty(windowId))
            {
                logger.LogWarning("Failed to get active window ID. The 'xprop' command might have failed or returned unexpected output.");
                return UnknownProcess;
            }

            var pid = await GetProcessIdFromWindowIdAsync(windowId);
            if (pid <= 0)
            {
                logger.LogWarning("Failed to get PID for window ID {WindowId}.", windowId);
                return UnknownProcess;
            }

            logger.LogDebug("Found PID {PID} for active window.", pid);
            var process = Process.GetProcessById(pid);

            return process.ProcessName;
        }
        catch (ArgumentException ex) when (ex.Message.Contains("is not running"))
        {
            // The process exited between getting the PID and getting the process info.
            logger.LogWarning("Process with PID {PID} exited before its name could be retrieved.", ex.TargetSite);
            return UnknownProcess;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while getting the active window process name.");
            return UnknownProcess;
        }
    }

    /// <summary>
    /// Retrieves the window ID of the currently active window using 'xprop'.
    /// </summary>
    /// <returns>The window ID as a hexadecimal string (e.g., "0x1a00005"), or null if not found.</returns>
    private async Task<string?> GetActiveWindowIdAsync()
    {
        var output = await ExecuteCommandAsync("xprop", "-root _NET_ACTIVE_WINDOW");
        var match = ActiveWindowIdRegex.Match(output);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Retrieves the Process ID (PID) associated with a given window ID.
    /// </summary>
    /// <param name="windowId">The ID of the window to query.</param>
    /// <returns>The PID as an integer, or -1 if it could not be determined.</returns>
    private async Task<int> GetProcessIdFromWindowIdAsync(string windowId)
    {
        var output = await ExecuteCommandAsync("xprop", $"-id {windowId} _NET_WM_PID");
        var match = WindowPidRegex.Match(output);
        return match.Success && int.TryParse(match.Groups[1].Value, out var pid) ? pid : -1;
    }

    /// <summary>
    /// Executes a shell command asynchronously and captures its standard output.
    /// </summary>
    /// <param name="command">The command or application to execute (e.g., "xprop").</param>
    /// <param name="args">The arguments to pass to the command.</param>
    /// <returns>The standard output of the command as a string.</returns>
    private async Task<string> ExecuteCommandAsync(string command, string args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.Start();
        var result = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return result;
    }
}