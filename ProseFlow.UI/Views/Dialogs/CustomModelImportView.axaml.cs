using Avalonia.Controls;
using ProseFlow.UI.ViewModels.Dialogs;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Dialogs;

public partial class CustomModelImportView : Window
{
    public CustomModelImportView()
    {
        InitializeComponent();
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Ensure the TaskCompletionSource is completed if the window is closed via other means
        if (DataContext is CustomModelImportViewModel vm) vm.CompletionSource.TrySetResult(null);
        
        base.OnClosing(e);
    }
}