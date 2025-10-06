namespace ProseFlow.Core.Models;

/// <summary>
/// Represents information about a GPU that can be used for selection in settings.
/// </summary>
public record GpuDeviceInfo
{
    /// <summary>
    /// The index of the GPU (0 for first GPU, 1 for second GPU, etc.)
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// The name of the GPU
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The total VRAM available on this GPU in GB
    /// </summary>
    public double? VramGb { get; init; }

    /// <summary>
    /// The type of GPU (NVIDIA, AMD, Intel)
    /// </summary>
    public string Type { get; init; } = string.Empty;
}
