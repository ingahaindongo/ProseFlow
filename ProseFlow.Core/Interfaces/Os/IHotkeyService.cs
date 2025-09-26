namespace ProseFlow.Core.Interfaces.Os;

/// <summary>
/// Defines the contract for a service that manages global hotkeys.
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>
    /// Fired when the hotkey combination for the Action Menu is pressed.
    /// </summary>
    event Action? ActionMenuHotkeyPressed;

    /// <summary>
    /// Fired when the hotkey combination for Smart Paste is pressed.
    /// </summary>
    event Action? SmartPasteHotkeyPressed;

    /// <summary>
    /// Starts the global hook to listen for keyboard events.
    /// </summary>
    Task StartHookAsync();

    /// <summary>
    /// Updates the hotkey combinations from string configurations.
    /// </summary>
    /// <param name="actionMenuHotkey">The hotkey string for the Action Menu.</param>
    /// <param name="smartPasteHotkey">The hotkey string for Smart Paste.</param>
    void UpdateHotkeys(string actionMenuHotkey, string smartPasteHotkey);
}