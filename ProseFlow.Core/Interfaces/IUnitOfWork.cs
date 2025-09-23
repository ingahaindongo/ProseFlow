using ProseFlow.Core.Interfaces.Repositories;

namespace ProseFlow.Core.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IActionRepository Actions { get; }
    IActionGroupRepository ActionGroups { get; }
    ICloudProviderConfigurationRepository CloudProviderConfigurations { get; }
    IHistoryRepository History { get; }
    ISettingsRepository Settings { get; }
    IUsageStatisticRepository UsageStatistics { get; }
    ILocalModelRepository LocalModels { get; }


    /// <summary>
    /// Saves all changes made in this unit of work to the underlying database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}