namespace FullBenchmark.Contracts.Domain.ValueObjects;

public record GpuInfo(
    string  Name,
    long?   AdapterRamBytes,
    string? DriverVersion,
    string? VideoProcessor,
    string? VideoModeDescription,
    int?    CurrentRefreshRate
);
