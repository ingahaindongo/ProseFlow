using Microsoft.EntityFrameworkCore;
using ProseFlow.Core.Interfaces.Repositories;
using ProseFlow.Core.Models;

namespace ProseFlow.Infrastructure.Data.Repositories;

public class HistoryRepository(AppDbContext context) : Repository<HistoryEntry>(context), IHistoryRepository
{
    /// <inheritdoc />
    public async Task<List<HistoryEntry>> GetAllOrderedByTimestampAsync(string? searchTerm = null, string? filterType = null)
    {
        var query = Context.History.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = filterType switch
            {
                "Action Name" => query.Where(h => h.ActionName.Contains(searchTerm)),
                "Input" => query.Where(h => h.InputText.Contains(searchTerm)),
                "Output" => query.Where(h => h.OutputText.Contains(searchTerm)),
                // Default to "All"
                _ => query.Where(h =>
                    h.ActionName.Contains(searchTerm) ||
                    h.InputText.Contains(searchTerm) ||
                    h.OutputText.Contains(searchTerm))
            };
        }
        
        return await query.OrderByDescending(h => h.Timestamp).ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<List<HistoryEntry>> GetRecentAsync(int count)
    {
        return await Context.History
            .OrderByDescending(h => h.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<HistoryEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await Context.History
            .Where(h => h.Timestamp >= startDate && h.Timestamp <= endDate)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task ClearAllAsync()
    {
        await Context.History.ExecuteDeleteAsync();
    }
}