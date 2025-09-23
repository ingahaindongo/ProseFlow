using ProseFlow.Core.Models;

namespace ProseFlow.Core.Interfaces.Repositories;

public interface IHistoryRepository : IRepository<HistoryEntry>
{
    Task<List<HistoryEntry>> GetAllOrderedByTimestampAsync();
    Task<List<HistoryEntry>> GetRecentAsync(int count);
    Task<List<HistoryEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task ClearAllAsync();
}