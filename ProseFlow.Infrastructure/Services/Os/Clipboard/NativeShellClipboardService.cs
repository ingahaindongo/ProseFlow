using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using ProseFlow.Core.Interfaces.Os;

namespace ProseFlow.Infrastructure.Services.Os.Clipboard;

/// <summary>
/// Implements the clipboard service contract using native shell commands (e.g., xclip, pbcopy/pbpaste, clip.exe).
/// Second-tier clipboard implementation, especially for environments like WSL.
/// </summary>
public class NativeShellClipboardService(ILogger<NativeShellClipboardService> logger) : IFallbackClipboardService
{
#pragma warning disable CS0169 // Field is never used
    private static bool? _xclipExists;
    private static bool? _pbcopyPbpasteExists;
    private static bool? _windowsToolsExist;
#pragma warning restore CS0169 // Field is never used

    /// <inheritdoc />
    public async Task<string?> GetTextAsync()
    {
        try
        {
#if WINDOWS
            if (_windowsToolsExist == null)
            {
                // Check for powershell (for get) and clip (for set)
                var (exitCode, _, _) = await ExecuteProcessAsync("cmd.exe", "/c \"where powershell.exe >nul 2>nul && where clip.exe >nul 2>nul\"");
                _windowsToolsExist = exitCode == 0;
                logger.LogInformation("Checked for Windows clipboard utilities (powershell, clip). Found: {WindowsToolsExist}", _windowsToolsExist);
            }

            if (_windowsToolsExist == true)
            {
                logger.LogDebug("Using PowerShell Get-Clipboard to get clipboard text.");
                // Use PowerShell to get clipboard content as there's no direct 'paste' command like clip.exe
                var (exitCode, output, error) = await ExecuteProcessAsync("powershell.exe", "-NoProfile -Command \"Get-Clipboard\"");
                if (exitCode == 0) return output.TrimEnd('\r', '\n');

                logger.LogWarning("PowerShell Get-Clipboard command failed with exit code {ExitCode}. Error: {Error}", exitCode, error);
            }
#elif OSX
            if (_pbcopyPbpasteExists == null)
            {
                var (exitCode, _, _) = await ExecuteBashCommandAsync("command -v pbcopy >/dev/null && command -v pbpaste >/dev/null");
                _pbcopyPbpasteExists = exitCode == 0;
                logger.LogInformation("Checked for pbcopy/pbpaste utilities. Found: {PbcopyPbpasteExists}", _pbcopyPbpasteExists);
            }

            if (_pbcopyPbpasteExists == true)
            {
                logger.LogDebug("Using pbpaste to get clipboard text.");
                var (exitCode, output, error) = await ExecuteBashCommandAsync("pbpaste");
                if (exitCode == 0) return output;

                logger.LogWarning("pbpaste command failed with exit code {ExitCode}. Error: {Error}", exitCode, error);
            }
#elif LINUX
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
#endif
            
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Native shell clipboard 'get' command failed unexpectedly.");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetTextAsync(string text)
    {
        var success = false;
        try
        {
#if WINDOWS
            if (_windowsToolsExist == true)
            {
                logger.LogDebug("Using clip.exe to set clipboard text.");
                // clip.exe takes standard input and puts it on the clipboard.
                var (exitCode, _, error) = await ExecuteProcessAsync("clip.exe", "", text);
                if (exitCode == 0)
                {
                    success = true;
                }
                else
                {
                    logger.LogWarning("clip.exe command failed with exit code {ExitCode}. Error: {Error}", exitCode, error);
                }
            }
#elif OSX
            if (_pbcopyPbpasteExists == true)
            {
                logger.LogDebug("Using pbcopy to set clipboard text.");
                var (exitCode, _, error) = await ExecuteBashCommandAsync("pbcopy", text);
                if (exitCode == 0)
                    success = true;
                else
                    logger.LogWarning("pbcopy command failed with exit code {ExitCode}. Error: {Error}", exitCode, error);
            }
#elif LINUX
            if (_xclipExists == true)
            {
                logger.LogDebug("Using xclip to set clipboard text.");
                var (exitCode, _, error) = await ExecuteBashCommandAsync("xclip -i -selection clipboard", text);
                if (exitCode == 0)
                    success = true;
                else
                    logger.LogWarning("xclip command failed with exit code {ExitCode}. Error: {Error}", exitCode, error);
            }
#endif
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Native shell clipboard 'set' command failed.", ex);
        }

        if (!success)
            throw new InvalidOperationException("No suitable native clipboard utility found or the operation failed.");
    }

    /// <summary>
    /// Executes a command via 'bash -c' and captures its output.
    /// </summary>
    private async Task<(int ExitCode, string Output, string Error)> ExecuteBashCommandAsync(string command, string? stdIn = null)
    {
        return await ExecuteProcessAsync("/bin/bash", $"-c \"{command}\"", stdIn);
    }

    /// <summary>
    /// Executes a process with the given file name, arguments, and optional standard input.
    /// This is a generic helper that works across platforms.
    /// </summary>
    private async Task<(int ExitCode, string Output, string Error)> ExecuteProcessAsync(string fileName, string arguments,
        string? stdIn = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
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
}