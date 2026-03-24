using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Contracts.Domain.Entities;

public class BenchmarkMetric
{
    public Guid       Id         { get; set; }
    public Guid       CaseId     { get; set; }

    public string     MetricName { get; set; } = string.Empty;
    public double     Value      { get; set; }
    public MetricUnit Unit       { get; set; }

    /// <summary>
    /// True = direct measurement output (MB/s, ops/sec).
    /// False = derived/aggregated metric (average, percentile).
    /// </summary>
    public bool       IsRawValue { get; set; }
    public string?    Notes      { get; set; }
    public DateTimeOffset CapturedAt { get; set; }

    // Navigation
    public BenchmarkCase Case { get; set; } = null!;
}
