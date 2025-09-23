using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.DTOs;
using Window = ShadUI.Window;

namespace ProseFlow.UI.ViewModels.Dialogs;

/// <summary>
/// A ViewModel for a single item in the conflict resolution list.
/// </summary>
public partial class ConflictItemViewModel(ActionConflict conflict) : ViewModelBase
{
    [ObservableProperty]
    private ActionConflict _conflict = conflict;

    [ObservableProperty]
    private ConflictResolutionType _selectedResolution = conflict.Resolution;
}

public partial class ConflictResolutionViewModel : ViewModelBase
{
    internal readonly TaskCompletionSource<List<ActionConflict>?> CompletionSource = new();

    public ObservableCollection<ConflictItemViewModel> Conflicts { get; } = [];
    public List<ConflictResolutionType> ResolutionTypes { get; } = Enum.GetValues<ConflictResolutionType>().ToList();

    public void Initialize(List<ActionConflict> conflicts)
    {
        Conflicts.Clear();
        foreach (var conflict in conflicts)
        {
            Conflicts.Add(new ConflictItemViewModel(conflict));
        }
    }

    [RelayCommand]
    private void ApplyToAll(ConflictResolutionType resolution)
    {
        foreach (var conflictVm in Conflicts)
        {
            conflictVm.SelectedResolution = resolution;
        }
    }

    [RelayCommand]
    private void Confirm(Window window)
    {
        var resolvedConflicts = Conflicts
            .Select(vm => vm.Conflict with { Resolution = vm.SelectedResolution })
            .ToList();

        CompletionSource.TrySetResult(resolvedConflicts);
        window.Close();
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        CompletionSource.TrySetResult(null);
        window.Close();
    }
}