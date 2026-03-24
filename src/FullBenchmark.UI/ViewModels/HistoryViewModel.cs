using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullBenchmark.Application.Services;
using FullBenchmark.Contracts.Domain.Entities;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace FullBenchmark.UI.ViewModels;

public sealed partial class HistoryViewModel : ObservableObject, INavigatedTo
{
    private readonly HistoryService    _history;
    private readonly SystemInfoService _systemInfo;

    public HistoryViewModel(HistoryService history, SystemInfoService systemInfo)
    {
        _history    = history;
        _systemInfo = systemInfo;
    }

    [ObservableProperty] private ObservableCollection<BenchmarkSession> _sessions = new();
    [ObservableProperty] private BenchmarkSession? _selectedSession;
    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private string? _errorMessage;

    // Trend chart
    public ISeries[]  TrendSeries { get; private set; } = [];
    public Axis[]     TrendXAxis  { get; } = [new Axis { IsVisible = false }];
    public Axis[]     TrendYAxis  { get; } = [new Axis { MinLimit = 0, MaxLimit = 1000 }];

    public void OnNavigatedTo() => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_systemInfo.CurrentProfile is null) return;
        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            var sessions = await _history.GetRecentSessionsAsync(
                _systemInfo.CurrentProfile.Id, limit: 50);
            Sessions = new ObservableCollection<BenchmarkSession>(sessions);

            // Build trend chart
            var trend = await _history.GetTrendAsync(
                _systemInfo.CurrentProfile.Id, limit: 50);

            BuildTrendChart(trend);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load history: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildTrendChart(IReadOnlyList<HistoricalTrendPoint> points)
    {
        if (points.Count == 0)
        {
            TrendSeries = [];
            OnPropertyChanged(nameof(TrendSeries));
            return;
        }

        var overallPts = points.Select(p => new ObservableValue(p.OverallScore)).ToList();
        var cpuPts     = points.Select(p => new ObservableValue(p.CpuScore ?? 0)).ToList();
        var memPts     = points.Select(p => new ObservableValue(p.MemoryScore ?? 0)).ToList();
        var diskPts    = points.Select(p => new ObservableValue(p.DiskScore ?? 0)).ToList();

        TrendSeries =
        [
            new LineSeries<ObservableValue>
            {
                Values = overallPts,
                Name   = "Overall",
                Stroke = new SolidColorPaint(new SKColor(0x78, 0x57, 0xFF)) { StrokeThickness = 2 },
                GeometrySize = 6,
                LineSmoothness = 0.5
            },
            new LineSeries<ObservableValue>
            {
                Values = cpuPts,
                Name   = "CPU",
                Stroke = new SolidColorPaint(new SKColor(0xFF, 0x4D, 0x6A)) { StrokeThickness = 1.5f },
                GeometrySize = 4,
                LineSmoothness = 0.5
            },
            new LineSeries<ObservableValue>
            {
                Values = memPts,
                Name   = "Memory",
                Stroke = new SolidColorPaint(new SKColor(0x4A, 0xB0, 0xFF)) { StrokeThickness = 1.5f },
                GeometrySize = 4,
                LineSmoothness = 0.5
            },
            new LineSeries<ObservableValue>
            {
                Values = diskPts,
                Name   = "Disk",
                Stroke = new SolidColorPaint(new SKColor(0xF5, 0xA6, 0x23)) { StrokeThickness = 1.5f },
                GeometrySize = 4,
                LineSmoothness = 0.5
            }
        ];
        OnPropertyChanged(nameof(TrendSeries));
    }
}
