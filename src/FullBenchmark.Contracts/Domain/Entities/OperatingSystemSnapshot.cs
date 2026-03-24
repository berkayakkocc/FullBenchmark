namespace FullBenchmark.Contracts.Domain.Entities;

public class OperatingSystemSnapshot
{
    public Guid           Id               { get; set; }
    public Guid           MachineProfileId { get; set; }
    public DateTimeOffset CapturedAt       { get; set; }

    public string  OsName        { get; set; } = string.Empty;
    public string  OsVersion     { get; set; } = string.Empty;
    public string  OsBuildNumber { get; set; } = string.Empty;
    public string  OsRevision    { get; set; } = string.Empty;
    public string  Architecture  { get; set; } = string.Empty;

    public string?         ServicePack     { get; set; }
    public DateTimeOffset? InstallDate     { get; set; }
    public DateTimeOffset? LastBootTime    { get; set; }
    public string?         SystemDirectory { get; set; }
    public string?         TimeZoneId      { get; set; }
    public int?            ProcessorCount  { get; set; }
    public long?           PageFileSizeBytes { get; set; }

    // Navigation
    public MachineProfile MachineProfile { get; set; } = null!;
}
