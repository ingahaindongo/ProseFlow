using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ProseFlow.UI.ViewModels.Windows;

/// <summary>
/// ViewModel for the application's splash screen.
/// It provides properties for displaying startup progress to receive updates from the main application thread.
/// </summary>
public partial class SplashScreenViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "Starting...";

    /// <summary>
    /// Reports a progress update from the startup thread.
    /// This method is thread-safe and marshals the update to the UI thread.
    /// </summary>
    /// <param name="message">The status message to display.</param>
    public void Report(string message)
    {
        // Updates from the startup thread must be dispatched to the UI thread.
        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = message;
        });
    }
}