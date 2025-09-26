using Avalonia.Controls;
using Avalonia.Input;
using ProseFlow.Application.DTOs;
using ProseFlow.UI.ViewModels.Windows;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Windows;

public partial class DiffViewWindow : Window
{
    private readonly ScrollViewer? _leftScrollViewer;
    private readonly ScrollViewer? _rightScrollViewer;
    private bool _isInternalScrollChange;

    public DiffViewWindow()
    {
        InitializeComponent();

        _leftScrollViewer = this.FindControl<ScrollViewer>("LeftScrollViewer");
        _rightScrollViewer = this.FindControl<ScrollViewer>("RightScrollViewer");

        if (_leftScrollViewer is not null)
            _leftScrollViewer.ScrollChanged += OnScrollChanged;
        
        if (_rightScrollViewer is not null)
            _rightScrollViewer.ScrollChanged += OnScrollChanged;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_isInternalScrollChange || _leftScrollViewer is null || _rightScrollViewer is null) return;

        _isInternalScrollChange = true;
        
        var offset = Equals(sender, _leftScrollViewer) ? _leftScrollViewer.Offset : _rightScrollViewer.Offset;

        _leftScrollViewer.Offset = offset;
        _rightScrollViewer.Offset = offset;

        _isInternalScrollChange = false;
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks
        if (_leftScrollViewer is not null)
            _leftScrollViewer.ScrollChanged -= OnScrollChanged;
        
        if (_rightScrollViewer is not null)
            _rightScrollViewer.ScrollChanged -= OnScrollChanged;
            
        // Ensure the TaskCompletionSource is completed with 'Cancelled' if the window is closed by the user (e.g., Alt+F4).
        if (DataContext is DiffViewModel vm) vm.CompletionSource.TrySetResult(new Cancelled());
        
        base.OnClosing(e);
    }
    
    private void RefineTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not DiffViewModel vm) return;
        vm.SubmitRefinementCommand.Execute(this);
        e.Handled = true;
    }
}