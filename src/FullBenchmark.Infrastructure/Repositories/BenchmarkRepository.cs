using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Repositories;
using FullBenchmark.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FullBenchmark.Infrastructure.Repositories;

public sealed class BenchmarkRepository : IBenchmarkRepository
{
    private readonly BenchmarkDbContext _db;

    public BenchmarkRepository(BenchmarkDbContext db) => _db = db;

    // ─── Sessions ────────────────────────────────────────────────────────────

    public async Task<BenchmarkSession> CreateSessionAsync(BenchmarkSession session, CancellationToken ct = default)
    {
        _db.BenchmarkSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public Task<BenchmarkSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
        => _db.BenchmarkSessions
               .Include(s => s.Cases).ThenInclude(c => c.Metrics)
               .Include(s => s.Cases).ThenInclude(c => c.Scores)
               .Include(s => s.Scores)
               .Include(s => s.Notes)
               .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public async Task<BenchmarkSession> UpdateSessionAsync(BenchmarkSession session, CancellationToken ct = default)
    {
        _db.BenchmarkSessions.Update(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<IReadOnlyList<BenchmarkSession>> GetSessionHistoryAsync(
        Guid machineProfileId, int limit = 50, CancellationToken ct = default)
    {
        var results = await _db.BenchmarkSessions
            .Where(s => s.MachineProfileId == machineProfileId)
            .OrderByDescending(s => s.StartedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);
        return results;
    }

    // ─── Cases ───────────────────────────────────────────────────────────────

    public async Task<BenchmarkCase> AddCaseAsync(BenchmarkCase benchCase, CancellationToken ct = default)
    {
        _db.BenchmarkCases.Add(benchCase);
        await _db.SaveChangesAsync(ct);
        return benchCase;
    }

    public async Task<BenchmarkCase> UpdateCaseAsync(BenchmarkCase benchCase, CancellationToken ct = default)
    {
        _db.BenchmarkCases.Update(benchCase);
        await _db.SaveChangesAsync(ct);
        return benchCase;
    }

    public async Task<IReadOnlyList<BenchmarkCase>> GetCasesForSessionAsync(
        Guid sessionId, CancellationToken ct = default)
    {
        var result = await _db.BenchmarkCases
            .Include(c => c.Metrics)
            .Include(c => c.Scores)
            .Where(c => c.SessionId == sessionId)
            .AsNoTracking()
            .ToListAsync(ct);
        return result;
    }

    // ─── Metrics / Scores ────────────────────────────────────────────────────

    public async Task AddMetricsAsync(IEnumerable<BenchmarkMetric> metrics, CancellationToken ct = default)
    {
        _db.BenchmarkMetrics.AddRange(metrics);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddScoresAsync(IEnumerable<BenchmarkScore> scores, CancellationToken ct = default)
    {
        _db.BenchmarkScores.AddRange(scores);
        await _db.SaveChangesAsync(ct);
    }

    // ─── Config ──────────────────────────────────────────────────────────────

    public Task<BenchmarkConfig?> GetDefaultConfigAsync(CancellationToken ct = default)
        => _db.BenchmarkConfigs.FirstOrDefaultAsync(c => c.IsDefault, ct);

    public async Task<BenchmarkConfig> SaveConfigAsync(BenchmarkConfig config, CancellationToken ct = default)
    {
        var existing = await _db.BenchmarkConfigs.FindAsync([config.Id], ct);
        if (existing is null)
            _db.BenchmarkConfigs.Add(config);
        else
            _db.BenchmarkConfigs.Update(config);

        await _db.SaveChangesAsync(ct);
        return config;
    }

    // ─── Trend / History ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<HistoricalTrendPoint>> GetTrendPointsAsync(
        Guid machineProfileId, int limit = 100, CancellationToken ct = default)
    {
        var result = await _db.TrendPoints
            .Where(t => t.MachineProfileId == machineProfileId)
            .OrderByDescending(t => t.RecordedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);
        return result;
    }

    public async Task AddTrendPointAsync(HistoricalTrendPoint point, CancellationToken ct = default)
    {
        _db.TrendPoints.Add(point);
        await _db.SaveChangesAsync(ct);
    }

    // ─── Notes ───────────────────────────────────────────────────────────────

    public async Task AddUserNoteAsync(UserNote note, CancellationToken ct = default)
    {
        _db.UserNotes.Add(note);
        await _db.SaveChangesAsync(ct);
    }
}
