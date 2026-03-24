namespace FullBenchmark.Contracts.Domain.ValueObjects;

public record NetworkAdapterInfo(
    string  Name,
    string? Description,
    string? MacAddress,
    string? AdapterType,
    long?   Speed,
    bool    IsPhysical
);
