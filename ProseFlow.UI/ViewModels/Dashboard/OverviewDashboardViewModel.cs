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
using ProseFlow.Application.Services;
using ProseFlow.Core.Models;
using ProseFlow.Infrastructure.Services.AiProviders.Local;
using SkiaSharp;

namespace ProseFlow.UI.ViewModels.Dashboard;

public partial class OverviewDashboardViewModel : DashboardViewModelBase, IDisposable
{
    public override string Title => "Overview";

    private readonly DashboardService _dashboardService;
    private readonly HistoryService _historyService;
    private readonly LocalModelManagerService _modelManager;
    private readonly SettingsService _settingsService;

    // KPI Properties
    [ObservableProperty] private int _totalActionsExecuted;
    [ObservableProperty] private long _totalCloudTokens;
    [ObservableProperty] private long _totalLocalTokens;

    // Status Widget Properties
    [ObservableProperty] private ModelStatus _localModelStatus;
    [ObservableProperty] private string _localModelName = "No Model Selected";
    [ObservableProperty] private bool _isModelLoading;

    // Recent Activity
    public ObservableCollection<HistoryEntry> RecentActivity { get; } = [];

    public OverviewDashboardViewModel(
        DashboardService dashboardService,
        HistoryService historyService,
        LocalModelManagerService modelManager,
        SettingsService settingsService)
    {
        _dashboardService = dashboardService;
        _historyService = historyService;
        _modelManager = modelManager;
        _settingsService = settingsService;

        _modelManager.StateChanged += OnModelStateChanged;
        OnModelStateChanged(); // Set initial state
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
        TotalCloudTokens = allHistory.Where(h => h.ProviderUsed == "Cloud")
            .Sum(h => h.PromptTokens + h.CompletionTokens);
        TotalLocalTokens = allHistory.Where(h => h.ProviderUsed == "Local")
            .Sum(h => h.PromptTokens + h.CompletionTokens);

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

    public void Dispose()
    {
        _modelManager.StateChanged -= OnModelStateChanged;
        GC.SuppressFinalize(this);
    }
}