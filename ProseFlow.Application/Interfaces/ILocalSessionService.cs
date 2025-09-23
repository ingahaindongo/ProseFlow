namespace ProseFlow.Application.Interfaces;

/// <summary>
/// Defines the contract for managing active, stateful Conversation instances.
/// </summary>
public interface ILocalSessionService
{
    /// <summary>
    /// Creates a new Conversation, stores it, and returns its unique ID.
    /// </summary>
    /// <returns>A new Guid representing the session, or null if a session could not be started.</returns>
    Guid? StartSession();

    /// <summary>
    /// Ends a session, disposes of its resources, and removes it from tracking.
    /// </summary>
    /// <param name="sessionId">The ID of the session to end.</param>
    void EndSession(Guid sessionId);
}