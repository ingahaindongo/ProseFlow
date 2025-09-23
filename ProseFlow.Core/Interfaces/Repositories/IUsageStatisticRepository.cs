using ProseFlow.Core.Models;

namespace ProseFlow.Core.Interfaces.Repositories;

public interface IUsageStatisticRepository : IRepository<UsageStatistic>
{
    Task<UsageStatistic?> GetByDateAsync(int year, int month);
}