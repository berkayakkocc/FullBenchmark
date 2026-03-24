using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Telemetry;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Telemetry.Windows.Providers;

/// <summary>
/// Manages a continuous polling loop backed by <see cref="ITelemetryProvider"/>.
/// Uses <see cref="PeriodicTimer"/> for precise, drift-resistant interval behaviour.
/// Events are raised on a thread-pool thread — WPF callers must marshal to the UI thread.
/// </summary>
public sealed class WindowsTelemetrySession : ITelemetrySession
{
    private readonly ITelemetryProvider _provider;
    private readonly Guid               _machineProfileId;
    private readonly ILogger<WindowsTelemetrySession> _logger;

    private PeriodicTimer?       _timer;
    private CancellationTokenSource? _cts;
    private Task?                _pollingTask;
    private TimeSpan             _pollingInterval = TimeSpan.FromSeconds(1);
    private bool                 _disposed;

    public bool     IsRunning       { get; private set; }
    public TimeSpan PollingInterval
    {
        get => _pollingInterval;
        set
        {
            if (value < TimeSpan.FromMilliseconds(100))
                throw new ArgumentOutOfRangeException(nameof(value), "Minimum polling interval is 100 ms.");
            _pollingInterval = value;
        }
    }

    public event EventHandler<TelemetrySample>? SampleCollected;
    public event EventHandler<Exception>?       SampleFailed;

    public WindowsTelemetrySession(
        ITelemetryProvider provider,
        Guid               machineProfileId,
        ILogger<WindowsTelemetrySession> logger)
    {
        _provider         = provider;
        _machineProfileId = machineProfileId;
        _logger           = logger;
    }

    public void Start(CancellationToken externalCt = default)
    {
        if (IsRunning || _disposed) return;

        _cts  = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        _timer = new PeriodicTimer(_pollingInterval);
        IsRunning = true;

        _pollingTask = Task.Run(() => RunLoopAsync(_cts.Token), _cts.Token);
        _logger.LogDebug("Telemetry session started (interval={Interval}ms).",
            (int)_pollingInterval.TotalMilliseconds);
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        _timer?.Dispose();
        IsRunning = false;
        _logger.LogDebug("Telemetry session stopped.");
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!await _timer!.WaitForNextTickAsync(ct)) break;
            }
            catch (OperationCanceledException) { break; }

            try
            {
                var sample = await _provider.ReadSampleAsync(_machineProfileId, ct);
                SampleCollected?.Invoke(this, sample);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telemetry sample collection failed");
                SampleFailed?.Invoke(this, ex);
            }
        }

        IsRunning = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts?.Dispose();
    }
}
