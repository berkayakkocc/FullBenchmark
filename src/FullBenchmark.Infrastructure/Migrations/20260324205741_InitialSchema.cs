using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FullBenchmark.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenchmarkConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CpuEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MemoryEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiskEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    GpuEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    NetworkEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    WarmupSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    CpuRunSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    MemoryRunSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    DiskRunSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    DiskTestFileSizeMB = table.Column<int>(type: "INTEGER", nullable: false),
                    DiskTestBlockSizeKB = table.Column<int>(type: "INTEGER", nullable: false),
                    DiskTestTempPath = table.Column<string>(type: "TEXT", nullable: true),
                    DiskCleanupAfterTest = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScoringSchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComparisonDatasets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ImportedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ScoringSchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComparisonDatasets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MachineProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MachineId = table.Column<string>(type: "TEXT", nullable: false),
                    MachineName = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    IsCurrentMachine = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComparisonDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DatasetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    CpuModel = table.Column<string>(type: "TEXT", nullable: false),
                    CpuCores = table.Column<int>(type: "INTEGER", nullable: false),
                    RamTotalGB = table.Column<double>(type: "REAL", nullable: false),
                    StorageDescription = table.Column<string>(type: "TEXT", nullable: true),
                    GpuModel = table.Column<string>(type: "TEXT", nullable: true),
                    OverallScore = table.Column<double>(type: "REAL", nullable: false),
                    CpuScore = table.Column<double>(type: "REAL", nullable: true),
                    MemoryScore = table.Column<double>(type: "REAL", nullable: true),
                    DiskScore = table.Column<double>(type: "REAL", nullable: true),
                    GpuScore = table.Column<double>(type: "REAL", nullable: true),
                    ScoringSchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    IsReferenceDevice = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComparisonDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComparisonDevices_ComparisonDatasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "ComparisonDatasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HardwareSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MachineProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapturedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CpuModel = table.Column<string>(type: "TEXT", nullable: false),
                    CpuManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    CpuPhysicalCores = table.Column<int>(type: "INTEGER", nullable: false),
                    CpuLogicalCores = table.Column<int>(type: "INTEGER", nullable: false),
                    CpuBaseClockMHz = table.Column<double>(type: "REAL", nullable: true),
                    CpuMaxClockMHz = table.Column<double>(type: "REAL", nullable: true),
                    CpuArchitecture = table.Column<string>(type: "TEXT", nullable: true),
                    CpuSocket = table.Column<string>(type: "TEXT", nullable: true),
                    CpuL2CacheKB = table.Column<string>(type: "TEXT", nullable: true),
                    CpuL3CacheKB = table.Column<string>(type: "TEXT", nullable: true),
                    RamTotalBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    RamSpeedMHz = table.Column<int>(type: "INTEGER", nullable: true),
                    RamChannels = table.Column<int>(type: "INTEGER", nullable: true),
                    RamType = table.Column<string>(type: "TEXT", nullable: true),
                    RamModules = table.Column<string>(type: "TEXT", nullable: false),
                    MotherboardManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    MotherboardProduct = table.Column<string>(type: "TEXT", nullable: true),
                    MotherboardVersion = table.Column<string>(type: "TEXT", nullable: true),
                    BiosVersion = table.Column<string>(type: "TEXT", nullable: true),
                    BiosReleaseDate = table.Column<long>(type: "INTEGER", nullable: true),
                    Disks = table.Column<string>(type: "TEXT", nullable: false),
                    Gpus = table.Column<string>(type: "TEXT", nullable: false),
                    NetworkAdapters = table.Column<string>(type: "TEXT", nullable: false),
                    HasBattery = table.Column<bool>(type: "INTEGER", nullable: false),
                    BatteryManufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    BatteryDesignCapacityMWh = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HardwareSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HardwareSnapshots_MachineProfiles_MachineProfileId",
                        column: x => x.MachineProfileId,
                        principalTable: "MachineProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MachineProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapturedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    OsName = table.Column<string>(type: "TEXT", nullable: false),
                    OsVersion = table.Column<string>(type: "TEXT", nullable: false),
                    OsBuildNumber = table.Column<string>(type: "TEXT", nullable: false),
                    OsRevision = table.Column<string>(type: "TEXT", nullable: false),
                    Architecture = table.Column<string>(type: "TEXT", nullable: false),
                    ServicePack = table.Column<string>(type: "TEXT", nullable: true),
                    InstallDate = table.Column<long>(type: "INTEGER", nullable: true),
                    LastBootTime = table.Column<long>(type: "INTEGER", nullable: true),
                    SystemDirectory = table.Column<string>(type: "TEXT", nullable: true),
                    TimeZoneId = table.Column<string>(type: "TEXT", nullable: true),
                    ProcessorCount = table.Column<int>(type: "INTEGER", nullable: true),
                    PageFileSizeBytes = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OsSnapshots_MachineProfiles_MachineProfileId",
                        column: x => x.MachineProfileId,
                        principalTable: "MachineProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TelemetrySamples",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MachineProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BenchmarkSessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CapturedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "REAL", nullable: false),
                    CpuFrequencyMHz = table.Column<double>(type: "REAL", nullable: true),
                    CpuTemperatureCelsius = table.Column<double>(type: "REAL", nullable: true),
                    CpuCoreUsages = table.Column<string>(type: "TEXT", nullable: false),
                    MemoryUsedBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    MemoryAvailableBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    PageFileUsagePercent = table.Column<double>(type: "REAL", nullable: true),
                    DiskReadBytesPerSec = table.Column<long>(type: "INTEGER", nullable: false),
                    DiskWriteBytesPerSec = table.Column<long>(type: "INTEGER", nullable: false),
                    DiskActiveTimePercent = table.Column<double>(type: "REAL", nullable: true),
                    NetworkSentBytesPerSec = table.Column<long>(type: "INTEGER", nullable: false),
                    NetworkReceivedBytesPerSec = table.Column<long>(type: "INTEGER", nullable: false),
                    GpuUsagePercent = table.Column<double>(type: "REAL", nullable: true),
                    GpuTemperatureCelsius = table.Column<double>(type: "REAL", nullable: true),
                    GpuMemoryUsedBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    BatteryChargePercent = table.Column<double>(type: "REAL", nullable: true),
                    IsOnAcPower = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetrySamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetrySamples_MachineProfiles_MachineProfileId",
                        column: x => x.MachineProfileId,
                        principalTable: "MachineProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MachineProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HardwareSnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OsSnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BenchmarkConfigId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OverallScore = table.Column<double>(type: "REAL", nullable: true),
                    CpuScore = table.Column<double>(type: "REAL", nullable: true),
                    MemoryScore = table.Column<double>(type: "REAL", nullable: true),
                    DiskScore = table.Column<double>(type: "REAL", nullable: true),
                    GpuScore = table.Column<double>(type: "REAL", nullable: true),
                    ScoringSchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkSessions_BenchmarkConfigs_BenchmarkConfigId",
                        column: x => x.BenchmarkConfigId,
                        principalTable: "BenchmarkConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BenchmarkSessions_HardwareSnapshots_HardwareSnapshotId",
                        column: x => x.HardwareSnapshotId,
                        principalTable: "HardwareSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BenchmarkSessions_MachineProfiles_MachineProfileId",
                        column: x => x.MachineProfileId,
                        principalTable: "MachineProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BenchmarkSessions_OsSnapshots_OsSnapshotId",
                        column: x => x.OsSnapshotId,
                        principalTable: "OsSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkloadName = table.Column<string>(type: "TEXT", nullable: false),
                    WorkloadDescription = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CompletedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    WarmupDurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    RunDurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualRunDurationMs = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoringSchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkCases_BenchmarkSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "BenchmarkSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrendPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MachineProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    OverallScore = table.Column<double>(type: "REAL", nullable: false),
                    CpuScore = table.Column<double>(type: "REAL", nullable: true),
                    MemoryScore = table.Column<double>(type: "REAL", nullable: true),
                    DiskScore = table.Column<double>(type: "REAL", nullable: true),
                    GpuScore = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrendPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrendPoints_BenchmarkSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "BenchmarkSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrendPoints_MachineProfiles_MachineProfileId",
                        column: x => x.MachineProfileId,
                        principalTable: "MachineProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    TagType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotes_BenchmarkSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "BenchmarkSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MetricName = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    Unit = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRawValue = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CapturedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkMetrics_BenchmarkCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "BenchmarkCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ScoreLevel = table.Column<string>(type: "TEXT", nullable: true),
                    ScoreName = table.Column<string>(type: "TEXT", nullable: false),
                    RawValue = table.Column<double>(type: "REAL", nullable: false),
                    NormalizedScore = table.Column<double>(type: "REAL", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    Badge = table.Column<int>(type: "INTEGER", nullable: false),
                    ScoringSchemaVersion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BenchmarkScores_BenchmarkCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "BenchmarkCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BenchmarkScores_BenchmarkSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "BenchmarkSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkCases_SessionId",
                table: "BenchmarkCases",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkMetrics_CaseId",
                table: "BenchmarkMetrics",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkScores_CaseId",
                table: "BenchmarkScores",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkScores_SessionId",
                table: "BenchmarkScores",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkSessions_BenchmarkConfigId",
                table: "BenchmarkSessions",
                column: "BenchmarkConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkSessions_HardwareSnapshotId",
                table: "BenchmarkSessions",
                column: "HardwareSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkSessions_MachineProfileId",
                table: "BenchmarkSessions",
                column: "MachineProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkSessions_OsSnapshotId",
                table: "BenchmarkSessions",
                column: "OsSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkSessions_StartedAt",
                table: "BenchmarkSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonDevices_Category",
                table: "ComparisonDevices",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonDevices_DatasetId",
                table: "ComparisonDevices",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonDevices_OverallScore",
                table: "ComparisonDevices",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_HardwareSnapshots_MachineProfileId",
                table: "HardwareSnapshots",
                column: "MachineProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MachineProfiles_IsCurrentMachine",
                table: "MachineProfiles",
                column: "IsCurrentMachine");

            migrationBuilder.CreateIndex(
                name: "IX_MachineProfiles_MachineId",
                table: "MachineProfiles",
                column: "MachineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsSnapshots_MachineProfileId",
                table: "OsSnapshots",
                column: "MachineProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySamples_BenchmarkSessionId",
                table: "TelemetrySamples",
                column: "BenchmarkSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySamples_CapturedAt",
                table: "TelemetrySamples",
                column: "CapturedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySamples_MachineProfileId",
                table: "TelemetrySamples",
                column: "MachineProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TrendPoints_MachineProfileId",
                table: "TrendPoints",
                column: "MachineProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TrendPoints_RecordedAt",
                table: "TrendPoints",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrendPoints_SessionId",
                table: "TrendPoints",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_SessionId",
                table: "UserNotes",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkMetrics");

            migrationBuilder.DropTable(
                name: "BenchmarkScores");

            migrationBuilder.DropTable(
                name: "ComparisonDevices");

            migrationBuilder.DropTable(
                name: "TelemetrySamples");

            migrationBuilder.DropTable(
                name: "TrendPoints");

            migrationBuilder.DropTable(
                name: "UserNotes");

            migrationBuilder.DropTable(
                name: "BenchmarkCases");

            migrationBuilder.DropTable(
                name: "ComparisonDatasets");

            migrationBuilder.DropTable(
                name: "BenchmarkSessions");

            migrationBuilder.DropTable(
                name: "BenchmarkConfigs");

            migrationBuilder.DropTable(
                name: "HardwareSnapshots");

            migrationBuilder.DropTable(
                name: "OsSnapshots");

            migrationBuilder.DropTable(
                name: "MachineProfiles");
        }
    }
}
