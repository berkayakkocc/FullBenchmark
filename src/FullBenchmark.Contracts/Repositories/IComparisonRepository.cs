using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Contracts.Repositories;

public interface IComparisonRepository
{
    Task<IReadOnlyList<ComparisonDevice>> GetAllDevicesAsync(
        int? schemaVersion = null, CancellationToken ct = default);

    Task<IReadOnlyList<ComparisonDevice>> GetDevicesByCategoryAsync(
        DeviceCategory category, CancellationToken ct = default);

    Task<IReadOnlyList<ComparisonDevice>> GetDevicesNearScoreAsync(
        double score, double radiusPercent = 0.15, int limit = 10, CancellationToken ct = default);

    Task<ComparisonDataset?>  GetActiveDatasetAsync(CancellationToken ct = default);

    Task<ComparisonDataset> ImportDatasetAsync(
        ComparisonDataset dataset, IEnumerable<ComparisonDevice> devices, CancellationToken ct = default);

    Task<bool> HasSeedDataAsync(CancellationToken ct = default);
}
