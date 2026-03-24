using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Telemetry;
using FullBenchmark.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Application.Services;

/// <summary>
/// Manages the live telemetry polling session for the current machine.
/// Maintains a ring buffer of recent samples for chart display and
/// exposes the session's events to the rest of the application.
/// </summary>
public sealed class TelemetryOrchestrator : IDisposable
{
    private readonly Func<Guid, ITelemetrySession> _sessionFactory;
    private readonly ILogger<TelemetryOrchestrator> _logger;

    private ITelemetrySession?          _session;
    private CancellationTokenSource?    _cts;
    private bool                        _disposed;

    /// <summary>Capacity: 300 samples = 5 minutes at 1 sample/s.</summary>
    private const int RingBufferCapacity = 300;
    private readonly TelemetryRingBuffer<TelemetrySample> _ringBuffer = new(RingBufferCapacity);

    public TelemetryOrchestrator(
        Func<Guid, ITelemetrySession>    sessionFactory,
        ILogger<TelemetryOrchestrator>   logger)
    {
        _sessionFactory = sessionFactory;
        _logger         = logger;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public bool IsRunning => _session?.IsRunning ?? false;

    /// <summary>
    /// Most-recently collected sample. Null before the first tick or when stopped.
    /// </summary>
    public TelemetrySample? LatestSample { get; private set; }

    /// <summary>Fires whenever a new <see cref="TelemetrySample"/> arrives.</summary>
    public event EventHandler<TelemetrySample>? SampleArrived;

    /// <summary>Fires on a telemetry collection error (non-fatal).</summary>
    public event EventHandler<Exception>? SampleError;

    /// <summary>
    /// Returns a snapshot of the ring buffer (oldest → newest).
    /// Suitable for rendering a history chart.
    /// </summary>
    public TelemetrySample[] GetRecentSamples() => _ringBuffer.ToArray();

    /// <summary>
    /// Starts the polling loop for the given machine profile.
    /// No-op if already running.
    /// </summary>
    public void Start(Guid machineProfileId, TimeSpan? interval = null)
    {
        if (IsRunning || _disposed) return;

        _cts     = new CancellationTokenSource();
        _session = _sessionFactory(machineProfileId);

        if (interval.HasValue)
            _session.PollingInterval = interval.Value;

        _session.SampleCollected += OnSampleCollected;
        _session.SampleFailed    += OnSampleFailed;
        _session.Start(_cts.Token);

        _logger.LogDebug("TelemetryOrchestrator started for machine {Id}.", machineProfileId);
    }

    /// <summary>Stops the polling loop. Ring buffer contents are preserved.</summary>
    public void Stop()
    {
        if (_session is null) return;

        _session.SampleCollected -= OnSampleCollected;
        _session.SampleFailed    -= OnSampleFailed;
        _session.Stop();
        _session.Dispose();
        _session = null;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _logger.LogDebug("TelemetryOrchestrator stopped.");
    }

    /// <summary>Clears the ring buffer (e.g., when switching views).</summary>
    public void ClearHistory() => _ringBuffer.Clear();

    // ── Event handlers ─────────────────────────────────────────────────────

    private void OnSampleCollected(object? _, TelemetrySample sample)
    {
        LatestSample = sample;
        _ringBuffer.Add(sample);
        SampleArrived?.Invoke(this, sample);
    }

    private void OnSampleFailed(object? _, Exception ex)
    {
        _logger.LogWarning(ex, "Telemetry sample error.");
        SampleError?.Invoke(this, ex);
    }

    // ── IDisposable ────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
