using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;

namespace ProseFlow.Application.Services;

/// <summary>
/// Manages CRUD operations for cloud provider configurations by coordinating with the repository.
/// </summary>
public class CloudProviderManagementService(IServiceScopeFactory scopeFactory, ILogger<CloudProviderManagementService> logger)
{
    /// <summary>
    /// Gets all ordered cloud provider configurations. Decryption is handled by the repository.
    /// </summary>
    public Task<List<CloudProviderConfiguration>> GetConfigurationsAsync()
    {
        return ExecuteQueryAsync(unitOfWork => unitOfWork.CloudProviderConfigurations.GetAllOrderedAsync());
    }

    /// <summary>
    /// Updates a configuration. Encryption is handled by the repository.
    /// </summary>
    public Task UpdateConfigurationAsync(CloudProviderConfiguration config)
    {
        return ExecuteCommandAsync(async unitOfWork =>
        {
            var trackedConfig = await unitOfWork.CloudProviderConfigurations.GetByIdAsync(config.Id);
            if (trackedConfig is null)
            {
                logger.LogWarning("Attempted to update non-existent provider configuration with ID {ConfigId}.", config.Id);
                return;
            }

            trackedConfig.Name = config.Name;
            trackedConfig.Model = config.Model;
            trackedConfig.ApiKey = config.ApiKey;
            trackedConfig.Temperature = config.Temperature;
            trackedConfig.IsEnabled = config.IsEnabled;
            trackedConfig.BaseUrl = config.BaseUrl;
            trackedConfig.ProviderType = config.ProviderType;

            // The repository will handle encrypting the API key before updating.
            unitOfWork.CloudProviderConfigurations.Update(trackedConfig);
        });
    }

    /// <summary>
    /// Creates a new configuration. Sort order calculation and encryption are handled by the repository.
    /// </summary>
    public Task CreateConfigurationAsync(CloudProviderConfiguration config)
    {
        return ExecuteCommandAsync(unitOfWork => unitOfWork.CloudProviderConfigurations.AddAsync(config));
    }

    /// <summary>
    /// Deletes a configuration by its ID.
    /// </summary>
    public Task DeleteConfigurationAsync(int configId)
    {
        return ExecuteCommandAsync(async unitOfWork =>
        {
            var config = await unitOfWork.CloudProviderConfigurations.GetByIdAsync(configId);
            if (config is not null) unitOfWork.CloudProviderConfigurations.Delete(config);
        });
    }

    /// <summary>
    /// Updates the sort order of all configurations.
    /// </summary>
    public Task UpdateConfigurationOrderAsync(List<CloudProviderConfiguration> orderedConfigs)
    {
        return ExecuteCommandAsync(unitOfWork => unitOfWork.CloudProviderConfigurations.UpdateOrderAsync(orderedConfigs));
    }
    
    #region Private Helpers

    /// <summary>
    /// Creates a UoW scope, executes a command, and saves changes.
    /// </summary>
    private async Task ExecuteCommandAsync(Func<IUnitOfWork, Task> command)
    {
        using var scope = scopeFactory.CreateScope();
        await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await command(unitOfWork);
        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a UoW scope and executes a query.
    /// </summary>
    private async Task<T> ExecuteQueryAsync<T>(Func<IUnitOfWork, Task<T>> query)
    {
        using var scope = scopeFactory.CreateScope();
        await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        return await query(unitOfWork);
    }

    #endregion
}