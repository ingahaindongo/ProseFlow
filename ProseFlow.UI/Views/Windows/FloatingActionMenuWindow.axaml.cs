using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ProseFlow.UI.ViewModels.Actions;
using ProseFlow.UI.ViewModels.Windows;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Windows;

public partial class FloatingActionMenuWindow : Window
{
    public FloatingActionMenuWindow()
    {
        InitializeComponent();
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // Position the window near the mouse cursor
        if (Screens.Primary != null)
            Position = new PixelPoint(
                (int)(Screens.Primary.WorkingArea.Center.X - Width / 2),
                (int)(Screens.Primary.WorkingArea.Center.Y - Height / 2 - 100)
            );
        

        // Focus the search box for immediate typing
        Dispatcher.UIThread.Post(() =>
        {
            Activate();
            Focus();
            SearchBox.Focus();
        }, DispatcherPriority.Background);
        
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not FloatingActionMenuViewModel vm) return;

        switch (e.Key)
        {
            case Key.Escape:
                vm.CancelSelectionCommand.Execute(null);
                Close();
                break;
            case Key.Enter:
                vm.ConfirmSelectionCommand.Execute(null);
                e.Handled = true;
                if (vm.ShouldClose) Close(); 
                break;
            case Key.Up:
                vm.SelectPreviousItemCommand.Execute(null);
                ScrollSelectedItemIntoView();
                e.Handled = true;
                break;
            case Key.Down:
                vm.SelectNextItemCommand.Execute(null);
                ScrollSelectedItemIntoView();
                e.Handled = true;
                break;
            case Key.Left:
                vm.CollapseSelectedItemCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Right:
                vm.ExpandSelectedItemCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void WindowBase_OnDeactivated(object? sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Finds the UI element corresponding to the currently selected item in the ViewModel
    /// and ensures it is visible within the ScrollViewer.
    /// </summary>
    private void ScrollSelectedItemIntoView()
    {
        if (DataContext is not FloatingActionMenuViewModel vm || vm.SelectedItem is null) return;

        // Use multiple dispatcher calls to ensure the visual tree is fully updated
        Dispatcher.UIThread.Post(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                TryScrollSelectedItemIntoView(vm);
            }, DispatcherPriority.Render);
        }, DispatcherPriority.Render);
    }

    private void TryScrollSelectedItemIntoView(FloatingActionMenuViewModel vm)
    {
        try
        {
            // Find the control that represents the selected item
            var selectedControl = this.GetVisualDescendants()
                .OfType<Control>()
                .FirstOrDefault(c => c.DataContext == vm.SelectedItem);

            if (selectedControl == null)
            {
                var selectedContainer = this.GetVisualDescendants()
                    .OfType<Control>()
                    .FirstOrDefault(c => c.DataContext == vm.SelectedItem);
                selectedContainer?.BringIntoView();
                return;
            }

            var scrollViewer = this.FindControl<ScrollViewer>("ActionListScrollViewer");
            if (scrollViewer == null) return;

            // Force layout update to ensure accurate measurements
            selectedControl.InvalidateMeasure();
            selectedControl.InvalidateArrange();
            
            if (scrollViewer.Content is not Control scrollContent) return;

            // Calculate control position relative to the scroll content
            var controlPosition = selectedControl.TranslatePoint(new Point(0, 0), scrollContent);
            if (!controlPosition.HasValue) return;

            var controlTop = controlPosition.Value.Y;
            var controlBottom = controlTop + selectedControl.Bounds.Height;
            
            var viewportHeight = scrollViewer.Viewport.Height;
            var currentScrollTop = scrollViewer.Offset.Y;
            var currentScrollBottom = currentScrollTop + viewportHeight;
            
            const double margin = 20;
            
            var newScrollY = currentScrollTop;
            
            // Check if control is above or below the visible area
            if (controlTop < currentScrollTop + margin)
                newScrollY = Math.Max(0, controlTop - margin);
            else if (controlBottom > currentScrollBottom - margin) 
                newScrollY = Math.Max(0, controlBottom - viewportHeight + margin);

            // Only scroll if we need to
            if (!(Math.Abs(newScrollY - currentScrollTop) > 1)) return;
            
            scrollViewer.Offset = scrollViewer.Offset.WithY(newScrollY);
                
            // For the last few items, ensure we scroll to the very bottom if needed
            Dispatcher.UIThread.Post(() =>
            {
                var maxScrollY = Math.Max(0, scrollContent.Bounds.Height - viewportHeight);
                if (newScrollY >= maxScrollY - 10) // Close to bottom
                    scrollViewer.Offset = scrollViewer.Offset.WithY(maxScrollY);
            }, DispatcherPriority.Background);
        }
        catch (Exception)
        {
            var selectedContainer = this.GetVisualDescendants()
                .OfType<Control>()
                .FirstOrDefault(c => c.DataContext == vm.SelectedItem);
            selectedContainer?.BringIntoView();
        }
    }

    private void ActionButton_OnPointerPressed(object? sender, RoutedEventArgs  e)
    {
        if (sender is not Button { DataContext: ActionItemViewModel action } || DataContext is not FloatingActionMenuViewModel vm) return;
        vm.SelectAndConfirmItemCommand.Execute(action);
        Close();
    }

    private void CustomInstructionButton_OnPointerPressed(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FloatingActionMenuViewModel vm) return;
        vm.ConfirmSelectionCommand.Execute(null);
        Close();
    }
}