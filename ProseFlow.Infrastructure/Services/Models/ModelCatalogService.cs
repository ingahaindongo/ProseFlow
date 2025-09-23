using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Models;

namespace ProseFlow.Infrastructure.Services.Models;

public class ModelCatalogService(ILogger<ModelCatalogService> logger) : IModelCatalogService
{
    private readonly HttpClient _httpClient = new();
    
    private List<ModelCatalogEntry>? _cachedModels;
    private DateTime _lastFetchTime;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<ModelCatalogEntry>> GetAvailableModelsAsync()
    {
        if (_cachedModels is not null && DateTime.UtcNow - _lastFetchTime < _cacheDuration)
            return _cachedModels;

        try
        {
            logger.LogInformation("Fetching model catalog from {Url}", Constants.ManifestUrl);
            var json = await _httpClient.GetStringAsync(Constants.ManifestUrl);
            var models = JsonSerializer.Deserialize<List<ModelCatalogEntry>>(json, _jsonSerializerOptions);

            if (models is not null)
            {
                _cachedModels = models;
                _lastFetchTime = DateTime.UtcNow;
                return models;
            }
        }
        catch (HttpRequestException)
        {
            logger.LogError("Failed to fetch model catalog, Check your internet connection.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch or parse the model catalog.");
            AppEvents.RequestNotification("Failed to fetch or parse the model catalog.", NotificationType.Error);
        }
        
        return [];
    }
}