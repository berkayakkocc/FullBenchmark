using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Telemetry.Windows.Providers;

/// <summary>
/// Reads CPU, disk, and network telemetry via Windows Performance Counters.
/// <para>
/// <b>Initialization contract:</b> Call <see cref="Initialize"/> once before the first read.
/// The first read after initialization may return 0.0 for all values — that is expected
/// behaviour (PerformanceCounter requires two samples to compute a rate).
/// </para>
/// <para>Must be disposed to release counter handles.</para>
/// </summary>
public sealed class PerformanceCounterTelemetryProvider : IDisposable
{
    private readonly ILogger<PerformanceCounterTelemetryProvider> _logger;

    private PerformanceCounter?   _cpuTotalCounter;
    private PerformanceCounter[]? _perCoreCpuCounters;

    private PerformanceCounter?   _diskReadBytesCounter;
    private PerformanceCounter?   _diskWriteBytesCounter;
    private PerformanceCounter?   _diskActiveTimeCounter;

    private (PerformanceCounter Sent, PerformanceCounter Recv)[] _networkCounters
        = Array.Empty<(PerformanceCounter, PerformanceCounter)>();

    private bool _initialized;
    private bool _disposed;

    public bool IsInitialized => _initialized;

    public PerformanceCounterTelemetryProvider(
        ILogger<PerformanceCounterTelemetryProvider> logger) => _logger = logger;

    /// <summary>
    /// Creates all counter instances and performs the mandatory "warm-up" read.
    /// Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    public void Initialize()
    {
        if (_initialized || _disposed) return;

        InitializeCpuCounters();
        InitializeDiskCounters();
        InitializeNetworkCounters();

        // Perform initial NextValue() calls to seed the rate computation baseline.
        // Values returned here are discarded.
        _ = ReadCpuRaw();
        _ = ReadDiskRaw();
        _ = ReadNetworkRaw();

        _initialized = true;
        _logger.LogDebug("PerformanceCounterTelemetryProvider initialised.");
    }

    // ─── CPU ─────────────────────────────────────────────────────────────────

    private void InitializeCpuCounters()
    {
        try
        {
            _cpuTotalCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create total CPU PerformanceCounter");
        }

        try
        {
            // Enumerate per-core instances — named "0", "1", … (excluding "_Total")
            var instances = new PerformanceCounterCategory("Processor")
                .GetInstanceNames()
                .Where(n => n != "_Total")
                .OrderBy(n => int.TryParse(n, out var i) ? i : int.MaxValue)
                .ToArray();

            _perCoreCpuCounters = instances
                .Select(inst =>
                {
                    try { return new PerformanceCounter("Processor", "% Processor Time", inst, true); }
                    catch { return null; }
                })
                .Where(c => c != null)
                .ToArray()!;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enumerate per-core CPU counters");
            _perCoreCpuCounters = Array.Empty<PerformanceCounter>();
        }
    }

    public readonly record struct CpuReading(double TotalPercent, double[] CorePercents);

    public CpuReading ReadCpu()
    {
        if (!_initialized) return new CpuReading(0, Array.Empty<double>());
        return ReadCpuRaw();
    }

    private CpuReading ReadCpuRaw()
    {
        float total = 0;
        double[] coreValues = Array.Empty<double>();

        try { total = _cpuTotalCounter?.NextValue() ?? 0; }
        catch (Exception ex) { _logger.LogTrace(ex, "CPU total counter read failed"); }

        if (_perCoreCpuCounters is { Length: > 0 })
        {
            coreValues = new double[_perCoreCpuCounters.Length];
            for (var i = 0; i < _perCoreCpuCounters.Length; i++)
            {
                try { coreValues[i] = _perCoreCpuCounters[i].NextValue(); }
                catch { coreValues[i] = 0; }
            }
        }

        return new CpuReading(Math.Min(total, 100.0), coreValues);
    }

    // ─── Disk ────────────────────────────────────────────────────────────────

    private void InitializeDiskCounters()
    {
        try
        {
            _diskReadBytesCounter  = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec",  "_Total", true);
            _diskWriteBytesCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total", true);
            _diskActiveTimeCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time",          "_Total", true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create disk PerformanceCounters");
        }
    }

    public readonly record struct DiskReading(long ReadBytesPerSec, long WriteBytesPerSec, double? ActivePercent);

    public DiskReading ReadDisk()
    {
        if (!_initialized) return new DiskReading(0, 0, null);
        return ReadDiskRaw();
    }

    private DiskReading ReadDiskRaw()
    {
        long read = 0, write = 0;
        double? active = null;

        try { read  = (long)(_diskReadBytesCounter?.NextValue()  ?? 0); } catch { }
        try { write = (long)(_diskWriteBytesCounter?.NextValue() ?? 0); } catch { }
        try
        {
            var v = _diskActiveTimeCounter?.NextValue();
            if (v.HasValue) active = Math.Min(v.Value, 100.0);
        }
        catch { }

        return new DiskReading(read, write, active);
    }

    // ─── Network ─────────────────────────────────────────────────────────────

    private void InitializeNetworkCounters()
    {
        try
        {
            var instances = new PerformanceCounterCategory("Network Interface").GetInstanceNames();
            var pairs = new List<(PerformanceCounter, PerformanceCounter)>();

            foreach (var inst in instances)
            {
                try
                {
                    var sent = new PerformanceCounter("Network Interface", "Bytes Sent/sec",     inst, true);
                    var recv = new PerformanceCounter("Network Interface", "Bytes Received/sec", inst, true);
                    pairs.Add((sent, recv));
                }
                catch { /* skip unreachable adapter */ }
            }

            _networkCounters = pairs.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enumerate network interface counters");
        }
    }

    public readonly record struct NetworkReading(long SentBytesPerSec, long ReceivedBytesPerSec);

    public NetworkReading ReadNetwork()
    {
        if (!_initialized) return new NetworkReading(0, 0);
        return ReadNetworkRaw();
    }

    private NetworkReading ReadNetworkRaw()
    {
        long totalSent = 0, totalRecv = 0;
        foreach (var (sent, recv) in _networkCounters)
        {
            try { totalSent += (long)sent.NextValue(); } catch { }
            try { totalRecv += (long)recv.NextValue(); } catch { }
        }
        return new NetworkReading(totalSent, totalRecv);
    }

    // ─── Disposal ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cpuTotalCounter?.Dispose();
        if (_perCoreCpuCounters != null)
            foreach (var c in _perCoreCpuCounters) c?.Dispose();

        _diskReadBytesCounter?.Dispose();
        _diskWriteBytesCounter?.Dispose();
        _diskActiveTimeCounter?.Dispose();

        foreach (var (sent, recv) in _networkCounters)
        {
            sent?.Dispose();
            recv?.Dispose();
        }
    }
}
