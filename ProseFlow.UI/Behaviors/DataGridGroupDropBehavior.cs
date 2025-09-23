using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace ProseFlow.UI.Behaviors;

/// <summary>
/// An attached behavior that adds drag-and-drop reordering for DataGrid group headers.
/// It invokes a command with the source and target group keys (names) when a drop occurs.
/// </summary>
public class DataGridGroupDropBehavior : AvaloniaObject
{
    private const string GroupDragKey = "DataGridGroupKey";

    // The command to execute on the ViewModel when a drop occurs.
    public static readonly AttachedProperty<ICommand> CommandProperty =
        AvaloniaProperty.RegisterAttached<DataGridGroupDropBehavior, DataGrid, ICommand>(
            "Command", coerce: OnCommandChanged);

    public static ICommand GetCommand(DataGrid element)
    {
        return element.GetValue(CommandProperty);
    }

    public static void SetCommand(DataGrid element, ICommand value)
    {
        element.SetValue(CommandProperty, value);
    }

    /// <summary>
    /// Called when the ReorderCommand property is attached to a DataGrid.
    /// This is where we hook up our event handlers.
    /// </summary>
    private static ICommand OnCommandChanged(AvaloniaObject target, ICommand command)
    {
        if (target is not DataGrid dataGrid) return command;

        // Enable dropping on the DataGrid itself is sufficient.
        DragDrop.SetAllowDrop(dataGrid, true);

        // Subscribe to events on the DataGrid.
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
        if (!e.GetCurrentPoint(dataGrid).Properties.IsLeftButtonPressed) return;

        // The key is to find the DataGridRowGroupHeader as the source of the press.
        var header = (e.Source as Control)?.FindAncestorOfType<DataGridRowGroupHeader>();
        if (header?.DataContext is not DataGridCollectionViewGroup group) return;

        // The 'Key' property of the group's DataContext holds the value we grouped by (the group name).
        if (group.Key is not { } groupKey) return;

        var dataObject = new DataObject();
        dataObject.Set(GroupDragKey, groupKey);

        await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);
    }

    /// <summary>
    /// Handles the visual feedback as an item is dragged over the DataGrid.
    /// </summary>
    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        // Check if we are dragging the type of data this behavior handles.
        var isSupported = e.Data.Contains(GroupDragKey);
        e.DragEffects = isSupported ? DragDropEffects.Move : DragDropEffects.None;
    }

    /// <summary>
    /// Executes when the user drops an item onto the DataGrid.
    /// </summary>
    private static void OnDrop(object? sender, DragEventArgs e)
    {
        if (sender is not DataGrid dataGrid) return;

        // Find the target group header.
        var targetHeader = (e.Source as Control)?.FindAncestorOfType<DataGridRowGroupHeader>();
        if (targetHeader?.DataContext is not DataGridCollectionViewGroup targetGroup) return;

        // Get the keys for both the dragged and target groups.
        if (e.Data.Get(GroupDragKey) is not { } draggedKey || targetGroup.Key is not { } targetKey) return;
        
        if (Equals(draggedKey, targetKey)) return;

        // Execute the command on the ViewModel with the group keys.
        var command = GetCommand(dataGrid);
        var parameter = (dragged: draggedKey, target: targetKey);
        if (command.CanExecute(parameter)) command.Execute(parameter);
    }
}