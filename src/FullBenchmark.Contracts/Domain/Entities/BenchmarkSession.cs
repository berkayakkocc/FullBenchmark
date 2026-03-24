using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Contracts.Domain.Entities;

public class BenchmarkSession
{
    public Guid            Id               { get; set; }
    public Guid            MachineProfileId { get; set; }
    public Guid            HardwareSnapshotId { get; set; }
    public Guid            OsSnapshotId     { get; set; }
    public Guid            BenchmarkConfigId{ get; set; }

    public DateTimeOffset  StartedAt        { get; set; }
    public DateTimeOffset? CompletedAt      { get; set; }
    public BenchmarkStatus Status           { get; set; }

    // Cached top-level scores for fast history display
    public double? OverallScore  { get; set; }
    public double? CpuScore      { get; set; }
    public double? MemoryScore   { get; set; }
    public double? DiskScore     { get; set; }
    public double? GpuScore      { get; set; }

    public int     ScoringSchemaVersion { get; set; }
    public string? ErrorMessage         { get; set; }

    // Navigation
    public MachineProfile          MachineProfile   { get; set; } = null!;
    public HardwareSnapshot        HardwareSnapshot { get; set; } = null!;
    public OperatingSystemSnapshot OsSnapshot       { get; set; } = null!;
    public BenchmarkConfig         BenchmarkConfig  { get; set; } = null!;
    public ICollection<BenchmarkCase>   Cases  { get; set; } = new List<BenchmarkCase>();
    public ICollection<BenchmarkScore>  Scores { get; set; } = new List<BenchmarkScore>();
    public ICollection<UserNote>        Notes  { get; set; } = new List<UserNote>();
}
