using ProseFlow.Core.Interfaces.Os;
using SharpHook;
using Action = System.Action;
using EventMask = SharpHook.Data.EventMask;
using KeyCode = SharpHook.Data.KeyCode;

namespace ProseFlow.Infrastructure.Services.Os.Hotkeys;

/// <summary>
/// Implements global hotkey management using SharpHook.
/// </summary>
public sealed class HotkeyService(HotkeyRecordingService recordingService) : IHotkeyService
{
    private readonly TaskPoolGlobalHook _hook = new();

    private (KeyCode key, EventMask modifiers) _actionMenuCombination;
    private (KeyCode key, EventMask modifiers) _smartPasteCombination;

    /// <inheritdoc />
    public event Action? ActionMenuHotkeyPressed;
    
    /// <inheritdoc />
    public event Action? SmartPasteHotkeyPressed;

    /// <inheritdoc />
    public Task StartHookAsync()
    {
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        return _hook.RunAsync();
    }

    /// <inheritdoc />
    public void UpdateHotkeys(string actionMenuHotkey, string smartPasteHotkey)
    {
        _actionMenuCombination = HotkeyConverter.FromFriendlyString(actionMenuHotkey);
        _smartPasteCombination = HotkeyConverter.FromFriendlyString(smartPasteHotkey);
    }

    /// <summary>
    /// Handles a key press event, checking for hotkeys and starting the recording service if necessary.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="e">The event arguments.</param>
    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var capturedKey = e.Data.KeyCode;
        var rawModifiers = e.RawEvent.Mask;
        
        // If the recording service is active, capture and process the key press.
        if (recordingService.IsRecording)
        {
            // If the key pressed is a modifier, just update the UI feedback and wait.
            if (HotkeyConverter.IsModifier(capturedKey))
            {
                var modifiersOnlyString = HotkeyConverter.ToFriendlyString(KeyCode.VcUndefined, rawModifiers);
                recordingService.OnRecordingStateUpdated(modifiersOnlyString + "...");
            }
            else // The key is a non-modifier; this finalizes the hotkey.
            {
                recordingService.OnHotkeyDetected(capturedKey, rawModifiers);
                recordingService.EndRecording();
            }
            return;
        }
        
        // Normal hotkey detection logic

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
        if (capturedKey == _actionMenuCombination.key && normalizedModifiers == _actionMenuCombination.modifiers)
            ActionMenuHotkeyPressed?.Invoke();

        // Check for Smart Paste Hotkey
        if (capturedKey == _smartPasteCombination.key && normalizedModifiers == _smartPasteCombination.modifiers)
            SmartPasteHotkeyPressed?.Invoke();
    }
    
    /// <summary>
    /// Handles a key release event, updating the live feedback text when a modifier is released.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="e">The event arguments.</param>
    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        // This event is now only used to update the live feedback text when a modifier is released.
        if (!recordingService.IsRecording) return;
        
        var rawModifiers = e.RawEvent.Mask;
        var modifiersOnlyString = HotkeyConverter.ToFriendlyString(KeyCode.VcUndefined, rawModifiers);
        recordingService.OnRecordingStateUpdated(modifiersOnlyString + "...");
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        _hook.KeyPressed -= OnKeyPressed;
        _hook.KeyReleased -= OnKeyReleased;
        _hook.Dispose();
    }
}