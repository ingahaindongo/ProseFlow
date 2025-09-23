using System.Collections.Concurrent;
using LLama.Batched;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Interfaces;

namespace ProseFlow.Infrastructure.Services.AiProviders.Local;

/// <summary>
/// Manages active, stateful Conversation instances for the local provider.
/// </summary>
public class LocalSessionService(
    ILogger<LocalSessionService> logger,
    LocalModelManagerService modelManager) : ILocalSessionService
{
    private readonly ConcurrentDictionary<Guid, Conversation> _activeSessions = new();

    /// <summary>
    /// Creates a new Conversation, stores it, and returns its unique ID.
    /// </summary>
    /// <returns>A new Guid representing the session, or null if the model is not loaded.</returns>
    public Guid? StartSession()
    {
        if (modelManager.Status == ModelStatus.Loading)
        {
            logger.LogInformation("Waiting for local model to finish loading before creating a new session.");
            while (modelManager.Status == ModelStatus.Loading) Thread.Sleep(100);
        }
        
        if (!modelManager.IsLoaded || modelManager.Executor is null)
        {
            logger.LogError("Cannot start a new session because the local model's BatchedExecutor is not available.");
            return null;
        }

        var sessionId = Guid.NewGuid();
        var conversation = modelManager.Executor.Create();
        
        if (_activeSessions.TryAdd(sessionId, conversation))
        {
            logger.LogInformation("Started new local conversation session with ID: {SessionId}", sessionId);
            return sessionId;
        }

        logger.LogError("Failed to add new session to the dictionary, a Guid collision may have occurred.");
        conversation.Dispose(); // Clean up the created conversation if it can't be added.
        return null;
    }

    /// <summary>
    /// Retrieves an active Conversation by its ID.
    /// </summary>
    /// <param name="sessionId">The ID of the session to retrieve.</param>
    /// <returns>The Conversation instance, or null if not found.</returns>
    public Conversation? GetSession(Guid sessionId)
    {
        _activeSessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>
    /// Ends a session, disposes of its resources, and removes it from tracking.
    /// </summary>
    /// <param name="sessionId">The ID of the session to end.</param>
    public void EndSession(Guid sessionId)
    {
        if (_activeSessions.TryRemove(sessionId, out var session))
        {
            session.Dispose();
            logger.LogInformation("Ended and disposed local conversation session with ID: {SessionId}", sessionId);
        }
    }
}