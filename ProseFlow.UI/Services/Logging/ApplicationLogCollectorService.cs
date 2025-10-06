using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Models;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace ProseFlow.UI.Services.Logging;

/// <summary>
/// A custom Serilog sink that captures log events in-memory for real-time display in the UI.
/// </summary>
public class ApplicationLogCollectorService : ILogEventSink
{
    private const int MaxLogHistory = 500;
    private readonly ConcurrentQueue<LogEntry> _logHistory = new();
    private readonly MessageTemplateTextFormatter _formatter;

    /// <summary>
    /// Fired whenever a new log message is captured.
    /// </summary>
    public event Action<LogEntry>? LogMessageReceived;

    /// <summary>
    /// Creates a new instance of the log collector.
    /// </summary>
    /// <param name="outputTemplate">The Serilog output template to use for formatting messages.</param>
    public ApplicationLogCollectorService(string outputTemplate)
    {
        _formatter = new MessageTemplateTextFormatter(outputTemplate);
    }
    
    /// <summary>
    /// Emits a log event to the sink.
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        var appLogLevel = logEvent.Level switch
        {
            LogEventLevel.Information => LogLevel.Info,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Error,
            _ => (LogLevel?)null
        };

        if (appLogLevel is null) return;
        
        // Create a StringWriter to capture the formatted output
        using var writer = new StringWriter();
        
        // Use the formatter to write the log event to the writer
        _formatter.Format(logEvent, writer);

        // Get the final, fully-formatted string
        var formattedMessage = writer.ToString().Trim();
        
        if (string.IsNullOrWhiteSpace(formattedMessage)) return;

        // Use the new formatted message to create the LogEntry
        var logEntry = new LogEntry(logEvent.Timestamp.DateTime, appLogLevel.Value, formattedMessage);

        // Add to history and trim if necessary
        _logHistory.Enqueue(logEntry);
        while (_logHistory.Count > MaxLogHistory) _logHistory.TryDequeue(out _);

        // Notify subscribers
        LogMessageReceived?.Invoke(logEntry);
    }

    /// <summary>
    /// Gets a snapshot of the most recent log messages.
    /// </summary>
    public IReadOnlyCollection<LogEntry> GetLogHistory()
    {
        return _logHistory.ToList();
    }
}