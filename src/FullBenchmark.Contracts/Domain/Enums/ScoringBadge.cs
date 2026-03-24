namespace FullBenchmark.Contracts.Domain.Enums;

public enum ScoringBadge
{
    Unknown      = 0,
    BelowAverage = 1,  // 0–200
    Average      = 2,  // 201–400
    Good         = 3,  // 401–600
    VeryGood     = 4,  // 601–800
    Excellent    = 5,  // 801–950
    Outstanding  = 6   // 951–1000
}
