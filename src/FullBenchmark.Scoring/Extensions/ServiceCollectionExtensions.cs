using FullBenchmark.Contracts.Scoring;
using FullBenchmark.Scoring.Engines;
using Microsoft.Extensions.DependencyInjection;

namespace FullBenchmark.Scoring.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the scoring engine.
    /// The engine is stateless — registered as singleton for efficiency.
    /// </summary>
    public static IServiceCollection AddScoring(this IServiceCollection services)
    {
        services.AddSingleton<IScoringEngine, ScoringEngineV1>();
        return services;
    }
}
