using System.Collections.Concurrent;
using LLama.Native;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Models;

namespace ProseFlow.Infrastructure.Services.AiProviders.Local;

/// <summary>
/// A singleton service to manage global native-level interactions with the Llama.cpp library.
/// This is the ONLY class that should directly call `LLama.Native` APIs.
/// </summary>
public class LocalNativeManager
{
    private const int MaxLogHistory = 500;
    private readonly ConcurrentQueue<LogEntry> _logHistory = new();
    
    // A reference to the delegate must be kept to prevent it from being garbage collected.
    private NativeLogConfig.LLamaLogCallback? _logCallbackDelegate;
    private bool _isInitialized;
    private readonly object _initLock = new();

    /// <summary>
    /// Fired whenever a new log message is captured from the native Llama.cpp library.
    /// </summary>
    public event Action<LogEntry>? LogMessageReceived;

    /// <summary>
    /// Initializes the native log callback. This should only be called once at application startup.
    /// </summary>
    public void Initialize()
    {
        lock (_initLock)
        {
            if (_isInitialized) return;
            
            _logCallbackDelegate = HandleNativeLogMessage;
            NativeLogConfig.llama_log_set(_logCallbackDelegate);
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Gets a snapshot of the most recent log messages.
    /// </summary>
    public IReadOnlyCollection<LogEntry> GetLogHistory()
    {
        return _logHistory.ToList();
    }

    private void HandleNativeLogMessage(LLamaLogLevel level, string message)
    {
        LogLevel? appLogLevel = level switch
        {
            LLamaLogLevel.Info => LogLevel.Info,
            LLamaLogLevel.Warning => LogLevel.Warning,
            LLamaLogLevel.Error => LogLevel.Error,
            _ => null
        };
        
        if (appLogLevel is null) return;
        
        // Sanitize the message from native code.
        var sanitizedMessage = message.Trim();
        if (string.IsNullOrWhiteSpace(sanitizedMessage)) return;

        var logEntry = new LogEntry(DateTime.Now, appLogLevel.Value, sanitizedMessage);

        // Add to history and trim if necessary
        _logHistory.Enqueue(logEntry);
        while (_logHistory.Count > MaxLogHistory) _logHistory.TryDequeue(out _);

        // Notify subscribers
        LogMessageReceived?.Invoke(logEntry);
    }
}