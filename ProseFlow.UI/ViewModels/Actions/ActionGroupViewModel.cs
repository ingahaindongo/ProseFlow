using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProseFlow.UI.ViewModels.Actions;

/// <summary>
/// Represents a group of actions (e.g., "Contextual Actions", "General Actions").
/// </summary>
public partial class ActionGroupViewModel(string name) : ViewModelBase
{
    [ObservableProperty]
    private string _name = name;
    
    [ObservableProperty]
    private bool _isExpanded = true;
    
    [ObservableProperty]
    private bool _isSelected;

    public ObservableCollection<ActionItemViewModel> Actions { get; init; } = [];
}