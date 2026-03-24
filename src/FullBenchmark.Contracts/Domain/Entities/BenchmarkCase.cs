using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Contracts.Domain.Entities;

public class BenchmarkCase
{
    public Guid            Id        { get; set; }
    public Guid            SessionId { get; set; }

    public BenchmarkCategory Category            { get; set; }
    public string            WorkloadName        { get; set; } = string.Empty;
    public string            WorkloadDescription { get; set; } = string.Empty;
    public BenchmarkStatus   Status              { get; set; }

    public DateTimeOffset? StartedAt   { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public int     WarmupDurationMs   { get; set; }
    public int     RunDurationMs      { get; set; }
    public int     ActualRunDurationMs{ get; set; }
    public int     ScoringSchemaVersion { get; set; }
    public string? ErrorMessage       { get; set; }

    // Navigation
    public BenchmarkSession                Session { get; set; } = null!;
    public ICollection<BenchmarkMetric>    Metrics { get; set; } = new List<BenchmarkMetric>();
    public ICollection<BenchmarkScore>     Scores  { get; set; } = new List<BenchmarkScore>();
}
