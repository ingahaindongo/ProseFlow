using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.DTOs;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.Application.Services;

public class ActionOrchestrationService : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOsService _osService;
    private readonly IReadOnlyDictionary<string, IAiProvider> _providers;
    private readonly ILocalSessionService _localSessionService;
    private readonly ILogger<ActionOrchestrationService> _logger;

    public ActionOrchestrationService(IServiceScopeFactory scopeFactory, IOsService osService,
        IEnumerable<IAiProvider> providers, ILocalSessionService localSessionService, ILogger<ActionOrchestrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _osService = osService;
        _localSessionService = localSessionService;
        _providers = providers.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public void Initialize()
    {
        _osService.ActionMenuHotkeyPressed += async () => await HandleActionMenuHotkeyAsync();
        _osService.SmartPasteHotkeyPressed += async () => await HandleSmartPasteHotkeyAsync();
    }

    private async Task HandleActionMenuHotkeyAsync()
    {
        var activeAppContext = await _osService.GetActiveWindowProcessNameAsync();
        var allActions = await ExecuteQueryAsync(unitOfWork => unitOfWork.Actions.GetAllOrderedAsync());

        // Filter actions based on context
        var availableActions = allActions
            .Where(a => a.ApplicationContext.Count == 0 ||
                        a.ApplicationContext.Contains(activeAppContext, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (availableActions.Count == 0)
        {
            AppEvents.RequestNotification("No actions available for the current application.",
                NotificationType.Warning);
            return;
        }

        var request = await AppEvents.RequestFloatingMenuAsync(availableActions, activeAppContext);
        if (request is not null)
            await ProcessRequestAsync(request);
    }

    private async Task HandleSmartPasteHotkeyAsync()
    {
        var result = await ExecuteQueryAsync(async unitOfWork =>
        {
            var settings = await unitOfWork.Settings.GetGeneralSettingsAsync();
            if (settings.SmartPasteActionId is null)
                return new { Action = (Action?)null, IsConfigured = false };

            var action = (await unitOfWork.Actions
                .GetByExpressionAsync(a => a.Id == settings.SmartPasteActionId.Value)).FirstOrDefault();
            
            return new { Action = action, IsConfigured = true };
        });

        if (!result.IsConfigured)
        {
            AppEvents.RequestNotification("Smart Paste action not configured in settings.", NotificationType.Warning);
            return;
        }

        if (result.Action is null)
        {
            AppEvents.RequestNotification("The configured Smart Paste action was not found.", NotificationType.Error);
            return;
        }

        var request = new ActionExecutionRequest(result.Action, result.Action.OpenInWindow, null);
        await ProcessRequestAsync(request);
    }

    private async Task ProcessRequestAsync(ActionExecutionRequest request)
    {
        var overallStopwatch = Stopwatch.StartNew();
        AppEvents.RequestNotification("Processing...", NotificationType.Info);

        // Local stateful session ID (If local provider is used)
        Guid? localSessionId = null; 
        
        try
        {
            var userInput = await _osService.GetSelectedTextAsync();
            if (string.IsNullOrWhiteSpace(userInput))
            {
                AppEvents.RequestNotification("No text selected or clipboard is empty.", NotificationType.Warning);
                return;
            }

            // Initialize the conversation transcript
            var conversationHistory = new List<ChatMessage>();

            // Add the system prompt (the main rules).
            var systemInstruction = request.ActionToExecute.ExplainChanges
                ? $"{request.ActionToExecute.Instruction}\n\nIMPORTANT: After your main response, add a section that starts with '---EXPLANATION---' and explain the changes you made."
                : request.ActionToExecute.Instruction;
            conversationHistory.Add(new ChatMessage("system", systemInstruction));
            conversationHistory.Add(new ChatMessage("user", $"{request.ActionToExecute.Prefix}{userInput}"));

            if (request.ForceOpenInWindow || request.ActionToExecute.OpenInWindow)
            {
                // Windowed processing loop
                while (true)
                {
                    var executionResult = await ExecuteRequestWithFallbackAsync(conversationHistory, request.ProviderOverride, localSessionId);

                    if (executionResult is null)
                    {
                        // Both primary and fallback failed, or no providers are configured.
                        AppEvents.RequestNotification("All available AI providers failed.", NotificationType.Error);
                        break;
                    }

                    if (executionResult.Value.Provider.Type == ProviderType.Local && localSessionId is null)
                    {
                        // If this is the first turn in a windowed local session. Create a new session.
                        localSessionId = _localSessionService.StartSession();
                        if (localSessionId is null)
                        {
                            AppEvents.RequestNotification("Failed to start a local model session.", NotificationType.Error);
                            return;
                        }
                    }

                    var (aiResponse, provider, latencyMs) = executionResult.Value;
                    conversationHistory.Add(new ChatMessage("assistant", aiResponse.Content));

                    // Log to DB
                    await LogToHistoryAsync(
                        request.ActionToExecute.Name,
                        provider.Name,
                        aiResponse.ProviderName,
                        conversationHistory.Last(m => m.Role == "user").Content,
                        aiResponse.Content,
                        aiResponse.PromptTokens,
                        aiResponse.CompletionTokens,
                        latencyMs,
                        aiResponse.TokensPerSecond);

                    // Parse and show the result window
                    var (mainOutput, explanation) = ParseOutput(aiResponse.Content, request.ActionToExecute.ExplainChanges);

                    // Show the window and wait for the user to either close it or request a refinement
                    var windowData = new ResultWindowData(request.ActionToExecute.Name, mainOutput, explanation);
                    var refinementRequest = await AppEvents.RequestResultWindowAsync(windowData);

                    if (refinementRequest is null)
                        break; // User closed the window

                    conversationHistory.Add(new ChatMessage("user", refinementRequest.NewInstruction));
                }
            }
            else
            {
                // In-Place execution
                var executionResult = await ExecuteRequestWithFallbackAsync(conversationHistory, request.ProviderOverride);

                if (executionResult is null)
                {
                    AppEvents.RequestNotification("All available AI providers failed.", NotificationType.Error);
                    return;
                }

                var (aiResponse, provider, latencyMs) = executionResult.Value;

                await LogToHistoryAsync(
                    request.ActionToExecute.Name,
                    provider.Name,
                    aiResponse.ProviderName,
                    conversationHistory.Last(m => m.Role == "user").Content,
                    aiResponse.Content,
                    aiResponse.PromptTokens,
                    aiResponse.CompletionTokens,
                    latencyMs,
                    aiResponse.TokensPerSecond);
                
                await _osService.PasteTextAsync(aiResponse.Content);
            }

            overallStopwatch.Stop();
            AppEvents.RequestNotification(
                $"'{request.ActionToExecute.Name}' completed in {overallStopwatch.Elapsed.TotalSeconds:F2}s.",
                NotificationType.Success);
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            _logger.LogError(ex, "Error executing action: {ActionName}", request.ActionToExecute.Name);
            var displayMessage = ex is InvalidOperationException ? ex.Message : "An unexpected error occurred.";
            AppEvents.RequestNotification($"Error: {displayMessage}", NotificationType.Error);
        }
        finally
        {
            // Clean up the stateful session if one was created.
            if (localSessionId.HasValue) _localSessionService.EndSession(localSessionId.Value);
        }
    }

    /// <summary>
    /// Executes an AI request, trying the primary provider first and then the fallback provider upon failure.
    /// </summary>
    /// <returns>A tuple containing the response, the successful provider, and latency, or null if all attempts fail.</returns>
    private async Task<(AiResponse Response, IAiProvider Provider, double LatencyMs)?> ExecuteRequestWithFallbackAsync(
        List<ChatMessage> messages, string? providerOverride, Guid? sessionId = null)
    {
        var settings = await ExecuteQueryAsync(unitOfWork => unitOfWork.Settings.GetProviderSettingsAsync());

        // 1. Determine Primary Provider
        var primaryProvider = await GetProviderAsync(providerOverride, settings.PrimaryServiceType);

        if (primaryProvider is not null)
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await primaryProvider.GenerateResponseAsync(messages, CancellationToken.None, sessionId);
                stopwatch.Stop();
                _logger.LogInformation("Primary provider '{ProviderName}' succeeded.", primaryProvider.Name);
                return (response, primaryProvider, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary provider '{ProviderName}' failed. Attempting fallback.", primaryProvider.Name);
                AppEvents.RequestNotification($"Primary provider ({primaryProvider.Name}) failed. Trying fallback...", NotificationType.Warning);
            }

        // 2. Determine and Try Fallback Provider
        if (settings.FallbackServiceType.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("No fallback provider is configured.");
            return null;
        }
        
        var fallbackProvider = await GetProviderAsync(null, settings.FallbackServiceType); // No override for fallback

        if (fallbackProvider is not null && fallbackProvider.Name != primaryProvider?.Name)
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await fallbackProvider.GenerateResponseAsync(messages, CancellationToken.None, sessionId);
                stopwatch.Stop();
                _logger.LogInformation("Fallback provider '{ProviderName}' succeeded.", fallbackProvider.Name);
                return (response, fallbackProvider, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback provider '{ProviderName}' also failed.", fallbackProvider.Name);
            }

        // 3. Both failed or were not configured
        return null;
    }
    
    private (string MainOutput, string? Explanation) ParseOutput(string rawOutput, bool expectExplanation)
    {
        if (!expectExplanation || !rawOutput.Contains("---EXPLANATION---")) return (rawOutput.Trim(), null);
        
        var parts = rawOutput.Split(["---EXPLANATION---"], 2, StringSplitOptions.None);
        return (parts[0].Trim(), parts[1].Trim());
    }

    private Task<IAiProvider?> GetProviderAsync(string? providerOverride, string serviceType)
    {
        if (!string.IsNullOrWhiteSpace(providerOverride) &&
            _providers.TryGetValue(providerOverride, out var overriddenProvider))
            return Task.FromResult<IAiProvider?>(overriddenProvider);

        if (_providers.TryGetValue(serviceType, out var provider))
            return Task.FromResult<IAiProvider?>(provider);

        return Task.FromResult<IAiProvider?>(null);
    }
    
    private async Task LogToHistoryAsync(string actionName, string providerType, string modelUsed, string input, string output, long promptTokens, long completionTokens, double latencyMs, double inferenceSpeed)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var historyService = scope.ServiceProvider.GetRequiredService<HistoryService>();
            await historyService.AddHistoryEntryAsync(actionName, providerType, modelUsed, input, output, promptTokens, completionTokens, latencyMs, inferenceSpeed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log history entry.");
            AppEvents.RequestNotification("Failed to log to history", NotificationType.Warning);
        }
    }

    public void Dispose()
    {
        _osService.Dispose();
        foreach (var provider in _providers.Values)
        {
            provider.Dispose();
        }
        GC.SuppressFinalize(this);
    }
    
    #region Private Helpers

    /// <summary>
    /// Creates a UoW scope and executes a query.
    /// </summary>
    private async Task<T> ExecuteQueryAsync<T>(Func<IUnitOfWork, Task<T>> query)
    {
        using var scope = _scopeFactory.CreateScope();
        await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        return await query(unitOfWork);
    }

    #endregion
}