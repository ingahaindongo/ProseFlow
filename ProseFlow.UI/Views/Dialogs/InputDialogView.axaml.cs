using Avalonia.Controls;
using Avalonia.Input;
using ProseFlow.UI.ViewModels.Dialogs;

namespace ProseFlow.UI.Views.Dialogs;

public partial class InputDialogView : UserControl
{
    public InputDialogView()
    {
        InitializeComponent();
    }

    private void InputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not InputDialogViewModel vm) return;
        
        if (e.Key == Key.Enter)
        {
            vm.SubmitCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            vm.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }
}