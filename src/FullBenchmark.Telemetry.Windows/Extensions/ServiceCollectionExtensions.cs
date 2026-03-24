using FullBenchmark.Contracts.SystemInfo;
using FullBenchmark.Contracts.Telemetry;
using FullBenchmark.Telemetry.Windows.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Telemetry.Windows.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Windows-specific telemetry and system information providers.
    /// <para>
    /// <see cref="PerformanceCounterTelemetryProvider"/> and
    /// <see cref="LibreHardwareMonitorAdapter"/> are singletons because they hold
    /// expensive counter/hardware handles that must persist across reads.
    /// </para>
    /// </summary>
    public static IServiceCollection AddWindowsTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<PerformanceCounterTelemetryProvider>();
        services.AddSingleton<LibreHardwareMonitorAdapter>();
        services.AddSingleton<ITelemetryProvider, WindowsTelemetryProvider>();

        services.AddTransient<ISystemInfoProvider, WmiSystemInfoProvider>();

        // Factory for creating telemetry sessions bound to a specific machine profile
        services.AddTransient<Func<Guid, ITelemetrySession>>(sp => machineProfileId =>
        {
            var provider = sp.GetRequiredService<ITelemetryProvider>();
            var logger   = sp.GetRequiredService<ILogger<WindowsTelemetrySession>>();
            return new WindowsTelemetrySession(provider, machineProfileId, logger);
        });

        return services;
    }

    /// <summary>
    /// Initialises the PerformanceCounter pool and LibreHardwareMonitor adapter.
    /// Must be called once at application startup before telemetry is used.
    /// </summary>
    public static void InitialiseTelemetry(this IServiceProvider serviceProvider)
    {
        var perfCounters = serviceProvider.GetRequiredService<PerformanceCounterTelemetryProvider>();
        perfCounters.Initialize();

        var lhm = serviceProvider.GetRequiredService<LibreHardwareMonitorAdapter>();
        lhm.Initialize();
    }
}
