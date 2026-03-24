using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullBenchmark.Application.Services;
using FullBenchmark.Contracts.Benchmarks;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using FullBenchmark.Contracts.Repositories;
using System.Collections.ObjectModel;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace FullBenchmark.UI.ViewModels;

public sealed partial class RunBenchmarkViewModel : ObservableObject, INavigatedTo
{
    private readonly BenchmarkOrchestrator _orchestrator;
    private readonly SystemInfoService     _systemInfo;
    private readonly IBenchmarkRepository  _benchRepo;

    private CancellationTokenSource? _cts;

    public RunBenchmarkViewModel(
        BenchmarkOrchestrator  orchestrator,
        SystemInfoService      systemInfo,
        IBenchmarkRepository   benchRepo)
    {
        _orchestrator = orchestrator;
        _systemInfo   = systemInfo;
        _benchRepo    = benchRepo;

        _orchestrator.ProgressChanged += OnProgress;
        _orchestrator.RunCompleted    += OnRunCompleted;
    }

    // ── Config ─────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _cpuEnabled     = true;
    [ObservableProperty] private bool _memoryEnabled  = true;
    [ObservableProperty] private bool _diskEnabled    = true;
    [ObservableProperty] private bool _gpuEnabled     = false;
    [ObservableProperty] private bool _networkEnabled = false;

    [ObservableProperty] private int  _cpuRunSeconds    = 10;
    [ObservableProperty] private int  _memoryRunSeconds = 10;
    [ObservableProperty] private int  _diskFileSizeMb   = 512;

    // ── Progress ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _currentWorkload = "Idle";
    [ObservableProperty] private string _statusMessage   = "Ready";

    [ObservableProperty]
    private ObservableCollection<WorkloadProgressItem> _workloadProgress = new();

    // ── Results ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool    _hasResults;
    [ObservableProperty] private double  _overallScore;
    [ObservableProperty] private double? _cpuScore;
    [ObservableProperty] private double? _memScore;
    [ObservableProperty] private double? _diskScore;
    [ObservableProperty] private double? _gpuScore;
    [ObservableProperty] private ScoringBadge _badge = ScoringBadge.Unknown;
    [ObservableProperty] private string  _completionMessage = string.Empty;

    // ── Commands ───────────────────────────────────────────────────────────

    public void OnNavigatedTo() { /* nothing needed */ }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (_systemInfo.CurrentProfile is null ||
            _systemInfo.LatestHardware is null ||
            _systemInfo.LatestOs is null) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        HasResults = false;
        ProgressPercent = 0;
        WorkloadProgress.Clear();
        StatusMessage = "Starting…";

        var config = BuildConfig();
        config = await _benchRepo.SaveConfigAsync(config);

        try
        {
            await _orchestrator.RunAsync(
                _systemInfo.CurrentProfile.Id,
                _systemInfo.LatestHardware.Id,
                _systemInfo.LatestOs.Id,
                config,
                _cts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled.";
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private bool CanStart() => !IsRunning && _systemInfo.IsInitialized;

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Cancel() => _cts?.Cancel();

    // ── Event handlers ─────────────────────────────────────────────────────

    private void OnProgress(object? _, BenchmarkProgress p)
    {
        WpfApp.Current.Dispatcher.BeginInvoke(() =>
        {
            ProgressPercent = p.PercentComplete;
            CurrentWorkload = p.WorkloadName;
            StatusMessage   = p.StatusMessage ?? p.WorkloadName;

            var existing = WorkloadProgress.FirstOrDefault(w => w.Name == p.WorkloadName);
            if (existing is null)
                WorkloadProgress.Add(new WorkloadProgressItem(p.WorkloadName, p.PercentComplete));
            else
                existing.Percent = p.PercentComplete;
        });
    }

    private void OnRunCompleted(object? _, BenchmarkSession session)
    {
        WpfApp.Current.Dispatcher.BeginInvoke(() =>
        {
            IsRunning    = false;
            HasResults   = session.OverallScore.HasValue;
            OverallScore = session.OverallScore ?? 0;
            CpuScore     = session.CpuScore;
            MemScore     = session.MemoryScore;
            DiskScore    = session.DiskScore;
            GpuScore     = session.GpuScore;
            Badge        = ClassifyBadge(OverallScore);

            CompletionMessage = session.Status switch
            {
                BenchmarkStatus.Completed => $"Completed in {FormatDuration(session.StartedAt, session.CompletedAt)}",
                BenchmarkStatus.Cancelled => "Run cancelled.",
                BenchmarkStatus.Failed    => $"Failed: {session.ErrorMessage}",
                _                         => "Done."
            };
            StatusMessage = CompletionMessage;
            ProgressPercent = 100;
        });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private BenchmarkConfig BuildConfig() => new()
    {
        Id                   = Guid.NewGuid(),
        Name                 = $"Run {DateTime.Now:yyyy-MM-dd HH:mm}",
        CpuEnabled           = CpuEnabled,
        MemoryEnabled        = MemoryEnabled,
        DiskEnabled          = DiskEnabled,
        GpuEnabled           = GpuEnabled,
        NetworkEnabled       = NetworkEnabled,
        CpuRunSeconds        = CpuRunSeconds,
        MemoryRunSeconds     = MemoryRunSeconds,
        DiskRunSeconds       = 15,
        DiskTestFileSizeMB   = DiskFileSizeMb,
        DiskTestBlockSizeKB  = 128,
        DiskCleanupAfterTest = true,
        ScoringSchemaVersion = 1,
        CreatedAt            = DateTimeOffset.UtcNow
    };

    private static string FormatDuration(DateTimeOffset start, DateTimeOffset? end)
    {
        if (end is null) return "?";
        var ts = end.Value - start;
        return ts.TotalMinutes >= 1
            ? $"{(int)ts.TotalMinutes}m {ts.Seconds}s"
            : $"{ts.TotalSeconds:F1}s";
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

public sealed partial class WorkloadProgressItem : ObservableObject
{
    public string Name { get; }

    [ObservableProperty] private double _percent;

    public WorkloadProgressItem(string name, double percent)
    {
        Name    = name;
        Percent = percent;
    }
}
