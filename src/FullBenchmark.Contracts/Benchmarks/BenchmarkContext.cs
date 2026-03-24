using FullBenchmark.Contracts.Domain.Entities;

namespace FullBenchmark.Contracts.Benchmarks;

/// <summary>
/// Immutable execution context passed to every <see cref="IBenchmarkModule"/>.
/// Contains session binding, configuration, and progress reporting handle.
/// </summary>
public sealed class BenchmarkContext
{
    public required Guid                          SessionId         { get; init; }
    public required Guid                          CaseId            { get; init; }
    public required BenchmarkConfig               Config            { get; init; }
    public required IProgress<BenchmarkProgress>  ProgressReporter  { get; init; }
    public required CancellationToken             CancellationToken { get; init; }
}

public sealed class BenchmarkProgress
{
    public required string WorkloadName    { get; init; }
    public          double PercentComplete { get; init; }
    public          string? StatusMessage  { get; init; }
}
