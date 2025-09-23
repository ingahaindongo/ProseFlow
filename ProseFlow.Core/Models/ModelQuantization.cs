namespace ProseFlow.Core.Models;

/// <summary>
/// Represents a specific, downloadable version (quantization) of a model from the catalog.
/// </summary>
public record ModelQuantization
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public double RamRequiredGb { get; init; }
    public double FileSizeGb { get; init; }
    public required string Url { get; init; }
    public required string FileName { get; init; }
}