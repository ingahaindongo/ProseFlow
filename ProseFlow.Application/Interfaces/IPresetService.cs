using ProseFlow.Application.DTOs;

namespace ProseFlow.Application.Interfaces;

public interface IPresetService
{
    Task<List<PresetDto>> GetAvailablePresetsAsync();
    Task ImportPresetAsync(string resourcePath);
    Task<HashSet<string>> GetActionNamesFromPresetAsync(string resourcePath);
}