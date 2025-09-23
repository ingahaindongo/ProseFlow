using Microsoft.EntityFrameworkCore;
using ProseFlow.Core.Interfaces.Repositories;
using ProseFlow.Core.Models;

namespace ProseFlow.Infrastructure.Data.Repositories;

public class UsageStatisticRepository(AppDbContext context) : Repository<UsageStatistic>(context), IUsageStatisticRepository
{
    /// <inheritdoc />
    public async Task<UsageStatistic?> GetByDateAsync(int year, int month)
    {
        return await Context.UsageStatistics
            .FirstOrDefaultAsync(u => u.Year == year && u.Month == month);
    }
}