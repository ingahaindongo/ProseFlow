using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Interfaces.Repositories;
using ProseFlow.Infrastructure.Data.Repositories;
using ProseFlow.Infrastructure.Security;

namespace ProseFlow.Infrastructure.Data;

public class UnitOfWork(AppDbContext context, ApiKeyProtector keyProtector) : IUnitOfWork
{
    private IActionRepository? _actions;
    private IActionGroupRepository? _actionGroups;
    private ICloudProviderConfigurationRepository? _cloudConfigs;
    private IHistoryRepository? _history;
    private ISettingsRepository? _settings;
    private IUsageStatisticRepository? _usageStatistics;
    private ILocalModelRepository? _localModels;

    public IActionRepository Actions => _actions ??= new ActionRepository(context);
    public IActionGroupRepository ActionGroups => _actionGroups ??= new ActionGroupRepository(context);
    public ICloudProviderConfigurationRepository CloudProviderConfigurations => _cloudConfigs ??= new CloudProviderConfigurationRepository(context, keyProtector);
    public IHistoryRepository History => _history ??= new HistoryRepository(context);
    public ISettingsRepository Settings => _settings ??= new SettingsRepository(context);
    public IUsageStatisticRepository UsageStatistics => _usageStatistics ??= new UsageStatisticRepository(context);
    public ILocalModelRepository LocalModels => _localModels ??= new LocalModelRepository(context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
    
    public void Dispose()
    {
        context.Dispose();
        GC.SuppressFinalize(this);
    }
}