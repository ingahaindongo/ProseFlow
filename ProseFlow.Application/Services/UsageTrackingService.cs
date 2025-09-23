using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;

namespace ProseFlow.Application.Services;

/// <summary>
/// Manages reading, writing, and updating token usage statistics.
/// </summary>
public class UsageTrackingService(
    IServiceScopeFactory scopeFactory,
    ILogger<UsageTrackingService> logger) : IDisposable
{
    private readonly SemaphoreSlim _usageLock = new(1, 1);
    private UsageStatistic _currentMonthUsage = new();

    /// <summary>
    /// Initializes the service by loading the current month's usage statistics from the database.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _usageLock.WaitAsync();
        try
        {
            _currentMonthUsage = await GetOrCreateCurrentUsageStatisticAsync();
        }
        finally
        {
            _usageLock.Release();
        }
    }

    /// <summary>
    /// Gets a snapshot of the cached, in-memory usage statistics for the current month.
    /// Returns a copy to prevent modification of the internal state.
    /// </summary>
    public UsageStatistic GetCurrentUsage()
    {
        return new UsageStatistic
        {
            Id = _currentMonthUsage.Id,
            Year = _currentMonthUsage.Year,
            Month = _currentMonthUsage.Month,
            PromptTokens = _currentMonthUsage.PromptTokens,
            CompletionTokens = _currentMonthUsage.CompletionTokens
        };
    }

    /// <summary>
    /// Adds new token usage to the in-memory cache and persists the change to the database.
    /// </summary>
    public async Task AddUsageAsync(long promptTokens, long completionTokens)
    {
        await _usageLock.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            if (now.Year != _currentMonthUsage.Year || now.Month != _currentMonthUsage.Month)
                _currentMonthUsage = await GetOrCreateCurrentUsageStatisticAsync();

            _currentMonthUsage.PromptTokens += promptTokens;
            _currentMonthUsage.CompletionTokens += completionTokens;

            using var scope = scopeFactory.CreateScope();
            await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            try
            {
                // The _currentMonthUsage object is detached; Update() re-attaches it.
                unitOfWork.UsageStatistics.Update(_currentMonthUsage);
                await unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to save usage data to database. In-memory values were updated but may be out of sync.");
            }
        }
        finally
        {
            _usageLock.Release();
        }
    }

    /// <summary>
    /// Resets the current month's usage counters to zero, both in-memory and in the database.
    /// This operation is thread-safe.
    /// </summary>
    public async Task ResetUsageAsync()
    {
        await _usageLock.WaitAsync();
        try
        {
            _currentMonthUsage.PromptTokens = 0;
            _currentMonthUsage.CompletionTokens = 0;

            using var scope = scopeFactory.CreateScope();
            await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            unitOfWork.UsageStatistics.Update(_currentMonthUsage);
            await unitOfWork.SaveChangesAsync();
        }
        finally
        {
            _usageLock.Release();
        }
    }

    /// <summary>
    /// Disposes the semaphore.
    /// </summary>
    public void Dispose()
    {
        _usageLock.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Retrieves the usage statistic for the current month from the database,
    /// creating a new record if one doesn't exist.
    /// </summary>
    private async Task<UsageStatistic> GetOrCreateCurrentUsageStatisticAsync()
    {
        // This method performs a complete, isolated transaction (read and potential write).
        using var scope = scopeFactory.CreateScope();
        await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;
        var usage = await unitOfWork.UsageStatistics.GetByDateAsync(now.Year, now.Month);
        if (usage is not null)
            return usage;

        // If no record exists for the current month, create one.
        logger.LogInformation("No usage record for {Month}/{Year}. Creating a new one.", now.Month, now.Year);
        var newUsage = new UsageStatistic { Year = now.Year, Month = now.Month };

        await unitOfWork.UsageStatistics.AddAsync(newUsage);
        await unitOfWork.SaveChangesAsync();

        return newUsage;
    }
}