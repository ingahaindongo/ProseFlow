using Avalonia;
using Avalonia.Controls.Primitives;
using ProseFlow.Core.Enums;

namespace ProseFlow.UI.Controls;

/// <summary>
/// A custom control representing the ProseFlow Orb. It displays different visual states
/// based on application activity and user interaction.
/// </summary>
public class FloatingOrb : TemplatedControl
{
    /// <summary>
    /// Defines the State attached property.
    /// This property controls the visual appearance and animations of the Orb.
    /// </summary>
    public static readonly StyledProperty<ActionProcessingState> StateProperty =
        AvaloniaProperty.Register<FloatingOrb, ActionProcessingState>(nameof(State));

    /// <summary>
    /// Gets or sets the current visual state of the Orb.
    /// </summary>
    public ActionProcessingState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    /// <summary>
    /// Defines the ActionCount property.
    /// This property displays a numerical badge indicating the number of active background tasks.
    /// </summary>
    public static readonly StyledProperty<int> ActionCountProperty =
        AvaloniaProperty.Register<FloatingOrb, int>(nameof(ActionCount));

    /// <summary>
    /// Gets or sets the number of active actions to display in the badge.
    /// </summary>
    public int ActionCount
    {
        get => GetValue(ActionCountProperty);
        set => SetValue(ActionCountProperty, value);
    }

    /// <summary>
    /// Defines the IsBadgeVisible property.
    /// Controls the visibility of the action count badge.
    /// </summary>
    public static readonly StyledProperty<bool> IsBadgeVisibleProperty =
        AvaloniaProperty.Register<FloatingOrb, bool>(nameof(IsBadgeVisible));

    /// <summary>
    /// Gets or sets a value indicating whether the action count badge should be visible.
    /// </summary>
    public bool IsBadgeVisible
    {
        get => GetValue(IsBadgeVisibleProperty);
        set => SetValue(IsBadgeVisibleProperty, value);
    }
}