using FullBenchmark.Contracts.Benchmarks;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using System.Diagnostics;

namespace FullBenchmark.Benchmarks.Modules;

/// <summary>
/// Disk I/O benchmark suite — 4 workloads:
/// <list type="number">
///   <item>SequentialWrite — write <see cref="BenchmarkConfig.DiskTestFileSizeMB"/> MB sequentially, reports MB/s</item>
///   <item>SequentialRead  — read the file back sequentially, reports MB/s</item>
///   <item>RandomRead4K    — random 4 KB reads for <see cref="BenchmarkConfig.DiskRunSeconds"/> seconds, reports IOPS and MB/s</item>
///   <item>RandomWrite4K   — random 4 KB writes for <see cref="BenchmarkConfig.DiskRunSeconds"/> seconds, reports IOPS and MB/s</item>
/// </list>
/// The test file is created in <see cref="BenchmarkConfig.DiskTestTempPath"/> (defaults to <see cref="Path.GetTempPath"/>).
/// Cleanup is controlled by <see cref="BenchmarkConfig.DiskCleanupAfterTest"/>.
/// </summary>
public sealed class DiskBenchmarkModule : IBenchmarkModule
{
    public BenchmarkCategory Category    => BenchmarkCategory.Disk;
    public string            ModuleName  => "Disk Benchmark";
    public int               SchemaVersion => 1;

    public bool CanRunOnCurrentSystem(out string? reason)
    {
        reason = null;
        var tempDir = GetTempDir(null);
        try
        {
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            var probe = Path.Combine(tempDir, $"fb_probe_{Guid.NewGuid():N}.tmp");
            File.WriteAllBytes(probe, [0x00]);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex)
        {
            reason = $"Temp directory is not writable ({tempDir}): {ex.Message}";
            return false;
        }
    }

    public Task WarmupAsync(BenchmarkContext context, CancellationToken ct = default)
    {
        // Validate temp path and ensure directory exists
        var dir = GetTempDir(context.Config.DiskTestTempPath);
        Directory.CreateDirectory(dir);
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public async Task<BenchmarkModuleResult> RunAsync(BenchmarkContext context, CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var cfg       = context.Config;
        var tempDir   = GetTempDir(cfg.DiskTestTempPath);
        var testFile  = Path.Combine(tempDir, $"fb_bench_{context.SessionId:N}.bin");

        var results = new List<WorkloadResult>(4);

        try
        {
            // 1. Sequential Write
            ct.ThrowIfCancellationRequested();
            Report(context, "SequentialWrite", 0, "Writing test file…");
            results.Add(await SafeExecuteAsync("SequentialWrite",
                () => SequentialWriteAsync(testFile, cfg, ct)));

            // 2. Sequential Read
            ct.ThrowIfCancellationRequested();
            Report(context, "SequentialRead", 25, "Reading test file…");
            results.Add(await SafeExecuteAsync("SequentialRead",
                () => SequentialReadAsync(testFile, cfg, ct)));

            // 3. Random Read 4K
            ct.ThrowIfCancellationRequested();
            Report(context, "RandomRead4K", 50, "Random 4K reads…");
            results.Add(await SafeExecuteAsync("RandomRead4K",
                () => RandomReadAsync(testFile, cfg, ct)));

            // 4. Random Write 4K
            ct.ThrowIfCancellationRequested();
            Report(context, "RandomWrite4K", 75, "Random 4K writes…");
            results.Add(await SafeExecuteAsync("RandomWrite4K",
                () => RandomWriteAsync(testFile, cfg, ct)));
        }
        finally
        {
            if (cfg.DiskCleanupAfterTest)
                TryDelete(testFile);
        }

        Report(context, ModuleName, 100, "Complete");

        return new BenchmarkModuleResult
        {
            Category             = Category,
            WorkloadName         = ModuleName,
            Status               = BenchmarkStatus.Completed,
            StartedAt            = startedAt,
            CompletedAt          = DateTimeOffset.UtcNow,
            WorkloadResults      = results,
            ScoringSchemaVersion = SchemaVersion
        };
    }

    // ── Sequential Write ───────────────────────────────────────────────────

    private static Task<WorkloadResult> SequentialWriteAsync(
        string filePath, BenchmarkConfig cfg, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var started   = DateTimeOffset.UtcNow;
            long fileSize = (long)cfg.DiskTestFileSizeMB * 1024 * 1024;
            int  blockSize = cfg.DiskTestBlockSizeKB * 1024;
            var  buffer    = new byte[blockSize];
            new Random(42).NextBytes(buffer);

            var sw = Stopwatch.StartNew();
            using var fs = new FileStream(
                filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                blockSize, FileOptions.WriteThrough | FileOptions.SequentialScan);

            long written = 0;
            while (written < fileSize)
            {
                ct.ThrowIfCancellationRequested();
                int chunk = (int)Math.Min(blockSize, fileSize - written);
                fs.Write(buffer, 0, chunk);
                written += chunk;
            }
            fs.Flush(flushToDisk: true);
            sw.Stop();

            double mbPerSec = written / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;
            return Make("SequentialWrite", started,
            [
                M("MBPerSecond", mbPerSec, MetricUnit.MegabytesPerSecond),
                M("FileSizeMB",  written / 1024.0 / 1024.0, MetricUnit.Megabytes, isRaw: false)
            ]);
        }, ct);
    }

    // ── Sequential Read ────────────────────────────────────────────────────

    private static Task<WorkloadResult> SequentialReadAsync(
        string filePath, BenchmarkConfig cfg, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var started   = DateTimeOffset.UtcNow;
            int blockSize = cfg.DiskTestBlockSizeKB * 1024;
            var buffer    = new byte[blockSize];

            var sw = Stopwatch.StartNew();
            using var fs = new FileStream(
                filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                blockSize, FileOptions.SequentialScan);

            long bytesRead = 0;
            int  read;
            // Sequential read to EOF — partial reads at end-of-file are expected and intentional.
#pragma warning disable CA2022
            while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
#pragma warning restore CA2022
            {
                ct.ThrowIfCancellationRequested();
                bytesRead += read;
            }
            sw.Stop();

            double mbPerSec = bytesRead / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;
            return Make("SequentialRead", started,
            [
                M("MBPerSecond", mbPerSec, MetricUnit.MegabytesPerSecond)
            ]);
        }, ct);
    }

    // ── Random Read 4K ────────────────────────────────────────────────────

    private static Task<WorkloadResult> RandomReadAsync(
        string filePath, BenchmarkConfig cfg, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var started   = DateTimeOffset.UtcNow;
            const int Block = 4096;
            var buffer    = new byte[Block];
            var rng       = new Random(42);

            using var fs = new FileStream(
                filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                Block, FileOptions.RandomAccess);

            long numBlocks = fs.Length / Block;
            if (numBlocks == 0)
                return Make("RandomRead4K", started,
                    [M("IOPS", 0, MetricUnit.OperationsPerSecond)]);

            long deadline = Stopwatch.GetTimestamp() + (long)(cfg.DiskRunSeconds * Stopwatch.Frequency);
            long ops      = 0;
            var sw        = Stopwatch.StartNew();

            while (Stopwatch.GetTimestamp() < deadline)
            {
                ct.ThrowIfCancellationRequested();
                long blockIdx = (long)(rng.NextDouble() * numBlocks);
                fs.Seek(blockIdx * Block, SeekOrigin.Begin);
                fs.Read(buffer, 0, Block);
                ops++;
            }
            sw.Stop();

            double iops    = ops / sw.Elapsed.TotalSeconds;
            double mbPerSec = ops * Block / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;
            return Make("RandomRead4K", started,
            [
                M("IOPS",        iops,    MetricUnit.OperationsPerSecond),
                M("MBPerSecond", mbPerSec, MetricUnit.MegabytesPerSecond)
            ]);
        }, ct);
    }

    // ── Random Write 4K ───────────────────────────────────────────────────

    private static Task<WorkloadResult> RandomWriteAsync(
        string filePath, BenchmarkConfig cfg, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var started   = DateTimeOffset.UtcNow;
            const int Block = 4096;
            var buffer    = new byte[Block];
            new Random(99).NextBytes(buffer);
            var rng       = new Random(99);

            using var fs = new FileStream(
                filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite,
                Block, FileOptions.RandomAccess | FileOptions.WriteThrough);

            long numBlocks = new FileInfo(filePath).Length / Block;
            if (numBlocks == 0)
                return Make("RandomWrite4K", started,
                    [M("IOPS", 0, MetricUnit.OperationsPerSecond)]);

            long deadline = Stopwatch.GetTimestamp() + (long)(cfg.DiskRunSeconds * Stopwatch.Frequency);
            long ops      = 0;
            var sw        = Stopwatch.StartNew();

            while (Stopwatch.GetTimestamp() < deadline)
            {
                ct.ThrowIfCancellationRequested();
                long blockIdx = (long)(rng.NextDouble() * numBlocks);
                fs.Seek(blockIdx * Block, SeekOrigin.Begin);
                fs.Write(buffer, 0, Block);
                ops++;
            }
            sw.Stop();

            double iops    = ops / sw.Elapsed.TotalSeconds;
            double mbPerSec = ops * Block / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;
            return Make("RandomWrite4K", started,
            [
                M("IOPS",        iops,    MetricUnit.OperationsPerSecond),
                M("MBPerSecond", mbPerSec, MetricUnit.MegabytesPerSecond)
            ]);
        }, ct);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string GetTempDir(string? configured) =>
        string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "FullBenchmark")
            : configured;

    private static void Report(BenchmarkContext ctx, string workload,
        double pct, string msg) =>
        ctx.ProgressReporter.Report(new BenchmarkProgress
        { WorkloadName = workload, PercentComplete = pct, StatusMessage = msg });

    private static async Task<WorkloadResult> SafeExecuteAsync(
        string name, Func<Task<WorkloadResult>> fn)
    {
        try { return await fn(); }
        catch (OperationCanceledException)
        {
            return new WorkloadResult
            {
                WorkloadName = name,
                Status       = BenchmarkStatus.Cancelled,
                StartedAt    = DateTimeOffset.UtcNow,
                CompletedAt  = DateTimeOffset.UtcNow,
                ErrorMessage = "Cancelled"
            };
        }
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

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
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
