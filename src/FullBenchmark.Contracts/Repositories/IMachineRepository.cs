using FullBenchmark.Contracts.Domain.Entities;

namespace FullBenchmark.Contracts.Repositories;

public interface IMachineRepository
{
    Task<MachineProfile?> GetByMachineIdAsync(string machineId, CancellationToken ct = default);
    Task<MachineProfile?> GetCurrentMachineAsync(CancellationToken ct = default);
    Task<MachineProfile>  UpsertAsync(MachineProfile profile, CancellationToken ct = default);

    Task<HardwareSnapshot>         AddHardwareSnapshotAsync(HardwareSnapshot snapshot, CancellationToken ct = default);
    Task<OperatingSystemSnapshot>  AddOsSnapshotAsync(OperatingSystemSnapshot snapshot, CancellationToken ct = default);

    Task<HardwareSnapshot?>        GetLatestHardwareSnapshotAsync(Guid machineProfileId, CancellationToken ct = default);
    Task<OperatingSystemSnapshot?> GetLatestOsSnapshotAsync(Guid machineProfileId, CancellationToken ct = default);
}
