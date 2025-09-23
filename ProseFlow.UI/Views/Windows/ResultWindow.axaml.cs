using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProseFlow.UI.ViewModels.Windows;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Windows;

public partial class ResultWindow : Window
{
    private bool _isClosing;
    
    public ResultWindow()
    {
        InitializeComponent();
    }

    private void CloseWindow(object? sender, RoutedEventArgs e)
    {
        _isClosing = true;
        if (DataContext is ResultViewModel vm)
            vm.CloseCommand.Execute(this);
        else
            Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        // Shouldn't be called as I removed the window borders, but a defense in case
        if (!_isClosing && DataContext is ResultViewModel { IsRefinement: false } vm)
            vm.CompletionSource.TrySetResult(null); // Signal "end of session"
    }

    private void RefineTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not ResultViewModel vm) return;
        vm.RefineCommand.Execute(this);
        e.Handled = true;
    }
}