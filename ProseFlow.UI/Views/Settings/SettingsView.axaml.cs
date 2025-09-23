using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProseFlow.UI.ViewModels.Settings;

namespace ProseFlow.UI.Views.Settings;

public partial class SettingsView : UserControl
{
    private bool _isRecordingHotkey;
    private const string RecordingPrompt = "Press a key combination...";
    
    public SettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Activates recording mode when the user focuses on a hotkey input box.
    /// </summary>
    private void HotkeyBox_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        
        _isRecordingHotkey = true;
        textBox.Text = RecordingPrompt;
    }

    /// <summary>
    /// Deactivates recording mode and restores the original text if no new hotkey was set.
    /// </summary>
    private void HotkeyBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        
        _isRecordingHotkey = false;

        if (DataContext is SettingsViewModel { Settings: not null } vm)
            // If the user clicked away without setting a key, restore the original value.
            if (textBox.Text == RecordingPrompt)
                textBox.Text = textBox.Name == nameof(ActionMenuHotkeyBox)
                    ? vm.Settings.ActionMenuHotkey
                    : vm.Settings.SmartPasteHotkey;
    }
    
    /// <summary>
    /// Captures the key combination, formats it as a string, and updates the ViewModel.
    /// </summary>
    private void HotkeyBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isRecordingHotkey || sender is not TextBox textBox) return;
        
        // Prevent the key from being typed into the box
        e.Handled = true;

        var key = e.Key;
        var modifiers = e.KeyModifiers;
        
        // Escape key exits recording mode
        if (key == Key.Escape)
        {
            HotkeyBox_OnLostFocus(sender, e);
            return;
        }

        // Ignore presses of only modifier keys (e.g., just pressing Ctrl)
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
            return;

        var parts = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Cmd");
        
        parts.Add(key.ToString());

        var hotkeyString = string.Join("+", parts);

        // Update the view model directly
        if (DataContext is SettingsViewModel { Settings: not null } vm && vm.Settings.ActionMenuHotkey != RecordingPrompt && vm.Settings.SmartPasteHotkey != RecordingPrompt)
        {
            switch (textBox.Name)
            {
                case nameof(ActionMenuHotkeyBox):
                    vm.Settings.ActionMenuHotkey = hotkeyString;
                    break;
                case nameof(SmartPasteHotkeyBox):
                    vm.Settings.SmartPasteHotkey = hotkeyString;
                    break;
            }

            textBox.Text = hotkeyString;
            vm.ValidateHotkeys();
        }

        // Stop recording and move focus away to signify completion
        _isRecordingHotkey = false;

        MainContent.Focus();
    }
}