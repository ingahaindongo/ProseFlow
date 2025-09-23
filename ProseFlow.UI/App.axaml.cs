using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.Core.Interfaces;
using ProseFlow.Infrastructure.Data;
using ProseFlow.Infrastructure.Security;
using ProseFlow.Infrastructure.Services.AiProviders;
using ProseFlow.Infrastructure.Services.AiProviders.Local;
using ProseFlow.Infrastructure.Services.Models;
using ProseFlow.Infrastructure.Services.Monitoring;
using ProseFlow.Infrastructure.Services.Os;
using ProseFlow.Infrastructure.Services.Updates;
using ProseFlow.UI.Services;
using ProseFlow.UI.Services.ActiveWindow;
using ProseFlow.UI.ViewModels;
using ProseFlow.UI.ViewModels.About;
using ProseFlow.UI.ViewModels.Actions;
using ProseFlow.UI.ViewModels.Dashboard;
using ProseFlow.UI.ViewModels.Dialogs;
using ProseFlow.UI.ViewModels.Downloads;
using ProseFlow.UI.ViewModels.History;
using ProseFlow.UI.ViewModels.Onboarding;
using ProseFlow.UI.ViewModels.Providers;
using ProseFlow.UI.ViewModels.Settings;
using ProseFlow.UI.ViewModels.Windows;
using ProseFlow.UI.Views;
using ProseFlow.UI.Views.Dialogs;
using ProseFlow.UI.Views.Downloads;
using ProseFlow.UI.Views.Onboarding;
using ProseFlow.UI.Views.Providers;
using ProseFlow.UI.Views.Windows;
using Serilog;
using Serilog.Events;
using ShadUI;
using Velopack;

namespace ProseFlow.UI;

public class App : Avalonia.Application
{
    public IServiceProvider? Services { get; private set; }
    private TrayIcon? _trayIcon;

    public override void Initialize()
    {
        VelopackApp.Build().Run();
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        // Create main window but without initializing it. It acts as an owner for startup error dialogs.
        desktop.MainWindow = new MainWindow();
        desktop.MainWindow.Show();

        ILogger<App>? logger = null;
        var isInitialized = false;

        // Initialize database and services until successful, allowing reset on failure.
        while (!isInitialized)
        {
            // Configure services and build the provider for this attempt.
            var serviceProvider = ConfigureServices();
            logger = serviceProvider.GetRequiredService<ILogger<App>>();

            try
            {
                // Attempt to initialize the database.
                await using (var scope = serviceProvider.CreateAsyncScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await dbContext.Database.MigrateAsync();
                }

                // If successful, assign the provider to the App's properties and exit the loop.
                Services = serviceProvider;
                Ioc.Default.ConfigureServices(Services);
                isInitialized = true;
                logger.LogInformation("Database and services initialized successfully.");
            }
            catch (SqliteException ex)
            {
                logger.LogCritical(ex, "Database initialization failed due to a SQLite error (Code: {ErrorCode}). Prompting user for action.", ex.SqliteErrorCode);

                // The failed ServiceProvider must be disposed to release its hold on services.
                (serviceProvider as IDisposable)?.Dispose();

                // Clear all SQLite connection pools to ensure no locks are held.
                SqliteConnection.ClearAllPools();

                // Force garbage collection to release any lingering unmanaged resources.
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Create a temporary, dependency-free service to show the critical dialog.
                var tempDialogService = new DialogService(null!); 
                var userWantsToReset = await tempDialogService.ShowCriticalConfirmationDialogAsync(
                    desktop.MainWindow,
                    "Database Error",
                    "ProseFlow's data file is corrupted or inaccessible. To continue, the application must reset its data. This will erase all your settings, actions, and history. A backup of the corrupted file will be made.",
                    "Backup & Reset",
                    "Quit"
                );

                if (userWantsToReset)
                {
                    try
                    {
                        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        var proseFlowDataPath = Path.Combine(appDataPath, "ProseFlow");
                        var dbPath = Path.Combine(proseFlowDataPath, "proseflow.db");
                        if (File.Exists(dbPath))
                        {
                            var backupPath = Path.Combine(proseFlowDataPath, $"proseflow.db.corrupt-{DateTime.Now:yyyyMMddHHmmss}.bak");
                            File.Move(dbPath, backupPath, true);
                            logger.LogInformation("Corrupted database backed up to {BackupPath}", backupPath);
                        }
                    }
                    catch (Exception backupEx)
                    {
                        logger.LogError(backupEx, "Failed to back up and remove the corrupted database after cleanup.");
                        await tempDialogService.ShowCriticalConfirmationDialogAsync(desktop.MainWindow, "Fatal Error", "Could not remove the corrupted database file. Please find it in the application's data folder and delete it manually. The application will now exit.", "OK", "Close");
                        desktop.Shutdown();
                        return;
                    }
                }
                else // User chose to quit.
                {
                    desktop.Shutdown();
                    return;
                }
            }
        }
        
        if (Services is null || logger is null)
            return;

        // Initialize local model native manager
        var nativeManager = Services.GetRequiredService<LocalNativeManager>();
        nativeManager.Initialize();

        // Initialize services that depend on the database
        var usageTrackingService = Services.GetRequiredService<UsageTrackingService>();
        await usageTrackingService.InitializeAsync();
        var settingsService = Services.GetRequiredService<SettingsService>();

        // Perform silent update check on startup
        var updateService = Services.GetRequiredService<IUpdateService>();
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000); // Wait 5 seconds to not impact startup time
            await updateService.CheckForUpdateAsync();
        });

        // Check for local model on startup
        await using (var scope = Services.CreateAsyncScope())
        {
            var modelManager = scope.ServiceProvider.GetRequiredService<LocalModelManagerService>();
            try
            {
                var providerSettings = await settingsService.GetProviderSettingsAsync();
                if (providerSettings is { PrimaryServiceType: "Local", LocalModelLoadOnStartup: true })
                {
                    if (string.IsNullOrWhiteSpace(providerSettings.LocalModelPath) ||
                        !File.Exists(providerSettings.LocalModelPath))
                    {
                        logger.LogWarning("Auto-load skipped: Local model path is not configured or file does not exist.");
                    }
                    else
                    {
                        logger.LogInformation("Attempting to auto-load local model on startup...");
                        if (!Design.IsDesignMode) // Don't auto-load model in design mode
                            _ = modelManager.LoadModelAsync(providerSettings);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the local model auto-load check.");
            }
        }

        // Initialize and start background services
        var orchestrationService = Services.GetRequiredService<ActionOrchestrationService>();
        orchestrationService.Initialize();

        // Subscribe UI handlers to application-layer events
        var osService = Services.GetRequiredService<IOsService>();
        var generalSettings = await settingsService.GetGeneralSettingsAsync();
        _ = osService.StartHookAsync();
        osService.UpdateHotkeys(generalSettings.ActionMenuHotkey, generalSettings.SmartPasteHotkey);

        SubscribeToAppEvents();

        // Setup Dialogs
        var dialogManager = Services.GetRequiredService<DialogManager>();
        dialogManager.Register<InputDialogView, InputDialogViewModel>();
        dialogManager.Register<ModelLibraryView, ModelLibraryViewModel>();
        dialogManager.Register<DownloadsPopupView, DownloadsPopupViewModel>();

        // Don't shut down the app when the main window is closed.
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        desktop.Exit += OnApplicationExit;

        RequestedThemeVariant = generalSettings.Theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
        
        // Onboarding process for new users
        if (!generalSettings.IsOnboardingCompleted)
        {
            var onboardingVm = Services.GetRequiredService<OnboardingViewModel>();
            var onboardingWindow = new OnboardingWindow { DataContext = onboardingVm };

            var completedSuccessfully = await onboardingWindow.ShowDialog<bool>(desktop.MainWindow);
            if (completedSuccessfully)
            {
                // Save all settings gathered during onboarding
                await onboardingVm.SaveSettingsAsync();

                // Mark onboarding as complete and save
                generalSettings = await settingsService.GetGeneralSettingsAsync();
                generalSettings.IsOnboardingCompleted = true;
                await settingsService.SaveGeneralSettingsAsync(generalSettings);
            }
            else
            {
                // If the user closes the onboarding window, quit the application.
                desktop.Shutdown();
                return;
            }
        }

        // If onboarding completed, assign the view model
        desktop.MainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
        
        // Handle the closing event to hide the window instead of closing
        desktop.MainWindow.Closing += (_, e) =>
        {
            e.Cancel = true;
            desktop.MainWindow.Hide();
        };

        // Create and set up the system tray icon
        _trayIcon = CreateTrayIcon();
        if (_trayIcon is not null) TrayIcon.SetIcons(this, [_trayIcon]);

        base.OnFrameworkInitializationCompleted();
    }
    
    private TrayIcon? CreateTrayIcon()
    {
        if (Services is null) return null;

        var trayVm = Services.GetRequiredService<TrayIconViewModel>();
        var mainVm = Services.GetRequiredService<MainViewModel>();

        // Wire up the event to show the main window
        trayVm.ShowMainWindowRequested += () =>
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
                // Ensure we're on the UI thread before showing the window
                Dispatcher.UIThread.Post(() =>
                {
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();
                });
        };

        trayVm.ShowDownloadsRequested += () =>
        {
             if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
                 Dispatcher.UIThread.Post(() =>
                 {
                     desktop.MainWindow.Show();
                     desktop.MainWindow.Activate();
                     mainVm.ShowDownloadsPopupCommand.Execute(null);
                 });
        };

        // Define a converter for the menu item header
        var modelStatusToHeaderConverter = new FuncValueConverter<bool, string>(isLoaded =>
            isLoaded ? "Unload Local Model" : "Load Local Model");

        var providerTypeToHeaderConverter = new FuncValueConverter<string, string>(providerType =>
            $"Set Primary Provider ({providerType})");
        
        var downloadCountToHeaderConverter = new FuncValueConverter<int, string>(count => $"Downloads ({count})");

        // Build the context menu items
        var openItem = new NativeMenuItem
        {
            Header = "Open ProseFlow",
            Command = trayVm.OpenSettingsCommand
        };

        var toggleModelItem = new NativeMenuItem
        {
            Command = trayVm.ToggleLocalModelCommand
        };
        toggleModelItem.Bind(NativeMenuItem.HeaderProperty, new Binding(nameof(trayVm.IsModelLoaded))
        {
            Source = trayVm,
            Converter = modelStatusToHeaderConverter
        });
        toggleModelItem.Bind(NativeMenuItem.IsEnabledProperty, new Binding(nameof(trayVm.ManagerStatus))
        {
            Source = trayVm,
            Converter = new FuncValueConverter<ModelStatus, bool>(s => s != ModelStatus.Loading)
        });
        
        var downloadsItem = new NativeMenuItem
        {
            Command = trayVm.ShowDownloadsCommand
        };
        downloadsItem.Bind(NativeMenuItem.HeaderProperty,
            new Binding(nameof(trayVm.ActiveDownloadCount))
                { Source = trayVm, Converter = downloadCountToHeaderConverter });
        downloadsItem.Bind(NativeMenuItem.IsVisibleProperty,
            new Binding(nameof(trayVm.HasActiveDownloads)) { Source = trayVm });

        // Provider Type Sub-menu
        var cloudProviderItem = new NativeMenuItem
        {
            Header = "Cloud",
            Command = trayVm.SetProviderTypeCommand,
            CommandParameter = "Cloud"
        };
        cloudProviderItem.Bind(NativeMenuItem.IsCheckedProperty,
            new Binding(nameof(trayVm.CurrentProviderType))
            {
                Source = trayVm,
                Converter = new FuncValueConverter<string, bool>(t => t == "Cloud")
            });

        var localProviderItem = new NativeMenuItem
        {
            Header = "Local",
            Command = trayVm.SetProviderTypeCommand,
            CommandParameter = "Local"
        };
        localProviderItem.Bind(NativeMenuItem.IsCheckedProperty,
            new Binding(nameof(trayVm.CurrentProviderType))
            {
                Source = trayVm,
                Converter = new FuncValueConverter<string, bool>(t => t == "Local")
            });

        var setProviderSubMenu = new NativeMenuItem
        {
            Menu = new NativeMenu
            {
                Items = { cloudProviderItem, localProviderItem }
            }
        };
        setProviderSubMenu.Bind(NativeMenuItem.HeaderProperty, new Binding(nameof(trayVm.CurrentProviderType))
        {
            Source = trayVm,
            Converter = providerTypeToHeaderConverter
        });

        var quitItem = new NativeMenuItem
        {
            Header = "Quit",
            Command = trayVm.QuitApplicationCommand
        };

        // Create the TrayIcon instance
        var trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://ProseFlow/Assets/logo.ico"))),
            ToolTipText = "ProseFlow",
            Menu = new NativeMenu
            {
                Items =
                {
                    openItem,
                    new NativeMenuItemSeparator(),
                    toggleModelItem,
                    setProviderSubMenu,
                    new NativeMenuItemSeparator(),
                    downloadsItem,
                    new NativeMenuItemSeparator(),
                    quitItem
                }
            }
        };

        // Open settings on left-click
        trayIcon.Clicked += (_, _) => trayVm.OpenSettingsCommand.Execute(null);

        return trayIcon;
    }

    private void SubscribeToAppEvents()
    {
        if (Services is null) return;
        var notificationService = Services.GetRequiredService<NotificationService>();
        var dialogService = Services.GetRequiredService<IDialogService>();

        AppEvents.ShowNotificationRequested += (message, type) =>
            Dispatcher.UIThread.Post(() => notificationService.Show(message, type));

        AppEvents.ShowResultWindowAndAwaitRefinement += data =>
        {
            // This must be run on the UI thread.
            return Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var viewModel = new ResultViewModel(data);
                var window = new ResultWindow { DataContext = viewModel };
                window.Show();
                return await viewModel.CompletionSource.Task;
            });
        };

        AppEvents.ShowFloatingMenuRequested += async (actions, context) =>
        {
            var providerSettings = await Services.GetRequiredService<SettingsService>().GetProviderSettingsAsync();
            var viewModel = new FloatingActionMenuViewModel(actions, providerSettings, context);
            Dispatcher.UIThread.Post(() =>
            {
                var window = new FloatingActionMenuWindow
                {
                    DataContext = viewModel,
                    ShowActivated = true,
                    Topmost = true,
                    Focusable = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowState = WindowState.Normal,
                };
                window.Show();
            });

            return await viewModel.WaitForSelectionAsync();
        };

        AppEvents.ResolveConflictsRequested += conflicts => Dispatcher.UIThread.InvokeAsync(() => dialogService.ShowConflictResolutionDialogAsync(conflicts));
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Add Core/Infrastructure Services
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var proseFlowDataPath = Path.Combine(appDataPath, "ProseFlow");
        Directory.CreateDirectory(proseFlowDataPath);

        var logPath = Path.Combine(proseFlowDataPath, "logs", "proseflow-.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command",
                LogEventLevel.Warning)
            .MinimumLevel.Override("Velopack", LogEventLevel.Information) // Velopack logging
            .Enrich.FromLogContext()
#if DEBUG
            .WriteTo.Debug()
            .WriteTo.Console()
#endif
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Month)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(proseFlowDataPath, "keys")))
            .SetApplicationName("ProseFlow");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(proseFlowDataPath, "proseflow.db")}"));

        // Add Infrastructure Services
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ApiKeyProtector>();
        services.AddSingleton<UsageTrackingService>();
        services.AddSingleton<LocalModelManagerService>();
        services.AddSingleton<ILocalSessionService, LocalSessionService>();
        services.AddSingleton<IAiProvider, CloudProvider>();
        services.AddSingleton<IAiProvider, LocalProvider>();
        services.AddSingleton<IOsService, OsService>();
        services.AddSingleton<HardwareMonitoringService>();
        services.AddSingleton<LocalNativeManager>();
        
        // Model Download Services
        services.AddSingleton<IModelCatalogService, ModelCatalogService>();
        services.AddSingleton<IDownloadManager, DownloadManager>();
        services.AddSingleton<ILocalModelManagementService, LocalModelManagementService>();

        // Update Service
        services.AddSingleton<IUpdateService, UpdateService>();

        // Add Application Services
        services.AddSingleton<ActionOrchestrationService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ActionManagementService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<HistoryService>();
        services.AddScoped<CloudProviderManagementService>();
        services.AddSingleton<IPresetService, PresetService>();

        // Add Platform-Specific Services
        if (OperatingSystem.IsLinux())
            services.AddSingleton<IActiveWindowTracker, LinuxActiveWindowTracker>();
        else if (OperatingSystem.IsWindows())
            services.AddSingleton<IActiveWindowTracker, WindowsActiveWindowTracker>();
        else if (OperatingSystem.IsMacOS())
            services.AddSingleton<IActiveWindowTracker, MacOsActiveWindowTracker>();
        else
            services.AddSingleton<IActiveWindowTracker, DefaultActiveWindowTracker>();
        
        // Add UI Services
        services.AddSingleton<DialogManager>();
        services.AddSingleton<ToastManager>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<IDialogService, DialogService>();

        // Add ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<TrayIconViewModel>();

        // Dashboard ViewModels
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<OverviewDashboardViewModel>();
        services.AddTransient<CloudDashboardViewModel>();
        services.AddTransient<LocalDashboardViewModel>();

        // Other Page ViewModels
        services.AddTransient<ActionsViewModel>();
        services.AddTransient<ProvidersViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<HistoryViewModel>();
        services.AddTransient<AboutViewModel>();
        
        // Download Management ViewModels
        services.AddTransient<DownloadsPopupViewModel>();
        services.AddTransient<DownloadTaskViewModel>();
        services.AddTransient<AvailableModelViewModel>();
        services.AddTransient<LocalModelViewModel>();

        // Editor/Dialog ViewModels
        services.AddTransient<ActionEditorViewModel>();
        services.AddTransient<CloudProviderEditorViewModel>();
        services.AddTransient<InputDialogViewModel>();
        services.AddTransient<CustomModelImportViewModel>();
        services.AddTransient<ConflictResolutionViewModel>();
        services.AddTransient<ModelLibraryViewModel>();
        
        // Onboarding ViewModels
        services.AddTransient<OnboardingViewModel>();
        services.AddTransient<CloudOnboardingViewModel>();
        services.AddTransient<HotkeyTutorialViewModel>();


        return services.BuildServiceProvider();
    }

    private void OnApplicationExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (Services is null) return;

        var logger = Services.GetService<ILogger<App>>();
        logger?.LogInformation("Application exit requested. Cleaning up resources...");

        // Dispose the MainViewModel, which will cascade disposals down the ViewModel tree
        if (sender is IClassicDesktopStyleApplicationLifetime { MainWindow.DataContext: IDisposable disposable })
            disposable.Dispose();

        // Dispose singleton infrastructure services
        Services.GetService<HardwareMonitoringService>()?.Dispose();
        Services.GetService<IOsService>()?.Dispose();
        Services.GetService<LocalModelManagerService>()?.UnloadModel();

        logger?.LogInformation("Cleanup complete. Application will now exit.");
    }
}