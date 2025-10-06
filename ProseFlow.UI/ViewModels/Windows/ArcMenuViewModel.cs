using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.UI.ViewModels.Windows;

/// <summary>
/// ViewModel for the Arc Menu window, which displays a curated list of actions
/// when text is dropped onto the Floating Orb.
/// </summary>
public partial class ArcMenuViewModel : ViewModelBase
{
    internal readonly TaskCompletionSource<Action?> CompletionSource = new();

    /// <summary>
    /// The collection of action items to be displayed in the arc.
    /// </summary>
    public ObservableCollection<ArcMenuItemViewModel> Actions { get; } = !Design.IsDesignMode ? [] : // Placeholder actions for design time
        [
            new ArcMenuItemViewModel(new Action { Name = "Proofread", Icon = "Check" } , 0),
            new ArcMenuItemViewModel(new Action { Name = "Translate", Icon = "Check" } , 1),
            new ArcMenuItemViewModel(new Action { Name = "Summarize", Icon = "Check" } , 2),
            new ArcMenuItemViewModel(new Action { Name = "Make Notes", Icon = "Check" } , 3),
            new ArcMenuItemViewModel(new Action { Name = "Extract Key Points", Icon = "Check" } , 4),
            new ArcMenuItemViewModel(new Action { Name = "Expand", Icon = "Check" } , 5),
        ];

    /// <summary>
    /// Initializes the ViewModel with a list of relevant actions.
    /// </summary>
    /// <param name="actions">The actions to display.</param>
    public void Initialize(IEnumerable<Action> actions)
    {
        Actions.Clear();
        foreach (var (action, index) in actions.Select((a, i) => (a, i)))
        {
                Actions.Add(new ArcMenuItemViewModel(action, index));
        }
    }

    /// <summary>
    /// Command executed when an action pod in the arc is clicked.
    /// </summary>
    [RelayCommand]
    private void SelectAction(Action? action)
    {
        CompletionSource.TrySetResult(action);
    }
}