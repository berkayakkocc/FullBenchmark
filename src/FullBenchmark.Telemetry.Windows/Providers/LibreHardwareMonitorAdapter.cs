using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Telemetry.Windows.Providers;

/// <summary>
/// Wraps LibreHardwareMonitor to provide CPU temperatures, GPU load, GPU temperature,
/// and GPU memory usage. These metrics require either elevated privileges or the
/// WinRing0 driver bundled with LibreHardwareMonitor.
/// <para>
/// Initialises lazily on first read. If initialisation fails (e.g., no admin rights),
/// all methods return null — the rest of the telemetry pipeline continues unaffected.
/// </para>
/// </summary>
public sealed class LibreHardwareMonitorAdapter : IDisposable
{
    private readonly ILogger<LibreHardwareMonitorAdapter> _logger;

    private Computer? _computer;
    private bool      _available;
    private bool      _initialized;
    private bool      _disposed;

    public LibreHardwareMonitorAdapter(ILogger<LibreHardwareMonitorAdapter> logger)
        => _logger = logger;

    /// <summary>Attempts to open the hardware monitor. Safe to call multiple times.</summary>
    public void Initialize()
    {
        if (_initialized || _disposed) return;
        _initialized = true;

        try
        {
            _computer = new Computer
            {
                IsCpuEnabled    = true,
                IsGpuEnabled    = true,
                IsMemoryEnabled = false,   // RAM usage read via GlobalMemoryStatusEx (faster + no privilege)
                IsMotherboardEnabled = false,
                IsStorageEnabled     = false,
                IsNetworkEnabled     = false,
                IsBatteryEnabled     = false,
                IsControllerEnabled  = false
            };
            _computer.Open();
            _available = true;
            _logger.LogDebug("LibreHardwareMonitor opened successfully.");
        }
        catch (Exception ex)
        {
            _available = false;
            _logger.LogWarning(ex,
                "LibreHardwareMonitor could not be initialised. " +
                "CPU temperatures and GPU metrics will be unavailable. " +
                "Run the application as Administrator for full sensor access.");
        }
    }

    public readonly record struct SensorReading(
        double? CpuTemperatureCelsius,
        double? GpuUsagePercent,
        double? GpuTemperatureCelsius,
        long?   GpuMemoryUsedBytes);

    /// <summary>
    /// Reads all sensor values in a single sweep. Returns all-null on failure or unavailability.
    /// </summary>
    public SensorReading ReadSensors()
    {
        if (!_available || _computer is null)
            return new SensorReading(null, null, null, null);

        try
        {
            double? cpuTemp = null, gpuTemp = null, gpuLoad = null;
            long?   gpuMem  = null;

            foreach (var hw in _computer.Hardware)
            {
                hw.Update();

                if (hw.HardwareType == HardwareType.Cpu)
                    cpuTemp = ExtractCpuTemp(hw);
                else if (hw.HardwareType is HardwareType.GpuNvidia
                                         or HardwareType.GpuAmd
                                         or HardwareType.GpuIntel)
                    (gpuLoad, gpuTemp, gpuMem) = ExtractGpuMetrics(hw);
            }

            return new SensorReading(cpuTemp, gpuLoad, gpuTemp, gpuMem);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "LibreHardwareMonitor sensor read failed");
            return new SensorReading(null, null, null, null);
        }
    }

    private static double? ExtractCpuTemp(IHardware hw)
    {
        var sensors = hw.Sensors.Where(s => s.SensorType == SensorType.Temperature).ToList();

        // Prefer "CPU Package" or "Core (Tdie)" as the representative temperature
        var packageSensor = sensors.FirstOrDefault(
            s => s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)
              || s.Name.Contains("Tdie",    StringComparison.OrdinalIgnoreCase)
              || s.Name.Contains("CPU",     StringComparison.OrdinalIgnoreCase));

        var sensor = packageSensor ?? sensors.FirstOrDefault();
        return sensor?.Value is float v ? (double?)v : null;
    }

    private static (double? load, double? temp, long? memUsed) ExtractGpuMetrics(IHardware hw)
    {
        double? load = null, temp = null;
        long?   mem  = null;

        foreach (var sensor in hw.Sensors)
        {
            switch (sensor.SensorType)
            {
                case SensorType.Load when load is null &&
                    sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase):
                    if (sensor.Value is float lv) load = lv;
                    break;

                case SensorType.Temperature when temp is null:
                    if (sensor.Value is float tv) temp = tv;
                    break;

                case SensorType.SmallData when mem is null &&
                    sensor.Name.Contains("Memory Used", StringComparison.OrdinalIgnoreCase):
                    // SmallData reports MB
                    if (sensor.Value is float mv) mem = (long)(mv * 1024 * 1024);
                    break;
            }
        }

        return (load, temp, mem);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _computer?.Close(); } catch { }
    }
}
