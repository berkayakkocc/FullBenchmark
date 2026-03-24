using FullBenchmark.Benchmarks.Modules;
using FullBenchmark.Contracts.Benchmarks;
using Microsoft.Extensions.DependencyInjection;

namespace FullBenchmark.Benchmarks.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all <see cref="IBenchmarkModule"/> implementations.
    /// The orchestrator resolves them via <c>IEnumerable&lt;IBenchmarkModule&gt;</c>.
    /// </summary>
    public static IServiceCollection AddBenchmarkModules(this IServiceCollection services)
    {
        services.AddTransient<IBenchmarkModule, CpuBenchmarkModule>();
        services.AddTransient<IBenchmarkModule, MemoryBenchmarkModule>();
        services.AddTransient<IBenchmarkModule, DiskBenchmarkModule>();
        services.AddTransient<IBenchmarkModule, GpuBenchmarkModule>();
        services.AddTransient<IBenchmarkModule, NetworkBenchmarkModule>();
        return services;
    }
}
