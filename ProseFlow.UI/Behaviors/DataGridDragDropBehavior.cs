using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace ProseFlow.UI.Behaviors;

/// <summary>
/// An attached behavior that adds drag-and-drop reordering functionality to an Avalonia DataGrid.
/// It works by invoking a command on the ViewModel when a row is dropped onto another.
/// </summary>
public class DataGridDragDropBehavior : AvaloniaObject
{
    // The command to execute on the ViewModel when a drop occurs.
    public static readonly AttachedProperty<ICommand> ReorderCommandProperty =
        AvaloniaProperty.RegisterAttached<DataGridDragDropBehavior, DataGrid, ICommand>(
            "ReorderCommand", coerce: OnCommandChanged);

    // Attached property to store the DataGrid instance.
    private static readonly AttachedProperty<DataGrid?> DataGridProperty =
        AvaloniaProperty.RegisterAttached<DataGridDragDropBehavior, Control, DataGrid?>("DataGrid");

    public static ICommand GetReorderCommand(DataGrid element)
    {
        return element.GetValue(ReorderCommandProperty);
    }

    public static void SetReorderCommand(DataGrid element, ICommand value)
    {
        element.SetValue(ReorderCommandProperty, value);
    }

    /// <summary>
    /// Called when the ReorderCommand property is attached to a DataGrid.
    /// This is where we hook up our event handlers.
    /// </summary>
    private static ICommand OnCommandChanged(AvaloniaObject target, ICommand command)
    {
        if (target is not DataGrid dataGrid) return command;

        // Enable dropping on the DataGrid.
        DragDrop.SetAllowDrop(dataGrid, true);

        // Subscribe to events.
        dataGrid.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        dataGrid.AddHandler(DragDrop.DropEvent, OnDrop);
        dataGrid.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: true);

        return command;
    }

    /// <summary>
    /// Initiates the drag operation when the user presses the mouse on a row.
    /// </summary>
    private static async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;

        // Ensure the press is a primary-button click (e.g., left-click).
        if (!e.GetCurrentPoint(dataGrid).Properties.IsLeftButtonPressed) return;

        // Find the DataGridRow that was clicked.
        var source = e.Source as Control;
        var row = source?.FindAncestorOfType<DataGridRow>();
        if (row?.DataContext is null) return;
        
        if (source?.FindAncestorOfType<Button>(true) is not null) return;

        // The item being dragged.
        var draggedItem = row.DataContext;
        var dataObject = new DataObject();
        dataObject.Set(nameof(DataGridDragDropBehavior), draggedItem);

        // Start the drag operation.
        await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);
    }

    /// <summary>
    /// Handles the visual feedback as an item is dragged over the DataGrid.
    /// </summary>
    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        // Check if we are dragging the type of data this behavior handles.
        var isSupported = e.Data.Contains(nameof(DataGridDragDropBehavior));
        e.DragEffects = isSupported ? DragDropEffects.Move : DragDropEffects.None;
    }

    /// <summary>
    /// Executes when the user drops an item onto the DataGrid.
    /// </summary>
    private static void OnDrop(object? sender, DragEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;
        
        // Find the target row.
        var targetRow = (e.Source as Control)?.FindAncestorOfType<DataGridRow>();
        if (targetRow?.DataContext is null) return;
        
        var targetItem = targetRow.DataContext;

        // Get the dragged item from the data payload.
        if (e.Data.Get(nameof(DataGridDragDropBehavior)) is not { } draggedItem) return;
        
        // If dropping onto itself, do nothing.
        if (ReferenceEquals(draggedItem, targetItem)) return;
        
        // Execute the command on the ViewModel.
        var command = GetReorderCommand(dataGrid);
        var parameter = (draggedItem, targetItem);
        if (command?.CanExecute(parameter) == true) command.Execute(parameter);
    }
}