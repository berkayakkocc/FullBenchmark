using FullBenchmark.Contracts.Repositories;
using FullBenchmark.Infrastructure.Data;
using FullBenchmark.Infrastructure.Repositories;
using FullBenchmark.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FullBenchmark.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SQLite-backed EF Core context and all repository implementations.
    /// Call <paramref name="dbPath"/> from application startup with the production database path.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<BenchmarkDbContext>(opts =>
            opts.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IMachineRepository,    MachineRepository>();
        services.AddScoped<IBenchmarkRepository,  BenchmarkRepository>();
        services.AddScoped<IComparisonRepository, ComparisonRepository>();
        services.AddScoped<ComparisonDataSeeder>();

        return services;
    }

    /// <summary>
    /// Applies any pending EF Core migrations and optionally seeds initial comparison data.
    /// Call once at application startup before the main window is shown.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
        await ctx.Database.MigrateAsync(ct);

        var seeder = scope.ServiceProvider.GetRequiredService<ComparisonDataSeeder>();
        await seeder.SeedAsync(ct);
    }
}
