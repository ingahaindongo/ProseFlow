using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.UI.ViewModels.Windows;

/// <summary>
/// Represents a single, animatable action item in the Arc Menu.
/// </summary>
public partial class ArcMenuItemViewModel(Action action, int index) : ViewModelBase
{
    /// <summary>
    /// The action associated with this menu item.
    /// </summary>
    [ObservableProperty]
    private Action _action = action;

    /// <summary>
    /// The staggered delay for the entry animation, creating a "fanning out" effect.
    /// </summary>
    public TimeSpan AnimationDelay { get; } = TimeSpan.FromMilliseconds((index + 1) * 50);
}