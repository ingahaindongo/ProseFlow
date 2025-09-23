using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.UI.Utils;
using ProseFlow.Application.Events;
using ProseFlow.Application.Services;
using ProseFlow.Core.Models;
using ProseFlow.UI.Services;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.UI.ViewModels.Actions;

public partial class ActionsViewModel(
    ActionManagementService actionService,
    IDialogService dialogService) : ViewModelBase
{
    public override string Title => "Actions";
    public override IconSymbol Icon => IconSymbol.Workflow;

    private List<ActionGroup> _actionGroupsList = [];
    private readonly ObservableCollection<Action> _allActions = [];

    [ObservableProperty]
    private DataGridCollectionView? _groupedActions;

    public override async Task OnNavigatedToAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var groups = await actionService.GetActionGroupsWithActionsAsync();
        _actionGroupsList = groups.OrderBy(g => g.SortOrder).ToList();

        _allActions.Clear();
        
        // Create a flat list of all actions, pre-sorted by the group's sort order, then the action's sort order.
        var sortedActions = _actionGroupsList
            .SelectMany(g => g.Actions)
            .OrderBy(a => a.ActionGroup!.SortOrder)
            .ThenBy(a => a.SortOrder);

        foreach (var action in sortedActions) _allActions.Add(action);

        // The DataGridCollectionView will respect the pre-sorted order when creating groups.
        var collectionView = new DataGridCollectionView(_allActions);
        collectionView.GroupDescriptions.Add(new DataGridPathGroupDescription("ActionGroup.Name"));
        
        GroupedActions = collectionView;
    }

    [RelayCommand]
    private async Task AddActionAsync()
    {
        var newAction = new Action { Name = "New Action" };
        var result = await dialogService.ShowActionEditorDialogAsync(newAction);
        if (result) await LoadDataAsync();
    }
    
    [RelayCommand]
    private async Task AddGroupAsync()
    {
        var result = await dialogService.ShowInputDialogAsync(
            "Create New Group",
            "Enter a name for the new group:",
            "Create");
        
        if (result.Success && !string.IsNullOrWhiteSpace(result.Text))
        {
            await actionService.CreateActionGroupAsync(new ActionGroup { Name = result.Text });
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task EditActionAsync(Action? action)
    {
        if (action is null) return;
        var result = await dialogService.ShowActionEditorDialogAsync(action);
        if (result) await LoadDataAsync();
    }
    
    [RelayCommand]
    private async Task EditGroupAsync(object? groupKey)
    {
        if (groupKey is not string groupName || string.IsNullOrWhiteSpace(groupName)) return;

        var group = _actionGroupsList.FirstOrDefault(g => g.Name == groupName);
        if (group is null) return;

        var result = await dialogService.ShowInputDialogAsync(
            "Rename Group",
            $"Enter a new name for '{group.Name}':",
            "Rename",
            group.Name);
        
        if (result.Success && !string.IsNullOrWhiteSpace(result.Text))
        {
            group.Name = result.Text;
            await actionService.UpdateActionGroupAsync(group);
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private void DeleteAction(Action? action)
    {
        if (action is null) return;
        dialogService.ShowConfirmationDialogAsync(
            "Delete Action",
            $"Are you sure you want to delete the action '{action.Name}'?", async () =>
            {
                await actionService.DeleteActionAsync(action.Id);
                await LoadDataAsync();
            });
    }
    
    [RelayCommand]
    private void DeleteGroup(object? groupKey)
    {
        if (groupKey is not string groupName || string.IsNullOrWhiteSpace(groupName)) return;
        
        var group = _actionGroupsList.FirstOrDefault(g => g.Name == groupName);
        if (group is null) return;

        if (group.Id == 1)
        {
            AppEvents.RequestNotification("The default 'General' group cannot be deleted.", NotificationType.Warning);
            return;
        }

        dialogService.ShowConfirmationDialogAsync(
            $"Delete '{group.Name}' Group?",
            "The actions inside this group will NOT be deleted. They will be moved to the 'General' group.", async () =>
            {
                await actionService.DeleteActionGroupAsync(group.Id);
                await LoadDataAsync();
            });
    }

    [RelayCommand]
    private async Task ReorderAsync((object dragged, object target) items)
    {
        switch (items)
        {
            // Case 1: An Action is dropped onto another Action
            case { dragged: Action draggedAction, target: Action targetAction }:
            {
                // Find the index of the target action within its group
                var group = _actionGroupsList.FirstOrDefault(g => g.Id == targetAction.ActionGroupId);
                var newIndex = group?.Actions.OrderBy(a => a.SortOrder).ToList().FindIndex(a => a.Id == targetAction.Id) ?? 0;
                
                await actionService.UpdateActionOrderAsync(draggedAction.Id, targetAction.ActionGroupId, newIndex);
                break;
            }

            // Case 2: An Action is dropped onto a Group Header (string)
            case { dragged: Action draggedAction, target: string targetGroupName }:
            {
                var targetGroup = _actionGroupsList.FirstOrDefault(g => g.Name == targetGroupName);
                if (targetGroup is null || draggedAction.ActionGroupId == targetGroup.Id) return;

                // Move to the top of the new group
                await actionService.UpdateActionOrderAsync(draggedAction.Id, targetGroup.Id, 0);
                break;
            }

            // Case 3: A Group Header (string) is dropped onto another Group Header (string)
            case { dragged: string draggedGroupName, target: string targetGroupName }:
            {
                var orderedGroups = new ObservableCollection<ActionGroup>(_actionGroupsList);
                var draggedGroup = orderedGroups.FirstOrDefault(g => g.Name == draggedGroupName);
                var targetGroup = orderedGroups.FirstOrDefault(g => g.Name == targetGroupName);

                if (draggedGroup is null || targetGroup is null) return;

                var oldIndex = orderedGroups.IndexOf(draggedGroup);
                var newIndex = orderedGroups.IndexOf(targetGroup);
                
                if (oldIndex == -1 || newIndex == -1) return;
                
                orderedGroups.Move(oldIndex, newIndex);

                await actionService.UpdateActionGroupOrderAsync(orderedGroups.ToList());
                break;
            }
        }
        
        await LoadDataAsync();
    }
    
    [RelayCommand]
    private async Task ImportActionsAsync()
    {
        var filePath = await dialogService.ShowOpenFileDialogAsync("Import Actions", "JSON files", "*.json");
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            await actionService.ImportActionsFromJsonAsync(filePath);
            await LoadDataAsync();
            AppEvents.RequestNotification("Actions imported successfully.", NotificationType.Success);
        }
        catch (Exception)
        {
            AppEvents.RequestNotification("Failed to import actions.", NotificationType.Error);
        }
    }

    [RelayCommand]
    private async Task ExportActionsAsync()
    {
        var filePath =
            await dialogService.ShowSaveFileDialogAsync("Export Actions", "proseflow_actions.json", "JSON files",
                "*.json");
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            await actionService.ExportActionsToJsonAsync(filePath);
            AppEvents.RequestNotification("Actions exported successfully.", NotificationType.Success);
        }
        catch (Exception)
        {
            AppEvents.RequestNotification("Failed to export actions.", NotificationType.Error);
        }
    }
}