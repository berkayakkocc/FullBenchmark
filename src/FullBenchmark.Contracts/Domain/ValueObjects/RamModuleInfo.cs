namespace FullBenchmark.Contracts.Domain.ValueObjects;

public record RamModuleInfo(
    string? Manufacturer,
    string? PartNumber,
    long    CapacityBytes,
    int?    SpeedMHz,
    string? MemoryType,
    string? FormFactor,
    int?    BankLabel
);
