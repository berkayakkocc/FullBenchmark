namespace FullBenchmark.Contracts.Domain.ValueObjects;

public record DiskInfo(
    string  Model,
    string? SerialNumber,
    string? MediaType,
    string? InterfaceType,
    long    TotalBytes,
    long    FreeBytes,
    string? DriveLetter,
    string? FileSystem,
    bool    IsSystemDisk
);
