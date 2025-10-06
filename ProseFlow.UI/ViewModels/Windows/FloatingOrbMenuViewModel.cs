using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.UI.Services;

namespace ProseFlow.UI.ViewModels.Windows;

/// <summary>
/// ViewModel for the Floating Orb's context menu.
/// </summary>
public partial class FloatingOrbMenuViewModel(FloatingOrbService floatingOrbService) : ViewModelBase
{
    
    /// <summary>
    /// Opens the main application window.
    /// </summary>
    [RelayCommand]
    private void OpenApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
        {
            Dispatcher.UIThread.Post(() =>
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();
            });
        }
    }
    
    /// <summary>
    /// Shuts down the application.
    /// </summary>
    [RelayCommand]
    private void QuitApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
    }
    
    /// <summary>
    /// Closes the floating orb.
    /// </summary>
    [RelayCommand]
    private void CloseOrb()
    {
        floatingOrbService.SetEnabled(false);
    }
}