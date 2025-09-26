using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProseFlow.UI.ViewModels.Settings;

namespace ProseFlow.UI.Views.Settings;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Activates recording mode when the user focuses on a hotkey input box.
    /// </summary>
    private void HotkeyBox_OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is not TextBox textBox || DataContext is not SettingsViewModel vm) return;
        
        var hotkeyName = textBox.Name == nameof(ActionMenuHotkeyBox) ? "ActionMenu" : "SmartPaste";
        vm.StartRecordingCommand.Execute(hotkeyName);
    }

    /// <summary>
    /// Deactivates recording mode when the user clicks away from a hotkey input box.
    /// </summary>
    private void HotkeyBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;
        vm.StopRecordingCommand.Execute(null);
    }
}