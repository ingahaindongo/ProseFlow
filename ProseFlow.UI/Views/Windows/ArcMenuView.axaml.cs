using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using ProseFlow.UI.Controls;
using ProseFlow.UI.ViewModels.Windows;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Windows;

/// <summary>
/// A static class to define attached properties for animations.
/// </summary>
public static class AnimationAttach
{
    /// <summary>
    /// Defines an attached property to specify a delay for an animation.
    /// When this property is set on a control, it adds the "animate-in" class after the specified delay.
    /// </summary>
    public static readonly AttachedProperty<TimeSpan> DelayProperty =
        AvaloniaProperty.RegisterAttached<Animatable, TimeSpan>("Delay", typeof(AnimationAttach), inherits: true);

    public static TimeSpan GetDelay(Animatable element) => element.GetValue(DelayProperty);
    public static void SetDelay(Animatable element, TimeSpan value) => element.SetValue(DelayProperty, value);
    
    static AnimationAttach()
    {
        DelayProperty.Changed.AddClassHandler<Control>(OnAnimationDelayChanged);
    }
    
    private static async void OnAnimationDelayChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not TimeSpan delay) return;
        await System.Threading.Tasks.Task.Delay(delay);
        control.Classes.Add("animate-in");
    }
}

public partial class ArcMenuView : Window
{
    public ArcMenuView()
    {
        InitializeComponent();
        DataContext = new ArcMenuViewModel();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
    
        if (Owner is not Window owner) return;
        
        // Force a layout update to get the proper size
        InvalidateMeasure();
        InvalidateArrange();
        UpdateLayout();


        // Get the ArcPanel from the ItemsRepeater.
        if (ArcItemsRepeater.Layout is not ArcPanel arcPanel) return;

        // Get the calculated offset of the arc's center within the panel.
        var originOffset = arcPanel.OriginOffset;

        // Get owner's position and size.
        var ownerPos = owner.Position;
        var ownerSize = owner.Bounds.Size;
        
        // Calculate the center point of the owner window.
        var ownerCenterX = ownerPos.X + ownerSize.Width / 2;
        var ownerCenterY = ownerPos.Y + ownerSize.Height / 2;
        
        // Position this window so that the ArcPanel's origin aligns with the owner's center.
        Position = new PixelPoint(
            (int)(ownerCenterX - originOffset.X),
            (int)(ownerCenterY - originOffset.Y)
        );
        
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        // Automatically close the menu when the user clicks away
        Close();
    }
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is ArcMenuViewModel vm)
        {
            // Ensure the TaskCompletionSource is completed if the window is closed to inform the FloatingOrbService to pass
            vm.CompletionSource.TrySetResult(null);
        }
        base.OnClosing(e);
    }
}