namespace ProseFlow.Application.DTOs.Dashboard;

/// <summary>
/// A record representing provider performance metrics.
/// </summary>
/// <param name="ProviderName">The name of the AI provider.</param>
/// <param name="Model">The model used by the provider.</param>
/// <param name="UsageCount">The number of times the provider has been used.</param>
/// <param name="AverageLatencyMs">The average latency of the provider's responses.</param>
public record ProviderPerformanceDto(string ProviderName, string Model, int UsageCount, double AverageLatencyMs);