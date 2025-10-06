namespace ProseFlow.Core.Interfaces.Os;

/// <summary>
/// Defines a contract for a secondary or fallback clipboard service.
/// This allows for a tiered approach where a primary service can fail over
/// to this implementation if it's unavailable or fails.
/// </summary>
public interface IFallbackClipboardService
{
    /// <summary>
    /// Asynchronously gets text from the clipboard.
    /// </summary>
    /// <returns>The text content from the clipboard, or null if it's empty or fails.</returns>
    Task<string?> GetTextAsync();

    /// <summary>
    /// Asynchronously sets the text content of the clipboard.
    /// </summary>
    /// <param name="text">The text to place on the clipboard.</param>
    Task SetTextAsync(string text);
}