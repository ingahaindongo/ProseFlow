using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.Events;
using ProseFlow.Core.Models;
using ProseFlow.UI.ViewModels.Actions;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.UI.ViewModels.Windows;

public partial class FloatingActionMenuViewModel : ViewModelBase
{
    private readonly TaskCompletionSource<ActionExecutionRequest?> _selectionTcs = new();
    private readonly List<Action> _allAvailableActions;
    private readonly string _activeAppContext;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private object? _selectedItem;
    [ObservableProperty] private string _currentServiceTypeName = "Cloud";
    [ObservableProperty] private string _resultContainer = "Default";
    [ObservableProperty] private string _customInstruction = string.Empty;
    [ObservableProperty] private bool _isCustomInstructionActive;

    public bool ShouldClose { get; private set; }

    public ObservableCollection<ActionGroupViewModel> ActionGroups { get; } = [];

    public FloatingActionMenuViewModel(IEnumerable<Action> availableActions, ProviderSettings providerSettings,
        string activeAppContext)
    {
        _allAvailableActions = availableActions.ToList();
        _activeAppContext = activeAppContext;
        CurrentServiceTypeName = providerSettings.PrimaryServiceType;
        
        FilterAndGroupActions();
    }

    public Task<ActionExecutionRequest?> WaitForSelectionAsync()
    {
        return _selectionTcs.Task;
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterAndGroupActions();
    }

    partial void OnSelectedItemChanged(object? oldValue, object? newValue)
    {
        switch (oldValue)
        {
            // Deselect the old item
            case ActionGroupViewModel oldGroup:
                oldGroup.IsSelected = false;
                break;
            case ActionItemViewModel oldAction:
                oldAction.IsSelected = false;
                break;
        }

        switch (newValue)
        {
            // Select the new item
            case ActionGroupViewModel newGroup:
                newGroup.IsSelected = true;
                break;
            case ActionItemViewModel newAction:
                newAction.IsSelected = true;
                break;
        }
    }
    
    private void FilterAndGroupActions()
    {
        ActionGroups.Clear();
    
        // If searching, create a flat list under a "Search Results" group
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchResults = _allAvailableActions
                .Where(a => a.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .Select(a => new ActionItemViewModel(a)).ToList();
    
            if (searchResults.Count != 0)
            {
                var searchGroup = new ActionGroupViewModel("Search Results") { IsExpanded = true };
                foreach (var item in searchResults) searchGroup.Actions.Add(item);
                ActionGroups.Add(searchGroup);
            }
        }
        else
        {
            // Otherwise, group by the actual ActionGroup
            var groupedActions = _allAvailableActions
                .GroupBy(a => a.ActionGroup)
                .OrderBy(g => g.Key?.SortOrder ?? int.MaxValue);
    
            foreach (var group in groupedActions)
            {
                var groupName = group.Key?.Name ?? "Uncategorized";
                var actionGroupVm = new ActionGroupViewModel(groupName);
    
                foreach (var action in group.OrderBy(a => a.SortOrder))
                {
                    var isContextual = action.ApplicationContext.Count > 0 &&
                                       action.ApplicationContext.Any(a => a.Contains(_activeAppContext, StringComparison.OrdinalIgnoreCase));
                    actionGroupVm.Actions.Add(new ActionItemViewModel(action) { IsContextual = isContextual });
                }
                ActionGroups.Add(actionGroupVm);
            }
        }
    
        SelectedItem = GetFlatListOfVisibleItems().FirstOrDefault();
    }

    private List<object> GetFlatListOfVisibleItems()
    {
        var flatList = new List<object>();
        foreach (var group in ActionGroups)
        {
            flatList.Add(group);
            if (group.IsExpanded) flatList.AddRange(group.Actions);
        }
        return flatList;
    }
    
    [RelayCommand]
    private void ConfirmSelection()
    {
        ShouldClose = false;

        // Prioritize custom instruction if it exists
        if (IsCustomInstructionActive && !string.IsNullOrWhiteSpace(CustomInstruction))
        {
            var customAction = new Action
            {
                Name = "Custom Instruction",
                Instruction = CustomInstruction,
                OpenInWindow = false, // Default for custom actions is in-place replacement
                ExplainChanges = false,
                Prefix = string.Empty,
                Icon = "Sparkles"
            };

            var forceOpenInWindow = ResultContainer == "Default" ? customAction.OpenInWindow : ResultContainer == "Windowed";

            var request = new ActionExecutionRequest(
                customAction,
                forceOpenInWindow,
                CurrentServiceTypeName
            );

            if (!_selectionTcs.Task.IsCompleted)
                _selectionTcs.SetResult(request);

            ShouldClose = true;
            return;
        }
        
        switch (SelectedItem)
        {
            case null:
                CancelSelection();
                return;
            // If a group header is selected, Enter toggles its expansion state
            case ActionGroupViewModel group:
                group.IsExpanded = !group.IsExpanded;
                return;
            // If an action item is selected, execute it
            case ActionItemViewModel actionItem:
            {
                var forceOpenInWindow = ResultContainer == "Default" 
                    ? actionItem.Action.OpenInWindow 
                    : ResultContainer == "Windowed";

                var request = new ActionExecutionRequest(
                    actionItem.Action,
                    forceOpenInWindow,
                    CurrentServiceTypeName
                );

                if (!_selectionTcs.Task.IsCompleted)
                    _selectionTcs.SetResult(request);
            
                ShouldClose = true;
                break;
            }
        }
    }
    
    [RelayCommand]
    private void CancelSelection()
    {
        ShouldClose = true;
        if (!_selectionTcs.Task.IsCompleted)
            _selectionTcs.SetResult(null);
    }

    [RelayCommand]
    private void ToggleServiceType()
    {
        CurrentServiceTypeName = CurrentServiceTypeName == "Cloud" ? "Local" : "Cloud";
    }
    
    
    // Override the "Open in new window" behavior
    [RelayCommand]
    private void ToggleResultContainer()
    {
        var states = new[] { "Default", "Windowed", "In-place" };
        var currentIndex = Array.IndexOf(states, ResultContainer);
        ResultContainer = states[(currentIndex + 1) % states.Length];
    }

    [RelayCommand]
    private void SelectAndConfirmItem(object? item)
    {
        if (item is null) return;
        SelectedItem = item;
        ConfirmSelection();
    }

    [RelayCommand]
    private void SelectNextItem()
    {
        var flatList = GetFlatListOfVisibleItems();
        if (flatList.Count == 0) return;
        var currentIndex = SelectedItem != null ? flatList.IndexOf(SelectedItem) : -1;
        SelectedItem = flatList[(currentIndex + 1) % flatList.Count];
    }

    [RelayCommand]
    private void SelectPreviousItem()
    {
        var flatList = GetFlatListOfVisibleItems();
        if (flatList.Count == 0) return;
        var currentIndex = SelectedItem != null ? flatList.IndexOf(SelectedItem) : -1;
        var newIndex = currentIndex - 1 < 0 ? flatList.Count - 1 : currentIndex - 1;
        SelectedItem = flatList[newIndex];
    }

    [RelayCommand]
    private void CollapseSelectedItem()
    {
        if (SelectedItem is ActionGroupViewModel group)
        {
            group.IsExpanded = false;
        }
        else if (SelectedItem is ActionItemViewModel item)
        {
            var parentGroup = ActionGroups.FirstOrDefault(g => g.Actions.Contains(item));
            if (parentGroup is not null)
            {
                parentGroup.IsExpanded = false;
                SelectedItem = parentGroup; // Move selection to the group header
            }
        }
    }

    [RelayCommand]
    private void ExpandSelectedItem()
    {
        if (SelectedItem is ActionGroupViewModel group) group.IsExpanded = true;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var mainWindow =
            Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

        if (mainWindow?.DataContext is MainViewModel mainWindowViewModel)
        {
            mainWindow.Show();
            mainWindow.Activate();
            mainWindowViewModel.Navigate(
                mainWindowViewModel.PageViewModels.FirstOrDefault(x => x.Title == "Providers"));
        }

        CancelSelection();
    }
}