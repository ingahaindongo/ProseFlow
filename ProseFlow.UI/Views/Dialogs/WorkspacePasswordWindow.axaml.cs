using Avalonia.Controls;
using ProseFlow.UI.Models;
using ProseFlow.UI.ViewModels.Dialogs;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Dialogs;

public partial class WorkspacePasswordWindow : Window
{
    public WorkspacePasswordWindow()
    {
        InitializeComponent();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is WorkspacePasswordViewModel vm) 
            vm.CompletionSource.TrySetResult(new WorkspacePasswordResult(false, null));
    }
}