using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace ProseFlow.UI.ViewModels.Dashboard;

public abstract partial class DashboardViewModelBase : ViewModelBase
{
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _selectedDateRange = "Last 7 Days";
    public List<string> DateRanges { get; } = ["Today", "Last 7 Days", "Last 30 Days", "This Month", "All Time"];

    // Chart Properties
    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private Axis[] _xAxes = [new()];
    [ObservableProperty] private Axis[] _yAxes = [new()];
    
    partial void OnSelectedDateRangeChanged(string value)
    {
        _ = LoadDataAsync();
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadDataAsync();
    }

    protected abstract Task LoadDataAsync();
    
    protected (DateTime Start, DateTime End) GetDateRange()
    {
        var now = DateTime.UtcNow;
        return SelectedDateRange switch
        {
            "Today" => (now.Date, now.Date.AddDays(1).AddTicks(-1)),
            "Last 30 Days" => (now.AddDays(-30).Date, now.Date.AddDays(1).AddTicks(-1)),
            "This Month" => (new DateTime(now.Year, now.Month, 1), now.Date.AddDays(1).AddTicks(-1)),
            "All Time" => (DateTime.MinValue, DateTime.MaxValue),
            _ => (now.AddDays(-7).Date, now.Date.AddDays(1).AddTicks(-1)) // Default to "Last 7 Days"
        };
    }
}