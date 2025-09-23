namespace ProseFlow.Core.Models;

/// <summary>
/// Represents a single model entry in the remote catalog, containing metadata and available quantizations.
/// </summary>
public record ModelCatalogEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Creator { get; init; }
    public required string Description { get; init; }
    public required string Tag { get; init; }
    public required IEnumerable<ModelQuantization> Quantizations { get; init; } = [];
}