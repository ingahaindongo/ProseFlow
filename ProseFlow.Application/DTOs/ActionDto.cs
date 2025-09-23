using System.Text.Json.Serialization;

namespace ProseFlow.Application.DTOs;

/// <summary>
/// Data Transfer Object representing an Action for import/export operations.
/// This matches the structure of the JSON configuration file.
/// </summary>
public record ActionDto
{
    [JsonPropertyName("prefix")]
    public string Prefix { get; init; } = string.Empty;

    [JsonPropertyName("instruction")]
    public string Instruction { get; init; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; init; } = "avares://ProseFlow/Assets/Icons/default.svg";

    [JsonPropertyName("open_in_window")]
    public bool OpenInWindow { get; init; }

    [JsonPropertyName("explain_changes")]
    public bool ExplainChanges { get; init; }

    [JsonPropertyName("application_context")]
    public List<string> ApplicationContext { get; init; } = [];
}