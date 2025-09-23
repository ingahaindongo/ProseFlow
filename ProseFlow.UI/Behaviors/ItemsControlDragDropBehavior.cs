using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace ProseFlow.UI.Behaviors;

/// <summary>
/// An attached behavior that adds drag-and-drop reordering functionality to an Avalonia ItemsControl.
/// It works by invoking a command on the ViewModel when an item is dropped onto another.
/// </summary>
public class ItemsControlDragDropBehavior : AvaloniaObject
{
    // The command to execute on the ViewModel when a drop occurs.
    public static readonly AttachedProperty<ICommand> ReorderCommandProperty =
        AvaloniaProperty.RegisterAttached<ItemsControlDragDropBehavior, Control, ICommand>(
            "ReorderCommand", coerce: OnCommandChanged);

    public static ICommand GetReorderCommand(Control element)
    {
        return element.GetValue(ReorderCommandProperty);
    }

    public static void SetReorderCommand(Control element, ICommand value)
    {
        element.SetValue(ReorderCommandProperty, value);
    }

    /// <summary>
    /// Called when the ReorderCommand property is attached to a Control.
    /// This is where we hook up our event handlers.
    /// </summary>
    private static ICommand OnCommandChanged(AvaloniaObject target, ICommand command)
    {
        if (target is not Control control) return command;

        // Enable dropping on the control.
        DragDrop.SetAllowDrop(control, true);

        // Subscribe to events.
        control.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        control.AddHandler(DragDrop.DropEvent, OnDrop);
        control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: true);

        return command;
    }

    /// <summary>
    /// Initiates the drag operation when the user presses the mouse on an item.
    /// </summary>
    private static async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;

        // Ensure the press is a primary-button click (e.g., left-click).
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed) return;
        
        // Don't start a drag if clicking on a button.
        if ((e.Source as Control)?.FindAncestorOfType<Button>(true) is not null) return;

        var itemsControl = control.FindAncestorOfType<ItemsControl>(true);
        if (itemsControl is null) return;

        // Find the item container that was clicked.
        var sourceContainer = FindItemContainer(itemsControl, e.Source as Control);
        if (sourceContainer?.DataContext is null) return;
        
        var draggedItem = sourceContainer.DataContext;
        var dataObject = new DataObject();
        dataObject.Set(nameof(ItemsControlDragDropBehavior), draggedItem);

        // Start the drag operation.
        await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);
    }

    /// <summary>
    /// Handles the visual feedback as an item is dragged over the control.
    /// </summary>
    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        // Check if we are dragging the type of data this behavior handles.
        var isSupported = e.Data.Contains(nameof(ItemsControlDragDropBehavior));
        e.DragEffects = isSupported ? DragDropEffects.Move : DragDropEffects.None;
    }

    /// <summary>
    /// Executes when the user drops an item.
    /// </summary>
    private static void OnDrop(object? sender, DragEventArgs e)
    {
        if (sender is not Control control) return;

        var itemsControl = control.FindAncestorOfType<ItemsControl>(true);
        if (itemsControl is null) return;

        // Find the target container.
        var targetContainer = FindItemContainer(itemsControl, e.Source as Control);
        if (targetContainer?.DataContext is null) return;
        
        var targetItem = targetContainer.DataContext;

        // Get the dragged item from the data payload.
        if (e.Data.Get(nameof(ItemsControlDragDropBehavior)) is not { } draggedItem) return;
        
        // If dropping onto itself, do nothing.
        if (ReferenceEquals(draggedItem, targetItem)) return;
        
        // Execute the command on the ViewModel.
        var command = GetReorderCommand(control);
        var parameter = (draggedItem, targetItem);
        if (command.CanExecute(parameter)) command.Execute(parameter);
    }
    
    /// <summary>
    /// Walks up the visual tree from an element to find the corresponding ItemsControl item container.
    /// </summary>
    private static Control? FindItemContainer(ItemsControl itemsControl, Control? element)
    {
        while (element != null)
        {
            if (element is { } control && itemsControl.IndexFromContainer(control) != -1) return control;
            element = element.Parent as Control;
        }
        return null;
    }
}