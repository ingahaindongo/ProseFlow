using Microsoft.EntityFrameworkCore;
using ProseFlow.Core.Interfaces.Repositories;
using ProseFlow.Core.Models;
using ProseFlow.Infrastructure.Security;

namespace ProseFlow.Infrastructure.Data.Repositories;

/// <summary>
/// Implements the repository for CloudProviderConfiguration, handling data-specific logic
/// like encryption/decryption and sort order management.
/// </summary>
public class CloudProviderConfigurationRepository(AppDbContext context, ApiKeyProtector protector)
    : Repository<CloudProviderConfiguration>(context), ICloudProviderConfigurationRepository
{
    /// <inheritdoc />
    public async Task<List<CloudProviderConfiguration>> GetAllOrderedAsync()
    {
        var configs = await Context.CloudProviderConfigurations.OrderBy(c => c.SortOrder).ToListAsync();
        
        // Decrypt keys for UI/API usage after retrieval
        foreach (var config in configs)
        {
            if (string.IsNullOrWhiteSpace(config.ApiKey)) continue;
            try
            {
                config.ApiKey = protector.Unprotect(config.ApiKey);
            }
            catch
            {
                // If decryption fails, the key is considered invalid and presented as empty.
                config.ApiKey = string.Empty; 
            }
        }
        
        return configs;
    }

    /// <inheritdoc />
    public async Task UpdateOrderAsync(List<CloudProviderConfiguration> orderedConfigs)
    {
        for (var i = 0; i < orderedConfigs.Count; i++)
        {
            var configToUpdate = await Context.CloudProviderConfigurations.FindAsync(orderedConfigs[i].Id);
            if (configToUpdate != null) 
                configToUpdate.SortOrder = i;
        }
    }
    
    /// <summary>
    /// Overrides the base AddAsync to inject sort order calculation and API key encryption.
    /// </summary>
    public new async Task AddAsync(CloudProviderConfiguration entity)
    {
        // Calculate the next sort order before adding.
        var maxSortOrder = await Context.CloudProviderConfigurations.AnyAsync()
            ? await Context.CloudProviderConfigurations.MaxAsync(c => c.SortOrder)
            : 0;
        entity.SortOrder = maxSortOrder + 1;

        // Encrypt the API key.
        if (!string.IsNullOrWhiteSpace(entity.ApiKey)) 
            entity.ApiKey = protector.Protect(entity.ApiKey);

        // Call the base method to add the entity to the context.
        await base.AddAsync(entity);
    }
    
    /// <summary>
    /// Overrides the base Update to inject API key encryption.
    /// </summary>
    public new void Update(CloudProviderConfiguration entity)
    {
        // Encrypt the API key before marking the entity as modified.
        if (!string.IsNullOrWhiteSpace(entity.ApiKey)) 
            entity.ApiKey = protector.Protect(entity.ApiKey);

        // Call the base method to update the entity in the context.
        base.Update(entity);
    }
}