namespace FullBenchmark.Contracts.Domain.Entities;

public class BenchmarkConfig
{
    public Guid   Id        { get; set; }
    public string Name      { get; set; } = string.Empty;
    public bool   IsDefault { get; set; }

    // Enabled suites
    public bool CpuEnabled     { get; set; } = true;
    public bool MemoryEnabled  { get; set; } = true;
    public bool DiskEnabled    { get; set; } = true;
    public bool GpuEnabled     { get; set; } = false;
    public bool NetworkEnabled { get; set; } = false;

    // Timing
    public int WarmupSeconds    { get; set; } = 3;
    public int CpuRunSeconds    { get; set; } = 10;
    public int MemoryRunSeconds { get; set; } = 10;
    public int DiskRunSeconds   { get; set; } = 15;

    // Disk workload settings
    public int    DiskTestFileSizeMB  { get; set; } = 1024;
    public int    DiskTestBlockSizeKB { get; set; } = 128;
    public string? DiskTestTempPath   { get; set; }
    public bool   DiskCleanupAfterTest { get; set; } = true;

    // Scoring
    public int ScoringSchemaVersion { get; set; } = 1;

    public DateTimeOffset  CreatedAt  { get; set; }
    public DateTimeOffset? UpdatedAt  { get; set; }

    // Navigation
    public ICollection<BenchmarkSession> Sessions { get; set; } = new List<BenchmarkSession>();
}
