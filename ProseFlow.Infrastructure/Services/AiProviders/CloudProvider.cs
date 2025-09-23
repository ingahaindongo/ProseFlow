using System.Diagnostics;
using System.Text;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Code;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Events;
using ProseFlow.Application.Services;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;
using ChatMessage = ProseFlow.Core.Models.ChatMessage;

namespace ProseFlow.Infrastructure.Services.AiProviders;

/// <summary>
/// A provider that orchestrates requests across a user-defined, ordered chain of cloud services.
/// It leverages LlmTornado to handle different APIs seamlessly.
/// </summary>
public class CloudProvider(
    CloudProviderManagementService providerService,
    UsageTrackingService usageService,
    ILogger<CloudProvider> logger) : IAiProvider
{
    public string Name => "Cloud";
    public ProviderType Type => ProviderType.Cloud;

    public async Task<AiResponse> GenerateResponseAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken, Guid? sessionId = null)
    {
        var enabledConfigs = (await providerService.GetConfigurationsAsync())
            .Where(c => c.IsEnabled)
            .ToList();

        if (enabledConfigs.Count == 0)
        {
            AppEvents.RequestNotification("No enabled cloud providers are configured. Please add and enable one in settings.", NotificationType.Warning);
            throw new InvalidOperationException("No enabled cloud providers are configured. Please add and enable one in settings.");
        }

        var authentications = enabledConfigs.Select(config =>
        {
            var provider = MapToLlmTornadoProvider(config.ProviderType);
            return new ProviderAuthentication(provider, config.ApiKey);
        }).ToList();
        
        var api = new TornadoApi(authentications);
        
        // Create the LlmTornado message list from DTO
        var tornadoMessages = messages.Select(m => new LlmTornado.Chat.ChatMessage(
            m.Role switch {
                "system" => ChatMessageRoles.System,
                "user" => ChatMessageRoles.User,
                "assistant" => ChatMessageRoles.Assistant,
                _ => ChatMessageRoles.User
            },
            m.Content
        )).ToList();

        foreach (var config in enabledConfigs)
            try
            {
                var request = new ChatRequest
                {
                    Model = config.Model,
                    Temperature = config.Temperature,
                    Messages = tornadoMessages,
                    Stream = true,
                    CancellationToken = cancellationToken
                };

                // If a custom BaseUrl is provided, override the TornadoApi instance for this specific call.
                var conversationApi = !string.IsNullOrWhiteSpace(config.BaseUrl)
                    ? new TornadoApi(new Uri(config.BaseUrl), config.ApiKey)
                    : api;
                
                var fullContent = new StringBuilder();
                long promptTokens = 0;
                long completionTokens = 0;
                var stopwatch = new Stopwatch();

                var stream = conversationApi.Chat.StreamChatEnumerable(request);
                
                stopwatch.Start();
                await foreach (var chunk in stream.WithCancellation(cancellationToken))
                {
                    // Aggregate content from response chunks
                    var delta = chunk.Choices?.FirstOrDefault()?.Delta;
                    if (delta is { Role: ChatMessageRoles.Assistant, Content: { } contentPart }) fullContent.Append(contentPart);

                    // Aggregate usage data. The final counts are often in the last chunks.
                    if (chunk.Usage is not null)
                    {
                        promptTokens = chunk.Usage.PromptTokens;
                        completionTokens = chunk.Usage.CompletionTokens;
                    }
                }
                stopwatch.Stop();

                // Persist monthly aggregate usage
                if (promptTokens > 0 || completionTokens > 0) await usageService.AddUsageAsync(promptTokens, completionTokens);
            
                if (fullContent.Length > 0)
                {
                    double tokensPerSecond = 0;
                    switch (completionTokens)
                    {
                        case > 0 when stopwatch.Elapsed.TotalSeconds > 0:
                            tokensPerSecond = completionTokens / stopwatch.Elapsed.TotalSeconds;
                            break;
                        // Log if we can't calculate TPS despite having tokens
                        case > 0:
                            logger.LogWarning("Could not calculate Tokens Per Second for provider '{ProviderName}'. Elapsed time was zero.", config.Name);
                            break;
                    }
                    
                    return new AiResponse(fullContent.ToString(), promptTokens, completionTokens, config.Name, tokensPerSecond);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Provider '{config.Name}' failed: {ex.Message}. Trying next provider...";
                AppEvents.RequestNotification(errorMessage, NotificationType.Warning);
                logger.LogError(ex, "Provider '{ConfigName}' failed.", config.Name);
            }

        logger.LogError("All configured cloud providers failed to return a valid response.");
        throw new InvalidOperationException("All configured cloud providers failed to return a valid response.");
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
            ProviderType.Local => throw new InvalidOperationException($"Local LLMs are not supported in {nameof(CloudProvider)}, Use {nameof(LocalProvider)}."),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), $"Unsupported provider type: {providerType}")
        };
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}