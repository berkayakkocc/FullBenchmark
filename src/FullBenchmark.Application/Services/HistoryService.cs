using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using FullBenchmark.Contracts.Repositories;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Application.Services;

/// <summary>
/// Provides benchmark history and trend data for the current machine.
/// </summary>
public sealed class HistoryService
{
    private readonly IBenchmarkRepository  _repo;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(
        IBenchmarkRepository     repo,
        ILogger<HistoryService>  logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>
    /// Returns the most recent <paramref name="limit"/> sessions for the given machine,
    /// ordered newest-first.
    /// </summary>
    public async Task<IReadOnlyList<BenchmarkSession>> GetRecentSessionsAsync(
        Guid machineProfileId, int limit = 20, CancellationToken ct = default)
    {
        return await _repo.GetSessionHistoryAsync(machineProfileId, limit, ct);
    }

    /// <summary>
    /// Returns trend data for charting. Each point corresponds to one completed session.
    /// </summary>
    public async Task<IReadOnlyList<HistoricalTrendPoint>> GetTrendAsync(
        Guid machineProfileId, int limit = 100, CancellationToken ct = default)
    {
        return await _repo.GetTrendPointsAsync(machineProfileId, limit, ct);
    }

    /// <summary>
    /// Returns a session with its cases fully loaded (metrics included via EF navigation).
    /// </summary>
    public async Task<BenchmarkSession?> GetSessionDetailAsync(
        Guid sessionId, CancellationToken ct = default)
    {
        return await _repo.GetSessionAsync(sessionId, ct);
    }

    /// <summary>
    /// Attaches a user note to a session.
    /// </summary>
    public async Task AddNoteAsync(
        Guid sessionId, string content, CancellationToken ct = default)
    {
        await _repo.AddUserNoteAsync(new UserNote
        {
            Id        = Guid.NewGuid(),
            SessionId = sessionId,
            Content   = content,
            TagType   = TagType.Manual,
            CreatedAt = DateTimeOffset.UtcNow
        }, ct);
    }
}
