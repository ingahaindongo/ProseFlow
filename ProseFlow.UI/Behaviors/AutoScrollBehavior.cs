using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;

namespace ProseFlow.UI.Behaviors;

/// <summary>
/// An attached behavior that makes a ScrollViewer automatically scroll to the end
/// when the ItemsSource of a child ItemsControl is updated.
/// </summary>
public class AutoScrollBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<AutoScrollBehavior, ScrollViewer, bool>("IsEnabled");

    static AutoScrollBehavior()
    {
        IsEnabledProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs<bool>>(OnIsEnabledChanged));
    }

    public static bool GetIsEnabled(ScrollViewer element)
    {
        return element.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(ScrollViewer element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> e)
    {
        if (e.Sender is not ScrollViewer scrollViewer) return;

        if (e.NewValue.Value)
        {
            scrollViewer.AttachedToVisualTree += OnAttached;
            scrollViewer.DetachedFromVisualTree += OnDetached;
        }
        else
        {
            scrollViewer.AttachedToVisualTree -= OnAttached;
            scrollViewer.DetachedFromVisualTree -= OnDetached;
        }
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not ScrollViewer { Content: Grid grid } scrollViewer || grid.Children.Count == 0 || grid.Children[0] is not ItemsControl itemsControl) 
            return;
        
        if (itemsControl.Items is INotifyCollectionChanged collection)
            collection.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add) scrollViewer.ScrollToEnd();
            };
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Event handlers on the collection will be cleaned up with the control.
    }
}