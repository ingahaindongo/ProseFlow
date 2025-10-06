using Avalonia.Controls;
using ProseFlow.UI.ViewModels.Dialogs;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Dialogs;

public partial class ManageConnectionWindow : Window
{
    public ManageConnectionWindow()
    {
        InitializeComponent();
    }
    
    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Ensure the TaskCompletionSource is completed if the window is closed via the X button
        if (DataContext is ManageConnectionViewModel vm) 
            vm.CompletionSource.TrySetResult(vm.WasSuccess);
    }
}