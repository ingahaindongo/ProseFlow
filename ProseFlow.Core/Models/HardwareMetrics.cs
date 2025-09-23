namespace ProseFlow.Core.Models;

/// <summary>
/// A record to hold a snapshot of system hardware metrics.
/// </summary>
public record HardwareMetrics
{
    // CPU
    public double CpuUsagePercent { get; init; }

    // RAM
    public double RamUsagePercent { get; init; }
    public double RamUsedGb { get; init; }
    public double RamTotalGb { get; init; }

    // GPU
    public double GpuUsagePercent { get; init; }

    // VRAM
    public double VramUsagePercent { get; init; }
    public double VramUsedGb { get; init; }
    public double VramTotalGb { get; init; }
}