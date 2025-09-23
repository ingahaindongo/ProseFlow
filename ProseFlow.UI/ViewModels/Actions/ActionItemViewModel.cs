using CommunityToolkit.Mvvm.ComponentModel;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.UI.ViewModels.Actions;

public partial class ActionItemViewModel(Action action) : ViewModelBase
{
    [ObservableProperty]
    private Action _action = action;
    
    [ObservableProperty]
    private bool _isContextual;
    
    [ObservableProperty]
    private bool _isSelected;
}