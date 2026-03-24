using System.Text.Json;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.Enums;
using FullBenchmark.Contracts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FullBenchmark.Infrastructure.Data;

public class BenchmarkDbContext : DbContext
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.General);

    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options) { }

    public DbSet<MachineProfile>          MachineProfiles   => Set<MachineProfile>();
    public DbSet<HardwareSnapshot>        HardwareSnapshots => Set<HardwareSnapshot>();
    public DbSet<OperatingSystemSnapshot> OsSnapshots       => Set<OperatingSystemSnapshot>();
    public DbSet<TelemetrySample>         TelemetrySamples  => Set<TelemetrySample>();
    public DbSet<BenchmarkConfig>         BenchmarkConfigs  => Set<BenchmarkConfig>();
    public DbSet<BenchmarkSession>        BenchmarkSessions => Set<BenchmarkSession>();
    public DbSet<BenchmarkCase>           BenchmarkCases    => Set<BenchmarkCase>();
    public DbSet<BenchmarkMetric>         BenchmarkMetrics  => Set<BenchmarkMetric>();
    public DbSet<BenchmarkScore>          BenchmarkScores   => Set<BenchmarkScore>();
    public DbSet<ComparisonDataset>       ComparisonDatasets => Set<ComparisonDataset>();
    public DbSet<ComparisonDevice>        ComparisonDevices  => Set<ComparisonDevice>();
    public DbSet<HistoricalTrendPoint>    TrendPoints        => Set<HistoricalTrendPoint>();
    public DbSet<UserNote>                UserNotes          => Set<UserNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureMachineProfile(modelBuilder);
        ConfigureHardwareSnapshot(modelBuilder);
        ConfigureOsSnapshot(modelBuilder);
        ConfigureTelemetrySample(modelBuilder);
        ConfigureBenchmarkConfig(modelBuilder);
        ConfigureBenchmarkSession(modelBuilder);
        ConfigureBenchmarkCase(modelBuilder);
        ConfigureBenchmarkMetric(modelBuilder);
        ConfigureBenchmarkScore(modelBuilder);
        ConfigureComparisonDataset(modelBuilder);
        ConfigureComparisonDevice(modelBuilder);
        ConfigureTrendPoint(modelBuilder);
        ConfigureUserNote(modelBuilder);
    }

    // ─── MachineProfile ──────────────────────────────────────────────────────

    private static void ConfigureMachineProfile(ModelBuilder b)
    {
        b.Entity<MachineProfile>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.MachineId).IsUnique();
            e.HasIndex(m => m.IsCurrentMachine);

            e.Property(m => m.CreatedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));
            e.Property(m => m.LastSeenAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        });
    }

    // ─── HardwareSnapshot ────────────────────────────────────────────────────

    private static void ConfigureHardwareSnapshot(ModelBuilder b)
    {
        b.Entity<HardwareSnapshot>(e =>
        {
            e.HasKey(h => h.Id);
            e.HasIndex(h => h.MachineProfileId);

            e.Property(h => h.CapturedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

            e.Property(h => h.BiosReleaseDate)
             .HasConversion(
                 v => v.HasValue ? (long?)v.Value.ToUnixTimeMilliseconds() : null,
                 v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

            // JSON-serialised owned collections — stored as TEXT in SQLite
            e.Property(h => h.RamModules)
             .HasColumnType("TEXT")
             .HasConversion(
                 v => JsonSerializer.Serialize(v, _jsonOptions),
                 v => JsonSerializer.Deserialize<List<RamModuleInfo>>(v, _jsonOptions) ?? new List<RamModuleInfo>());

            e.Property(h => h.Disks)
             .HasColumnType("TEXT")
             .HasConversion(
                 v => JsonSerializer.Serialize(v, _jsonOptions),
                 v => JsonSerializer.Deserialize<List<DiskInfo>>(v, _jsonOptions) ?? new List<DiskInfo>());

            e.Property(h => h.Gpus)
             .HasColumnType("TEXT")
             .HasConversion(
                 v => JsonSerializer.Serialize(v, _jsonOptions),
                 v => JsonSerializer.Deserialize<List<GpuInfo>>(v, _jsonOptions) ?? new List<GpuInfo>());

            e.Property(h => h.NetworkAdapters)
             .HasColumnType("TEXT")
             .HasConversion(
                 v => JsonSerializer.Serialize(v, _jsonOptions),
                 v => JsonSerializer.Deserialize<List<NetworkAdapterInfo>>(v, _jsonOptions) ?? new List<NetworkAdapterInfo>());

            e.HasOne(h => h.MachineProfile)
             .WithMany(m => m.HardwareSnapshots)
             .HasForeignKey(h => h.MachineProfileId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ─── OperatingSystemSnapshot ─────────────────────────────────────────────

    private static void ConfigureOsSnapshot(ModelBuilder b)
    {
        b.Entity<OperatingSystemSnapshot>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.MachineProfileId);

            e.Property(o => o.CapturedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));
            e.Property(o => o.InstallDate)
             .HasConversion(
                 v => v.HasValue ? (long?)v.Value.ToUnixTimeMilliseconds() : null,
                 v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
            e.Property(o => o.LastBootTime)
             .HasConversion(
                 v => v.HasValue ? (long?)v.Value.ToUnixTimeMilliseconds() : null,
                 v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

            e.HasOne(o => o.MachineProfile)
             .WithMany(m => m.OsSnapshots)
             .HasForeignKey(o => o.MachineProfileId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ─── TelemetrySample ─────────────────────────────────────────────────────

    private static void ConfigureTelemetrySample(ModelBuilder b)
    {
        b.Entity<TelemetrySample>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedOnAdd();
            e.HasIndex(t => t.MachineProfileId);
            e.HasIndex(t => t.CapturedAt);
            e.HasIndex(t => t.BenchmarkSessionId);

            e.Property(t => t.CapturedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

            e.Property(t => t.CpuCoreUsages)
             .HasColumnType("TEXT")
             .HasConversion(
                 v => JsonSerializer.Serialize(v, _jsonOptions),
                 v => JsonSerializer.Deserialize<List<double>>(v, _jsonOptions) ?? new List<double>());

            e.HasOne(t => t.MachineProfile)
             .WithMany()
             .HasForeignKey(t => t.MachineProfileId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ─── BenchmarkConfig ─────────────────────────────────────────────────────

    private static void ConfigureBenchmarkConfig(ModelBuilder b)
    {
        b.Entity<BenchmarkConfig>(e =>
        {
            e.HasKey(c => c.Id);

            e.Property(c => c.CreatedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));
            e.Property(c => c.UpdatedAt)
             .HasConversion(
                 v => v.HasValue ? (long?)v.Value.ToUnixTimeMilliseconds() : null,
                 v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
        });
    }

    // ─── BenchmarkSession ────────────────────────────────────────────────────

    private static void ConfigureBenchmarkSession(ModelBuilder b)
    {
        b.Entity<BenchmarkSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.MachineProfileId);
            e.HasIndex(s => s.StartedAt);

            e.Property(s => s.Status).HasConversion<int>();
            e.Property(s => s.StartedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));
            e.Property(s => s.CompletedAt)
             .HasConversion(
                 v => v.HasValue ? (long?)v.Value.ToUnixTimeMilliseconds() : null,
                 v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

            e.HasOne(s => s.MachineProfile)
             .WithMany(m => m.BenchmarkSessions)
             .HasForeignKey(s => s.MachineProfileId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.BenchmarkConfig)
             .WithMany(c => c.Sessions)
             .HasForeignKey(s => s.BenchmarkConfigId)
             .OnDelete(DeleteBehavior.Restrict);

            // HardwareSnapshot / OsSnapshot: restrict delete to preserve history
            e.HasOne(s => s.HardwareSnapshot)
             .WithMany()
             .HasForeignKey(s => s.HardwareSnapshotId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.OsSnapshot)
             .WithMany()
             .HasForeignKey(s => s.OsSnapshotId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ─── BenchmarkCase ───────────────────────────────────────────────────────

    private static void ConfigureBenchmarkCase(ModelBuilder b)
    {
        b.Entity<BenchmarkCase>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.SessionId);

            e.Property(c => c.Status).HasConversion<int>();
            e.Property(c => c.Category).HasConversion<int>();

            e.Property(c => c.StartedAt)
             .HasConversion(
                 v => v.HasValue ? (long?)v.Value.ToUnixTimeMilliseconds() : null,
                 v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
            e.Property(c => c.CompletedAt)
             .HasConversion(
                 v => v.HasValue ? (long?)v.Value.ToUnixTimeMilliseconds() : null,
                 v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

            e.HasOne(c => c.Session)
             .WithMany(s => s.Cases)
             .HasForeignKey(c => c.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ─── BenchmarkMetric ─────────────────────────────────────────────────────

    private static void ConfigureBenchmarkMetric(ModelBuilder b)
    {
        b.Entity<BenchmarkMetric>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => m.CaseId);

            e.Property(m => m.Unit).HasConversion<int>();
            e.Property(m => m.CapturedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

            e.HasOne(m => m.Case)
             .WithMany(c => c.Metrics)
             .HasForeignKey(m => m.CaseId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ─── BenchmarkScore ──────────────────────────────────────────────────────

    private static void ConfigureBenchmarkScore(ModelBuilder b)
    {
        b.Entity<BenchmarkScore>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.SessionId);
            e.HasIndex(s => s.CaseId);

            e.Property(s => s.Badge).HasConversion<int>();

            e.HasOne(s => s.Session)
             .WithMany(session => session.Scores)
             .HasForeignKey(s => s.SessionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Case)
             .WithMany(c => c.Scores)
             .HasForeignKey(s => s.CaseId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }

    // ─── ComparisonDataset ───────────────────────────────────────────────────

    private static void ConfigureComparisonDataset(ModelBuilder b)
    {
        b.Entity<ComparisonDataset>(e =>
        {
            e.HasKey(d => d.Id);

            e.Property(d => d.ImportedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));
        });
    }

    // ─── ComparisonDevice ────────────────────────────────────────────────────

    private static void ConfigureComparisonDevice(ModelBuilder b)
    {
        b.Entity<ComparisonDevice>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.DatasetId);
            e.HasIndex(d => d.OverallScore);
            e.HasIndex(d => d.Category);

            e.Property(d => d.Category).HasConversion<int>();
            e.Property(d => d.RecordedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

            e.HasOne(d => d.Dataset)
             .WithMany(ds => ds.Devices)
             .HasForeignKey(d => d.DatasetId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ─── HistoricalTrendPoint ────────────────────────────────────────────────

    private static void ConfigureTrendPoint(ModelBuilder b)
    {
        b.Entity<HistoricalTrendPoint>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.MachineProfileId);
            e.HasIndex(t => t.RecordedAt);

            e.Property(t => t.RecordedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

            e.HasOne(t => t.MachineProfile)
             .WithMany(m => m.TrendPoints)
             .HasForeignKey(t => t.MachineProfileId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.Session)
             .WithMany()
             .HasForeignKey(t => t.SessionId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ─── UserNote ────────────────────────────────────────────────────────────

    private static void ConfigureUserNote(ModelBuilder b)
    {
        b.Entity<UserNote>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => n.SessionId);

            e.Property(n => n.TagType).HasConversion<int>();
            e.Property(n => n.CreatedAt)
             .HasConversion(v => v.ToUnixTimeMilliseconds(),
                            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

            e.HasOne(n => n.Session)
             .WithMany(s => s.Notes)
             .HasForeignKey(n => n.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
