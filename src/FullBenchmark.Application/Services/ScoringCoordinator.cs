using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Repositories;
using FullBenchmark.Contracts.Scoring;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Application.Services;

/// <summary>
/// Thin coordinator between <see cref="IScoringEngine"/> and the repositories.
/// Loads the cases with their metrics, delegates scoring, and persists results.
/// </summary>
public sealed class ScoringCoordinator
{
    private readonly IScoringEngine         _engine;
    private readonly IBenchmarkRepository   _repo;
    private readonly ILogger<ScoringCoordinator> _logger;

    public ScoringCoordinator(
        IScoringEngine               engine,
        IBenchmarkRepository         repo,
        ILogger<ScoringCoordinator>  logger)
    {
        _engine = engine;
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>
    /// Loads all cases for <paramref name="session"/>, runs the scoring engine,
    /// persists the resulting <see cref="BenchmarkScore"/> records, and returns
    /// the <see cref="ScoringResult"/>.
    /// </summary>
    public async Task<ScoringResult> ScoreAndPersistAsync(
        BenchmarkSession session, CancellationToken ct = default)
    {
        var cases = await _repo.GetCasesForSessionAsync(session.Id, ct);

        _logger.LogDebug(
            "Scoring session {Id}: {CaseCount} case(s).", session.Id, cases.Count);

        var result = _engine.Score(session, cases);

        await _repo.AddScoresAsync(result.AllScores, ct);

        _logger.LogInformation(
            "Scoring complete. Overall={Overall:F1}, CPU={Cpu}, Mem={Mem}, Disk={Disk}",
            result.OverallScore,
            result.CpuScore?.ToString("F1") ?? "n/a",
            result.MemoryScore?.ToString("F1") ?? "n/a",
            result.DiskScore?.ToString("F1") ?? "n/a");

        return result;
    }
}
