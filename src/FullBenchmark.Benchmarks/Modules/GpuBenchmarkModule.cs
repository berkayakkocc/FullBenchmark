using FullBenchmark.Contracts.Benchmarks;
using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Benchmarks.Modules;

/// <summary>
/// GPU benchmark stub.
/// GPU compute benchmarks require a platform-specific compute API (CUDA / OpenCL / DirectCompute).
/// This module reports <see cref="BenchmarkStatus.Skipped"/> until a compute backend is integrated.
/// </summary>
public sealed class GpuBenchmarkModule : IBenchmarkModule
{
    public BenchmarkCategory Category    => BenchmarkCategory.Gpu;
    public string            ModuleName  => "GPU Benchmark";
    public int               SchemaVersion => 1;

    public bool CanRunOnCurrentSystem(out string? reason)
    {
        reason = "GPU compute benchmarking is not yet implemented. " +
                 "A future release will add DirectCompute / Vulkan compute workloads.";
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
            ErrorMessage         = "GPU benchmarking not yet implemented."
        });
    }
}
