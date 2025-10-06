﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlmTornado;
using LlmTornado.Code;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Models;

namespace ProseFlow.UI.ViewModels.Onboarding;

public enum TestStatus { Idle, Testing, Success, Error }

public partial class CloudOnboardingViewModel : ViewModelBase
{
    [ObservableProperty]
    private ProviderType _selectedProviderType = ProviderType.OpenAi;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private string _modelName = "gpt-4o";

    [ObservableProperty]
    private float _temperature = 0.7f;

    [ObservableProperty]
    private TestStatus _status = TestStatus.Idle;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public List<ProviderType> AvailableProviderTypes { get; } = Enum.GetValues(typeof(ProviderType))
        .Cast<ProviderType>()
        .Where(p => p is not ProviderType.Local and not ProviderType.Cloud)
        .ToList();

    public CloudProviderConfiguration GetConfiguration()
    {
        return new CloudProviderConfiguration
        {
            Name = $"{SelectedProviderType} (Default)",
            ProviderType = SelectedProviderType,
            ApiKey = ApiKey,
            BaseUrl = BaseUrl,
            Model = ModelName,
            IsEnabled = true,
            Temperature = Temperature
        };
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            Status = TestStatus.Error;
            StatusMessage = "API Key cannot be empty.";
            return;
        }

        Status = TestStatus.Testing;
        StatusMessage = "Verifying key...";

        try
        {
            // If a custom BaseUrl is provided, use it for the API call.
            var api = !string.IsNullOrWhiteSpace(BaseUrl)
                ? new TornadoApi(new Uri(BaseUrl), ApiKey, MapToLlmTornadoProvider(SelectedProviderType))
                : new TornadoApi(ApiKey, MapToLlmTornadoProvider(SelectedProviderType));

            // Test authentication by getting models
            var models = await api.Models.GetModels();

            if (models != null && models.Count != 0)
            {
                Status = TestStatus.Success;
                StatusMessage = "Connection successful!";
            }
            else
            {
                Status = TestStatus.Error;
                StatusMessage = "Authentication seems to have failed. No models were found.";
            }
        }
        catch (Exception ex)
        {
            Status = TestStatus.Error;
            StatusMessage = $"Connection failed: {ex.Message}";
        }
    }
    
    private LLmProviders MapToLlmTornadoProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.OpenAi => LLmProviders.OpenAi,
            ProviderType.Groq => LLmProviders.Groq,
            ProviderType.Anthropic => LLmProviders.Anthropic,
            ProviderType.Google => LLmProviders.Google,
            ProviderType.Mistral => LLmProviders.Mistral,
            ProviderType.Perplexity => LLmProviders.Perplexity,
            ProviderType.OpenRouter => LLmProviders.OpenRouter,
            ProviderType.Custom => LLmProviders.Custom,
            ProviderType.Cohere => LLmProviders.Cohere,
            ProviderType.DeepInfra => LLmProviders.DeepInfra,
            ProviderType.DeepSeek => LLmProviders.DeepSeek,
            ProviderType.Voyage => LLmProviders.Voyage,
            ProviderType.XAi => LLmProviders.XAi,
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), $"Unsupported provider type: {providerType}")
        };
    }
}