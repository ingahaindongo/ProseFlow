using Microsoft.Extensions.Logging;
using ProseFlow.Core.Interfaces.Os;

namespace ProseFlow.Infrastructure.Services.Os.Clipboard;

/// <summary>
/// Implements the clipboard service contract using the TextCopy library.
/// Third-tier fallback clipboard implementation.
/// </summary>
public class TextCopyClipboardService(ILogger<TextCopyClipboardService> logger) : IFallbackClipboardService
{
    /// <inheritdoc />
    public async Task<string?> GetTextAsync()
    {
        try
        {
            return await TextCopy.ClipboardService.GetTextAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TextCopy clipboard 'get' failed.");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetTextAsync(string text)
    {
        try
        {
            await TextCopy.ClipboardService.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("TextCopy clipboard 'set' failed.", ex);
        }
    }
}