using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Events;
using ProseFlow.Core.Interfaces.Os;
using ProseFlow.Core.Models;

namespace ProseFlow.Infrastructure.Services.Os;

/// <summary>
/// Implements system-level interactions such as managing launch at login.
/// </summary>
public sealed class SystemService(ILogger<SystemService> logger) : ISystemService
{
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