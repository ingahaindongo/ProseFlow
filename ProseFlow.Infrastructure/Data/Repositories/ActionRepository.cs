using Microsoft.EntityFrameworkCore;
using ProseFlow.Core.Interfaces.Repositories;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.Infrastructure.Data.Repositories;

public class ActionRepository(AppDbContext context) : Repository<Action>(context), IActionRepository
{
    /// <inheritdoc />
    public async Task<List<Action>> GetAllOrderedAsync()
    {
        return await Context.Actions.OrderBy(a => a.SortOrder).Include(a => a.ActionGroup).ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<int> GetMaxSortOrderAsync()
    {
        return await Context.Actions.AnyAsync()
            ? await Context.Actions.MaxAsync(a => a.SortOrder)
            : 0;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetAllNamesAsync()
    {
        return await Context.Actions.Select(a => a.Name).ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task UpdateOrderAsync(List<Action> orderedActions)
    {
        for (var i = 0; i < orderedActions.Count; i++)
        {
            var actionToUpdate = await Context.Actions.FindAsync(orderedActions[i].Id);
            if (actionToUpdate != null) actionToUpdate.SortOrder = i;
        }
    }
}