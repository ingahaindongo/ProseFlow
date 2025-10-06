using Avalonia;
using Avalonia.Controls;
using ProseFlow.Application.Events;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Overrides the base method to handle changes to Avalonia properties.
    /// This is used to detect when the window is minimized or restored.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != WindowStateProperty) return;
        var newWindowState = change.GetNewValue<WindowState>();
        var isVisible = newWindowState != WindowState.Minimized;
        AppEvents.OnMainWindowVisibilityChanged(isVisible);
    }
}