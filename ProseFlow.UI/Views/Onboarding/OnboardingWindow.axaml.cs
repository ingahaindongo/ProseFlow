using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProseFlow.UI.ViewModels.Onboarding;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Onboarding;

public partial class OnboardingWindow : Window
{
    private bool _isRecordingHotkey;
    private const string RecordingPrompt = "Press a key combination...";
    
    public OnboardingWindow()
    {
        InitializeComponent();
        DataContextChanged += async (_, _) =>
        {
            if (DataContext is OnboardingViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
    }

    private void Button_SkipOnboard(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
    
    private void OnboardingWindow_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is OnboardingViewModel vm)
        {
            vm.OnClosing();
        }
    }
    
    private void HotkeyBox_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        
        _isRecordingHotkey = true;
        textBox.Text = RecordingPrompt;
    }

    private void HotkeyBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        
        _isRecordingHotkey = false;

        if (DataContext is OnboardingViewModel vm && textBox.Text == RecordingPrompt) textBox.Text = vm.ActionMenuHotkey;
    }
    
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

        // Ignore presses of only modifier keys
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
        if (DataContext is OnboardingViewModel vm)
        {
            vm.ActionMenuHotkey = hotkeyString;
            textBox.Text = hotkeyString;
        }

        // Stop recording and move focus away to signify completion
        _isRecordingHotkey = false;
        MainContentArea.Focus();
    }
}