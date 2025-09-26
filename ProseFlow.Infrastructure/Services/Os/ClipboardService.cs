
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using ProseFlow.Core.Interfaces.Os;
using SharpHook;
using KeyCode = SharpHook.Data.KeyCode;

namespace ProseFlow.Infrastructure.Services.Os;

/// <summary>
/// Implements clipboard interactions with platform-specific handling.
/// </summary>
public sealed class ClipboardService(ILogger<ClipboardService> logger) : IClipboardService
{
    private readonly EventSimulator _simulator = new();

    public async Task<string?> GetSelectedTextAsync()
    {
        var originalClipboardText = await GetClipboardTextAsync();

        // Clear clipboard temporarily to reliably detect if copy worked
        await SetClipboardTextAsync(string.Empty);

        await SimulateCopyKeyPressAsync();

        // Give the OS a moment to process the copy
        await Task.Delay(150);

        var selectedText = await GetClipboardTextAsync();

        // Restore original clipboard content if it existed
        if (originalClipboardText != null) await SetClipboardTextAsync(originalClipboardText);

        // If the clipboard has new, non-empty content, it's our selected text.
        return !string.IsNullOrEmpty(selectedText) ? selectedText : null;
    }

    public async Task PasteTextAsync(string text)
    {
        // Use platform-specific clipboard handling to avoid WSL issues.
        var originalClipboardText = await GetClipboardTextAsync();

        await SetClipboardTextAsync(text);
        await SimulatePasteKeyPressAsync();

        // Give the OS a moment to process the paste, then restore the clipboard.
        await Task.Delay(150);
        if (originalClipboardText != null) await SetClipboardTextAsync(originalClipboardText);
    }

    #region Simulation Helpers

    private Task SimulateCopyKeyPressAsync()
    {
        var modifier = OperatingSystem.IsMacOS() ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;
        _simulator.SimulateKeyPress(modifier);
        _simulator.SimulateKeyPress(KeyCode.VcC);
        _simulator.SimulateKeyRelease(KeyCode.VcC);
        _simulator.SimulateKeyRelease(modifier);
        return Task.CompletedTask;
    }

    private Task SimulatePasteKeyPressAsync()
    {
        var modifier = OperatingSystem.IsMacOS() ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;
        _simulator.SimulateKeyPress(modifier);
        _simulator.SimulateKeyPress(KeyCode.VcV);
        _simulator.SimulateKeyRelease(KeyCode.VcV);
        _simulator.SimulateKeyRelease(modifier);
        return Task.CompletedTask;
    }

    #endregion

    #region Platform-Specific Clipboard Handling

#pragma warning disable CS0169 // Field is never used
    private static bool? _xclipExists;
#pragma warning restore CS0169 // Field is never used

    /// <summary>
    /// Gets text from the clipboard using a platform-specific strategy.
    /// On Linux, it prioritizes 'xclip' to avoid issues with TextCopy in WSL.
    /// </summary>
    private async Task<string?> GetClipboardTextAsync()
    {
#if LINUX
        if (_xclipExists == null)
        {
            var (exitCode, _, _) = await ExecuteBashCommandAsync("command -v xclip");
            _xclipExists = exitCode == 0;
            logger.LogInformation("Checked for xclip utility. Found: {XclipExists}", _xclipExists);
        }

        if (_xclipExists == true)
        {
            logger.LogDebug("Using xclip to get clipboard text.");
            var (exitCode, output, error) = await ExecuteBashCommandAsync("xclip -o -selection clipboard");
            if (exitCode == 0) return output.TrimEnd('\n', '\r');
            
            logger.LogWarning("xclip command failed with exit code {ExitCode}. Error: {Error}", exitCode, error);
        }

        logger.LogDebug("Falling back to TextCopy for getting clipboard text.");
#endif
        return await TextCopy.ClipboardService.GetTextAsync();
    }

    /// <summary>
    /// Sets text on the clipboard using a platform-specific strategy.
    /// On Linux, it prioritizes 'xclip' to avoid issues with TextCopy in WSL.
    /// </summary>
    private async Task SetClipboardTextAsync(string text)
    {
#if LINUX
        if (_xclipExists == true)
        {
            logger.LogDebug("Using xclip to set clipboard text.");
            var (exitCode, _, error) = await ExecuteBashCommandAsync("xclip -i -selection clipboard", text);
            if (exitCode == 0) return;

            logger.LogWarning("xclip command failed with exit code {ExitCode}. Error: {Error}", exitCode, error);
        }
        
        logger.LogDebug("Falling back to TextCopy for setting clipboard text.");
#endif
        await TextCopy.ClipboardService.SetTextAsync(text);
    }

    /// <summary>
    /// Executes a command via 'bash -c' and captures its output.
    /// </summary>
    private async Task<(int ExitCode, string Output, string Error)> ExecuteBashCommandAsync(string command,
        string? stdIn = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = stdIn != null,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            }
        };

        process.Start();

        if (stdIn != null)
        {
            await process.StandardInput.WriteAsync(stdIn);
            process.StandardInput.Close();
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        return (process.ExitCode, output, error);
    }

    #endregion
}