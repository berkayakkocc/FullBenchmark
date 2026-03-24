using FullBenchmark.Contracts.Domain.Entities;

namespace FullBenchmark.Contracts.Scoring;

public sealed class ScoringResult
{
    public required Guid                        SessionId            { get; init; }
    public required double                      OverallScore         { get; init; }
    public          double?                     CpuScore             { get; init; }
    public          double?                     MemoryScore          { get; init; }
    public          double?                     DiskScore            { get; init; }
    public          double?                     GpuScore             { get; init; }
    public required int                         ScoringSchemaVersion { get; init; }
    public required IReadOnlyList<BenchmarkScore> AllScores          { get; init; }
}
