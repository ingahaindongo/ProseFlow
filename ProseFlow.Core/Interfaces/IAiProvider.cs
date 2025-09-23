using ProseFlow.Core.Enums;
using ProseFlow.Core.Models;

namespace ProseFlow.Core.Interfaces;

/// <summary>
/// Defines the contract for an AI provider that can generate text-based responses.
/// </summary>
public interface IAiProvider : IDisposable
{
    /// <summary>
    /// Gets the unique name of the provider (e.g., "Cloud", "Local").
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the type of the provider (e.g., Cloud, Local).
    /// </summary>
    ProviderType Type { get; }

    /// <summary>
    /// Asynchronously generates a response from the AI model.
    /// </summary>
    /// <param name="messages">A collection of chat messages to be processed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <param name="sessionId">Optional conversational session identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the AI-generated text.</returns>
    Task<AiResponse> GenerateResponseAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken, Guid? sessionId = null);
}