namespace FullBenchmark.Contracts.Domain.Entities;

public class TelemetrySample
{
    public long           Id                    { get; set; }   // auto-increment for perf
    public Guid           MachineProfileId      { get; set; }
    public Guid?          BenchmarkSessionId    { get; set; }
    public DateTimeOffset CapturedAt            { get; set; }

    // ── CPU ──────────────────────────────────────────────────────────────────
    public double        CpuUsagePercent        { get; set; }
    public double?       CpuFrequencyMHz        { get; set; }
    public double?       CpuTemperatureCelsius  { get; set; }
    /// <summary>Per-core usage percentages. Empty list when unavailable or fewer cores than expected.</summary>
    public List<double>  CpuCoreUsages          { get; set; } = new();

    // ── Memory ───────────────────────────────────────────────────────────────
    public long   MemoryUsedBytes      { get; set; }
    public long   MemoryAvailableBytes { get; set; }
    public double? PageFileUsagePercent { get; set; }

    // ── Disk (aggregate across all physical disks) ───────────────────────────
    public long   DiskReadBytesPerSec    { get; set; }
    public long   DiskWriteBytesPerSec   { get; set; }
    public double? DiskActiveTimePercent { get; set; }

    // ── Network (aggregate across all active adapters) ───────────────────────
    public long NetworkSentBytesPerSec     { get; set; }
    public long NetworkReceivedBytesPerSec { get; set; }

    // ── GPU (first GPU via LibreHardwareMonitor – null if unavailable) ────────
    public double? GpuUsagePercent       { get; set; }
    public double? GpuTemperatureCelsius { get; set; }
    public long?   GpuMemoryUsedBytes    { get; set; }

    // ── Power ────────────────────────────────────────────────────────────────
    public double? BatteryChargePercent { get; set; }
    public bool?   IsOnAcPower          { get; set; }

    // Navigation
    public MachineProfile MachineProfile { get; set; } = null!;
}
