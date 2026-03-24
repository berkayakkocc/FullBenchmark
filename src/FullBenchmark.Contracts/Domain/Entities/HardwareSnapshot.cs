using FullBenchmark.Contracts.Domain.ValueObjects;

namespace FullBenchmark.Contracts.Domain.Entities;

public class HardwareSnapshot
{
    public Guid            Id               { get; set; }
    public Guid            MachineProfileId { get; set; }
    public DateTimeOffset  CapturedAt       { get; set; }

    // ── CPU ──────────────────────────────────────────────────────────────────
    public string  CpuModel          { get; set; } = string.Empty;
    public string? CpuManufacturer   { get; set; }
    public int     CpuPhysicalCores  { get; set; }
    public int     CpuLogicalCores   { get; set; }
    public double? CpuBaseClockMHz   { get; set; }
    public double? CpuMaxClockMHz    { get; set; }
    public string? CpuArchitecture   { get; set; }
    public string? CpuSocket         { get; set; }
    public string? CpuL2CacheKB      { get; set; }
    public string? CpuL3CacheKB      { get; set; }

    // ── RAM ──────────────────────────────────────────────────────────────────
    public long    RamTotalBytes  { get; set; }
    public int?    RamSpeedMHz    { get; set; }
    public int?    RamChannels    { get; set; }
    public string? RamType        { get; set; }   // DDR4, DDR5, etc.
    public List<RamModuleInfo> RamModules { get; set; } = new();

    // ── Motherboard / BIOS ───────────────────────────────────────────────────
    public string?        MotherboardManufacturer { get; set; }
    public string?        MotherboardProduct      { get; set; }
    public string?        MotherboardVersion      { get; set; }
    public string?        BiosVersion             { get; set; }
    public DateTimeOffset? BiosReleaseDate        { get; set; }

    // ── Storage ──────────────────────────────────────────────────────────────
    public List<DiskInfo> Disks { get; set; } = new();

    // ── GPU ──────────────────────────────────────────────────────────────────
    public List<GpuInfo> Gpus { get; set; } = new();

    // ── Network ──────────────────────────────────────────────────────────────
    public List<NetworkAdapterInfo> NetworkAdapters { get; set; } = new();

    // ── Power ────────────────────────────────────────────────────────────────
    public bool    HasBattery                { get; set; }
    public string? BatteryManufacturer       { get; set; }
    public int?    BatteryDesignCapacityMWh  { get; set; }

    // Navigation
    public MachineProfile MachineProfile { get; set; } = null!;
}
