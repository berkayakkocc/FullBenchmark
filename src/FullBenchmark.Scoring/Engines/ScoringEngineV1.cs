using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using FullBenchmark.Contracts.Scoring;

namespace FullBenchmark.Scoring.Engines;

/// <summary>
/// Scoring engine schema v1.
///
/// <para><b>Scale:</b> 0–1000 normalized. 500 = reference tier (late-2021 desktop, i7-12700 / Ryzen 7 5800X).</para>
///
/// <para><b>Category weights (sum to 1.0):</b></para>
/// <list type="table">
///   <item><term>CPU</term><description>40 %</description></item>
///   <item><term>Memory</term><description>25 %</description></item>
///   <item><term>Disk</term><description>25 %</description></item>
///   <item><term>GPU</term><description>10 % (zeroed when not available)</description></item>
/// </list>
///
/// <para><b>Normalization formula:</b>
///   <c>normalizedScore = Clamp(raw / reference * 500, 0, 1000)</c>
/// </para>
/// <para>
///   A machine performing exactly at the reference tier scores 500 on every workload.
///   A machine twice as fast scores 1000; half as fast scores 250.
/// </para>
///
/// <para><b>Reference values (late-2021 desktop CPU + DDR4-3200 + NVMe SSD + NV/AMD midrange GPU):</b></para>
/// <list type="bullet">
///   <item>CPU ST Integer:   800 M ops/s</item>
///   <item>CPU MT Integer:   8000 M ops/s   (≈ 10 cores × ST)</item>
///   <item>CPU ST Float:     400 M ops/s</item>
///   <item>CPU MT Float:     3600 M ops/s</item>
///   <item>CPU SHA-256:      800 MB/s</item>
///   <item>RAM Alloc:        3000 MB/s</item>
///   <item>RAM Copy:         20 GB/s         (DDR4-3200 dual channel ≈ 48 GB/s peak, .NET copy ≈ 20)</item>
///   <item>RAM Latency:      80 ns/access    (DRAM CAS latency at DDR4-3200)</item>
///   <item>RAM Bandwidth:    18 GB/s         (sustained sequential read)</item>
///   <item>Disk Seq Write:   1500 MB/s       (NVMe Gen3 typical)</item>
///   <item>Disk Seq Read:    2500 MB/s</item>
///   <item>Disk Rnd Read:    25 000 IOPS</item>
///   <item>Disk Rnd Write:   100 000 IOPS</item>
/// </list>
/// </summary>
public sealed class ScoringEngineV1 : IScoringEngine
{
    public int SchemaVersion => 1;

    // ── Category weights ───────────────────────────────────────────────────
    private const double CpuWeight    = 0.40;
    private const double MemoryWeight = 0.25;
    private const double DiskWeight   = 0.25;
    private const double GpuWeight    = 0.10;

    // ── Within-category workload weights ──────────────────────────────────
    // CPU: bias toward multi-thread (reflects real-world workload mix)
    private static readonly Dictionary<string, double> CpuWorkloadWeights = new()
    {
        ["SingleThreadInteger"] = 0.15,
        ["MultiThreadInteger"]  = 0.30,
        ["SingleThreadFloat"]   = 0.15,
        ["MultiThreadFloat"]    = 0.30,
        ["HashThroughput"]      = 0.10,
    };

    // Memory: bandwidth-heavy weighting
    private static readonly Dictionary<string, double> MemoryWorkloadWeights = new()
    {
        ["AllocationStress"]     = 0.20,
        ["CopyThroughput"]       = 0.35,
        ["LatencyApproximation"] = 0.20,
        ["SustainedBandwidth"]   = 0.25,
    };

    // Disk: sequential bias (reflects most common workloads)
    private static readonly Dictionary<string, double> DiskWorkloadWeights = new()
    {
        ["SequentialWrite"] = 0.25,
        ["SequentialRead"]  = 0.35,
        ["RandomRead4K"]    = 0.25,
        ["RandomWrite4K"]   = 0.15,
    };

    // ── Reference values (= 500 pts on our scale) ─────────────────────────
    // CPU
    private const double RefCpuStInteger  = 800_000_000.0;    // ops/s
    private const double RefCpuMtInteger  = 8_000_000_000.0;
    private const double RefCpuStFloat    = 400_000_000.0;
    private const double RefCpuMtFloat    = 3_600_000_000.0;
    private const double RefCpuHash       = 800.0;            // MB/s
    // Memory
    private const double RefMemAlloc      = 3_000.0;          // MB/s
    private const double RefMemCopy       = 20.0;             // GB/s
    private const double RefMemLatency    = 80.0;             // ns/access (lower = better)
    private const double RefMemBandwidth  = 18.0;             // GB/s
    // Disk
    private const double RefDiskSeqWrite  = 1_500.0;          // MB/s
    private const double RefDiskSeqRead   = 2_500.0;          // MB/s
    private const double RefDiskRndRead   = 25_000.0;         // IOPS
    private const double RefDiskRndWrite  = 100_000.0;        // IOPS

    // ── IScoringEngine ─────────────────────────────────────────────────────

    public ScoringResult Score(BenchmarkSession session, IReadOnlyList<BenchmarkCase> cases)
    {
        var allScores = new List<BenchmarkScore>();

        double? cpuScore    = ScoreCpuCategory(session, cases, allScores);
        double? memScore    = ScoreMemoryCategory(session, cases, allScores);
        double? diskScore   = ScoreDiskCategory(session, cases, allScores);
        double? gpuScore    = null; // GPU stub — not scored in v1

        double overall = ComputeOverall(cpuScore, memScore, diskScore, gpuScore);

        var overallScore = new BenchmarkScore
        {
            Id                   = Guid.NewGuid(),
            SessionId            = session.Id,
            CaseId               = null,
            ScoreLevel           = null,    // null = session-level
            ScoreName            = "Overall",
            RawValue             = 0,
            NormalizedScore      = overall,
            Weight               = 1.0,
            Badge                = Classify(overall),
            ScoringSchemaVersion = SchemaVersion
        };
        allScores.Insert(0, overallScore);

        return new ScoringResult
        {
            SessionId            = session.Id,
            OverallScore         = overall,
            CpuScore             = cpuScore,
            MemoryScore          = memScore,
            DiskScore            = diskScore,
            GpuScore             = gpuScore,
            ScoringSchemaVersion = SchemaVersion,
            AllScores            = allScores
        };
    }

    // ── Category scorers ───────────────────────────────────────────────────

    private double? ScoreCpuCategory(BenchmarkSession session,
        IReadOnlyList<BenchmarkCase> cases, List<BenchmarkScore> allScores)
    {
        var cpuCase = cases.FirstOrDefault(c => c.Category == BenchmarkCategory.Cpu);
        if (cpuCase is null) return null;

        var workloadScores = new Dictionary<string, double>();
        var metrics        = cpuCase.Metrics.ToLookup(m => m.MetricName);

        double? stInt = SingleMetric(metrics, "SingleThreadInteger", "OpsPerSecond");
        double? mtInt = SingleMetric(metrics, "MultiThreadInteger",  "OpsPerSecond");
        double? stFlt = SingleMetric(metrics, "SingleThreadFloat",   "OpsPerSecond");
        double? mtFlt = SingleMetric(metrics, "MultiThreadFloat",    "OpsPerSecond");
        double? hash  = SingleMetric(metrics, "HashThroughput",      "MBPerSecond");

        if (stInt.HasValue) workloadScores["SingleThreadInteger"] = Normalize(stInt.Value, RefCpuStInteger);
        if (mtInt.HasValue) workloadScores["MultiThreadInteger"]  = Normalize(mtInt.Value, RefCpuMtInteger);
        if (stFlt.HasValue) workloadScores["SingleThreadFloat"]   = Normalize(stFlt.Value, RefCpuStFloat);
        if (mtFlt.HasValue) workloadScores["MultiThreadFloat"]    = Normalize(mtFlt.Value, RefCpuMtFloat);
        if (hash.HasValue)  workloadScores["HashThroughput"]      = Normalize(hash.Value,  RefCpuHash);

        EmitWorkloadScores(session, cpuCase, "CPU", workloadScores, CpuWorkloadWeights, allScores);

        double category = WeightedAverage(workloadScores, CpuWorkloadWeights);
        EmitCategoryScore(session, cpuCase, "CPU", category, CpuWeight, allScores);
        return category;
    }

    private double? ScoreMemoryCategory(BenchmarkSession session,
        IReadOnlyList<BenchmarkCase> cases, List<BenchmarkScore> allScores)
    {
        var memCase = cases.FirstOrDefault(c => c.Category == BenchmarkCategory.Memory);
        if (memCase is null) return null;

        var workloadScores = new Dictionary<string, double>();
        var metrics        = memCase.Metrics.ToLookup(m => m.MetricName);

        double? alloc    = SingleMetric(metrics, "AllocationStress",     "MBPerSecond");
        double? copy     = SingleMetric(metrics, "CopyThroughput",       "GBPerSecond");
        double? latency  = SingleMetric(metrics, "LatencyApproximation", "NsPerAccess");
        double? bandwidth= SingleMetric(metrics, "SustainedBandwidth",   "GBPerSecond");

        if (alloc.HasValue)    workloadScores["AllocationStress"]     = Normalize(alloc.Value,     RefMemAlloc);
        if (copy.HasValue)     workloadScores["CopyThroughput"]       = Normalize(copy.Value,      RefMemCopy);
        if (latency.HasValue)  workloadScores["LatencyApproximation"] = NormalizeInverse(latency.Value, RefMemLatency);
        if (bandwidth.HasValue)workloadScores["SustainedBandwidth"]   = Normalize(bandwidth.Value, RefMemBandwidth);

        EmitWorkloadScores(session, memCase, "Memory", workloadScores, MemoryWorkloadWeights, allScores);

        double category = WeightedAverage(workloadScores, MemoryWorkloadWeights);
        EmitCategoryScore(session, memCase, "Memory", category, MemoryWeight, allScores);
        return category;
    }

    private double? ScoreDiskCategory(BenchmarkSession session,
        IReadOnlyList<BenchmarkCase> cases, List<BenchmarkScore> allScores)
    {
        var diskCase = cases.FirstOrDefault(c => c.Category == BenchmarkCategory.Disk);
        if (diskCase is null) return null;

        var workloadScores = new Dictionary<string, double>();
        var metrics        = diskCase.Metrics.ToLookup(m => m.MetricName);

        double? seqW = SingleMetric(metrics, "SequentialWrite", "MBPerSecond");
        double? seqR = SingleMetric(metrics, "SequentialRead",  "MBPerSecond");
        double? rndR = SingleMetric(metrics, "RandomRead4K",    "IOPS");
        double? rndW = SingleMetric(metrics, "RandomWrite4K",   "IOPS");

        if (seqW.HasValue) workloadScores["SequentialWrite"] = Normalize(seqW.Value, RefDiskSeqWrite);
        if (seqR.HasValue) workloadScores["SequentialRead"]  = Normalize(seqR.Value, RefDiskSeqRead);
        if (rndR.HasValue) workloadScores["RandomRead4K"]    = Normalize(rndR.Value, RefDiskRndRead);
        if (rndW.HasValue) workloadScores["RandomWrite4K"]   = Normalize(rndW.Value, RefDiskRndWrite);

        EmitWorkloadScores(session, diskCase, "Disk", workloadScores, DiskWorkloadWeights, allScores);

        double category = WeightedAverage(workloadScores, DiskWorkloadWeights);
        EmitCategoryScore(session, diskCase, "Disk", category, DiskWeight, allScores);
        return category;
    }

    // ── Math helpers ───────────────────────────────────────────────────────

    /// <summary>Higher raw = better. <c>raw / reference × 500</c>, clamped to [0, 1000].</summary>
    private static double Normalize(double raw, double reference)
        => Math.Clamp(raw / reference * 500.0, 0.0, 1000.0);

    /// <summary>Lower raw = better (e.g., latency). <c>reference / raw × 500</c>, clamped to [0, 1000].</summary>
    private static double NormalizeInverse(double raw, double reference)
        => raw <= 0 ? 0 : Math.Clamp(reference / raw * 500.0, 0.0, 1000.0);

    private static double WeightedAverage(
        Dictionary<string, double> scores,
        Dictionary<string, double> weights)
    {
        double weightSum  = 0;
        double weightedSum = 0;
        foreach (var (key, score) in scores)
        {
            if (!weights.TryGetValue(key, out double w)) continue;
            weightedSum += score * w;
            weightSum   += w;
        }
        return weightSum > 0 ? weightedSum / weightSum : 0;
    }

    private static double ComputeOverall(
        double? cpu, double? mem, double? disk, double? gpu)
    {
        double sum = 0, wSum = 0;
        if (cpu.HasValue)  { sum += cpu.Value  * CpuWeight;    wSum += CpuWeight; }
        if (mem.HasValue)  { sum += mem.Value  * MemoryWeight; wSum += MemoryWeight; }
        if (disk.HasValue) { sum += disk.Value * DiskWeight;   wSum += DiskWeight; }
        if (gpu.HasValue)  { sum += gpu.Value  * GpuWeight;    wSum += GpuWeight; }
        return wSum > 0 ? Math.Round(sum / wSum, 1) : 0;
    }

    private static ScoringBadge Classify(double score) => score switch
    {
        <= 200  => ScoringBadge.BelowAverage,
        <= 400  => ScoringBadge.Average,
        <= 600  => ScoringBadge.Good,
        <= 800  => ScoringBadge.VeryGood,
        <= 950  => ScoringBadge.Excellent,
        _       => ScoringBadge.Outstanding
    };

    // ── Metric extraction ──────────────────────────────────────────────────

    /// <summary>
    /// Looks up a metric by its fully-qualified name: <c>{workloadName}_{metricName}</c>.
    /// <para>
    /// The orchestrator (<see cref="Application.Services.BenchmarkOrchestrator"/>) is
    /// responsible for prefixing each <see cref="BenchmarkMetric.MetricName"/> with its
    /// parent workload name before persisting, e.g. "SingleThreadInteger_OpsPerSecond".
    /// This disambiguates workloads that report the same metric name (e.g. multiple
    /// workloads all reporting "OpsPerSecond").
    /// </para>
    /// </summary>
    private static double? SingleMetric(
        ILookup<string, BenchmarkMetric> byName,
        string workloadName,
        string metricName)
    {
        var metric = byName[$"{workloadName}_{metricName}"].FirstOrDefault();
        return metric?.Value;
    }

    // ── Score record emission ──────────────────────────────────────────────

    private void EmitWorkloadScores(
        BenchmarkSession session, BenchmarkCase bCase,
        string categoryName,
        Dictionary<string, double> scores,
        Dictionary<string, double> weights,
        List<BenchmarkScore> target)
    {
        foreach (var (workload, score) in scores)
        {
            double w = weights.TryGetValue(workload, out var wv) ? wv : 0;
            target.Add(new BenchmarkScore
            {
                Id                   = Guid.NewGuid(),
                SessionId            = session.Id,
                CaseId               = bCase.Id,
                ScoreLevel           = workload,
                ScoreName            = $"{categoryName}.{workload}",
                RawValue             = 0,
                NormalizedScore      = Math.Round(score, 1),
                Weight               = w,
                Badge                = Classify(score),
                ScoringSchemaVersion = SchemaVersion
            });
        }
    }

    private void EmitCategoryScore(
        BenchmarkSession session, BenchmarkCase bCase,
        string categoryName, double score, double categoryWeight,
        List<BenchmarkScore> target)
    {
        target.Add(new BenchmarkScore
        {
            Id                   = Guid.NewGuid(),
            SessionId            = session.Id,
            CaseId               = bCase.Id,
            ScoreLevel           = categoryName,
            ScoreName            = categoryName,
            RawValue             = 0,
            NormalizedScore      = Math.Round(score, 1),
            Weight               = categoryWeight,
            Badge                = Classify(score),
            ScoringSchemaVersion = SchemaVersion
        });
    }
}
