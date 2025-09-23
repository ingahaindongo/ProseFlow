using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ProseFlow.Application.DTOs.Dashboard;
using ProseFlow.Application.Services;
using SkiaSharp;

namespace ProseFlow.UI.ViewModels.Dashboard;

public partial class CloudDashboardViewModel(DashboardService dashboardService) : DashboardViewModelBase
{
    public override string Title => "Cloud";

    // KPIs
    [ObservableProperty] private long _totalCloudTokens;
    [ObservableProperty] private int _totalCloudActions;
    [ObservableProperty] private string _mostUsedProvider = "N/A";
    [ObservableProperty] private string _inferenceSpeed = "N/A";

    
    // Grids
    public ObservableCollection<ActionUsageDto> TopCloudActions { get; } = [];
    public ObservableCollection<ProviderPerformanceDto> ProviderPerformance { get; } = [];

    protected override async Task LoadDataAsync()
    {
        IsLoading = true;
        var (startDate, endDate) = GetDateRange();

        // Fetch data in parallel
        var dailyUsageTask = dashboardService.GetDailyUsageAsync(startDate, endDate, "Cloud");
        var topActionsTask = dashboardService.GetTopActionsAsync(startDate, endDate, "Cloud");
        var performanceTask = dashboardService.GetCloudProviderPerformanceAsync(startDate, endDate);

        await Task.WhenAll(dailyUsageTask, topActionsTask, performanceTask);

        var dailyUsage = await dailyUsageTask;
        var performance = await performanceTask;
        
        // Update KPIs
        TotalCloudTokens = dailyUsage.Sum(d => d.PromptTokens + d.CompletionTokens);
        TotalCloudActions = await dashboardService.GetTotalUsageCountAsync(startDate, endDate, "Cloud");
        MostUsedProvider = performance.FirstOrDefault()?.ProviderName ?? "N/A";
        InferenceSpeed = $"{dailyUsage.Average(d => d.TokensPerSecond):F2} T/s";

        // Update Grids
        TopCloudActions.Clear();
        foreach (var action in await topActionsTask) TopCloudActions.Add(action);

        ProviderPerformance.Clear();
        foreach (var p in performance) ProviderPerformance.Add(p);

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
                Fill = new SolidColorPaint(SKColor.Parse("#3b82f6")),
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
}