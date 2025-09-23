using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ProseFlow.UI.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace ProseFlow.UI.ViewModels.Dashboard;

public partial class DashboardViewModel : ViewModelBase, IDisposable
{
    public override string Title => "Dashboard";
    public override IconSymbol Icon => IconSymbol.LayoutDashboard;

    [ObservableProperty]
    private int _selectedTabIndex;

    public ObservableCollection<DashboardViewModelBase> Tabs { get; } = [];

    public DashboardViewModel(IServiceProvider serviceProvider)
    {
        // Populate tabs
        Tabs.Add(serviceProvider.GetRequiredService<OverviewDashboardViewModel>());
        Tabs.Add(serviceProvider.GetRequiredService<CloudDashboardViewModel>());
        Tabs.Add(serviceProvider.GetRequiredService<LocalDashboardViewModel>());
    }

    public override async Task OnNavigatedToAsync()
    {
        // When the main dashboard page is navigated to, load data for the currently selected tab.
        if (Tabs.Count > SelectedTabIndex) await Tabs[SelectedTabIndex].OnNavigatedToAsync();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        // When the tab changes, load data for the new tab.
        if (Tabs.Count > value) _ = Tabs[value].OnNavigatedToAsync();
    }

    public void Dispose()
    {
        foreach (var tab in Tabs)
            if (tab is IDisposable disposableTab)
                disposableTab.Dispose();

        GC.SuppressFinalize(this);
    }
}