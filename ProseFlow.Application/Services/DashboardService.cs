using ProseFlow.Application.DTOs.Dashboard;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;

namespace ProseFlow.Application.Services;

/// <summary>
/// Provides aggregated data and statistics for the application dashboard.
/// </summary>
public class DashboardService(IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Gets a summary of token usage per day for a given date range, optionally filtered by provider type.
    /// </summary>
    public async Task<List<DailyUsageDto>> GetDailyUsageAsync(DateTime startDate, DateTime endDate, string? providerType = null)
    {
        var historyEntries = await GetHistoryByDateRangeAsync(startDate, endDate, providerType);

        return historyEntries
            .GroupBy(e => DateOnly.FromDateTime(e.Timestamp))
            .Select(g => new DailyUsageDto(
                g.Key,
                g.Sum(e => e.PromptTokens),
                g.Sum(e => e.CompletionTokens),
                g.Average(e => e.TokensPerSecond)
            ))
            .OrderBy(d => d.Date)
            .ToList();
    }

    /// <summary>
    /// Gets a summary of the most frequently used actions for a given date range, optionally filtered by provider type.
    /// </summary>
    public async Task<List<ActionUsageDto>> GetTopActionsAsync(DateTime startDate, DateTime endDate, string? providerType = null, int count = 5)
    {
        var historyEntries = await GetHistoryByDateRangeAsync(startDate, endDate, providerType);

        return historyEntries
            .GroupBy(e => e.ActionName)
            .Select(g => new ActionUsageDto(
                g.Key,
                g.Count(),
                g.Average(e => e.PromptTokens + e.CompletionTokens)
            ))
            .OrderByDescending(a => a.UsageCount)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Gets the total number of actions executed within a date range, optionally filtered by provider type.
    /// </summary>
    public async Task<int> GetTotalUsageCountAsync(DateTime startDate, DateTime endDate, string? providerType = null)
    {
        var historyEntries = await GetHistoryByDateRangeAsync(startDate, endDate, providerType);
        return historyEntries.Count;
    }

    /// <summary>
    /// Gets history entries for a given date range, optionally filtered by provider type.
    /// </summary>
    public async Task<List<HistoryEntry>> GetHistoryByDateRangeAsync(DateTime startDate, DateTime endDate, string? providerType = null)
    {
        var entries = await unitOfWork.History.GetByDateRangeAsync(startDate, endDate);
        return string.IsNullOrWhiteSpace(providerType)
            ? entries
            : entries.Where(e => e.ProviderUsed.Equals(providerType, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Gets cloud provider performance for a given date range.
    /// </summary>
    public async Task<List<ProviderPerformanceDto>> GetCloudProviderPerformanceAsync(DateTime startDate, DateTime endDate)
    {
        var cloudEntries = await GetHistoryByDateRangeAsync(startDate, endDate, "Cloud");

        // We group by the specific model used to get performance stats per model.
        return cloudEntries
            .GroupBy(e => e.ModelUsed)
            .Select(g =>
            {
                var entry = g.First();
                return new ProviderPerformanceDto(
                    entry.ModelUsed, // Using ModelUsed as the primary name here
                    entry.ModelUsed,
                    g.Count(),
                    g.Average(e => e.LatencyMs)
                );
            })
            .Where(p => p.UsageCount > 0)
            .OrderByDescending(p => p.UsageCount)
            .ToList();
    }
}