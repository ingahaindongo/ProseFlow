using System;
using ProseFlow.UI.ViewModels.Actions;
using ShadUI;

namespace ProseFlow.UI.Views.Actions;

public partial class ActionEditorView : Window
{
    public ActionEditorView()
    {
        InitializeComponent();
    }
    
    private async void Window_OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ActionEditorViewModel vm) await vm.OnNavigatedToAsync();
    }
}