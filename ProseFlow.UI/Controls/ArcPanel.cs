using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace ProseFlow.UI.Controls;

/// <summary>
/// A custom non-virtualizing layout that arranges items in a semicircular arc.
/// This class is designed to be used with an ItemsRepeater.
/// </summary>
public class ArcPanel : NonVirtualizingLayout
{
    /// <summary>
    /// Defines the Radius property.
    /// This determines how far from the center the items are placed.
    /// </summary>
    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<ArcPanel, double>(nameof(Radius), 100.0);

    /// <summary>
    /// Defines the StartAngle property.
    /// This is the angle in degrees where the first item is placed (0 is right, -90 is up).
    /// </summary>
    public static readonly StyledProperty<double> StartAngleProperty =
        AvaloniaProperty.Register<ArcPanel, double>(nameof(StartAngle), -150.0);

    /// <summary>
    /// Defines the SweepAngle property.
    /// This is the total angle in degrees that the arc covers.
    /// </summary>
    public static readonly StyledProperty<double> SweepAngleProperty =
        AvaloniaProperty.Register<ArcPanel, double>(nameof(SweepAngle), 120.0);

    /// <summary>
    /// Defines the AutoEdgeDetect property.
    /// When enabled, the arc will automatically adjust its orientation based on screen position.
    /// </summary>
    public static readonly StyledProperty<bool> AutoEdgeDetectProperty =
        AvaloniaProperty.Register<ArcPanel, bool>(nameof(AutoEdgeDetect));


    /// <summary>
    /// Defines a read-only property that exposes the arc's origin point relative to the panel's top-left corner.
    /// </summary>
    private static readonly DirectProperty<ArcPanel, Point> OriginOffsetProperty =
        AvaloniaProperty.RegisterDirect<ArcPanel, Point>(nameof(OriginOffset), o => o.OriginOffset);

    /// <summary>
    /// The backing field for the <see cref="OriginOffset"/> property.
    /// </summary>
    private Point _originOffset;

    /// <summary>
    /// Gets the calculated offset of the arc's origin (center) from the top-left corner of the panel.
    /// This is crucial for correctly positioning the parent window.
    /// </summary>
    public Point OriginOffset
    {
        get => _originOffset;
        private set => SetAndRaise(OriginOffsetProperty, ref _originOffset, value);
    }

    /// <summary>
    /// Gets or sets the radius of the arc. This determines how far from the center the items are placed.
    /// </summary>
    public double Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets the starting angle in degrees for the arc. 0 degrees is to the right, -90 is up.
    /// </summary>
    public double StartAngle
    {
        get => GetValue(StartAngleProperty);
        set => SetValue(StartAngleProperty, value);
    }

    /// <summary>
    /// Gets or sets the total angle in degrees that the arc covers.
    /// </summary>
    public double SweepAngle
    {
        get => GetValue(SweepAngleProperty);
        set => SetValue(SweepAngleProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the arc should automatically adjust its orientation
    /// to fit within the screen bounds.
    /// </summary>
    public bool AutoEdgeDetect
    {
        get => GetValue(AutoEdgeDetectProperty);
        set => SetValue(AutoEdgeDetectProperty, value);
    }

    /// <summary>
    /// Measures the desired size of the layout based on its children, arranging them in a conceptual arc.
    /// This determines the total width and height required to contain all children.
    /// </summary>
    /// <param name="context">The context object that provides information for the layout pass.</param>
    /// <param name="availableSize">The available size that this panel can give to its children.</param>
    /// <returns>The total size required by the panel.</returns>
    protected override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
    {
        var children = context.Children;
        if (children.Count == 0) return new Size();

        // If AutoEdgeDetect is enabled, adjust the angles before measuring
        var effectiveStartAngle = StartAngle;
        var effectiveSweepAngle = SweepAngle;
        
        if (AutoEdgeDetect)
        {
            AdjustAnglesForScreenBounds(context, out effectiveStartAngle, out effectiveSweepAngle);
        }

        double maxX = 0, maxY = 0;
        double minX = double.MaxValue, minY = double.MaxValue;

        var angleStep = children.Count > 1 ? effectiveSweepAngle / (children.Count - 1) : 0;

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            child.Measure(availableSize);
        
            var angle = effectiveStartAngle + i * angleStep;
            var angleInRadians = angle * (Math.PI / 180.0);
        
            // Calculate the center position of this item
            var centerX = Radius * Math.Cos(angleInRadians);
            var centerY = Radius * Math.Sin(angleInRadians);

            // Calculate the actual bounds of the child
            var childLeft = centerX - child.DesiredSize.Width / 2;
            var childTop = centerY - child.DesiredSize.Height / 2;
            var childRight = centerX + child.DesiredSize.Width / 2;
            var childBottom = centerY + child.DesiredSize.Height / 2;

            minX = Math.Min(minX, childLeft);
            minY = Math.Min(minY, childTop);
            maxX = Math.Max(maxX, childRight);
            maxY = Math.Max(maxY, childBottom);
        }
    
        var totalWidth = maxX - minX;
        var totalHeight = maxY - minY;
        
        return new Size(totalWidth, totalHeight);
    }

    /// <summary>
    /// Arranges the child elements within the final allocated size, positioning them along the arc.
    /// </summary>
    /// <param name="context">The context object that provides information for the layout pass.</param>
    /// <param name="finalSize">The final area within the parent that this panel should use to arrange its children.</param>
    /// <returns>The actual size used by the panel.</returns>
    protected override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
    {
        var children = context.Children;
        if (children.Count == 0) return finalSize;

        // If AutoEdgeDetect is enabled, adjust the angles before arranging
        var effectiveStartAngle = StartAngle;
        var effectiveSweepAngle = SweepAngle;
        
        if (AutoEdgeDetect)
        {
            AdjustAnglesForScreenBounds(context, out effectiveStartAngle, out effectiveSweepAngle);
        }

        // First, calculate the bounds to find the offset needed
        double maxX = 0, maxY = 0;
        double minX = double.MaxValue, minY = double.MaxValue;
        var angleStep = children.Count > 1 ? effectiveSweepAngle / (children.Count - 1) : 0;

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var angle = effectiveStartAngle + i * angleStep;
            var angleInRadians = angle * (Math.PI / 180.0);
        
            var centerX = Radius * Math.Cos(angleInRadians);
            var centerY = Radius * Math.Sin(angleInRadians);

            var childLeft = centerX - child.DesiredSize.Width / 2;
            var childTop = centerY - child.DesiredSize.Height / 2;
            var childRight = centerX + child.DesiredSize.Width / 2;
            var childBottom = centerY + child.DesiredSize.Height / 2;

            minX = Math.Min(minX, childLeft);
            minY = Math.Min(minY, childTop);
            maxX = Math.Max(maxX, childRight);
            maxY = Math.Max(maxY, childBottom);
        }

        // Calculate offset to make all items visible (shift everything so minX/minY become 0)
        var offsetX = -minX;
        var offsetY = -minY;

        // Store the calculated offset so the parent window can use it for correct positioning.
        OriginOffset = new Point(offsetX, offsetY);
        
        // Arrange all children with the offset
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var angle = effectiveStartAngle + i * angleStep;
            var angleInRadians = angle * (Math.PI / 180.0);

            var x = Radius * Math.Cos(angleInRadians) - (child.DesiredSize.Width / 2) + offsetX;
            var y = Radius * Math.Sin(angleInRadians) - (child.DesiredSize.Height / 2) + offsetY;
        
            child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
        }

        return finalSize;
    }

    /// <summary>
    /// Adjusts the arc angles based on the position of the owner window relative to screen edges.
    /// This helps ensure the arc menu is fully visible.
    /// </summary>
    /// <param name="context">The layout context, used to find the visual root.</param>
    /// <param name="startAngle">The calculated start angle for the arc.</param>
    /// <param name="sweepAngle">The calculated sweep angle for the arc.</param>
    private void AdjustAnglesForScreenBounds(NonVirtualizingLayoutContext context, out double startAngle, out double sweepAngle)
    {
        // Default to the original values
        startAngle = StartAngle;
        sweepAngle = SweepAngle;

        // Get the first child to find the visual root
        var firstChild = context.Children.FirstOrDefault();
        if (firstChild == null) return;
        
        // Traverse up to find the root, which should be the popup window. Then get its owner.
        if (firstChild.GetVisualRoot() is not Window { Owner: Window owner }) return;

        // Get screen bounds
        var screen = owner.Screens.ScreenFromVisual(owner);

        if (screen == null) return;

        var screenBounds = screen.WorkingArea;
        
        // Use the owner's bounds and position for edge detection.
        var ownerBounds = owner.Bounds;
        var ownerPosition = owner.Position;
        
        var centerX = ownerPosition.X + ownerBounds.Width / 2;
        var centerY = ownerPosition.Y + ownerBounds.Height / 2;

        // Determine which edges we're close to (using a threshold)
        const int threshold = 250;
        
        var nearLeftEdge = centerX < screenBounds.X + threshold;
        var nearRightEdge = centerX > screenBounds.Right - threshold;
        var nearTopEdge = centerY < screenBounds.Y + threshold;
        var nearBottomEdge = centerY > screenBounds.Bottom - threshold;
        
        // Adjust angles based on which edges we're near
        if (nearLeftEdge && nearTopEdge)
        {
            // Top-left corner: open to the bottom-right
            startAngle = -30;
            sweepAngle = 120;
        }
        else if (nearRightEdge && nearTopEdge)
        {
            // Top-right corner: open to the bottom-left
            startAngle = 60;
            sweepAngle = 120;
        }
        else if (nearLeftEdge && nearBottomEdge)
        {
            // Bottom-left corner: open to the top-right
            startAngle = -120;
            sweepAngle = 120;
        }
        else if (nearRightEdge && nearBottomEdge)
        {
            // Bottom-right corner: open to the top-left
            startAngle = 150;
            sweepAngle = 120;
        }
        else if (nearLeftEdge)
        {
            // Left edge: open to the right
            startAngle = -60;
            sweepAngle = 120;
        }
        else if (nearRightEdge)
        {
            // Right edge: open to the left
            startAngle = 120;
            sweepAngle = 120;
        }
        else if (nearTopEdge)
        {
            // Top edge: open downward
            startAngle = 30;
            sweepAngle = 120;
        }
        else if (nearBottomEdge)
        {
            // Bottom edge: open upward
            startAngle = -150;
            sweepAngle = 120;
        }
    }
}