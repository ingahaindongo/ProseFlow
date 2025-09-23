using System;
using Avalonia.Controls;
using ProseFlow.UI.ViewModels.Providers;

namespace ProseFlow.UI.Views.Providers;

public partial class ModelLibraryView : UserControl
{
    public ModelLibraryView()
    {
        InitializeComponent();
    }
    
    private async void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ModelLibraryViewModel vm) await vm.OnNavigatedToAsync();
    }
}