using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Events;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;
using SharpHook;
using TextCopy;
using Action = System.Action;
using EventMask = SharpHook.Data.EventMask;
using KeyCode = SharpHook.Data.KeyCode;

namespace ProseFlow.Infrastructure.Services.Os;

/// <summary>
/// Implements OS-level interactions using SharpHook for cross-platform global hotkeys
/// and platform-specific code for other features.
/// </summary>
public sealed class OsService(IActiveWindowTracker activeWindowTracker, ILogger<OsService> logger) : IOsService
{
    private readonly TaskPoolGlobalHook _hook = new();
    private readonly EventSimulator _simulator = new();

    private (KeyCode key, EventMask modifiers) _actionMenuCombination;
    private (KeyCode key, EventMask modifiers) _smartPasteCombination;

    public event Action? ActionMenuHotkeyPressed;
    public event Action? SmartPasteHotkeyPressed;

    public Task StartHookAsync()
    {
        _hook.KeyPressed += OnKeyPressed;
        return _hook.RunAsync();
    }

    public void UpdateHotkeys(string actionMenuHotkey, string smartPasteHotkey)
    {
        _actionMenuCombination = ParseHotkeyStringToSharpHook(actionMenuHotkey);
        _smartPasteCombination = ParseHotkeyStringToSharpHook(smartPasteHotkey);
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var currentKey = e.Data.KeyCode;
        var rawModifiers = e.RawEvent.Mask;

        // Normalize the pressed modifiers to their generic equivalents.
        var normalizedModifiers = EventMask.None;
        if (rawModifiers.HasFlag(EventMask.LeftCtrl) || rawModifiers.HasFlag(EventMask.RightCtrl))
            normalizedModifiers |= EventMask.Ctrl;
        if (rawModifiers.HasFlag(EventMask.LeftShift) || rawModifiers.HasFlag(EventMask.RightShift))
            normalizedModifiers |= EventMask.Shift;
        if (rawModifiers.HasFlag(EventMask.LeftAlt) || rawModifiers.HasFlag(EventMask.RightAlt))
            normalizedModifiers |= EventMask.Alt;
        if (rawModifiers.HasFlag(EventMask.LeftMeta) || rawModifiers.HasFlag(EventMask.RightMeta))
            normalizedModifiers |= EventMask.Meta;

        // Check for Action Menu Hotkey
        if (currentKey == _actionMenuCombination.key && normalizedModifiers == _actionMenuCombination.modifiers)
            ActionMenuHotkeyPressed?.Invoke();

        // Check for Smart Paste Hotkey
        if (currentKey == _smartPasteCombination.key && normalizedModifiers == _smartPasteCombination.modifiers)
            SmartPasteHotkeyPressed?.Invoke();
    }

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

    public Task<string> GetActiveWindowProcessNameAsync()
    {
        return activeWindowTracker.GetActiveWindowProcessNameAsync();
    }

    public void SetLaunchAtLogin(bool isEnabled)
    {
#if WINDOWS
        SetLaunchAtLogin_Windows(isEnabled);
#elif OSX
    SetLaunchAtLogin_MacOS(isEnabled);
#elif LINUX
    SetLaunchAtLogin_Linux(isEnabled);
#endif
    }

    public void Dispose()
    {
        _hook.KeyPressed -= OnKeyPressed;
        _hook.Dispose();
    }

    #region Simulation and Parsing Helpers

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

    private (KeyCode key, EventMask modifiers) ParseHotkeyStringToSharpHook(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey)) return (KeyCode.VcUndefined, EventMask.None);

        var modifiers = EventMask.None;
        var key = KeyCode.VcUndefined;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                    modifiers |= EventMask.Ctrl;
                    break;
                case "SHIFT":
                    modifiers |= EventMask.Shift;
                    break;
                case "ALT":
                    modifiers |= EventMask.Alt;
                    break;
                case "CMD":
                case "WIN":
                case "META":
                    modifiers |= EventMask.Meta;
                    break;
                default:
                    if (!Enum.TryParse($"Vc{part}", true, out key)) key = KeyCode.VcUndefined;
                    break;
            }

        return (key, modifiers);
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
        return await ClipboardService.GetTextAsync();
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
        await ClipboardService.SetTextAsync(text);
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

    #region Platform-Specific Launch At Login

#if WINDOWS
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private void SetLaunchAtLogin_Windows(bool isEnabled)
    {
        try
        {
            const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(registryKeyPath, true)
                            ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(registryKeyPath);

            var appPath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(appPath)) return;

            if (isEnabled)
                key.SetValue(Constants.AppName, $"\"{appPath}\"");
            else
                key.DeleteValue(Constants.AppName, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set startup registry key");
            AppEvents.RequestNotification("Set Launch at Login failed, could not set startup registry key.", NotificationType.Error);
        }
    }
#endif


#if OSX
    private void SetLaunchAtLogin_MacOS(bool isEnabled)
    {
        try
        {
            var launchAgentsDir =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library",
                    "LaunchAgents");
            var plistFile = Path.Combine(launchAgentsDir, "com.proseflow.app.plist");

            Directory.CreateDirectory(launchAgentsDir);

            if (isEnabled)
            {
                var appPath = AppContext.BaseDirectory;
                // For .app bundles, the path points inside, we need the path to the bundle itself.
                var bundleIndex = appPath.IndexOf(".app/", StringComparison.OrdinalIgnoreCase);
                if (bundleIndex != -1) appPath = appPath[..(bundleIndex + 4)];

                var plistContent = $"""
                                    <?xml version="1.0" encoding="UTF-8"?>
                                    <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "https://www.apple.com/DTDs/PropertyList-1.0.dtd">
                                    <plist version="1.0">
                                    <dict>
                                        <key>Label</key>
                                        <string>com.proseflow.app</string>
                                        <key>ProgramArguments</key>
                                        <array>
                                            <string>{appPath}</string>
                                        </array>
                                        <key>RunAtLoad</key>
                                        <true/>
                                    </dict>
                                    </plist>
                                    """;
                File.WriteAllText(plistFile, plistContent);
            }
            else
            {
                if (File.Exists(plistFile)) File.Delete(plistFile);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set macOS launch agent");
            AppEvents.RequestNotification("Set Launch at Login failed, could not set launch agent.", NotificationType.Error);
        }
    }
#endif

#if LINUX
    private void SetLaunchAtLogin_Linux(bool isEnabled)
    {
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            // Application Menu Entry
            var appDir = Path.Combine(userProfile, ".local", "share", "applications");
            var appFile = Path.Combine(appDir, "proseflow.desktop");

            // Autostart
            var autostartDir = Path.Combine(userProfile, ".config", "autostart");
            var desktopFile = Path.Combine(autostartDir, "proseflow.desktop");

            // Define a standard path for the icon, and since it doesn't belong to a specific theme, I will just use hicolor.
            var iconDir = Path.Combine(userProfile, ".local", "share", "icons", "hicolor", "scalable", "apps");
            var iconFile = Path.Combine(iconDir, "proseflow.svg");

            Directory.CreateDirectory(autostartDir);

            if (isEnabled)
            {
                // Handle the Executable Path
                var appImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
                var appPath = !string.IsNullOrEmpty(appImagePath)
                    ? appImagePath
                    : Environment.ProcessPath;

                if (string.IsNullOrWhiteSpace(appPath))
                {
                    logger.LogError("Could not determine application path for creating autostart entry.");
                    AppEvents.RequestNotification("Set Launch at Login failed, Could not determine application path for creating autostart entry.", NotificationType.Error);
                    return;
                }
                
                // Get the main application assembly (ProseFlow)
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly == null)
                {
                    logger.LogError("Could not get the entry assembly to find the icon resource.");
                    AppEvents.RequestNotification("Set Launch at Login failed, Could not get the entry assembly to find the icon resource.", NotificationType.Error);
                    return;
                }
                
                // The logo is embedded in the assets folder of the ProseFlow assembly
                const string resourceName = "ProseFlow.UI.Assets.logo.svg"; // Assembly name (ProseFlow.UI) can't be fetched from entryAssembly since we're using a custom name
                using (var resourceStream = entryAssembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        logger.LogError("Could not find embedded icon resource: {ResourceName}", resourceName);
                        AppEvents.RequestNotification("Set Launch at Login could not set application icon.", NotificationType.Warning);
                    }
                    else
                    {
                        using var fileStream = new FileStream(iconFile, FileMode.Create, FileAccess.Write);
                        resourceStream.CopyTo(fileStream);
                    }
                }

                // Create the .desktop file
                var desktopContent = $"""
                                      [Desktop Entry]
                                      Version={Constants.AppVersion}
                                      Type=Application
                                      Name={Constants.AppName}
                                      Comment=An AI-powered copilot for text processing and writing
                                      TryExec=sh -c "{appPath}"
                                      Exec=sh -c "{appPath}"
                                      Path={Path.GetDirectoryName(appPath)}
                                      Icon={iconFile}
                                      Terminal=false
                                      Categories=Office;Utility;TextEditor;
                                      Keywords=AI;writing;assistant;copilot;text;editor;prose;flow;markdown;document;processing;
                                      StartupWMClass=ProseFlow
                                      StartupNotify=true
                                      X-GNOME-Autostart-enabled=true
                                      X-GNOME-Autostart-Delay=10
                                      X-MATE-Autostart-Delay=10
                                      X-KDE-autostart-after=panel
                                      """;
                File.WriteAllText(desktopFile, desktopContent);
                File.WriteAllText(appFile, desktopContent);
            }
            else
            {
                if (File.Exists(desktopFile)) File.Delete(desktopFile);
                if (File.Exists(appFile)) File.Delete(appFile);
                if (File.Exists(iconFile)) File.Delete(iconFile);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set Linux autostart file");
            AppEvents.RequestNotification("Set Launch at Login could not set autostart file.", NotificationType.Error);
        }
    }
#endif

    #endregion
}