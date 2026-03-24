using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Contracts.Benchmarks;

/// <summary>Result returned by a single <see cref="IBenchmarkModule"/> run.</summary>
public sealed class BenchmarkModuleResult
{
    public required BenchmarkCategory        Category             { get; init; }
    public required string                   WorkloadName         { get; init; }
    public required BenchmarkStatus          Status               { get; init; }
    public required DateTimeOffset           StartedAt            { get; init; }
    public required DateTimeOffset           CompletedAt          { get; init; }
    public          IReadOnlyList<WorkloadResult> WorkloadResults { get; init; } = [];
    public          string?                  ErrorMessage         { get; init; }
    public          int                      ScoringSchemaVersion { get; init; } = 1;
}

/// <summary>Result for one individual workload within a module (e.g., CpuSingleThreadInteger).</summary>
public sealed class WorkloadResult
{
    public required string                   WorkloadName    { get; init; }
    public required BenchmarkStatus          Status          { get; init; }
    public required DateTimeOffset           StartedAt       { get; init; }
    public required DateTimeOffset           CompletedAt     { get; init; }
    public          IReadOnlyList<BenchmarkMetric> Metrics  { get; init; } = [];
    public          string?                  ErrorMessage    { get; init; }
}
