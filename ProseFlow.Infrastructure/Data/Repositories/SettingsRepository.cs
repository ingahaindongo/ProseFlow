using ProseFlow.Core.Interfaces.Repositories;
using ProseFlow.Core.Models;

namespace ProseFlow.Infrastructure.Data.Repositories;

public class SettingsRepository(AppDbContext context) : ISettingsRepository
{
    /// <inheritdoc />
    public async Task<GeneralSettings> GetGeneralSettingsAsync()
    {
        // Find the singleton record with ID=1 or throw if the database is not seeded correctly.
        return await context.GeneralSettings.FindAsync(1)
               ?? throw new InvalidOperationException("General settings record not found in the database. Ensure it is seeded.");
    }

    /// <inheritdoc />
    public Task UpdateGeneralSettingsAsync(GeneralSettings settings)
    {
        // Ensure the ID is always 1 for the singleton record.
        settings.Id = 1; 
        context.GeneralSettings.Update(settings);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<ProviderSettings> GetProviderSettingsAsync()
    {
        return await context.ProviderSettings.FindAsync(1)
               ?? throw new InvalidOperationException("Provider settings record not found in the database. Ensure it is seeded.");
    }

    /// <inheritdoc />
    public Task UpdateProviderSettingsAsync(ProviderSettings settings)
    {
        settings.Id = 1;
        context.ProviderSettings.Update(settings);
        return Task.CompletedTask;
    }
}