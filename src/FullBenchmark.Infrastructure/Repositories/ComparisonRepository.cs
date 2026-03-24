using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using FullBenchmark.Contracts.Repositories;
using FullBenchmark.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FullBenchmark.Infrastructure.Repositories;

public sealed class ComparisonRepository : IComparisonRepository
{
    private readonly BenchmarkDbContext _db;

    public ComparisonRepository(BenchmarkDbContext db) => _db = db;

    public async Task<IReadOnlyList<ComparisonDevice>> GetAllDevicesAsync(
        int? schemaVersion = null, CancellationToken ct = default)
    {
        var query = _db.ComparisonDevices
            .Include(d => d.Dataset)
            .Where(d => d.Dataset.IsActive);

        if (schemaVersion.HasValue)
            query = query.Where(d => d.ScoringSchemaVersion == schemaVersion.Value);

        var result = await query.OrderByDescending(d => d.OverallScore)
                                .AsNoTracking()
                                .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<ComparisonDevice>> GetDevicesByCategoryAsync(
        DeviceCategory category, CancellationToken ct = default)
    {
        var result = await _db.ComparisonDevices
            .Include(d => d.Dataset)
            .Where(d => d.Dataset.IsActive && d.Category == category)
            .OrderByDescending(d => d.OverallScore)
            .AsNoTracking()
            .ToListAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<ComparisonDevice>> GetDevicesNearScoreAsync(
        double score, double radiusPercent = 0.15, int limit = 10, CancellationToken ct = default)
    {
        var delta  = score * radiusPercent;
        var low    = score - delta;
        var high   = score + delta;

        var result = await _db.ComparisonDevices
            .Include(d => d.Dataset)
            .Where(d => d.Dataset.IsActive && d.OverallScore >= low && d.OverallScore <= high)
            .OrderBy(d => Math.Abs(d.OverallScore - score))
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);
        return result;
    }

    public Task<ComparisonDataset?> GetActiveDatasetAsync(CancellationToken ct = default)
        => _db.ComparisonDatasets.FirstOrDefaultAsync(d => d.IsActive, ct);

    public async Task<ComparisonDataset> ImportDatasetAsync(
        ComparisonDataset dataset, IEnumerable<ComparisonDevice> devices, CancellationToken ct = default)
    {
        // Deactivate all existing datasets when importing a new one
        var existing = await _db.ComparisonDatasets.Where(d => d.IsActive).ToListAsync(ct);
        existing.ForEach(d => d.IsActive = false);

        dataset.IsActive = true;
        _db.ComparisonDatasets.Add(dataset);

        foreach (var device in devices)
        {
            device.DatasetId = dataset.Id;
            _db.ComparisonDevices.Add(device);
        }

        await _db.SaveChangesAsync(ct);
        return dataset;
    }

    public Task<bool> HasSeedDataAsync(CancellationToken ct = default)
        => _db.ComparisonDatasets.AnyAsync(d => d.Source == "Seeded", ct);
}
