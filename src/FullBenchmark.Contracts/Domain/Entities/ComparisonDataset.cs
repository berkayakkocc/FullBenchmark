namespace FullBenchmark.Contracts.Domain.Entities;

public class ComparisonDataset
{
    public Guid   Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string Version     { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTimeOffset ImportedAt { get; set; }

    public int    ScoringSchemaVersion { get; set; }
    public string Source               { get; set; } = "Seeded";   // "Seeded" | "Imported"
    public bool   IsActive             { get; set; } = true;

    // Navigation
    public ICollection<ComparisonDevice> Devices { get; set; } = new List<ComparisonDevice>();
}
