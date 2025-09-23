using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProseFlow.Core.Interfaces;

#if OSX
using AppKit;
#endif

namespace ProseFlow.UI.Services.ActiveWindow;

/// <summary>
/// Tracks the active window process on Apple macOS using the AppKit framework.
/// </summary>
#pragma warning disable CS9113 // Parameter is unread.
public class MacOsActiveWindowTracker(ILogger<MacOsActiveWindowTracker> logger) : IActiveWindowTracker
#pragma warning restore CS9113 // Parameter is unread.
{
    private const string UnknownProcess = "unknown.exe";

    /// <inheritdoc />
    public Task<string> GetActiveWindowProcessNameAsync()
    {
#if OSX
        try
        {
            var frontmostApp = NSWorkspace.SharedWorkspace.FrontmostApplication;
            if (frontmostApp == null)
                return Task.FromResult(UnknownProcess);

            // Fall back to the BundleIdentifier (e.g., "com.microsoft.VSCode") if the URL is unavailable.
            var processName = Path.GetFileName(frontmostApp.ExecutableUrl?.Path)
                                 ?? frontmostApp.BundleIdentifier
                                 ?? UnknownProcess;

            return Task.FromResult(processName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while getting the active window process on macOS.");
            return Task.FromResult(UnknownProcess);
        }
#else
        return Task.FromResult(UnknownProcess);
#endif
    }
}