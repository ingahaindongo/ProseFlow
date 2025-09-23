using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.DTOs;
using ProseFlow.Application.Interfaces;

namespace ProseFlow.Application.Services;

/// <summary>
/// Manages loading and importing of built-in action presets from embedded resources.
/// </summary>
public class PresetService(ActionManagementService actionService, ILogger<PresetService> logger) : IPresetService
{
    private const string ManifestResourcePath = "ProseFlow.UI.Assets.Presets.presets-manifest.json";

    public async Task<List<PresetDto>> GetAvailablePresetsAsync()
    {
        try
        {
            var assembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Could not get entry assembly.");
            await using var stream = assembly.GetManifestResourceStream(ManifestResourcePath);
            if (stream is null)
            {
                logger.LogError("Preset manifest resource not found at path: {Path}", ManifestResourcePath);
                return [];
            }

            var presets = await JsonSerializer.DeserializeAsync<List<PresetDto>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return presets ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load or parse action presets manifest.");
            return [];
        }
    }

    public async Task ImportPresetAsync(string resourcePath)
    {
        try
        {
            var assembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Could not get entry assembly.");
            await using var stream = assembly.GetManifestResourceStream(resourcePath);

            if (stream is null)
            {
                logger.LogError("Preset resource file not found at path: {Path}", resourcePath);
                return;
            }

            await actionService.ImportActionsFromJsonStreamAsync(stream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import preset from resource: {Path}", resourcePath);
            throw; // Re-throw to allow UI to catch and display an error
        }
    }

    /// <inheritdoc />
    public async Task<HashSet<string>> GetActionNamesFromPresetAsync(string resourcePath)
    {
        try
        {
            var assembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Could not get entry assembly.");
            await using var stream = assembly.GetManifestResourceStream(resourcePath);

            if (stream is null)
            {
                logger.LogWarning("Could not find preset resource at {Path} to get action names.", resourcePath);
                return [];
            }

            var presetData = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, ActionDto>>>(stream);

            return presetData?
                .SelectMany(group => group.Value.Keys)
                .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read action names from preset resource: {Path}", resourcePath);
            return [];
        }
    }
}