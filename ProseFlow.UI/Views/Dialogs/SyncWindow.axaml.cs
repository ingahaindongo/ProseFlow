using Avalonia.Controls;
using ProseFlow.UI.ViewModels.Dialogs;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Dialogs;

public partial class SyncWindow : Window
{
    public SyncWindow()
    {
        InitializeComponent();
    }
    
    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is SyncViewModel vm) 
            vm.CompletionSource.TrySetResult(vm.WasSuccess);
    }
}