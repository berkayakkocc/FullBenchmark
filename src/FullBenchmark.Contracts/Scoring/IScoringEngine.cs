using FullBenchmark.Contracts.Domain.Entities;

namespace FullBenchmark.Contracts.Scoring;

/// <summary>
/// Stateless scoring engine. Takes completed benchmark cases with their metrics
/// and produces a fully structured <see cref="ScoringResult"/>.
/// Implementation is versioned — the schema version must match the session's version.
/// </summary>
public interface IScoringEngine
{
    int SchemaVersion { get; }

    ScoringResult Score(BenchmarkSession session, IReadOnlyList<BenchmarkCase> cases);
}
