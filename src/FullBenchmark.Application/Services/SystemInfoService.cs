using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Repositories;
using FullBenchmark.Contracts.SystemInfo;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Application.Services;

/// <summary>
/// Bootstraps the current machine's identity in the database.
/// On first run: creates <see cref="MachineProfile"/>, <see cref="HardwareSnapshot"/>,
/// and <see cref="OperatingSystemSnapshot"/>.
/// On subsequent runs: updates <see cref="MachineProfile.LastSeenAt"/> and
/// adds a fresh hardware + OS snapshot (hardware can change between runs).
/// </summary>
public sealed class SystemInfoService
{
    private readonly ISystemInfoProvider _provider;
    private readonly IMachineRepository  _machines;
    private readonly ILogger<SystemInfoService> _logger;

    public SystemInfoService(
        ISystemInfoProvider provider,
        IMachineRepository  machines,
        ILogger<SystemInfoService> logger)
    {
        _provider = provider;
        _machines = machines;
        _logger   = logger;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public MachineProfile?          CurrentProfile { get; private set; }
    public HardwareSnapshot?        LatestHardware { get; private set; }
    public OperatingSystemSnapshot? LatestOs       { get; private set; }
    public bool                     IsInitialized  { get; private set; }

    /// <summary>
    /// Runs the full machine discovery and database upsert.
    /// Safe to call multiple times — idempotent after first successful run.
    /// </summary>
    public async Task InitialiseAsync(CancellationToken ct = default)
    {
        if (IsInitialized) return;

        try
        {
            _logger.LogInformation("Initialising machine profile…");

            string machineId   = await _provider.GetMachineIdAsync(ct);
            string machineName = await _provider.GetMachineNameAsync(ct);
            string? mfr        = await _provider.GetManufacturerAsync(ct);
            string? model      = await _provider.GetModelAsync(ct);

            // ── MachineProfile upsert ──────────────────────────────────────
            var existing = await _machines.GetByMachineIdAsync(machineId, ct);
            var profile  = existing ?? new MachineProfile
            {
                Id         = Guid.NewGuid(),
                MachineId  = machineId,
                CreatedAt  = DateTimeOffset.UtcNow,
                IsCurrentMachine = true
            };

            profile.MachineName  = machineName;
            profile.Manufacturer = mfr;
            profile.Model        = model;
            profile.LastSeenAt   = DateTimeOffset.UtcNow;
            profile.IsCurrentMachine = true;

            profile = await _machines.UpsertAsync(profile, ct);

            // ── Snapshots ──────────────────────────────────────────────────
            var hw = await _provider.GetHardwareSnapshotAsync(profile.Id, ct);
            hw.Id = Guid.NewGuid();
            hw = await _machines.AddHardwareSnapshotAsync(hw, ct);

            var os = await _provider.GetOsSnapshotAsync(profile.Id, ct);
            os.Id = Guid.NewGuid();
            os = await _machines.AddOsSnapshotAsync(os, ct);

            CurrentProfile = profile;
            LatestHardware = hw;
            LatestOs       = os;
            IsInitialized  = true;

            _logger.LogInformation(
                "Machine profile ready. Id={ProfileId}, Name={Name}, MachineId={MachineId}",
                profile.Id, profile.MachineName, profile.MachineId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Machine profile initialisation failed.");
            throw;
        }
    }
}
