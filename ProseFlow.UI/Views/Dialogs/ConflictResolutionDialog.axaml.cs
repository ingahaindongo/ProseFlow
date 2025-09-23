using Avalonia.Controls;
using ProseFlow.UI.ViewModels.Dialogs;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Dialogs;

public partial class ConflictResolutionDialog : Window
{
    public ConflictResolutionDialog()
    {
        InitializeComponent();
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Ensure the TaskCompletionSource is completed if the window is closed via other means
        if (DataContext is ConflictResolutionViewModel vm)
        {
            vm.CompletionSource.TrySetResult(null);
        }
        
        base.OnClosing(e);
    }
}