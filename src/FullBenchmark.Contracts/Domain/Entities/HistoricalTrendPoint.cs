namespace FullBenchmark.Contracts.Domain.Entities;

public class HistoricalTrendPoint
{
    public Guid           Id               { get; set; }
    public Guid           MachineProfileId { get; set; }
    public Guid           SessionId        { get; set; }
    public DateTimeOffset RecordedAt       { get; set; }

    public double  OverallScore  { get; set; }
    public double? CpuScore      { get; set; }
    public double? MemoryScore   { get; set; }
    public double? DiskScore     { get; set; }
    public double? GpuScore      { get; set; }

    // Navigation
    public MachineProfile    MachineProfile { get; set; } = null!;
    public BenchmarkSession  Session        { get; set; } = null!;
}
