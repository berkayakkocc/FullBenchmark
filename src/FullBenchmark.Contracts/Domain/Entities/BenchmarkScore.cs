using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Contracts.Domain.Entities;

public class BenchmarkScore
{
    public Guid  Id        { get; set; }
    public Guid  SessionId { get; set; }
    public Guid? CaseId    { get; set; }

    /// <summary>
    /// Hierarchical level: null = overall session score; "CPU"/"Memory"/"Disk"/"GPU" = category score;
    /// workload name = individual sub-test score.
    /// </summary>
    public string? ScoreLevel { get; set; }
    public string  ScoreName  { get; set; } = string.Empty;

    /// <summary>The raw performance measurement on which normalization was applied.</summary>
    public double RawValue        { get; set; }

    /// <summary>Normalized score 0–1000. 500 = reference hardware tier (late-2021 i7/Ryzen 7).</summary>
    public double NormalizedScore { get; set; }

    /// <summary>Weight this score contributes to its parent category or overall score.</summary>
    public double Weight          { get; set; }

    public ScoringBadge Badge               { get; set; }
    public int          ScoringSchemaVersion { get; set; }

    // Navigation
    public BenchmarkSession  Session { get; set; } = null!;
    public BenchmarkCase?    Case    { get; set; }
}
