using FullBenchmark.Contracts.Domain.Entities;

namespace FullBenchmark.Contracts.Telemetry;

/// <summary>
/// Manages a continuous telemetry polling loop backed by <see cref="ITelemetryProvider"/>.
/// Raises <see cref="SampleCollected"/> on each successful tick.
/// </summary>
public interface ITelemetrySession : IDisposable
{
    bool     IsRunning       { get; }
    TimeSpan PollingInterval { get; set; }

    /// <summary>Raised on the thread-pool; callers must marshal to UI thread if needed.</summary>
    event EventHandler<TelemetrySample> SampleCollected;
    event EventHandler<Exception>       SampleFailed;

    void Start(CancellationToken ct = default);
    void Stop();
}
