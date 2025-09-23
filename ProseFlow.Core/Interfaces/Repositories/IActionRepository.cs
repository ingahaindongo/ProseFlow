using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.Core.Interfaces.Repositories;

public interface IActionRepository : IRepository<Action>
{
    Task<List<Action>> GetAllOrderedAsync();
    Task<int> GetMaxSortOrderAsync();
    Task<List<string>> GetAllNamesAsync();
    Task UpdateOrderAsync(List<Action> orderedActions);
}