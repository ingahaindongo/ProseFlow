using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using ProseFlow.Core.Interfaces.Os;

namespace ProseFlow.UI.Services;

/// <summary>
/// Implements the clipboard service contract using the Avalonia UI framework's clipboard APIs.
/// This serves as the high-priority fallback for clipboard operations and is only available in classic desktop environments.
/// All clipboard operations are dispatched to the UI thread to ensure thread safety.
/// </summary>
public class AvaloniaClipboardService : IFallbackClipboardService
{
    /// <summary>
    /// Gets the clipboard instance from the main window.
    /// </summary>
    private static IClipboard? GetClipboard()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.Clipboard
            : null;
    }

    /// <inheritdoc />
    public Task<string?> GetTextAsync()
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var clipboard = GetClipboard();
            return clipboard is not null ? await clipboard.GetTextAsync() : null;
        });
    }

    /// <inheritdoc />
    public Task SetTextAsync(string text)
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var clipboard = GetClipboard();
            if (clipboard is null)
                throw new InvalidOperationException("Clipboard is not available in the current application context.");
            await clipboard.SetTextAsync(text);
        });
    }
}