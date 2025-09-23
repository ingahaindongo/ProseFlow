using Microsoft.Extensions.Logging;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;

namespace ProseFlow.Application.Services;

/// <summary>
/// Manages loading and saving of global application settings.
/// This service handles the GeneralSettings and the local ProviderSettings entities.
/// </summary>
public class SettingsService(IUnitOfWork unitOfWork, ILogger<SettingsService> logger)
{
    public async Task<GeneralSettings> GetGeneralSettingsAsync()
    {
        var generalSettings = await unitOfWork.Settings.GetGeneralSettingsAsync();
        if (generalSettings == null)
        {
            logger.LogCritical("General settings record not found in the database. Ensure it is seeded.");
            throw new InvalidOperationException(
                "General settings record not found in the database. Ensure it is seeded.");
        }

        return generalSettings;
    }

    public async Task SaveGeneralSettingsAsync(GeneralSettings settings)
    {
        await unitOfWork.Settings.UpdateGeneralSettingsAsync(settings);
        await unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the top-level provider settings, which include local model configuration
    /// and the primary/fallback service type choice.
    /// </summary>
    /// <returns>The ProviderSettings entity.</returns>
    public async Task<ProviderSettings> GetProviderSettingsAsync()
    {
        var providerSettings = await unitOfWork.Settings.GetProviderSettingsAsync();
        if (providerSettings == null)
        {
            logger.LogCritical("Provider settings record not found in the database. Ensure it is seeded.");
            throw new InvalidOperationException(
                "Provider settings record not found in the database. Ensure it is seeded.");
        }

        return providerSettings;
    }

    /// <summary>
    /// Saves the top-level provider settings.
    /// </summary>
    /// <param name="settings">The ProviderSettings entity to save.</param>
    public async Task SaveProviderSettingsAsync(ProviderSettings settings)
    {
        await unitOfWork.Settings.UpdateProviderSettingsAsync(settings);
        await unitOfWork.SaveChangesAsync();
    }
}