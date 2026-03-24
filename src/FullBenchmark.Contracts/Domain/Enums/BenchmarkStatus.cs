namespace FullBenchmark.Contracts.Domain.Enums;

public enum BenchmarkStatus
{
    Pending   = 0,
    Warmup    = 1,
    Running   = 2,
    Completed = 3,
    Failed    = 4,
    Cancelled = 5,
    Skipped   = 6
}
