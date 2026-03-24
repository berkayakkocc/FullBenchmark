using FullBenchmark.Contracts.Benchmarks;
using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Benchmarks.Modules;

/// <summary>
/// Network benchmark stub.
/// Accurate network benchmarking requires a cooperative remote endpoint and a known baseline
/// network topology — none of which can be assumed in a local benchmark tool.
/// This module reports <see cref="BenchmarkStatus.Skipped"/> until a loopback or configurable
/// remote endpoint strategy is implemented.
/// </summary>
public sealed class NetworkBenchmarkModule : IBenchmarkModule
{
    public BenchmarkCategory Category    => BenchmarkCategory.Network;
    public string            ModuleName  => "Network Benchmark";
    public int               SchemaVersion => 1;

    public bool CanRunOnCurrentSystem(out string? reason)
    {
        reason = "Network benchmarking is not yet implemented. " +
                 "A future release will add loopback latency and local LAN throughput workloads.";
        return false;
    }

    public Task WarmupAsync(BenchmarkContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<BenchmarkModuleResult> RunAsync(BenchmarkContext context, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult(new BenchmarkModuleResult
        {
            Category             = Category,
            WorkloadName         = ModuleName,
            Status               = BenchmarkStatus.Skipped,
            StartedAt            = now,
            CompletedAt          = now,
            ScoringSchemaVersion = SchemaVersion,
            ErrorMessage         = "Network benchmarking not yet implemented."
        });
    }
}
