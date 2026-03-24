using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Contracts.Benchmarks;

/// <summary>
/// Contract for a single benchmark suite (CPU, Memory, Disk, GPU, Network).
/// Each module is responsible for one <see cref="BenchmarkCategory"/> and may run
/// multiple internal workloads, reporting each as a separate <see cref="BenchmarkWorkloadResult"/>.
/// </summary>
public interface IBenchmarkModule
{
    BenchmarkCategory Category   { get; }
    string            ModuleName { get; }

    /// <summary>Schema version for result interpretation and scoring compatibility.</summary>
    int SchemaVersion { get; }

    /// <summary>
    /// Quick pre-flight check. Returns false with a human-readable reason if this
    /// module cannot run (e.g., no writable disk path for disk module, no GPU detected).
    /// </summary>
    bool CanRunOnCurrentSystem(out string? reason);

    /// <summary>Runs the warmup phase. May throw <see cref="OperationCanceledException"/>.</summary>
    Task WarmupAsync(BenchmarkContext context, CancellationToken ct = default);

    /// <summary>
    /// Executes all workloads for this module. Must respect <see cref="BenchmarkContext.CancellationToken"/>.
    /// Returns a result even on partial failure — individual workloads carry their own status.
    /// </summary>
    Task<BenchmarkModuleResult> RunAsync(BenchmarkContext context, CancellationToken ct = default);
}
