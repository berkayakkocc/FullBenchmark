using FullBenchmark.Contracts.Benchmarks;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FullBenchmark.Benchmarks.Modules;

/// <summary>
/// Memory benchmark suite — 4 workloads:
/// <list type="number">
///   <item>AllocationStress     — GC allocation throughput in MB/s</item>
///   <item>CopyThroughput       — Buffer.BlockCopy bandwidth in GB/s</item>
///   <item>LatencyApproximation — random pointer-chase access time in ns/op (exceeds L3)</item>
///   <item>SustainedBandwidth   — sequential read bandwidth in GB/s</item>
/// </list>
/// </summary>
public sealed class MemoryBenchmarkModule : IBenchmarkModule
{
    public BenchmarkCategory Category    => BenchmarkCategory.Memory;
    public string            ModuleName  => "Memory Benchmark";
    public int               SchemaVersion => 1;

    public bool CanRunOnCurrentSystem(out string? reason) { reason = null; return true; }

    public Task WarmupAsync(BenchmarkContext context, CancellationToken ct = default)
    {
        // Touch a small array to warm JIT paths
        var warmup = new byte[65536];
        Buffer.BlockCopy(warmup, 0, warmup, 32768, 32768);
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task<BenchmarkModuleResult> RunAsync(BenchmarkContext context, CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        int dur       = context.Config.MemoryRunSeconds;

        (string Name, Func<WorkloadResult> Fn)[] workloads =
        [
            ("AllocationStress",     () => AllocationStressWorkload(dur)),
            ("CopyThroughput",       () => CopyThroughputWorkload(dur)),
            ("LatencyApproximation", () => LatencyWorkload()),
            ("SustainedBandwidth",   () => SustainedBandwidthWorkload(dur)),
        ];

        var results = new List<WorkloadResult>(workloads.Length);
        for (int i = 0; i < workloads.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            context.ProgressReporter.Report(new BenchmarkProgress
            {
                WorkloadName    = workloads[i].Name,
                PercentComplete = (double)i / workloads.Length * 100,
                StatusMessage   = $"Running {workloads[i].Name}…"
            });
            results.Add(SafeExecute(workloads[i].Name, workloads[i].Fn));
        }

        context.ProgressReporter.Report(new BenchmarkProgress
        { WorkloadName = ModuleName, PercentComplete = 100, StatusMessage = "Complete" });

        return Task.FromResult(new BenchmarkModuleResult
        {
            Category             = Category,
            WorkloadName         = ModuleName,
            Status               = BenchmarkStatus.Completed,
            StartedAt            = startedAt,
            CompletedAt          = DateTimeOffset.UtcNow,
            WorkloadResults      = results,
            ScoringSchemaVersion = SchemaVersion
        });
    }

    // ── Workload implementations ───────────────────────────────────────────

    /// <summary>
    /// Measures GC allocation throughput.
    /// Allocates many small arrays of varying sizes and measures total bytes allocated / second.
    /// </summary>
    private static WorkloadResult AllocationStressWorkload(int seconds)
    {
        var started = DateTimeOffset.UtcNow;

        long allocBefore = GC.GetTotalAllocatedBytes(precise: false);
        long deadline    = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);

        var live = new byte[100][];   // keep a small fraction alive to create GC pressure
        int liveIdx = 0;

        while (Stopwatch.GetTimestamp() < deadline)
        {
            for (int i = 0; i < 200; i++)
            {
                // Cycle through 512 B, 1 KB, 2 KB, 4 KB allocations
                var arr = new byte[512 << (i & 3)];
                // Commit pages so the allocator does real work
                arr[0] = (byte)i;
                arr[arr.Length - 1] = (byte)i;
                // Keep every 20th array alive to exercise GC
                if (i % 20 == 0) { live[liveIdx % 100] = arr; liveIdx++; }
            }
        }

        long allocAfter = GC.GetTotalAllocatedBytes(precise: false);
        GC.KeepAlive(live);

        double mbAllocated = (allocAfter - allocBefore) / 1024.0 / 1024.0;
        double mbPerSec    = mbAllocated / seconds;

        return Make("AllocationStress", started,
        [
            M("MBPerSecond", mbPerSec, MetricUnit.MegabytesPerSecond),
            M("TotalMBAllocated", mbAllocated, MetricUnit.Megabytes, isRaw: false)
        ]);
    }

    /// <summary>
    /// Measures raw memory copy bandwidth using <see cref="Buffer.BlockCopy"/>.
    /// Pre-allocates a 128 MB source and destination buffer; measures sustained copy rate.
    /// </summary>
    private static WorkloadResult CopyThroughputWorkload(int seconds)
    {
        var started = DateTimeOffset.UtcNow;

        const int BufSize = 128 * 1024 * 1024; // 128 MB
        var src = new byte[BufSize];
        var dst = new byte[BufSize];
        new Random(42).NextBytes(src);          // commit pages

        long deadline   = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);
        long bytesCopied = 0;
        while (Stopwatch.GetTimestamp() < deadline)
        {
            Buffer.BlockCopy(src, 0, dst, 0, BufSize);
            bytesCopied += BufSize;
        }
        GC.KeepAlive(dst);

        double gbPerSec = bytesCopied / 1e9 / seconds;
        return Make("CopyThroughput", started,
        [
            M("GBPerSecond", gbPerSec, MetricUnit.GigabytesPerSecond)
        ]);
    }

    /// <summary>
    /// Approximates DRAM access latency via random pointer-chasing through a 32 MB array.
    /// The working set deliberately exceeds typical L3 caches (8–32 MB),
    /// ensuring accesses go to main memory.
    /// Reports nanoseconds per random access.
    /// </summary>
    private static WorkloadResult LatencyWorkload()
    {
        var started = DateTimeOffset.UtcNow;

        // 8M ints = 32 MB — exceeds L3 on most consumer CPUs
        const int n = 8 * 1024 * 1024;
        var indices = new int[n];
        for (int i = 0; i < n; i++) indices[i] = i;

        // Fisher-Yates shuffle → random permutation (not timed)
        var rng = new Random(42);
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Pointer-chase: each access depends on the previous result — prevents prefetch
        const int Iterations = 2_000_000;
        var sw  = Stopwatch.StartNew();
        int idx = 0;
        for (int k = 0; k < Iterations; k++)
            idx = indices[idx];
        double elapsedNs = sw.Elapsed.TotalNanoseconds;
        GC.KeepAlive(idx);

        double nsPerAccess = elapsedNs / Iterations;
        return Make("LatencyApproximation", started,
        [
            M("NsPerAccess", nsPerAccess, MetricUnit.Nanoseconds)
        ]);
    }

    /// <summary>
    /// Measures sustained sequential read bandwidth through a 128 MB buffer.
    /// Uses <see cref="MemoryMarshal"/> cast to long to enable JIT vectorization.
    /// Reports GB/s.
    /// </summary>
    private static WorkloadResult SustainedBandwidthWorkload(int seconds)
    {
        var started = DateTimeOffset.UtcNow;

        const int BufSize = 128 * 1024 * 1024; // 128 MB
        var buffer = new byte[BufSize];
        new Random(42).NextBytes(buffer);   // commit pages and prevent COW

        var longView = MemoryMarshal.Cast<byte, long>(buffer.AsSpan());
        long deadline  = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);
        long bytesRead = 0;
        long checksum  = 0;

        while (Stopwatch.GetTimestamp() < deadline)
        {
            // Sequential pass — JIT will auto-vectorize this loop
            for (int i = 0; i < longView.Length; i++)
                checksum += longView[i];
            bytesRead += BufSize;
        }
        GC.KeepAlive(checksum);

        double gbPerSec = bytesRead / 1e9 / seconds;
        return Make("SustainedBandwidth", started,
        [
            M("GBPerSecond", gbPerSec, MetricUnit.GigabytesPerSecond)
        ]);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static WorkloadResult SafeExecute(string name, Func<WorkloadResult> fn)
    {
        try { return fn(); }
        catch (Exception ex)
        {
            return new WorkloadResult
            {
                WorkloadName = name,
                Status       = BenchmarkStatus.Failed,
                StartedAt    = DateTimeOffset.UtcNow,
                CompletedAt  = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    private static WorkloadResult Make(string name, DateTimeOffset started,
        IReadOnlyList<BenchmarkMetric> metrics) => new()
    {
        WorkloadName = name,
        Status       = BenchmarkStatus.Completed,
        StartedAt    = started,
        CompletedAt  = DateTimeOffset.UtcNow,
        Metrics      = metrics
    };

    private static BenchmarkMetric M(string name, double value, MetricUnit unit,
        bool isRaw = true) => new()
    {
        MetricName = name,
        Value      = value,
        Unit       = unit,
        IsRawValue = isRaw,
        CapturedAt = DateTimeOffset.UtcNow
    };
}
