using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using FullBenchmark.Contracts.Repositories;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Application.Services;

/// <summary>
/// Provides device comparison data.
/// Finds seeded/imported reference devices near the current machine's score
/// so the user can see how their machine stacks up.
/// </summary>
public sealed class ComparisonService
{
    private readonly IComparisonRepository _repo;
    private readonly ILogger<ComparisonService> _logger;

    public ComparisonService(
        IComparisonRepository       repo,
        ILogger<ComparisonService>  logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>
    /// Returns up to <paramref name="limit"/> devices whose overall score falls within
    /// ±<paramref name="radiusPercent"/> of <paramref name="score"/>.
    /// </summary>
    public Task<IReadOnlyList<ComparisonDevice>> GetNearScoreAsync(
        double score,
        double radiusPercent = 0.15,
        int    limit         = 10,
        CancellationToken ct = default)
    {
        return _repo.GetDevicesNearScoreAsync(score, radiusPercent, limit, ct);
    }

    /// <summary>
    /// Returns all devices in the active dataset filtered by category,
    /// ordered by overall score descending.
    /// </summary>
    public async Task<IReadOnlyList<ComparisonDevice>> GetByCategoryAsync(
        DeviceCategory category, CancellationToken ct = default)
    {
        var devices = await _repo.GetDevicesByCategoryAsync(category, ct);
        return [.. devices.OrderByDescending(d => d.OverallScore)];
    }

    /// <summary>
    /// Returns all devices from the active dataset, ordered by overall score descending.
    /// </summary>
    public async Task<IReadOnlyList<ComparisonDevice>> GetAllAsync(
        CancellationToken ct = default)
    {
        var devices = await _repo.GetAllDevicesAsync(ct: ct);
        return [.. devices.OrderByDescending(d => d.OverallScore)];
    }

    /// <summary>
    /// Determines the percentile rank of <paramref name="score"/> relative to
    /// all devices in the active dataset.
    /// Returns a value in [0, 100] where 100 = best in dataset.
    /// Returns null if the dataset is empty.
    /// </summary>
    public async Task<double?> GetPercentileAsync(
        double score, CancellationToken ct = default)
    {
        var all = await _repo.GetAllDevicesAsync(ct: ct);
        if (all.Count == 0) return null;

        int below = all.Count(d => d.OverallScore < score);
        return (double)below / all.Count * 100.0;
    }

    /// <summary>
    /// Builds a simple summary string for display (e.g. "Better than 72% of devices").
    /// </summary>
    public async Task<string> GetComparisonSummaryAsync(
        double score, CancellationToken ct = default)
    {
        var pct = await GetPercentileAsync(score, ct);
        if (pct is null) return "No comparison data available.";

        var near = await GetNearScoreAsync(score, 0.15, 3, ct);
        string nearStr = near.Count > 0
            ? $" Similar to: {string.Join(", ", near.Take(3).Select(d => d.DeviceName))}."
            : string.Empty;

        return $"Better than {pct.Value:F0}% of devices in the dataset.{nearStr}";
    }
}
