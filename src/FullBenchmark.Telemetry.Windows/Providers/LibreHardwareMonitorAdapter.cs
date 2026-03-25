using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

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
    private const string AgentDebugLogPath = "C:/Users/tr/OneDrive/Documents/GitHub/FullBenchmark/debug-969090.log";

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
                IsMotherboardEnabled = true, // Some systems expose CPU temp via motherboard/EC sensors.
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
        #region agent log
        AgentDebugLog("baseline", "H1", "LibreHardwareMonitorAdapter.ReadSensors:71", "ReadSensors entry", new { available = _available, hasComputer = _computer is not null, initialized = _initialized });
        #endregion
        if (!_available || _computer is null)
            return new SensorReading(null, null, null, null);

        try
        {
            double? cpuTemp = null, gpuTemp = null, gpuLoad = null;
            long?   gpuMem  = null;

            foreach (var hw in _computer.Hardware)
            {
                UpdateHardwareRecursive(hw);

                if (hw.HardwareType == HardwareType.Cpu)
                    cpuTemp = ExtractCpuTemp(hw);
                else if (hw.HardwareType == HardwareType.Motherboard && cpuTemp is null)
                    cpuTemp = ExtractCpuTemp(hw);
                else if (hw.HardwareType is HardwareType.GpuNvidia
                                         or HardwareType.GpuAmd
                                         or HardwareType.GpuIntel)
                    (gpuLoad, gpuTemp, gpuMem) = ExtractGpuMetrics(hw);
            }

            #region agent log
            AgentDebugLog("baseline", "H1", "LibreHardwareMonitorAdapter.ReadSensors:91", "ReadSensors result", new { cpuTemp, gpuTemp, gpuLoad, gpuMem, hardwareCount = _computer.Hardware.Count });
            #endregion
            return new SensorReading(cpuTemp, gpuLoad, gpuTemp, gpuMem);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "LibreHardwareMonitor sensor read failed");
            #region agent log
            AgentDebugLog("baseline", "H2", "LibreHardwareMonitorAdapter.ReadSensors:96", "ReadSensors exception", new { error = ex.GetType().Name, message = ex.Message });
            #endregion
            return new SensorReading(null, null, null, null);
        }
    }

    private static double? ExtractCpuTemp(IHardware hw)
    {
        var sensors = EnumerateTemperatureSensors(hw).ToList();

        // Prefer "CPU Package" or "Core (Tdie)" as the representative temperature
        var packageSensor = sensors.FirstOrDefault(
            s => s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)
              || s.Name.Contains("Tdie",    StringComparison.OrdinalIgnoreCase)
              || s.Name.Contains("CPU",     StringComparison.OrdinalIgnoreCase));

        var sensor = packageSensor ?? sensors.FirstOrDefault();
        double? selectedValue = sensor?.Value is float sv ? sv : null;
        // Some systems expose placeholder 0C values; treat them as invalid and fallback.
        if (selectedValue is <= 0)
        {
            sensor = sensors.FirstOrDefault(s => s.Value is float v && v > 0 && v < 150);
            selectedValue = sensor?.Value is float fv ? fv : null;
        }
        #region agent log
        AgentDebugLog("baseline", "H3", "LibreHardwareMonitorAdapter.ExtractCpuTemp:118", "CPU temperature sensor resolution", new
        {
            hardwareName = hw.Name,
            temperatureSensorCount = sensors.Count,
            selectedSensor = sensor?.Name,
            selectedValue,
            candidates = sensors.Select(s => new { s.Name, value = s.Value }).ToArray()
        });
        #endregion
        return selectedValue;
    }

    private static IEnumerable<ISensor> EnumerateTemperatureSensors(IHardware hw)
    {
        foreach (var sensor in hw.Sensors.Where(s => s.SensorType == SensorType.Temperature))
            yield return sensor;

        foreach (var sub in hw.SubHardware)
        {
            foreach (var sensor in EnumerateTemperatureSensors(sub))
                yield return sensor;
        }
    }

    private static void UpdateHardwareRecursive(IHardware hw)
    {
        hw.Update();
        foreach (var sub in hw.SubHardware)
            UpdateHardwareRecursive(sub);
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
