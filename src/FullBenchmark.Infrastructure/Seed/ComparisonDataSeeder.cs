using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using FullBenchmark.Contracts.Repositories;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Infrastructure.Seed;

/// <summary>
/// Seeds the comparison dataset on first run.
/// All scores are tier estimates and clearly labeled Source = "Seeded".
/// They represent approximate performance tiers — not measured values — and are
/// intended only to provide meaningful comparison context until real data is imported.
/// Schema version 1 scoring: 500 = reference (late-2021 i7-12700 / Ryzen 7 5800X class).
/// </summary>
public sealed class ComparisonDataSeeder
{
    private readonly IComparisonRepository _repo;
    private readonly ILogger<ComparisonDataSeeder> _logger;

    public ComparisonDataSeeder(IComparisonRepository repo, ILogger<ComparisonDataSeeder> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _repo.HasSeedDataAsync(ct))
        {
            _logger.LogDebug("Comparison seed data already present — skipping.");
            return;
        }

        _logger.LogInformation("Seeding comparison dataset v1.0...");

        var dataset = new ComparisonDataset
        {
            Id                  = Guid.NewGuid(),
            Name                = "FullBenchmark Reference Devices v1.0",
            Version             = "1.0.0",
            Description         = "Estimated tier reference data. Not measured. Scores approximate published performance tiers relative to SchemaVersion 1 baseline.",
            ImportedAt          = DateTimeOffset.UtcNow,
            ScoringSchemaVersion = 1,
            Source              = "Seeded",
            IsActive            = true
        };

        var devices = BuildSeedDevices(dataset.Id);
        await _repo.ImportDatasetAsync(dataset, devices, ct);
        _logger.LogInformation("Seeded {Count} reference devices.", devices.Count);
    }

    private static List<ComparisonDevice> BuildSeedDevices(Guid datasetId)
    {
        var now = DateTimeOffset.UtcNow;

        ComparisonDevice D(
            string name, string maker, DeviceCategory cat,
            string cpu, int cores, double ramGB, string? storage, string? gpu,
            double overall, double? cpuS, double? memS, double? diskS, double? gpuS,
            bool isRef = false)
            => new()
            {
                Id                   = Guid.NewGuid(),
                DatasetId            = datasetId,
                DeviceName           = name,
                Manufacturer         = maker,
                Category             = cat,
                CpuModel             = cpu,
                CpuCores             = cores,
                RamTotalGB           = ramGB,
                StorageDescription   = storage,
                GpuModel             = gpu,
                OverallScore         = overall,
                CpuScore             = cpuS,
                MemoryScore          = memS,
                DiskScore            = diskS,
                GpuScore             = gpuS,
                ScoringSchemaVersion = 1,
                IsReferenceDevice    = isRef,
                Source               = "Seeded",
                RecordedAt           = now
            };

        return new List<ComparisonDevice>
        {
            // ── OFFICE / BUDGET ──────────────────────────────────────────────────────
            D("Budget Office PC",         "Generic",   DeviceCategory.Office,
              "Intel Core i3-12100",        4,   8,  "SATA SSD 256 GB",    null,
              160, 140, 150, 180, null),

            D("Office Thin Client",       "Dell",      DeviceCategory.Office,
              "Intel Celeron N4500",        2,   4,  "eMMC 64 GB",         null,
              85,  60,  90,  95,  null),

            D("Mid-Range Office Laptop",  "Lenovo",    DeviceCategory.Office,
              "Intel Core i5-1235U",        10,  8,  "NVMe SSD 512 GB",    null,
              260, 240, 270, 280, null),

            // ── BALANCED / MAINSTREAM ────────────────────────────────────────────────
            D("Mainstream Desktop",       "HP",        DeviceCategory.Balanced,
              "Intel Core i5-12400",        6,  16,  "NVMe SSD 512 GB",    "NVIDIA GTX 1660 Super",
              360, 340, 380, 370, 350,  false),

            D("Reference Mid Laptop",     "ASUS",      DeviceCategory.Balanced,
              "AMD Ryzen 5 5600H",          6,  16,  "NVMe SSD 512 GB",    null,
              340, 330, 350, 345, null),

            // ── REFERENCE BASELINE (SchemaVersion 1 = ~500 points) ──────────────────
            D("Reference Desktop (i7-12700)", "Intel",  DeviceCategory.Balanced,
              "Intel Core i7-12700",        12, 32,  "NVMe SSD 1 TB",      "NVIDIA RTX 3060",
              500, 500, 500, 500, 500,  true),    // <── scoring baseline

            D("Reference Desktop (R7 5800X)", "AMD",   DeviceCategory.Balanced,
              "AMD Ryzen 7 5800X",          8,  32,  "NVMe SSD 1 TB",      "AMD RX 6700 XT",
              490, 495, 490, 482, 485,  true),

            // ── DEVELOPER ────────────────────────────────────────────────────────────
            D("Developer Workstation",    "Lenovo",    DeviceCategory.Developer,
              "Intel Core i7-1365U",        10, 32,  "NVMe SSD 1 TB",      null,
              420, 400, 450, 440, null),

            D("Developer High-End Laptop","Dell",      DeviceCategory.Developer,
              "AMD Ryzen 9 7940HS",         8,  32,  "NVMe SSD 2 TB",      null,
              580, 610, 570, 565, null),

            D("Mac-class Dev Laptop",     "Generic",   DeviceCategory.Developer,
              "Intel Core Ultra 7 165H",    16, 64,  "NVMe SSD 2 TB",      null,
              640, 650, 635, 620, null),

            // ── GAMING ───────────────────────────────────────────────────────────────
            D("Entry Gaming PC",          "Generic",   DeviceCategory.Gaming,
              "Intel Core i5-12400F",       6,  16,  "NVMe SSD 512 GB",    "NVIDIA RTX 3060",
              430, 340, 380, 370, 600),

            D("Mid Gaming Desktop",       "Generic",   DeviceCategory.Gaming,
              "Intel Core i7-12700KF",      12, 32,  "NVMe SSD 1 TB",      "NVIDIA RTX 3070 Ti",
              600, 540, 510, 490, 780),

            D("High-End Gaming Desktop",  "Generic",   DeviceCategory.Gaming,
              "AMD Ryzen 7 7700X",          8,  32,  "NVMe SSD 2 TB",      "NVIDIA RTX 4080",
              740, 620, 580, 560, 950),

            D("Gaming Laptop Mid",        "MSI",       DeviceCategory.Gaming,
              "Intel Core i7-12700H",       14, 16,  "NVMe SSD 1 TB",      "NVIDIA RTX 3070",
              520, 490, 480, 460, 680),

            // ── WORKSTATION ──────────────────────────────────────────────────────────
            D("Workstation (Xeon)",       "Dell",      DeviceCategory.Workstation,
              "Intel Xeon W-2235",          6,  64,  "NVMe SSD 2 TB RAID", "NVIDIA RTX A4000",
              560, 480, 560, 540, 700),

            D("High Workstation",         "HP",        DeviceCategory.Workstation,
              "AMD Threadripper PRO 5955WX",16, 128, "NVMe SSD 4 TB",      "NVIDIA RTX A6000",
              860, 920, 870, 780, 880),

            D("Consumer Workstation",     "Generic",   DeviceCategory.Workstation,
              "Intel Core i9-12900K",       16, 64,  "NVMe SSD 2 TB",      "NVIDIA RTX 3080",
              700, 690, 650, 620, 840),

            // ── MOBILE / ULTRABOOK ───────────────────────────────────────────────────
            D("Ultrabook Entry",          "Lenovo",    DeviceCategory.Mobile,
              "Intel Core i5-1335U",        10, 8,   "NVMe SSD 256 GB",    null,
              230, 220, 240, 220, null),

            D("Ultrabook Premium",        "Dell",      DeviceCategory.Mobile,
              "Intel Core i7-1365U",        10, 16,  "NVMe SSD 1 TB",      null,
              360, 350, 370, 380, null),

            D("Gaming Laptop High-End",   "ASUS",      DeviceCategory.Mobile,
              "AMD Ryzen 9 7945HX",         16, 32,  "NVMe SSD 2 TB",      "NVIDIA RTX 4090 Laptop",
              780, 790, 750, 720, 870),

            // ── SERVER ───────────────────────────────────────────────────────────────
            D("Server (Dual Xeon)",       "Generic",   DeviceCategory.Server,
              "Intel Xeon Silver 4314 ×2",  32, 256, "NVMe SSD 8 TB",      null,
              820, 880, 810, 760, null),
        };
    }
}
