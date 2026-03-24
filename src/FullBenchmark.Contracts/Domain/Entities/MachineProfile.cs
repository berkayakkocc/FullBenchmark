namespace FullBenchmark.Contracts.Domain.Entities;

public class MachineProfile
{
    public Guid   Id              { get; set; }

    /// <summary>
    /// Stable hardware fingerprint derived from CPU processor ID + motherboard serial + MAC address.
    /// Survives OS reinstalls. Used to correlate runs across history.
    /// </summary>
    public string MachineId       { get; set; } = string.Empty;

    public string MachineName     { get; set; } = string.Empty;
    public string? Manufacturer   { get; set; }
    public string? Model          { get; set; }
    public bool   IsCurrentMachine{ get; set; }

    public DateTimeOffset CreatedAt  { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }

    // Navigation
    public ICollection<HardwareSnapshot>        HardwareSnapshots { get; set; } = new List<HardwareSnapshot>();
    public ICollection<OperatingSystemSnapshot> OsSnapshots       { get; set; } = new List<OperatingSystemSnapshot>();
    public ICollection<BenchmarkSession>        BenchmarkSessions { get; set; } = new List<BenchmarkSession>();
    public ICollection<HistoricalTrendPoint>    TrendPoints       { get; set; } = new List<HistoricalTrendPoint>();
}
