using Serilog.Core;
using Serilog.Events;

namespace ProseFlow.UI.Services.Logging;

/// <summary>
/// A Serilog enricher that adds a "ClassName" property to log events, 
/// containing only the class name from the full SourceContext.
/// </summary>
public class ClassNameEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Check if the SourceContext property exists
        if (!logEvent.Properties.TryGetValue("SourceContext", out var sourceContextProperty) ||
            sourceContextProperty is not ScalarValue { Value: string fullName }) return;
        
        // Find the last dot to get the class name
        var lastDotIndex = fullName.LastIndexOf('.');
        var className = fullName[(lastDotIndex + 1)..];

        // Create the new property and add it to the log event
        var classNameProperty = propertyFactory.CreateProperty("ClassName", className);
        logEvent.AddOrUpdateProperty(classNameProperty);
    }
}