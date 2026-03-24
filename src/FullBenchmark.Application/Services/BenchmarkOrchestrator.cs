using FullBenchmark.Contracts.Benchmarks;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using FullBenchmark.Contracts.Repositories;
using Microsoft.Extensions.Logging;
#pragma warning disable CA1305 // ToString with IFormatProvider — not needed here

namespace FullBenchmark.Application.Services;

/// <summary>
/// Coordinates a full benchmark run:
/// <list type="number">
///   <item>Creates a <see cref="BenchmarkSession"/> in the database.</item>
///   <item>For each enabled <see cref="IBenchmarkModule"/>: creates a <see cref="BenchmarkCase"/>, runs warmup + run, persists metrics.</item>
///   <item>Calls <see cref="ScoringCoordinator"/> to produce and persist scores.</item>
///   <item>Adds a <see cref="HistoricalTrendPoint"/> for chart history.</item>
/// </list>
///
/// <para>
/// Metric naming convention: metrics are stored as <c>{WorkloadName}_{MetricName}</c>
/// (e.g. "SingleThreadInteger_OpsPerSecond"). This allows the scoring engine to
/// unambiguously resolve workload-level metrics from the flat per-case metric collection.
/// </para>
/// </summary>
public sealed class BenchmarkOrchestrator
{
    private readonly IEnumerable<IBenchmarkModule> _modules;
    private readonly IBenchmarkRepository          _benchRepo;
    private readonly ScoringCoordinator            _scoring;
    private readonly ILogger<BenchmarkOrchestrator> _logger;

    public BenchmarkOrchestrator(
        IEnumerable<IBenchmarkModule>    modules,
        IBenchmarkRepository             benchRepo,
        ScoringCoordinator               scoring,
        ILogger<BenchmarkOrchestrator>   logger)
    {
        _modules   = modules;
        _benchRepo = benchRepo;
        _scoring   = scoring;
        _logger    = logger;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Fires with coarse progress updates (module-level, 0–100 %).</summary>
    public event EventHandler<BenchmarkProgress>? ProgressChanged;

    /// <summary>Fires once when the run completes (or fails). Carries the final session.</summary>
    public event EventHandler<BenchmarkSession>? RunCompleted;

    /// <summary>
    /// Starts a benchmark run synchronously-inside-Task (runs on a thread-pool thread).
    /// The caller does not need to marshal to the UI thread — <see cref="ProgressChanged"/>
    /// and <see cref="RunCompleted"/> fire on the worker thread.
    /// </summary>
    public async Task<BenchmarkSession> RunAsync(
        Guid             machineProfileId,
        Guid             hardwareSnapshotId,
        Guid             osSnapshotId,
        BenchmarkConfig  config,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Benchmark run starting for machine {Id}.", machineProfileId);

        // ── Create session ─────────────────────────────────────────────────
        var session = await _benchRepo.CreateSessionAsync(new BenchmarkSession
        {
            Id                   = Guid.NewGuid(),
            MachineProfileId     = machineProfileId,
            HardwareSnapshotId   = hardwareSnapshotId,
            OsSnapshotId         = osSnapshotId,
            BenchmarkConfigId    = config.Id,
            StartedAt            = DateTimeOffset.UtcNow,
            Status               = BenchmarkStatus.Running,
            ScoringSchemaVersion = config.ScoringSchemaVersion
        }, ct);

        try
        {
            await RunModulesAsync(session, config, ct);

            // ── Score and persist ──────────────────────────────────────────
            var scoring = await _scoring.ScoreAndPersistAsync(session, ct);

            session.OverallScore = scoring.OverallScore;
            session.CpuScore     = scoring.CpuScore;
            session.MemoryScore  = scoring.MemoryScore;
            session.DiskScore    = scoring.DiskScore;
            session.GpuScore     = scoring.GpuScore;
            session.Status       = BenchmarkStatus.Completed;
            session.CompletedAt  = DateTimeOffset.UtcNow;
            session = await _benchRepo.UpdateSessionAsync(session, ct);

            // ── Trend point ────────────────────────────────────────────────
            await _benchRepo.AddTrendPointAsync(new HistoricalTrendPoint
            {
                Id               = Guid.NewGuid(),
                MachineProfileId = machineProfileId,
                SessionId        = session.Id,
                RecordedAt       = DateTimeOffset.UtcNow,
                OverallScore     = scoring.OverallScore,
                CpuScore         = scoring.CpuScore,
                MemoryScore      = scoring.MemoryScore,
                DiskScore        = scoring.DiskScore,
                GpuScore         = scoring.GpuScore
            }, ct);

            _logger.LogInformation(
                "Benchmark run completed. Overall={Score:F1}, SessionId={Id}",
                scoring.OverallScore, session.Id);
        }
        catch (OperationCanceledException)
        {
            session.Status      = BenchmarkStatus.Cancelled;
            session.CompletedAt = DateTimeOffset.UtcNow;
            session = await _benchRepo.UpdateSessionAsync(session, default);
            _logger.LogInformation("Benchmark run cancelled. SessionId={Id}", session.Id);
        }
        catch (Exception ex)
        {
            session.Status       = BenchmarkStatus.Failed;
            session.ErrorMessage = ex.Message;
            session.CompletedAt  = DateTimeOffset.UtcNow;
            session = await _benchRepo.UpdateSessionAsync(session, default);
            _logger.LogError(ex, "Benchmark run failed. SessionId={Id}", session.Id);
        }
        finally
        {
            RunCompleted?.Invoke(this, session);
        }

        return session;
    }

    // ── Module execution ───────────────────────────────────────────────────

    private async Task RunModulesAsync(
        BenchmarkSession session, BenchmarkConfig config, CancellationToken ct)
    {
        var activeModules = GetActiveModules(config).ToList();
        int total         = activeModules.Count;

        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var module = activeModules[i];

            Report($"Preparing {module.ModuleName}…",
                   (double)i / total * 100, module.ModuleName);

            // Pre-flight check
            if (!module.CanRunOnCurrentSystem(out string? skipReason))
            {
                _logger.LogInformation(
                    "Skipping {Module}: {Reason}", module.ModuleName, skipReason);
                await PersistSkippedCaseAsync(session, module, skipReason, ct);
                continue;
            }

            var benchCase = await _benchRepo.AddCaseAsync(new BenchmarkCase
            {
                Id          = Guid.NewGuid(),
                SessionId   = session.Id,
                Category    = module.Category,
                WorkloadName= module.ModuleName,
                Status      = BenchmarkStatus.Warmup,
                StartedAt   = DateTimeOffset.UtcNow,
                ScoringSchemaVersion = module.SchemaVersion
            }, ct);

            var progress = new Progress<BenchmarkProgress>(p =>
                Report(p.StatusMessage ?? p.WorkloadName,
                       (i + p.PercentComplete / 100.0) / total * 100,
                       p.WorkloadName));

            var ctx = new BenchmarkContext
            {
                SessionId         = session.Id,
                CaseId            = benchCase.Id,
                Config            = config,
                ProgressReporter  = progress,
                CancellationToken = ct
            };

            // Warmup
            var warmupStart = DateTimeOffset.UtcNow;
            try
            {
                await module.WarmupAsync(ctx, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Module} warmup failed — continuing to run phase.", module.ModuleName);
            }

            benchCase.WarmupDurationMs = (int)(DateTimeOffset.UtcNow - warmupStart).TotalMilliseconds;
            benchCase.Status           = BenchmarkStatus.Running;
            benchCase = await _benchRepo.UpdateCaseAsync(benchCase, ct);

            // Run
            var runStart = DateTimeOffset.UtcNow;
            var moduleResult = await module.RunAsync(ctx, ct);
            benchCase.ActualRunDurationMs = (int)(DateTimeOffset.UtcNow - runStart).TotalMilliseconds;

            // Persist metrics (prefixed with workload name)
            await PersistMetricsAsync(benchCase.Id, moduleResult, ct);

            benchCase.Status      = moduleResult.Status;
            benchCase.CompletedAt = DateTimeOffset.UtcNow;
            await _benchRepo.UpdateCaseAsync(benchCase, ct);
        }
    }

    private async Task PersistMetricsAsync(
        Guid caseId, BenchmarkModuleResult moduleResult, CancellationToken ct)
    {
        var metrics = new List<BenchmarkMetric>();

        foreach (var workload in moduleResult.WorkloadResults)
        {
            if (workload.Status != BenchmarkStatus.Completed) continue;
            foreach (var metric in workload.Metrics)
            {
                metrics.Add(new BenchmarkMetric
                {
                    Id         = Guid.NewGuid(),
                    CaseId     = caseId,
                    // Prefix with workload name so scoring engine can disambiguate
                    MetricName = $"{workload.WorkloadName}_{metric.MetricName}",
                    Value      = metric.Value,
                    Unit       = metric.Unit,
                    IsRawValue = metric.IsRawValue,
                    Notes      = metric.Notes,
                    CapturedAt = metric.CapturedAt
                });
            }
        }

        if (metrics.Count > 0)
            await _benchRepo.AddMetricsAsync(metrics, ct);
    }

    private async Task PersistSkippedCaseAsync(
        BenchmarkSession session, IBenchmarkModule module,
        string? reason, CancellationToken ct)
    {
        var benchCase = await _benchRepo.AddCaseAsync(new BenchmarkCase
        {
            Id           = Guid.NewGuid(),
            SessionId    = session.Id,
            Category     = module.Category,
            WorkloadName = module.ModuleName,
            Status       = BenchmarkStatus.Skipped,
            StartedAt    = DateTimeOffset.UtcNow,
            CompletedAt  = DateTimeOffset.UtcNow,
            ErrorMessage = reason,
            ScoringSchemaVersion = module.SchemaVersion
        }, ct);
        _ = benchCase;
    }

    private IEnumerable<IBenchmarkModule> GetActiveModules(BenchmarkConfig cfg)
    {
        foreach (var m in _modules)
        {
            bool enabled = m.Category switch
            {
                BenchmarkCategory.Cpu     => cfg.CpuEnabled,
                BenchmarkCategory.Memory  => cfg.MemoryEnabled,
                BenchmarkCategory.Disk    => cfg.DiskEnabled,
                BenchmarkCategory.Gpu     => cfg.GpuEnabled,
                BenchmarkCategory.Network => cfg.NetworkEnabled,
                _                         => false
            };
            if (enabled) yield return m;
        }
    }

    private void Report(string msg, double pct, string workload) =>
        ProgressChanged?.Invoke(this, new BenchmarkProgress
        {
            WorkloadName    = workload,
            PercentComplete = Math.Clamp(pct, 0, 100),
            StatusMessage   = msg
        });
}
