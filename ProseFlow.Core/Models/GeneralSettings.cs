using ProseFlow.Core.Abstracts;
using ProseFlow.Core.Enums;

namespace ProseFlow.Core.Models;

/// <summary>
/// Stores general application settings, intended to be a single record in the database.
/// </summary>
public class GeneralSettings : EntityBase
{
    /// <summary>
    /// The system-wide hotkey to trigger the Floating Action Menu.
    /// </summary>
    public string ActionMenuHotkey { get; set; } = "Ctrl+J";

    /// <summary>
    /// The system-wide hotkey for the "Smart Paste" functionality.
    /// </summary>
    public string SmartPasteHotkey { get; set; } = "Ctrl+Shift+V";

    /// <summary>
    /// The ID of the Action to be used for "Smart Paste". Null if not configured.
    /// </summary>
    public int? SmartPasteActionId { get; set; }

    /// <summary>
    /// If true, the application will attempt to launch automatically on system startup.
    /// </summary>
    public bool LaunchAtLogin { get; set; }

    /// <summary>
    /// The application's visual theme ("System", "Light", or "Dark").
    /// </summary>
    public string Theme { get; set; } = nameof(ThemeType.System);
    
    /// <summary>
    /// Flag indicating whether the user has completed the first-run onboarding.
    /// </summary>
    public bool IsOnboardingCompleted { get; set; }
}