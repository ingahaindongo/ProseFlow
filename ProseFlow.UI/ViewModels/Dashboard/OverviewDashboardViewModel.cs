using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ProseFlow.Application.DTOs;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.Core.Models;
using ProseFlow.Infrastructure.Services.AiProviders.Local;
using ProseFlow.UI.Services.Logging;
using SkiaSharp;
using Timer = System.Timers.Timer;

namespace ProseFlow.UI.ViewModels.Dashboard;

public partial class TrackedActionViewModel(TrackedAction action) : ObservableObject
{
    public TrackedAction Action { get; } = action;

    [ObservableProperty]
    private string _elapsedTime = "0s";

    public void UpdateElapsedTime()
    {
        var elapsed = DateTime.UtcNow - Action.StartTime;
        ElapsedTime = elapsed.TotalMinutes >= 1 ? $"{elapsed.Minutes}m {elapsed.Seconds}s" : $"{elapsed.Seconds}s";
    }
}

public partial class OverviewDashboardViewModel : DashboardViewModelBase, IDisposable
{
    public override string Title => "Overview";

    private readonly DashboardService _dashboardService;
    private readonly HistoryService _historyService;
    private readonly LocalModelManagerService _modelManager;
    private readonly SettingsService _settingsService;
    private readonly ApplicationLogCollectorService _logCollectorService;
    private readonly IBackgroundActionTrackerService _trackerService;
    private readonly Timer _elapsedTimeTimer;

    // KPI Properties
    [ObservableProperty] private int _totalActionsExecuted;
    [ObservableProperty] private long _totalTokens;

    // Status Widget Properties
    [ObservableProperty] private ModelStatus _localModelStatus;
    [ObservableProperty] private string _localModelName = "No Model Selected";
    [ObservableProperty] private bool _isModelLoading;

    // Recent Activity
    public ObservableCollection<HistoryEntry> RecentActivity { get; } = [];
    
    // Active Processes
    public ObservableCollection<TrackedActionViewModel> ActiveActions { get; } = [];
    
    // Application Log Console
    public ObservableCollection<LogEntry> ApplicationLogs { get; } = [];

    public OverviewDashboardViewModel(
        DashboardService dashboardService,
        HistoryService historyService,
        LocalModelManagerService modelManager,
        SettingsService settingsService,
        ApplicationLogCollectorService logCollectorService,
        IBackgroundActionTrackerService trackerService)
    {
        _dashboardService = dashboardService;
        _historyService = historyService;
        _modelManager = modelManager;
        _settingsService = settingsService;
        _logCollectorService = logCollectorService;
        _trackerService = trackerService;
        
        // Initialize the collection and subscribe to events for live updates
        foreach (var action in _trackerService.GetActiveActions())
        {
            ActiveActions.Add(new TrackedActionViewModel(action));
        }
        _trackerService.ActionAdded += OnActionAdded;
        _trackerService.ActionRemoved += OnActionRemoved;

        _modelManager.StateChanged += OnModelStateChanged;
        _logCollectorService.LogMessageReceived += OnApplicationLogMessageReceived;
        
        OnModelStateChanged(); // Set initial state
        foreach (var log in _logCollectorService.GetLogHistory()) ApplicationLogs.Add(log);
        
        // Timer to update elapsed time for active actions
        _elapsedTimeTimer = new Timer(1000);
        _elapsedTimeTimer.Elapsed += (_, _) => UpdateElapsedTimes();
        _elapsedTimeTimer.AutoReset = true;
        _elapsedTimeTimer.Start();
    }
    
    private void OnActionAdded(TrackedAction action)
    {
        Dispatcher.UIThread.Post(() => ActiveActions.Add(new TrackedActionViewModel(action)));
    }
    
    private void OnActionRemoved(TrackedAction action)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var actionToRemove = ActiveActions.FirstOrDefault(a => a.Action.Id == action.Id);
            if (actionToRemove != null)
            {
                ActiveActions.Remove(actionToRemove);
            }
        });
    }
    
    private void UpdateElapsedTimes()
    {
        foreach (var vm in ActiveActions)
        {
            vm.UpdateElapsedTime();
        }
    }

    protected override async Task LoadDataAsync()
    {
        IsLoading = true;
        var (startDate, endDate) = GetDateRange();

        // Fetch data in parallel
        var allHistoryTask = _dashboardService.GetHistoryByDateRangeAsync(startDate, endDate);
        var recentHistoryTask = _historyService.GetRecentHistoryAsync(5);
        var settingsTask = _settingsService.GetProviderSettingsAsync();

        await Task.WhenAll(allHistoryTask, recentHistoryTask, settingsTask);

        var allHistory = await allHistoryTask;

        // Update KPIs
        TotalActionsExecuted = allHistory.Count;
        TotalTokens = allHistory.Sum(h => h.PromptTokens + h.CompletionTokens);

        // Update Recent Activity
        RecentActivity.Clear();
        foreach (var entry in await recentHistoryTask) RecentActivity.Add(entry);

        // Update Chart with advanced hover logic
        UpdateUsageChart(allHistory);

        // Update Local Model Name
        var settings = await settingsTask;
        LocalModelName = string.IsNullOrWhiteSpace(settings.LocalModelPath)
            ? "No Model Selected"
            : Path.GetFileName(settings.LocalModelPath);

        IsLoading = false;
    }

    private void UpdateUsageChart(List<HistoryEntry> history)
    {
        var actionsByDay = history
            .GroupBy(h => h.Timestamp.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var uniqueActionNames = history.Select(h => h.ActionName).Distinct().ToList();

        var series = uniqueActionNames.Select(actionName => new StackedColumnSeries<long>
        {
            Name = actionName,
            Values = actionsByDay.Select(day =>
                day.Where(h => h.ActionName == actionName)
                    .Sum(h => h.PromptTokens + h.CompletionTokens)
            ).ToList()
        });

        Series = series.ToArray<ISeries>();

        XAxes =
        [
            new Axis
            {
                Labels = actionsByDay.Select(g => g.Key.ToString("MMM d")).ToList(),
                TextSize = 12,
                NamePaint = Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Dark ? new SolidColorPaint(SKColors.White) : new SolidColorPaint(SKColors.Black),
                SeparatorsPaint = new SolidColorPaint(SKColors.Transparent)
            }
        ];

        YAxes =
        [
            new Axis
            {
                Name = "Total Tokens",
                NameTextSize = 12,
                TextSize = 12,
                NamePaint = Avalonia.Application.Current?.ActualThemeVariant == ThemeVariant.Dark ? new SolidColorPaint(SKColors.White) : new SolidColorPaint(SKColors.Black),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 0.5f }
            }
        ];
    }

    private void OnModelStateChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            LocalModelStatus = _modelManager.Status;
            IsModelLoading = _modelManager.Status == ModelStatus.Loading;
        });
    }

    private void OnApplicationLogMessageReceived(LogEntry logEntry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ApplicationLogs.Add(logEntry);
            // Trim the collection to prevent UI performance degradation
            if (ApplicationLogs.Count > 500) ApplicationLogs.RemoveAt(0);
        });
    }

    [RelayCommand]
    private async Task ToggleLocalModel()
    {
        if (_modelManager.IsLoaded)
        {
            _modelManager.UnloadModel();
        }
        else
        {
            var settings = await _settingsService.GetProviderSettingsAsync();
            await _modelManager.LoadModelAsync(settings);
        }
    }
    
    [RelayCommand]
    private void CancelAction(Guid id)
    {
        _trackerService.RequestCancellation(id);
    }

    public void Dispose()
    {
        _elapsedTimeTimer.Stop();
        _elapsedTimeTimer.Dispose();
        _modelManager.StateChanged -= OnModelStateChanged;
        _logCollectorService.LogMessageReceived -= OnApplicationLogMessageReceived;
        _trackerService.ActionAdded -= OnActionAdded;
        _trackerService.ActionRemoved -= OnActionRemoved;
        GC.SuppressFinalize(this);
    }
}