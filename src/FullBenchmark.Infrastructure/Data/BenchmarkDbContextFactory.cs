using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FullBenchmark.Infrastructure.Data;

/// <summary>
/// Used exclusively by EF Core design-time tooling (dotnet ef migrations add, etc.).
/// At runtime the context is created via DI with proper connection string from configuration.
/// </summary>
public sealed class BenchmarkDbContextFactory : IDesignTimeDbContextFactory<BenchmarkDbContext>
{
    public BenchmarkDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BenchmarkDbContext>();

        // Design-time uses a local dev database; actual runtime path is injected via DI
        var devDbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FullBenchmark",
            "benchmark_dev.db");

        Directory.CreateDirectory(Path.GetDirectoryName(devDbPath)!);

        optionsBuilder.UseSqlite($"Data Source={devDbPath}");

        return new BenchmarkDbContext(optionsBuilder.Options);
    }
}
