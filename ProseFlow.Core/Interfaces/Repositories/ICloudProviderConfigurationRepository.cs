using ProseFlow.Core.Models;

namespace ProseFlow.Core.Interfaces.Repositories;

public interface ICloudProviderConfigurationRepository : IRepository<CloudProviderConfiguration>
{
    Task<List<CloudProviderConfiguration>> GetAllOrderedAsync();
    Task UpdateOrderAsync(List<CloudProviderConfiguration> orderedConfigs);
}