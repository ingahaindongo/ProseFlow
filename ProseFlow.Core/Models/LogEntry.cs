using ProseFlow.Core.Enums;

namespace ProseFlow.Core.Models;

/// <summary>
/// Represents a single log message captured from an underlying service like a local LLM.
/// </summary>
/// <param name="Timestamp">When the log message was generated.</param>
/// <param name="Level">The severity level of the message.</param>
/// <param name="Message">The content of the log message.</param>
public record LogEntry(DateTime Timestamp, LogLevel Level, string Message);