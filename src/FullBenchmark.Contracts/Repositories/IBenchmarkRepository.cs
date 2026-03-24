using FullBenchmark.Contracts.Domain.Entities;

namespace FullBenchmark.Contracts.Repositories;

public interface IBenchmarkRepository
{
    Task<BenchmarkSession>  CreateSessionAsync(BenchmarkSession session, CancellationToken ct = default);
    Task<BenchmarkSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<BenchmarkSession>  UpdateSessionAsync(BenchmarkSession session, CancellationToken ct = default);

    Task<BenchmarkCase>  AddCaseAsync(BenchmarkCase benchCase, CancellationToken ct = default);
    Task<BenchmarkCase>  UpdateCaseAsync(BenchmarkCase benchCase, CancellationToken ct = default);
    Task                 AddMetricsAsync(IEnumerable<BenchmarkMetric> metrics, CancellationToken ct = default);
    Task                 AddScoresAsync(IEnumerable<BenchmarkScore> scores, CancellationToken ct = default);

    Task<IReadOnlyList<BenchmarkSession>> GetSessionHistoryAsync(
        Guid machineProfileId, int limit = 50, CancellationToken ct = default);

    Task<BenchmarkConfig?> GetDefaultConfigAsync(CancellationToken ct = default);
    Task<BenchmarkConfig>  SaveConfigAsync(BenchmarkConfig config, CancellationToken ct = default);

    Task<IReadOnlyList<HistoricalTrendPoint>> GetTrendPointsAsync(
        Guid machineProfileId, int limit = 100, CancellationToken ct = default);
    Task AddTrendPointAsync(HistoricalTrendPoint point, CancellationToken ct = default);

    Task AddUserNoteAsync(UserNote note, CancellationToken ct = default);

    Task<IReadOnlyList<BenchmarkCase>> GetCasesForSessionAsync(Guid sessionId, CancellationToken ct = default);
}
