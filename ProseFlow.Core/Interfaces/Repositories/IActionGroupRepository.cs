using ProseFlow.Core.Models;

namespace ProseFlow.Core.Interfaces.Repositories;

public interface IActionGroupRepository : IRepository<ActionGroup>
{
    Task<List<ActionGroup>> GetAllOrderedAsync();
    Task<List<ActionGroup>> GetAllOrderedWithActionsAsync();
    Task<int> GetMaxSortOrderAsync();
    Task UpdateOrderAsync(List<ActionGroup> orderedGroups);
    Task<ActionGroup?> GetDefaultGroupAsync();
}