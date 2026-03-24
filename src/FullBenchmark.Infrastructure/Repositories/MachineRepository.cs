using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Repositories;
using FullBenchmark.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FullBenchmark.Infrastructure.Repositories;

public sealed class MachineRepository : IMachineRepository
{
    private readonly BenchmarkDbContext _db;

    public MachineRepository(BenchmarkDbContext db) => _db = db;

    public Task<MachineProfile?> GetByMachineIdAsync(string machineId, CancellationToken ct = default)
        => _db.MachineProfiles
               .FirstOrDefaultAsync(m => m.MachineId == machineId, ct);

    public Task<MachineProfile?> GetCurrentMachineAsync(CancellationToken ct = default)
        => _db.MachineProfiles
               .FirstOrDefaultAsync(m => m.IsCurrentMachine, ct);

    public async Task<MachineProfile> UpsertAsync(MachineProfile profile, CancellationToken ct = default)
    {
        var existing = await _db.MachineProfiles
            .FirstOrDefaultAsync(m => m.MachineId == profile.MachineId, ct);

        if (existing is null)
        {
            _db.MachineProfiles.Add(profile);
        }
        else
        {
            existing.MachineName      = profile.MachineName;
            existing.Manufacturer     = profile.Manufacturer;
            existing.Model            = profile.Model;
            existing.IsCurrentMachine = profile.IsCurrentMachine;
            existing.LastSeenAt       = profile.LastSeenAt;
            profile = existing;
        }

        await _db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<HardwareSnapshot> AddHardwareSnapshotAsync(
        HardwareSnapshot snapshot, CancellationToken ct = default)
    {
        _db.HardwareSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);
        return snapshot;
    }

    public async Task<OperatingSystemSnapshot> AddOsSnapshotAsync(
        OperatingSystemSnapshot snapshot, CancellationToken ct = default)
    {
        _db.OsSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);
        return snapshot;
    }

    public Task<HardwareSnapshot?> GetLatestHardwareSnapshotAsync(
        Guid machineProfileId, CancellationToken ct = default)
        => _db.HardwareSnapshots
               .Where(h => h.MachineProfileId == machineProfileId)
               .OrderByDescending(h => h.CapturedAt)
               .FirstOrDefaultAsync(ct);

    public Task<OperatingSystemSnapshot?> GetLatestOsSnapshotAsync(
        Guid machineProfileId, CancellationToken ct = default)
        => _db.OsSnapshots
               .Where(o => o.MachineProfileId == machineProfileId)
               .OrderByDescending(o => o.CapturedAt)
               .FirstOrDefaultAsync(ct);
}
