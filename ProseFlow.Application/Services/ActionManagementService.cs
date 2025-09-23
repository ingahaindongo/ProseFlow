using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.DTOs;
using ProseFlow.Application.Events;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.Application.Services;

/// <summary>
/// Manages CRUD operations and business logic for Actions and ActionGroups.
/// </summary>
public class ActionManagementService(IServiceScopeFactory scopeFactory, ILogger<ActionManagementService> logger)
{
    #region ActionGroup Management

    public Task<List<ActionGroup>> GetActionGroupsWithActionsAsync()
    {
        return ExecuteQueryAsync(unitOfWork => unitOfWork.ActionGroups.GetAllOrderedWithActionsAsync());
    }

    public Task<List<ActionGroup>> GetActionGroupsAsync()
    {
        return ExecuteQueryAsync(unitOfWork => unitOfWork.ActionGroups.GetAllOrderedAsync());
    }

    public Task CreateActionGroupAsync(ActionGroup group)
    {
        return ExecuteCommandAsync(async unitOfWork =>
        {
            var maxSortOrder = await unitOfWork.ActionGroups.GetMaxSortOrderAsync();
            group.SortOrder = maxSortOrder + 1;
            await unitOfWork.ActionGroups.AddAsync(group);
        });
    }

    public Task UpdateActionGroupAsync(ActionGroup group)
    {
        return ExecuteCommandAsync(unitOfWork =>
        {
            unitOfWork.ActionGroups.Update(group);
            return Task.CompletedTask;
        });
    }

    public Task DeleteActionGroupAsync(int groupId)
    {
        return ExecuteCommandAsync(async unitOfWork =>
        {
            // The default group (ID=1) cannot be deleted.
            if (groupId == 1)
            {
                logger.LogWarning("Attempted to delete the default 'General' group.");
                return;
            }

            var groupToDelete = await unitOfWork.ActionGroups.GetByIdAsync(groupId);
            if (groupToDelete is null) return;

            var defaultGroup = await unitOfWork.ActionGroups.GetDefaultGroupAsync() ??
                               throw new InvalidOperationException("Default 'General' group not found.");

            // Re-parent all actions from the deleted group to the default group.
            var actionsToMove = await unitOfWork.Actions.GetByExpressionAsync(a => a.ActionGroupId == groupId);
            foreach (var action in actionsToMove)
            {
                action.ActionGroupId = defaultGroup.Id;
                unitOfWork.Actions.Update(action);
            }

            unitOfWork.ActionGroups.Delete(groupToDelete);
        });
    }

    public Task UpdateActionGroupOrderAsync(List<ActionGroup> orderedGroups)
    {
        return ExecuteCommandAsync(unitOfWork => unitOfWork.ActionGroups.UpdateOrderAsync(orderedGroups));
    }

    #endregion

    #region Action Management

    public Task<List<Action>> GetActionsAsync()
    {
        return ExecuteQueryAsync(unitOfWork => unitOfWork.Actions.GetAllOrderedAsync());
    }

    public Task UpdateActionAsync(Action action)
    {
        return ExecuteCommandAsync(unitOfWork =>
        {
            unitOfWork.Actions.Update(action);
            return Task.CompletedTask;
        });
    }

    public Task CreateActionAsync(Action action)
    {
        return ExecuteCommandAsync(async unitOfWork =>
        {
            // If no group is assigned, add it to the default group.
            if (action.ActionGroupId == 0)
            {
                var defaultGroup = await unitOfWork.ActionGroups.GetDefaultGroupAsync() ??
                                   throw new InvalidOperationException("Default 'General' group not found.");
                action.ActionGroupId = defaultGroup.Id;
            }

            var maxSortOrder = await unitOfWork.Actions.GetMaxSortOrderAsync();
            action.SortOrder = maxSortOrder + 1;

            await unitOfWork.Actions.AddAsync(action);
        });
    }

    public Task DeleteActionAsync(int actionId)
    {
        return ExecuteCommandAsync(async unitOfWork =>
        {
            var action = await unitOfWork.Actions.GetByIdAsync(actionId);
            if (action is not null) unitOfWork.Actions.Delete(action);
        });
    }

    public Task UpdateActionOrderAsync(int actionId, int targetGroupId, int newIndex)
    {
        return ExecuteCommandAsync(async unitOfWork =>
        {
            var actionToMove = await unitOfWork.Actions.GetByIdAsync(actionId) ??
                               throw new InvalidOperationException("Action not found.");
            var originalGroupId = actionToMove.ActionGroupId;

            // Get all actions from the target group
            var targetGroupActions = (await unitOfWork.Actions.GetByExpressionAsync(a => a.ActionGroupId == targetGroupId))
                .OrderBy(a => a.SortOrder).ToList();

            // If moving within the same group, remove the item first to get the correct index
            if (originalGroupId == targetGroupId) targetGroupActions.RemoveAll(a => a.Id == actionId);

            // Insert at the new position and update the ActionGroupId
            actionToMove.ActionGroupId = targetGroupId;
            targetGroupActions.Insert(Math.Clamp(newIndex, 0, targetGroupActions.Count), actionToMove);

            // Re-number the sort order for the entire target group
            for (var i = 0; i < targetGroupActions.Count; i++)
            {
                targetGroupActions[i].SortOrder = i;
                unitOfWork.Actions.Update(targetGroupActions[i]);
            }

            // If moved from a different group, re-number the source group as well
            if (originalGroupId != targetGroupId)
            {
                var sourceGroupActions =
                    (await unitOfWork.Actions.GetByExpressionAsync(a => a.ActionGroupId == originalGroupId))
                    .OrderBy(a => a.SortOrder).ToList();
                for (var i = 0; i < sourceGroupActions.Count; i++)
                {
                    sourceGroupActions[i].SortOrder = i;
                    unitOfWork.Actions.Update(sourceGroupActions[i]);
                }
            }
        });
    }

    #endregion

    #region Import/Export

    public async Task ExportActionsToJsonAsync(string filePath)
    {
        var groupsWithActions = await GetActionGroupsWithActionsAsync();

        var exportData = groupsWithActions.ToDictionary(
            g => g.Name,
            g => g.Actions.ToDictionary(
                a => a.Name,
                a => new ActionDto
                {
                    Prefix = a.Prefix,
                    Instruction = a.Instruction,
                    Icon = a.Icon,
                    OpenInWindow = a.OpenInWindow,
                    ExplainChanges = a.ExplainChanges,
                    ApplicationContext = a.ApplicationContext
                }));

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(exportData, jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task ImportActionsFromJsonAsync(string filePath)
    {
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        await ImportActionsFromJsonStreamAsync(fileStream);
    }
    
    public async Task ImportActionsFromJsonStreamAsync(Stream jsonStream)
    {
        var importedGroups = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, ActionDto>>>(jsonStream);

        if (importedGroups is null)
        {
            AppEvents.RequestNotification("Invalid or empty import file.", NotificationType.Error);
            return;
        }

        // Use a single UnitOfWork for the entire import operation for performance and transactional integrity.
        using var scope = scopeFactory.CreateScope();
        await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var existingActions = await unitOfWork.Actions.GetAllAsync();
        var existingActionNames = new HashSet<string>(existingActions.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);

        var conflicts = new List<ActionConflict>();
        var nonConflicts = new List<(string GroupName, string ActionName, ActionDto Dto)>();

        // Detect Conflicts
        foreach (var (groupName, actions) in importedGroups)
        foreach (var (actionName, dto) in actions)
        {
            var existingAction = existingActions.FirstOrDefault(a => a.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase));
            if (existingAction is not null)
                conflicts.Add(new ActionConflict(existingAction, dto));
            else
                nonConflicts.Add((groupName, actionName, dto));
        }

        // Request User Resolution for Conflicts
        List<ActionConflict>? resolvedConflicts = null;
        if (conflicts.Count > 0)
        {
            resolvedConflicts = await AppEvents.RequestConflictResolutionAsync(conflicts);
            if (resolvedConflicts is null)
            {
                AppEvents.RequestNotification("Import canceled by user.", NotificationType.Info);
                return; // User canceled the dialog
            }
        }

        // Apply changes in a single transaction
        var existingGroups = await unitOfWork.ActionGroups.GetAllAsync();
        var maxActionSortOrder = await unitOfWork.Actions.GetMaxSortOrderAsync();
        var maxGroupSortOrder = await unitOfWork.ActionGroups.GetMaxSortOrderAsync();
        
        // Process resolved conflicts
        if (resolvedConflicts is not null)
        {
            foreach (var conflict in resolvedConflicts)
            {
                switch (conflict.Resolution)
                {
                    case ConflictResolutionType.Skip:
                        continue; // Do nothing
                    
                    case ConflictResolutionType.Overwrite:
                        var actionToUpdate = await unitOfWork.Actions.GetByIdAsync(conflict.ExistingAction.Id) ?? throw new InvalidOperationException("Action to overwrite not found during import.");
                        actionToUpdate.Prefix = conflict.ImportedActionDto.Prefix;
                        actionToUpdate.Instruction = conflict.ImportedActionDto.Instruction;
                        actionToUpdate.Icon = conflict.ImportedActionDto.Icon;
                        actionToUpdate.OpenInWindow = conflict.ImportedActionDto.OpenInWindow;
                        actionToUpdate.ExplainChanges = conflict.ImportedActionDto.ExplainChanges;
                        actionToUpdate.ApplicationContext = conflict.ImportedActionDto.ApplicationContext;
                        unitOfWork.Actions.Update(actionToUpdate);
                        break;
                        
                    case ConflictResolutionType.Rename:
                        var newName = GetUniqueActionName(conflict.ExistingAction.Name, existingActionNames);
                        existingActionNames.Add(newName); // Add to the set to handle multiple renames of the same original name
                        var groupName = importedGroups.FirstOrDefault(g => g.Value.ContainsKey(conflict.ExistingAction.Name)).Key;
                        await CreateNewActionAsync(unitOfWork, newName, groupName, conflict.ImportedActionDto, existingGroups, ++maxActionSortOrder, ++maxGroupSortOrder);
                        break;
                }
            }
        }
        
        // Process non-conflicting actions
        foreach (var (groupName, actionName, dto) in nonConflicts)
        {
            await CreateNewActionAsync(unitOfWork, actionName, groupName, dto, existingGroups, ++maxActionSortOrder, ++maxGroupSortOrder);
        }

        await unitOfWork.SaveChangesAsync();
        AppEvents.RequestNotification("Actions imported successfully.", NotificationType.Success);
    }

    #endregion
    
    #region Private Helpers

    private static string GetUniqueActionName(string originalName, ICollection<string> existingNames)
    {
        var newName = originalName;
        var suffix = 1;
        while (existingNames.Contains(newName, StringComparer.OrdinalIgnoreCase))
        {
            newName = $"{originalName} ({suffix++})";
        }
        return newName;
    }

    private static async Task CreateNewActionAsync(IUnitOfWork unitOfWork, string actionName, string? groupName, ActionDto dto, ICollection<ActionGroup> existingGroups, int actionSortOrder, int groupSortOrder)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            groupName = "General"; // Fallback to default group if group name is missing
        }

        var group = existingGroups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            group = new ActionGroup { Name = groupName, SortOrder = groupSortOrder };
            await unitOfWork.ActionGroups.AddAsync(group);
            existingGroups.Add(group);
        }

        var newAction = new Action
        {
            Name = actionName,
            Prefix = dto.Prefix,
            Instruction = dto.Instruction,
            Icon = dto.Icon,
            OpenInWindow = dto.OpenInWindow,
            ExplainChanges = dto.ExplainChanges,
            ApplicationContext = dto.ApplicationContext,
            ActionGroupId = group.Id,
            ActionGroup = group,
            SortOrder = actionSortOrder
        };

        await unitOfWork.Actions.AddAsync(newAction);
    }
    
    /// <summary>
    /// Creates a UoW scope, executes a command, and saves changes.
    /// </summary>
    private async Task ExecuteCommandAsync(Func<IUnitOfWork, Task> command)
    {
        using var scope = scopeFactory.CreateScope();
        await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await command(unitOfWork);
        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a UoW scope and executes a query.
    /// </summary>
    private async Task<T> ExecuteQueryAsync<T>(Func<IUnitOfWork, Task<T>> query)
    {
        using var scope = scopeFactory.CreateScope();
        await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        return await query(unitOfWork);
    }

    #endregion
}