using FullBenchmark.Contracts.Domain.Entities;

namespace FullBenchmark.Contracts.Telemetry;

/// <summary>
/// Abstraction over a platform-specific telemetry backend.
/// One implementation: WindowsTelemetryProvider (composite WMI + PerformanceCounter + LibreHardwareMonitor).
/// </summary>
public interface ITelemetryProvider : IDisposable
{
    /// <summary>Returns false when required counters or sensors are unavailable on this machine.</summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Takes a single synchronous telemetry reading.
    /// Never throws — unavailable metrics are represented as null.
    /// </summary>
    Task<TelemetrySample> ReadSampleAsync(Guid machineProfileId, CancellationToken ct = default);
}
