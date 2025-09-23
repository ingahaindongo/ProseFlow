using ProseFlow.Core.Models;

namespace ProseFlow.Core.Interfaces.Repositories;

public interface ISettingsRepository
{
    Task<GeneralSettings> GetGeneralSettingsAsync();
    Task UpdateGeneralSettingsAsync(GeneralSettings settings);
    Task<ProviderSettings> GetProviderSettingsAsync();
    Task UpdateProviderSettingsAsync(ProviderSettings settings);
}