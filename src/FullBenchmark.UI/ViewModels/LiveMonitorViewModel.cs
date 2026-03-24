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
using System.Windows;
using WpfApp = System.Windows.Application;

namespace FullBenchmark.UI.ViewModels;

public sealed partial class LiveMonitorViewModel : ObservableObject, INavigatedTo, IDisposable
{
    private readonly TelemetryOrchestrator _telemetry;
    private readonly SystemInfoService     _systemInfo;
    private bool _disposed;

    // Ring-buffer collections for charts (100 points each)
    private const int MaxPoints = 100;
    private readonly ObservableCollection<ObservableValue> _cpuSeries    = [];
    private readonly ObservableCollection<ObservableValue> _ramSeries     = [];
    private readonly ObservableCollection<ObservableValue> _diskSeries    = [];
    private readonly ObservableCollection<ObservableValue> _networkSeries = [];
    private readonly ObservableCollection<ObservableValue> _gpuSeries     = [];

    public LiveMonitorViewModel(
        TelemetryOrchestrator telemetry,
        SystemInfoService     systemInfo)
    {
        _telemetry  = telemetry;
        _systemInfo = systemInfo;

        InitCharts();
        _telemetry.SampleArrived += OnSampleArrived;
    }

    // ── Live readings ──────────────────────────────────────────────────────
    [ObservableProperty] private double  _cpuUsage;
    [ObservableProperty] private double  _ramUsedGb;
    [ObservableProperty] private double  _ramTotalGb;
    [ObservableProperty] private double  _ramUsagePercent;
    [ObservableProperty] private double  _diskReadMbps;
    [ObservableProperty] private double  _diskWriteMbps;
    [ObservableProperty] private double  _diskActivePercent;
    [ObservableProperty] private double  _netSentMbps;
    [ObservableProperty] private double  _netRecvMbps;
    [ObservableProperty] private double? _cpuTempC;
    [ObservableProperty] private double? _gpuUsage;
    [ObservableProperty] private double? _gpuTempC;
    [ObservableProperty] private double? _batteryPercent;
    [ObservableProperty] private bool    _isOnAcPower = true;

    // ── State ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private string _statusText = "Not running";

    // ── Charts ─────────────────────────────────────────────────────────────
    public ISeries[]  CpuChartSeries     { get; private set; } = [];
    public ISeries[]  RamChartSeries     { get; private set; } = [];
    public ISeries[]  DiskChartSeries    { get; private set; } = [];
    public ISeries[]  NetworkChartSeries { get; private set; } = [];

    public Axis[] TimeAxis { get; } =
    [
        new Axis { Labels = null, IsVisible = false }
    ];
    public Axis[] PercentAxis { get; } =
    [
        new Axis { MinLimit = 0, MaxLimit = 100, IsVisible = false }
    ];
    public Axis[] BytesAxis { get; } =
    [
        new Axis { MinLimit = 0, IsVisible = false }
    ];

    // ── Commands ───────────────────────────────────────────────────────────

    public void OnNavigatedTo()
    {
        if (!IsRunning) StartCommand.Execute(null);
    }

    [RelayCommand]
    private void Start()
    {
        if (IsRunning || _systemInfo.CurrentProfile is null) return;
        _telemetry.Start(_systemInfo.CurrentProfile.Id, TimeSpan.FromSeconds(1));
        IsRunning  = true;
        StatusText = "Live — updating every second";
    }

    [RelayCommand]
    private void Stop()
    {
        _telemetry.Stop();
        IsRunning  = false;
        StatusText = "Stopped";
    }

    // ── Sample handler ─────────────────────────────────────────────────────

    private void OnSampleArrived(object? _, TelemetrySample s)
    {
        WpfApp.Current.Dispatcher.BeginInvoke(() =>
        {
            // CPU
            CpuUsage = s.CpuUsagePercent;
            Push(_cpuSeries, CpuUsage);

            // RAM
            {
                long total = s.MemoryUsedBytes + s.MemoryAvailableBytes;
                RamTotalGb       = total / 1e9;
                RamUsedGb        = s.MemoryUsedBytes / 1e9;
                RamUsagePercent  = total > 0 ? s.MemoryUsedBytes * 100.0 / total : 0;
                Push(_ramSeries, RamUsagePercent);
            }

            // Disk
            DiskReadMbps       = s.DiskReadBytesPerSec  / 1e6;
            DiskWriteMbps      = s.DiskWriteBytesPerSec / 1e6;
            DiskActivePercent  = s.DiskActiveTimePercent ?? 0;
            Push(_diskSeries, DiskActivePercent);

            // Network
            NetSentMbps = s.NetworkSentBytesPerSec     / 1e6;
            NetRecvMbps = s.NetworkReceivedBytesPerSec / 1e6;
            Push(_networkSeries, NetRecvMbps);

            // Sensors
            CpuTempC    = s.CpuTemperatureCelsius;
            GpuUsage    = s.GpuUsagePercent;
            GpuTempC    = s.GpuTemperatureCelsius;
            if (s.GpuUsagePercent.HasValue) Push(_gpuSeries, s.GpuUsagePercent.Value);

            // Power
            IsOnAcPower    = s.IsOnAcPower ?? true;
            BatteryPercent = s.BatteryChargePercent;
        });
    }

    // ── Chart helpers ──────────────────────────────────────────────────────

    private void InitCharts()
    {
        // Pre-fill with zeroes
        for (int i = 0; i < MaxPoints; i++)
        {
            _cpuSeries.Add(new ObservableValue(0));
            _ramSeries.Add(new ObservableValue(0));
            _diskSeries.Add(new ObservableValue(0));
            _networkSeries.Add(new ObservableValue(0));
            _gpuSeries.Add(new ObservableValue(0));
        }

        CpuChartSeries =
        [
            new LineSeries<ObservableValue>
            {
                Values = _cpuSeries,
                Stroke = new SolidColorPaint(new SKColor(0x78, 0x57, 0xFF)) { StrokeThickness = 2 },
                Fill   = new LinearGradientPaint(
                    new[] { new SKColor(0x78, 0x57, 0xFF, 80), new SKColor(0x78, 0x57, 0xFF, 0) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        ];

        RamChartSeries =
        [
            new LineSeries<ObservableValue>
            {
                Values = _ramSeries,
                Stroke = new SolidColorPaint(new SKColor(0x4A, 0xB0, 0xFF)) { StrokeThickness = 2 },
                Fill   = new LinearGradientPaint(
                    new[] { new SKColor(0x4A, 0xB0, 0xFF, 80), new SKColor(0x4A, 0xB0, 0xFF, 0) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        ];

        DiskChartSeries =
        [
            new LineSeries<ObservableValue>
            {
                Values = _diskSeries,
                Stroke = new SolidColorPaint(new SKColor(0xF5, 0xA6, 0x23)) { StrokeThickness = 2 },
                Fill   = new LinearGradientPaint(
                    new[] { new SKColor(0xF5, 0xA6, 0x23, 80), new SKColor(0xF5, 0xA6, 0x23, 0) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        ];

        NetworkChartSeries =
        [
            new LineSeries<ObservableValue>
            {
                Values = _networkSeries,
                Stroke = new SolidColorPaint(new SKColor(0x2B, 0xDB, 0x8C)) { StrokeThickness = 2 },
                Fill   = new LinearGradientPaint(
                    new[] { new SKColor(0x2B, 0xDB, 0x8C, 80), new SKColor(0x2B, 0xDB, 0x8C, 0) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            }
        ];
    }

    private static void Push(ObservableCollection<ObservableValue> series, double value)
    {
        series.RemoveAt(0);
        series.Add(new ObservableValue(value));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _telemetry.SampleArrived -= OnSampleArrived;
    }
}
