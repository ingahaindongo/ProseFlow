namespace ProseFlow.Application.DTOs;

/// <summary>
/// A Data Transfer Object representing a single Action Preset from the manifest.
/// </summary>
public record PresetDto(
    string Id,
    string Name,
    string Description,
    string ResourcePath);