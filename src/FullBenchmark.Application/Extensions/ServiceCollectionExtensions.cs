using FullBenchmark.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FullBenchmark.Application.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application-layer services.
    /// Infrastructure, telemetry, benchmark modules, and scoring must be registered
    /// separately before calling this method.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Singletons — stateful, shared across the app lifetime
        services.AddSingleton<SystemInfoService>();
        services.AddSingleton<TelemetryOrchestrator>();

        // Transient — stateless or short-lived per operation
        services.AddTransient<ScoringCoordinator>();
        services.AddTransient<BenchmarkOrchestrator>();
        services.AddTransient<ComparisonService>();
        services.AddTransient<HistoryService>();

        return services;
    }
}
