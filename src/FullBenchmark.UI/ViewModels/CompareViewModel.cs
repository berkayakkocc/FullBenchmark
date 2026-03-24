using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullBenchmark.Application.Services;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using System.Collections.ObjectModel;

namespace FullBenchmark.UI.ViewModels;

public sealed partial class CompareViewModel : ObservableObject, INavigatedTo
{
    private readonly ComparisonService _comparison;
    private readonly HistoryService    _history;
    private readonly SystemInfoService _systemInfo;

    public CompareViewModel(
        ComparisonService  comparison,
        HistoryService     history,
        SystemInfoService  systemInfo)
    {
        _comparison = comparison;
        _history    = history;
        _systemInfo = systemInfo;

        Categories = Enum.GetValues<DeviceCategory>().ToList();
    }

    [ObservableProperty] private ObservableCollection<ComparisonDevice> _devices = new();
    [ObservableProperty] private ObservableCollection<ComparisonDevice> _nearbyDevices = new();
    [ObservableProperty] private double  _myOverallScore;
    [ObservableProperty] private string  _comparisonSummary = string.Empty;
    [ObservableProperty] private double? _percentileRank;
    [ObservableProperty] private bool    _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private DeviceCategory? _selectedCategory;
    [ObservableProperty] private string  _filterText = string.Empty;

    public List<DeviceCategory> Categories { get; }

    public void OnNavigatedTo() => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            // Get current machine's latest score
            if (_systemInfo.CurrentProfile is not null)
            {
                var sessions = await _history.GetRecentSessionsAsync(
                    _systemInfo.CurrentProfile.Id, limit: 1);
                if (sessions.Count > 0 && sessions[0].OverallScore.HasValue)
                {
                    MyOverallScore    = sessions[0].OverallScore!.Value;
                    ComparisonSummary = await _comparison.GetComparisonSummaryAsync(MyOverallScore);
                    PercentileRank    = await _comparison.GetPercentileAsync(MyOverallScore);

                    var nearby = await _comparison.GetNearScoreAsync(MyOverallScore, limit: 6);
                    NearbyDevices = new ObservableCollection<ComparisonDevice>(nearby);
                }
            }

            await RefreshDeviceListAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load comparison data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FilterByCategory(DeviceCategory? category)
    {
        SelectedCategory = category;
        await RefreshDeviceListAsync();
    }

    private async Task RefreshDeviceListAsync()
    {
        IReadOnlyList<ComparisonDevice> devices = SelectedCategory.HasValue
            ? await _comparison.GetByCategoryAsync(SelectedCategory.Value)
            : await _comparison.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(FilterText))
            devices = devices.Where(d =>
                d.DeviceName.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                d.CpuModel.Contains(FilterText, StringComparison.OrdinalIgnoreCase)).ToList();

        Devices = new ObservableCollection<ComparisonDevice>(devices);
    }

    partial void OnFilterTextChanged(string value) => _ = RefreshDeviceListAsync();
}
