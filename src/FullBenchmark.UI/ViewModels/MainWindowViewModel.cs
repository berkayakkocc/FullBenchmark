using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullBenchmark.UI.Models;
using System.Collections.ObjectModel;

namespace FullBenchmark.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private NavigationItem? _selectedNavItem;

    public ObservableCollection<NavigationItem> NavItems { get; }

    public MainWindowViewModel(
        DashboardViewModel    dashboard,
        LiveMonitorViewModel  liveMonitor,
        RunBenchmarkViewModel runBenchmark,
        HistoryViewModel      history,
        CompareViewModel      compare,
        SettingsViewModel     settings)
    {
        NavItems = new ObservableCollection<NavigationItem>
        {
            new() { Label = "Dashboard",     Icon = "⊞", ViewModel = dashboard },
            new() { Label = "Live Monitor",  Icon = "⚡", ViewModel = liveMonitor },
            new() { Label = "Run Benchmark", Icon = "▶",  ViewModel = runBenchmark },
            new() { Label = "History",       Icon = "⏱", ViewModel = history },
            new() { Label = "Compare",       Icon = "≈",  ViewModel = compare },
            new() { Label = "Settings",      Icon = "⚙",  ViewModel = settings },
        };

        // Start on dashboard
        NavigateTo(NavItems[0]);
    }

    [RelayCommand]
    public void NavigateTo(NavigationItem item)
    {
        if (SelectedNavItem is not null)
            SelectedNavItem.IsSelected = false;

        item.IsSelected    = true;
        SelectedNavItem    = item;
        CurrentViewModel   = item.ViewModel;

        // Notify the page it's being shown
        if (item.ViewModel is INavigatedTo nav)
            nav.OnNavigatedTo();
    }
}

/// <summary>Implemented by ViewModels that need to refresh data when navigated to.</summary>
public interface INavigatedTo
{
    void OnNavigatedTo();
}
