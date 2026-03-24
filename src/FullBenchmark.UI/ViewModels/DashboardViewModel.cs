using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullBenchmark.Application.Services;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using System.Collections.ObjectModel;

namespace FullBenchmark.UI.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject, INavigatedTo
{
    private readonly SystemInfoService  _systemInfo;
    private readonly HistoryService     _history;
    private readonly ComparisonService  _comparison;

    public DashboardViewModel(
        SystemInfoService  systemInfo,
        HistoryService     history,
        ComparisonService  comparison)
    {
        _systemInfo = systemInfo;
        _history    = history;
        _comparison = comparison;
    }

    // ── Machine info ───────────────────────────────────────────────────────
    [ObservableProperty] private string _machineName     = "Loading…";
    [ObservableProperty] private string _cpuModel        = "—";
    [ObservableProperty] private string _ramTotal        = "—";
    [ObservableProperty] private string _storageInfo     = "—";
    [ObservableProperty] private string _gpuModel        = "—";
    [ObservableProperty] private string _osName          = "—";
    [ObservableProperty] private string _osBuild         = "—";

    // ── Latest scores ──────────────────────────────────────────────────────
    [ObservableProperty] private double _overallScore;
    [ObservableProperty] private double? _cpuScore;
    [ObservableProperty] private double? _memoryScore;
    [ObservableProperty] private double? _diskScore;
    [ObservableProperty] private double? _gpuScore;
    [ObservableProperty] private ScoringBadge _overallBadge = ScoringBadge.Unknown;
    [ObservableProperty] private bool _hasScores;
    [ObservableProperty] private string _lastRunDate = "No runs yet";

    // ── Comparison ─────────────────────────────────────────────────────────
    [ObservableProperty] private string _comparisonSummary = "Run a benchmark to compare your device.";
    [ObservableProperty] private ObservableCollection<ComparisonDevice> _nearbyDevices = new();

    // ── State ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public void OnNavigatedTo() => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading    = true;
        ErrorMessage = null;
        try
        {
            // Machine info
            if (_systemInfo.IsInitialized)
            {
                MachineName = _systemInfo.CurrentProfile?.MachineName ?? Environment.MachineName;
                var hw = _systemInfo.LatestHardware;
                if (hw is not null)
                {
                    CpuModel = hw.CpuModel;
                    RamTotal = hw.RamTotalBytes > 0
                        ? $"{hw.RamTotalBytes / 1024.0 / 1024 / 1024:F0} GB"
                        : "—";
                    GpuModel = hw.Gpus.Count > 0 ? hw.Gpus[0].Name : "—";
                    var systemDisk = hw.Disks.FirstOrDefault(d => d.IsSystemDisk);
                    StorageInfo = systemDisk is not null
                        ? $"{systemDisk.Model} ({systemDisk.TotalBytes / 1024.0 / 1024 / 1024:F0} GB {systemDisk.MediaType ?? ""})"
                        : hw.Disks.Count > 0 ? $"{hw.Disks[0].Model}" : "—";
                }
                var os = _systemInfo.LatestOs;
                if (os is not null)
                {
                    OsName  = os.OsName;
                    OsBuild = $"Build {os.OsBuildNumber}";
                }
            }

            // Latest session scores
            if (_systemInfo.CurrentProfile is not null)
            {
                var sessions = await _history.GetRecentSessionsAsync(
                    _systemInfo.CurrentProfile.Id, limit: 1);

                if (sessions.Count > 0)
                {
                    var latest = sessions[0];
                    OverallScore = latest.OverallScore ?? 0;
                    CpuScore     = latest.CpuScore;
                    MemoryScore  = latest.MemoryScore;
                    DiskScore    = latest.DiskScore;
                    GpuScore     = latest.GpuScore;
                    HasScores    = latest.OverallScore.HasValue;
                    LastRunDate  = latest.StartedAt.LocalDateTime.ToString("MMM d, yyyy  HH:mm");
                    OverallBadge = ClassifyBadge(OverallScore);

                    if (HasScores)
                    {
                        ComparisonSummary = await _comparison.GetComparisonSummaryAsync(OverallScore);
                        var nearby = await _comparison.GetNearScoreAsync(OverallScore, limit: 5);
                        NearbyDevices = new ObservableCollection<ComparisonDevice>(nearby);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static ScoringBadge ClassifyBadge(double score) => score switch
    {
        > 950  => ScoringBadge.Outstanding,
        > 800  => ScoringBadge.Excellent,
        > 600  => ScoringBadge.VeryGood,
        > 400  => ScoringBadge.Good,
        > 200  => ScoringBadge.Average,
        > 0    => ScoringBadge.BelowAverage,
        _      => ScoringBadge.Unknown
    };
}
