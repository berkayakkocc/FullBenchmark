using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Telemetry;
using FullBenchmark.Telemetry.Windows.Native;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Management;
using System.Text.Json;

namespace FullBenchmark.Telemetry.Windows.Providers;

/// <summary>
/// Composite <see cref="ITelemetryProvider"/> for Windows.
/// Combines:
/// <list type="bullet">
///   <item>PerformanceCounters — CPU usage per core + aggregate, disk throughput, network throughput</item>
///   <item>GlobalMemoryStatusEx (P/Invoke) — physical/virtual memory, faster and more accurate than WMI</item>
///   <item>LibreHardwareMonitor — CPU/GPU temperatures, GPU load/memory (requires elevated privileges)</item>
///   <item>GetSystemPowerStatus (P/Invoke) — battery charge and AC state</item>
/// </list>
/// </summary>
public sealed class WindowsTelemetryProvider : ITelemetryProvider
{
    private readonly PerformanceCounterTelemetryProvider _perfCounters;
    private readonly LibreHardwareMonitorAdapter         _lhm;
    private readonly ILogger<WindowsTelemetryProvider>   _logger;
    private const string AgentDebugLogPath = "C:/Users/tr/OneDrive/Documents/GitHub/FullBenchmark/debug-969090.log";
    private bool _disposed;

    public WindowsTelemetryProvider(
        PerformanceCounterTelemetryProvider perfCounters,
        LibreHardwareMonitorAdapter         lhm,
        ILogger<WindowsTelemetryProvider>   logger)
    {
        _perfCounters = perfCounters;
        _lhm          = lhm;
        _logger       = logger;
    }

    public Task<bool> IsAvailableAsync()
        => Task.FromResult(_perfCounters.IsInitialized);

    /// <summary>
    /// Collects one telemetry snapshot synchronously (on the calling thread).
    /// The caller is responsible for running this on an appropriate thread.
    /// Never throws — unavailable metrics are null.
    /// </summary>
    public Task<TelemetrySample> ReadSampleAsync(
        Guid machineProfileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var sample = new TelemetrySample
        {
            MachineProfileId = machineProfileId,
            CapturedAt       = DateTimeOffset.UtcNow
        };

        // ── CPU (PerformanceCounter) ──────────────────────────────────────────
        try
        {
            var cpu = _perfCounters.ReadCpu();
            sample.CpuUsagePercent = cpu.TotalPercent;
            sample.CpuCoreUsages   = new List<double>(cpu.CorePercents);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "CPU counter read failed");
        }

        // ── Memory (GlobalMemoryStatusEx) ─────────────────────────────────────
        try
        {
            var mem = NativeMemoryReader.ReadMemory();
            if (mem.HasValue)
            {
                sample.MemoryUsedBytes      = mem.Value.TotalPhysicalBytes - mem.Value.AvailablePhysicalBytes;
                sample.MemoryAvailableBytes = mem.Value.AvailablePhysicalBytes;

                if (mem.Value.TotalPageFileBytes > 0)
                {
                    var pageFree = mem.Value.AvailablePageFileBytes;
                    var pageTotal = mem.Value.TotalPageFileBytes;
                    sample.PageFileUsagePercent =
                        (1.0 - (double)pageFree / pageTotal) * 100.0;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Memory read failed");
        }

        // ── Disk (PerformanceCounter) ─────────────────────────────────────────
        try
        {
            var disk = _perfCounters.ReadDisk();
            sample.DiskReadBytesPerSec    = disk.ReadBytesPerSec;
            sample.DiskWriteBytesPerSec   = disk.WriteBytesPerSec;
            sample.DiskActiveTimePercent  = disk.ActivePercent;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Disk counter read failed");
        }

        // ── Network (PerformanceCounter) ──────────────────────────────────────
        try
        {
            var net = _perfCounters.ReadNetwork();
            sample.NetworkSentBytesPerSec     = net.SentBytesPerSec;
            sample.NetworkReceivedBytesPerSec = net.ReceivedBytesPerSec;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Network counter read failed");
        }

        // ── Hardware sensors (LibreHardwareMonitor) ───────────────────────────
        try
        {
            var sensors = _lhm.ReadSensors();
            sample.CpuTemperatureCelsius = sensors.CpuTemperatureCelsius;
            sample.GpuUsagePercent       = sensors.GpuUsagePercent;
            sample.GpuTemperatureCelsius = sensors.GpuTemperatureCelsius;
            sample.GpuMemoryUsedBytes    = sensors.GpuMemoryUsedBytes;
            #region agent log
            AgentDebugLog("baseline", "H4", "WindowsTelemetryProvider.ReadSampleAsync:122", "Provider sensor assignment", new { sample.CpuTemperatureCelsius, sample.GpuTemperatureCelsius, sample.GpuUsagePercent });
            #endregion

            // Some Ryzen systems report 0C/null via LHM CPU sensor; fallback to WMI thermal zones.
            if (!sample.CpuTemperatureCelsius.HasValue)
            {
                sample.CpuTemperatureCelsius = TryReadCpuTempFromWmi();
                #region agent log
                AgentDebugLog("baseline", "H6", "WindowsTelemetryProvider.ReadSampleAsync:130", "WMI CPU temp fallback", new { sample.CpuTemperatureCelsius });
                #endregion
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "LHM sensor read failed");
            #region agent log
            AgentDebugLog("baseline", "H2", "WindowsTelemetryProvider.ReadSampleAsync:126", "Provider sensor exception", new { error = ex.GetType().Name, message = ex.Message });
            #endregion
        }

        // ── Power (GetSystemPowerStatus) ──────────────────────────────────────
        try
        {
            var power = NativeMemoryReader.ReadPower();
            sample.IsOnAcPower          = power.IsOnAcPower;
            sample.BatteryChargePercent = power.BatteryChargePercent;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Power status read failed");
        }

        return Task.FromResult(sample);
    }

    private static double? TryReadCpuTempFromWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject obj in searcher.Get())
            {
                var raw = obj["CurrentTemperature"];
                if (raw is null) continue;
                double kelvinTimes10 = Convert.ToDouble(raw);
                double celsius = (kelvinTimes10 / 10.0) - 273.15;
                if (celsius > 0 && celsius < 150)
                    return Math.Round(celsius, 1);
            }
        }
        catch
        {
        }
        return null;
    }

    private static void AgentDebugLog(string runId, string hypothesisId, string location, string message, object data)
    {
        try
        {
            var payload = new
            {
                sessionId = "969090",
                runId,
                hypothesisId,
                location,
                message,
                data,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            File.AppendAllText(AgentDebugLogPath, JsonSerializer.Serialize(payload) + Environment.NewLine);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _perfCounters.Dispose();
        _lhm.Dispose();
    }
}
