namespace ProseFlow.Core.Models;

/// <summary>
/// Represents a single message in a conversation.
/// </summary>
/// <param name="Role">The role of the message author ("system", "user", or "assistant").</param>
/// <param name="Content">The text content of the message.</param>
public record ChatMessage(string Role, string Content);