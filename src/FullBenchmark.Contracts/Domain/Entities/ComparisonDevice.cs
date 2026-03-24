using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Contracts.Domain.Entities;

public class ComparisonDevice
{
    public Guid           Id        { get; set; }
    public Guid           DatasetId { get; set; }

    public string         DeviceName    { get; set; } = string.Empty;
    public string         Manufacturer  { get; set; } = string.Empty;
    public DeviceCategory Category      { get; set; }

    // Representative hardware summary (descriptive, not live telemetry)
    public string  CpuModel            { get; set; } = string.Empty;
    public int     CpuCores            { get; set; }
    public double  RamTotalGB          { get; set; }
    public string? StorageDescription  { get; set; }
    public string? GpuModel            { get; set; }

    // Scores under the matching ScoringSchemaVersion
    public double  OverallScore        { get; set; }
    public double? CpuScore            { get; set; }
    public double? MemoryScore         { get; set; }
    public double? DiskScore           { get; set; }
    public double? GpuScore            { get; set; }

    public int    ScoringSchemaVersion { get; set; }
    public bool   IsReferenceDevice    { get; set; }
    public string Source               { get; set; } = "Seeded";
    public DateTimeOffset RecordedAt   { get; set; }

    // Navigation
    public ComparisonDataset Dataset { get; set; } = null!;
}
