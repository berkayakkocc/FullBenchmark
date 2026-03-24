using FullBenchmark.Contracts.Benchmarks;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using System.Diagnostics;
using System.Security.Cryptography;

namespace FullBenchmark.Benchmarks.Modules;

/// <summary>
/// CPU benchmark suite — 5 workloads:
/// <list type="number">
///   <item>SingleThreadInteger  — integer ALU throughput (single thread, LCG)</item>
///   <item>MultiThreadInteger   — integer ALU throughput (all logical cores)</item>
///   <item>SingleThreadFloat    — FP throughput (single thread, sqrt/mul)</item>
///   <item>MultiThreadFloat     — FP throughput (all logical cores)</item>
///   <item>HashThroughput       — SHA-256 throughput in MB/s</item>
/// </list>
/// All workloads run for <see cref="BenchmarkConfig.CpuRunSeconds"/> seconds.
/// </summary>
public sealed class CpuBenchmarkModule : IBenchmarkModule
{
    public BenchmarkCategory Category    => BenchmarkCategory.Cpu;
    public string            ModuleName  => "CPU Benchmark";
    public int               SchemaVersion => 1;

    public bool CanRunOnCurrentSystem(out string? reason) { reason = null; return true; }

    public Task WarmupAsync(BenchmarkContext context, CancellationToken ct = default)
    {
        // JIT warm-up: short burst of the heaviest path
        RunIntegerCore(Math.Max(1, context.Config.WarmupSeconds / 4), singleThread: true);
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task<BenchmarkModuleResult> RunAsync(BenchmarkContext context, CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        int dur       = context.Config.CpuRunSeconds;

        (string Name, Func<WorkloadResult> Fn)[] workloads =
        [
            ("SingleThreadInteger", () => StIntegerWorkload(dur)),
            ("MultiThreadInteger",  () => MtIntegerWorkload(dur)),
            ("SingleThreadFloat",   () => StFloatWorkload(dur)),
            ("MultiThreadFloat",    () => MtFloatWorkload(dur)),
            ("HashThroughput",      () => HashThroughputWorkload(dur)),
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

    // ── Workload entry points ──────────────────────────────────────────────

    private static WorkloadResult StIntegerWorkload(int seconds)
    {
        var t = DateTimeOffset.UtcNow;
        double ops = RunIntegerCore(seconds, singleThread: true);
        return Make("SingleThreadInteger", t, [M("OpsPerSecond", ops, MetricUnit.OperationsPerSecond)]);
    }

    private static WorkloadResult MtIntegerWorkload(int seconds)
    {
        var t = DateTimeOffset.UtcNow;
        double ops = RunIntegerCore(seconds, singleThread: false);
        return Make("MultiThreadInteger", t, [M("OpsPerSecond", ops, MetricUnit.OperationsPerSecond)]);
    }

    private static WorkloadResult StFloatWorkload(int seconds)
    {
        var t = DateTimeOffset.UtcNow;
        double ops = RunFloatCore(seconds, singleThread: true);
        return Make("SingleThreadFloat", t, [M("OpsPerSecond", ops, MetricUnit.OperationsPerSecond)]);
    }

    private static WorkloadResult MtFloatWorkload(int seconds)
    {
        var t = DateTimeOffset.UtcNow;
        double ops = RunFloatCore(seconds, singleThread: false);
        return Make("MultiThreadFloat", t, [M("OpsPerSecond", ops, MetricUnit.OperationsPerSecond)]);
    }

    private static WorkloadResult HashThroughputWorkload(int seconds)
    {
        var t = DateTimeOffset.UtcNow;
        double mbps = RunSha256Core(seconds);
        return Make("HashThroughput", t, [M("MBPerSecond", mbps, MetricUnit.MegabytesPerSecond)]);
    }

    // ── Core measurement kernels ───────────────────────────────────────────

    private static double RunIntegerCore(int seconds, bool singleThread)
    {
        long deadline = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);

        if (singleThread)
        {
            long acc = 1L, count = 0;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                // 32 multiply-add-XOR ops per outer iteration
                for (int i = 0; i < 32; i++)
                    acc = (acc * 6364136223846793005L + 1442695040888963407L) ^ (acc >> 33);
                count += 32;
            }
            GC.KeepAlive(acc);
            return count / (double)seconds;
        }

        int cores = Environment.ProcessorCount;
        long[] perCore = new long[cores];
        Parallel.For(0, cores, c =>
        {
            long a = (long)(c + 1), n = 0;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                for (int i = 0; i < 32; i++)
                    a = (a * 6364136223846793005L + 1442695040888963407L) ^ (a >> 33);
                n += 32;
            }
            GC.KeepAlive(a);
            perCore[c] = n;
        });
        return perCore.Sum() / (double)seconds;
    }

    private static double RunFloatCore(int seconds, bool singleThread)
    {
        long deadline = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);

        if (singleThread)
        {
            double acc = 1.0, count = 0;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                for (int i = 0; i < 32; i++)
                    acc = Math.Sqrt(acc * 1.0000001 + 0.5) * Math.PI;
                count += 32;
            }
            GC.KeepAlive(acc);
            return count / seconds;
        }

        int cores = Environment.ProcessorCount;
        double[] perCore = new double[cores];
        Parallel.For(0, cores, c =>
        {
            double a = (double)(c + 1), n = 0;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                for (int i = 0; i < 32; i++)
                    a = Math.Sqrt(a * 1.0000001 + 0.5) * Math.PI;
                n += 32;
            }
            GC.KeepAlive(a);
            perCore[c] = n;
        });
        return perCore.Sum() / seconds;
    }

    private static double RunSha256Core(int seconds)
    {
        const int ChunkSize = 65536; // 64 KB
        var data = new byte[ChunkSize];
        new Random(42).NextBytes(data);

        long deadline = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);
        long bytesHashed = 0;
        using var sha = SHA256.Create();
        while (Stopwatch.GetTimestamp() < deadline)
        {
            sha.ComputeHash(data);
            bytesHashed += ChunkSize;
        }
        return bytesHashed / 1024.0 / 1024.0 / seconds;
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

    private static BenchmarkMetric M(string name, double value, MetricUnit unit) => new()
    {
        MetricName = name,
        Value      = value,
        Unit       = unit,
        IsRawValue = true,
        CapturedAt = DateTimeOffset.UtcNow
    };
}
