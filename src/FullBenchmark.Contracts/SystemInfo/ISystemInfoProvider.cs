using FullBenchmark.Contracts.Domain.Entities;

namespace FullBenchmark.Contracts.SystemInfo;

/// <summary>
/// Platform abstraction for enumerating static system hardware and OS properties.
/// Windows implementation uses WMI/CIM (System.Management) and Win32 APIs.
/// Expensive — results are meant to be cached as snapshots, not polled repeatedly.
/// </summary>
public interface ISystemInfoProvider
{
    Task<HardwareSnapshot>        GetHardwareSnapshotAsync(Guid machineProfileId, CancellationToken ct = default);
    Task<OperatingSystemSnapshot> GetOsSnapshotAsync(Guid machineProfileId, CancellationToken ct = default);

    /// <summary>
    /// Returns a stable identifier for this physical machine.
    /// Derived from CPU processor ID + motherboard serial + first physical MAC address.
    /// Reproducible across reboots and OS reinstalls.
    /// </summary>
    Task<string> GetMachineIdAsync(CancellationToken ct = default);
    Task<string> GetMachineNameAsync(CancellationToken ct = default);
    Task<string?> GetManufacturerAsync(CancellationToken ct = default);
    Task<string?> GetModelAsync(CancellationToken ct = default);
}
