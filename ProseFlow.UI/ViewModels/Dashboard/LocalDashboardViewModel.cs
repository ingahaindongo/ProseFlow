using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ProseFlow.Application.DTOs.Dashboard;
using ProseFlow.Application.Services;
using ProseFlow.Core.Models;
using ProseFlow.Infrastructure.Services.AiProviders.Local;
using ProseFlow.Infrastructure.Services.Monitoring;
using SkiaSharp;

namespace ProseFlow.UI.ViewModels.Dashboard;

public partial class LocalDashboardViewModel : DashboardViewModelBase, IDisposable
{
    private readonly DashboardService _dashboardService;
    private readonly HardwareMonitoringService _hardwareMonitoringService;
    private readonly LocalNativeManager _localNativeManager;

    public override string Title => "Local";

    // KPIs
    [ObservableProperty] private long _totalLocalTokens;
    [ObservableProperty] private int _totalLocalActions;

    // Live Hardware Metrics
    [ObservableProperty] private double _cpuUsagePercent;
    [ObservableProperty] private double _ramUsageGb;
    [ObservableProperty] private double _totalRamGb;
    [ObservableProperty] private double _ramUsagePercent;
    [ObservableProperty] private double _gpuUsagePercent;
    [ObservableProperty] private double _vramUsageGb;
    [ObservableProperty] private double _totalVramGb;
    [ObservableProperty] private double _vramFreeGb;
    [ObservableProperty] private double _vramUsagePercent;

    // Inference Metrics
    [ObservableProperty] private string _inferenceSpeed = "N/A";

    // Grid
    public ObservableCollection<ActionUsageDto> TopLocalActions { get; } = [];
    
    // Log Console
    public ObservableCollection<LogEntry> LlmLogs { get; } = [];

    public LocalDashboardViewModel(DashboardService dashboardService, HardwareMonitoringService hardwareMonitoringService, LocalNativeManager localNativeManager)
    {
        _dashboardService = dashboardService;
        _hardwareMonitoringService = hardwareMonitoringService;
        _localNativeManager = localNativeManager;
        
        // Subscribe to hardware updates and set initial state
        _hardwareMonitoringService.MetricsUpdated += OnMetricsUpdated;
        OnMetricsUpdated(_hardwareMonitoringService.GetCurrentMetrics()); // Load initial data
        
        // Subscribe to log updates and set initial state
        _localNativeManager.LogMessageReceived += OnLogMessageReceived;
        foreach (var log in _localNativeManager.GetLogHistory()) LlmLogs.Add(log);
    }

    private void OnMetricsUpdated(HardwareMetrics metrics)
    {
        // Must marshal to UI thread for bindings to update safely
        Dispatcher.UIThread.Post(() =>
        {
            CpuUsagePercent = metrics.CpuUsagePercent;
            RamUsageGb = metrics.RamUsedGb;
            TotalRamGb = metrics.RamTotalGb;
            RamUsagePercent = metrics.RamUsagePercent;
            GpuUsagePercent = metrics.GpuUsagePercent;
            VramUsageGb = metrics.VramUsedGb;
            TotalVramGb = metrics.VramTotalGb;
            VramFreeGb = metrics.VramTotalGb > 0 ? Math.Round(metrics.VramTotalGb - metrics.VramUsedGb, 1) : 0;
            VramUsagePercent = metrics.VramUsagePercent;
        });
    }
    
    private void OnLogMessageReceived(LogEntry logEntry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LlmLogs.Add(logEntry);
            // Trim the collection to prevent UI performance degradation
            if (LlmLogs.Count > 500) LlmLogs.RemoveAt(0);
        });
    }

    protected override async Task LoadDataAsync()
    {
        IsLoading = true;
        var (startDate, endDate) = GetDateRange();

        var dailyUsageTask = _dashboardService.GetDailyUsageAsync(startDate, endDate, "Local");
        var topActionsTask = _dashboardService.GetTopActionsAsync(startDate, endDate, "Local");

        await Task.WhenAll(dailyUsageTask, topActionsTask);

        var dailyUsage = await dailyUsageTask;

        // Update KPIs
        TotalLocalTokens = dailyUsage.Sum(d => d.PromptTokens + d.CompletionTokens);
        TotalLocalActions = await _dashboardService.GetTotalUsageCountAsync(startDate, endDate, "Local");
        InferenceSpeed = $"{dailyUsage.Average(d => d.TokensPerSecond):F2} T/s";

        // Update Grid
        TopLocalActions.Clear();
        foreach (var action in await topActionsTask) TopLocalActions.Add(action);

        // Update Chart
        UpdateUsageChart(dailyUsage);

        IsLoading = false;
    }

    private void UpdateUsageChart(List<DailyUsageDto> dailyUsage)
    {
        Series =
        [
            new ColumnSeries<long>
            {
                Name = "Prompt Tokens",
                Values = dailyUsage.Select(d => d.PromptTokens).ToList(),
                Stroke = null,
                Fill = new SolidColorPaint(SKColor.Parse("#16a34a")), // Green for local
                MaxBarWidth = 40,
                Rx = 4,
                Ry = 4
            },

            new ColumnSeries<long>
            {
                Name = "Completion Tokens",
                Values = dailyUsage.Select(d => d.CompletionTokens).ToList(),
                Stroke = null,
                Fill = new SolidColorPaint(SKColor.Parse("#a8a29e")),
                MaxBarWidth = 40,
                Rx = 4,
                Ry = 4
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels = dailyUsage.Select(d => d.Date.ToString("MMM d")).ToList(),
                LabelsRotation = 0,
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(SKColors.Transparent)
            }
        ];

        YAxes =
        [
            new Axis
            {
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray) { StrokeThickness = 0.5f }
            }
        ];
    }
    
    public void Dispose()
    {
        _hardwareMonitoringService.MetricsUpdated -= OnMetricsUpdated;
        _localNativeManager.LogMessageReceived -= OnLogMessageReceived;
        GC.SuppressFinalize(this);
    }
}