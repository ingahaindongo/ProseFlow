using System.Diagnostics;
using System.Text;
using LLama;
using LLama.Batched;
using LLama.Sampling;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;
using ProseFlow.Infrastructure.Services.AiProviders.Local;

namespace ProseFlow.Infrastructure.Services.AiProviders;

/// <summary>
/// An AI provider that uses a local model via the LLamaSharp library and a BatchedExecutor.
/// It supports both stateless and stateful (session-based) inference.
/// </summary>
public class LocalProvider(
    ILogger<LocalProvider> logger,
    LocalModelManagerService modelManager,
    ILocalSessionService sessionService,
    IUnitOfWork unitOfWork) : IAiProvider
{
    // A semaphore to ensure that only one thread can execute an inference call at a time.
    private readonly SemaphoreSlim _inferenceLock = new(1, 1);

    public string Name => "Local";
    public ProviderType Type => ProviderType.Local;

    public async Task<AiResponse> GenerateResponseAsync(IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken, Guid? sessionId = null)
    {
        // Wait for any ongoing inference to complete before proceeding.
        await _inferenceLock.WaitAsync(cancellationToken);
        try
        {
            var settings = await unitOfWork.Settings.GetProviderSettingsAsync()
                           ?? throw new InvalidOperationException("Provider settings not found in the database.");

            if (!modelManager.IsLoaded || modelManager.Executor is null)
                await modelManager.LoadModelAsync(settings);

            if (!modelManager.IsLoaded || modelManager.Executor is null)
            {
                var errorMessage =
                    $"Local model is not loaded. Status: {modelManager.Status}. Error: {modelManager.ErrorMessage}";
                logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Reset the idle timer to prevent auto-unloading during use.
            modelManager.ResetIdleTimer();

            var executor = modelManager.Executor;

            // Determine if we are using a persistent session or creating a temporary one.
            Conversation? conversation;
            bool isTemporarySession;

            if (sessionId.HasValue && sessionService is LocalSessionService localSessionService)
            {
                conversation = localSessionService.GetSession(sessionId.Value);
                isTemporarySession = false;
            }
            else
            {
                conversation = executor.Create();
                isTemporarySession = true;
            }

            if (conversation is null)
                throw new InvalidOperationException(
                    $"Could not find or create a local conversation session (ID: {sessionId}).");

            try
            {
                var isNewConversation = conversation.TokenCount == 0;

                // Format the prompt
                var formattedPrompt = BuildPrompt(messages, executor.Model, isNewConversation, settings);

                // Prompt the model
                var promptTokens = executor.Context.Tokenize(formattedPrompt, addBos: isNewConversation, special: true);
                conversation.Prompt(promptTokens);

                var promptTokenCount = promptTokens.Length;
                long completionTokenCount = 0;

                // Perform the inference loop
                var responseBuilder = new StringBuilder();
                var sampler = new DefaultSamplingPipeline { Temperature = settings.LocalModelTemperature };
                var decoder = new StreamingTokenDecoder(executor.Context);
                var stopwatch = new Stopwatch();

                // Periodic TPS measurement
                var tpsMeasurements = new List<double>();
                const double measurementIntervalSeconds = 1.0;
                var lastMeasurementTime = TimeSpan.Zero;
                long lastMeasurementTokens = 0;

                var maxTokensToGenerate = settings.LocalModelMaxTokens;

                stopwatch.Start();
                for (var i = 0; i < maxTokensToGenerate; i++)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    if (conversation.RequiresInference)
                        await executor.Infer(cancellationToken);

                    if (!conversation.RequiresSampling) continue;

                    var token = conversation.Sample(sampler);
                    completionTokenCount++;
                    if (token.IsEndOfGeneration(executor.Model.NativeHandle.Vocab) ||
                        token.IsControl(executor.Model.NativeHandle.Vocab))
                        break;

                    decoder.Add(token);
                    responseBuilder.Append(decoder.Read());

                    conversation.Prompt(token);

                    var elapsed = stopwatch.Elapsed;
                    if ((elapsed - lastMeasurementTime).TotalSeconds >= measurementIntervalSeconds)
                    {
                        var timeDelta = (elapsed - lastMeasurementTime).TotalSeconds;
                        var tokenDelta = completionTokenCount - lastMeasurementTokens;

                        if (timeDelta > 0)
                        {
                            var intervalTps = tokenDelta / timeDelta;
                            tpsMeasurements.Add(intervalTps);
                        }

                        lastMeasurementTime = elapsed;
                        lastMeasurementTokens = completionTokenCount;
                    }
                }

                stopwatch.Stop();

                var averageTps = tpsMeasurements.Count != 0
                    ? tpsMeasurements.Average()
                    : completionTokenCount > 0 && stopwatch.Elapsed.TotalSeconds > 0
                        ? completionTokenCount / stopwatch.Elapsed.TotalSeconds
                        : 0;

                logger.LogInformation(
                    "Local inference completed. Generated {CompletionTokens} tokens in {ElapsedMilliseconds} ms. Average TPS: {TokensPerSecond:F2}",
                    completionTokenCount, stopwatch.ElapsedMilliseconds, averageTps);

                return new AiResponse(
                    responseBuilder.ToString().Trim(),
                    promptTokenCount,
                    completionTokenCount,
                    Path.GetFileNameWithoutExtension(settings.LocalModelPath),
                    averageTps);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Local inference failed.");
                throw;
            }
            finally
            {
                // If we created a temporary conversation, ensure it's disposed of immediately.
                if (isTemporarySession) conversation.Dispose();
            }
        }
        finally
        {
            _inferenceLock.Release();
        }
    }

    /// <summary>
    /// Builds a prompt string from chat history using ChatML format.
    /// </summary>
    /// <param name="messages">The list of chat messages.</param>
    /// <param name="model">The loaded LLamaWeights model containing the template.</param>
    /// <param name="isNewSession">If true, the entire history is rendered. If false, only the last user message is rendered.</param>
    /// <param name="settings">The provider settings containing the model path and parameters.</param>
    private string BuildPrompt(IEnumerable<ChatMessage> messages, LLamaWeights model, bool isNewSession,
        ProviderSettings settings)
    {
        try
        {
            var messageList = messages.ToList();
            var template = new LLamaTemplate(model.NativeHandle);

            if (isNewSession)
            {
                // For a new conversation, build the entire history.
                foreach (var message in messageList) template.Add(message.Role, message.Content);
            }
            else
            {
                // For an existing conversation, just append the latest user message.
                var lastUserMessage =
                    messageList.LastOrDefault(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase));
                if (lastUserMessage != null) template.Add("user", lastUserMessage.Content);
            }

            // Always add the start token for the assistant's turn.
            template.AddAssistant = true;

            var result = Encoding.UTF8.GetString(template.Apply());
            template.Clear();

            return result;
        }
        catch (Exception ex)
        {
            var errorMessage =
                $"Failed to build prompt: {Path.GetFileNameWithoutExtension(settings.LocalModelPath)} model's embedded prompt template is missing or incorrect.";
            logger.LogCritical(ex, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    public void Dispose()
    {
        _inferenceLock.Dispose();
        GC.SuppressFinalize(this);
    }
}