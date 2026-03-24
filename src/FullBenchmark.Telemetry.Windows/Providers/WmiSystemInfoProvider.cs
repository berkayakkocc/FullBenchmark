using System.Management;
using FullBenchmark.Contracts.Domain.Entities;
using FullBenchmark.Contracts.Domain.ValueObjects;
using FullBenchmark.Contracts.SystemInfo;
using FullBenchmark.Core.Utilities;
using FullBenchmark.Telemetry.Windows.Native;
using Microsoft.Extensions.Logging;

namespace FullBenchmark.Telemetry.Windows.Providers;

/// <summary>
/// Windows implementation of <see cref="ISystemInfoProvider"/> using WMI (System.Management).
/// All WMI calls are run on a background thread via Task.Run — WMI is synchronous.
/// Results are intended to be cached by the caller (SystemInfoService), not polled.
/// </summary>
public sealed class WmiSystemInfoProvider : ISystemInfoProvider
{
    private readonly ILogger<WmiSystemInfoProvider> _logger;

    public WmiSystemInfoProvider(ILogger<WmiSystemInfoProvider> logger) => _logger = logger;

    // ─── ISystemInfoProvider ─────────────────────────────────────────────────

    public Task<HardwareSnapshot> GetHardwareSnapshotAsync(
        Guid machineProfileId, CancellationToken ct = default)
        => Task.Run(() => BuildHardwareSnapshot(machineProfileId), ct);

    public Task<OperatingSystemSnapshot> GetOsSnapshotAsync(
        Guid machineProfileId, CancellationToken ct = default)
        => Task.Run(() => BuildOsSnapshot(machineProfileId), ct);

    public Task<string> GetMachineIdAsync(CancellationToken ct = default)
        => Task.Run(BuildMachineId, ct);

    public Task<string> GetMachineNameAsync(CancellationToken ct = default)
        => Task.Run(() => Environment.MachineName, ct);

    public Task<string?> GetManufacturerAsync(CancellationToken ct = default)
        => Task.Run(() =>
        {
            try
            {
                return WmiHelper.Query("SELECT Manufacturer FROM Win32_ComputerSystem")
                    .Select(o => WmiHelper.GetString(o, "Manufacturer"))
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query manufacturer");
                return null;
            }
        }, ct);

    public Task<string?> GetModelAsync(CancellationToken ct = default)
        => Task.Run(() =>
        {
            try
            {
                return WmiHelper.Query("SELECT Model FROM Win32_ComputerSystem")
                    .Select(o => WmiHelper.GetString(o, "Model"))
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query model");
                return null;
            }
        }, ct);

    // ─── Hardware snapshot ────────────────────────────────────────────────────

    private HardwareSnapshot BuildHardwareSnapshot(Guid machineProfileId)
    {
        var snap = new HardwareSnapshot
        {
            Id               = Guid.NewGuid(),
            MachineProfileId = machineProfileId,
            CapturedAt       = DateTimeOffset.UtcNow
        };

        PopulateCpu(snap);
        PopulateRam(snap);
        PopulateMotherboardAndBios(snap);
        PopulateDisks(snap);
        PopulateGpus(snap);
        PopulateNetworkAdapters(snap);
        PopulateBattery(snap);

        return snap;
    }

    private void PopulateCpu(HardwareSnapshot snap)
    {
        try
        {
            var cpus = WmiHelper.Query(
                "SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, " +
                "MaxClockSpeed, Architecture, SocketDesignation, L2CacheSize, L3CacheSize " +
                "FROM Win32_Processor");

            var first = cpus.FirstOrDefault();
            if (first is null) return;

            snap.CpuModel         = WmiHelper.GetString(first, "Name") ?? "Unknown CPU";
            snap.CpuManufacturer  = WmiHelper.GetString(first, "Manufacturer");
            snap.CpuPhysicalCores = WmiHelper.GetInt32(first, "NumberOfCores") ?? 0;
            snap.CpuLogicalCores  = WmiHelper.GetInt32(first, "NumberOfLogicalProcessors") ?? 0;

            var baseClockMHz = WmiHelper.GetUInt32(first, "MaxClockSpeed");
            snap.CpuBaseClockMHz = baseClockMHz.HasValue ? (double)baseClockMHz.Value : null;

            var archVal = WmiHelper.GetInt32(first, "Architecture");
            snap.CpuArchitecture = archVal.HasValue
                ? WmiHelper.GetCpuArchitectureName((ushort)archVal.Value)
                : null;

            snap.CpuSocket  = WmiHelper.GetString(first, "SocketDesignation");
            snap.CpuL2CacheKB = FormatCacheSize(WmiHelper.GetUInt32(first, "L2CacheSize"));
            snap.CpuL3CacheKB = FormatCacheSize(WmiHelper.GetUInt32(first, "L3CacheSize"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query CPU via Win32_Processor");
        }
    }

    private void PopulateRam(HardwareSnapshot snap)
    {
        // Total RAM from GlobalMemoryStatusEx (most accurate)
        var mem = NativeMemoryReader.ReadMemory();
        if (mem.HasValue)
            snap.RamTotalBytes = mem.Value.TotalPhysicalBytes;

        // Per-module details from Win32_PhysicalMemory
        try
        {
            var modules = WmiHelper.Query(
                "SELECT Manufacturer, PartNumber, Capacity, Speed, MemoryType, FormFactor, BankLabel " +
                "FROM Win32_PhysicalMemory");

            foreach (var m in modules)
            {
                var capacityBytes = (long)(WmiHelper.GetUInt64(m, "Capacity") ?? 0);
                var typeCode      = WmiHelper.GetInt32(m, "MemoryType");
                var ffCode        = WmiHelper.GetInt32(m, "FormFactor");

                snap.RamModules.Add(new RamModuleInfo(
                    Manufacturer: WmiHelper.GetString(m, "Manufacturer"),
                    PartNumber:   WmiHelper.GetString(m, "PartNumber")?.Trim(),
                    CapacityBytes: capacityBytes,
                    SpeedMHz:     WmiHelper.GetInt32(m, "Speed"),
                    MemoryType:   DecodeMemoryType(typeCode),
                    FormFactor:   DecodeFormFactor(ffCode),
                    BankLabel:    ParseBankLabel(WmiHelper.GetString(m, "BankLabel"))));
            }

            if (snap.RamModules.Count > 0)
            {
                snap.RamSpeedMHz = snap.RamModules.Max(r => r.SpeedMHz);
                snap.RamType     = snap.RamModules.Select(r => r.MemoryType)
                                                  .FirstOrDefault(t => t != null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query RAM modules via Win32_PhysicalMemory");
        }
    }

    private void PopulateMotherboardAndBios(HardwareSnapshot snap)
    {
        try
        {
            var boards = WmiHelper.Query("SELECT Manufacturer, Product, Version, SerialNumber FROM Win32_BaseBoard");
            var board  = boards.FirstOrDefault();
            if (board is not null)
            {
                snap.MotherboardManufacturer = WmiHelper.GetString(board, "Manufacturer");
                snap.MotherboardProduct      = WmiHelper.GetString(board, "Product");
                snap.MotherboardVersion      = WmiHelper.GetString(board, "Version");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query motherboard via Win32_BaseBoard");
        }

        try
        {
            var bioses = WmiHelper.Query("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");
            var bios   = bioses.FirstOrDefault();
            if (bios is not null)
            {
                snap.BiosVersion      = WmiHelper.GetString(bios, "SMBIOSBIOSVersion");
                snap.BiosReleaseDate  = WmiHelper.ParseWmiDate(bios, "ReleaseDate");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query BIOS via Win32_BIOS");
        }
    }

    private void PopulateDisks(HardwareSnapshot snap)
    {
        try
        {
            var physicalDisks = WmiHelper.Query(
                "SELECT DeviceID, Model, SerialNumber, InterfaceType, MediaType, Size " +
                "FROM Win32_DiskDrive");

            // Build a drive-letter → free-space map for logical disks
            var logicalDiskFreeMap = BuildLogicalDiskFreeMap();
            // Build mapping: physical DeviceID → list of drive letters
            var deviceToLetters = BuildPhysicalToLogicalMap();

            foreach (var disk in physicalDisks)
            {
                var deviceId = WmiHelper.GetString(disk, "DeviceID") ?? "";
                var sizeBytes = (long)(WmiHelper.GetUInt64(disk, "Size") ?? 0);

                // Find associated drive letters
                var letters = deviceToLetters.TryGetValue(deviceId, out var lst) ? lst : new List<string>();
                var driveLetter = letters.FirstOrDefault();
                var freeBytes   = driveLetter != null && logicalDiskFreeMap.TryGetValue(driveLetter, out var free)
                    ? free : 0L;
                var fs = driveLetter != null ? GetFileSystem(driveLetter) : null;

                snap.Disks.Add(new DiskInfo(
                    Model:         WmiHelper.GetString(disk, "Model") ?? "Unknown",
                    SerialNumber:  WmiHelper.GetString(disk, "SerialNumber"),
                    MediaType:     WmiHelper.GetString(disk, "MediaType"),
                    InterfaceType: WmiHelper.GetString(disk, "InterfaceType"),
                    TotalBytes:    sizeBytes,
                    FreeBytes:     freeBytes,
                    DriveLetter:   driveLetter,
                    FileSystem:    fs,
                    IsSystemDisk:  IsSystemDisk(letters)));
            }

            // If WMI disk enumeration returned nothing, fall back to DriveInfo
            if (snap.Disks.Count == 0)
                PopulateDisksFromDriveInfo(snap);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query disks via Win32_DiskDrive — falling back to DriveInfo");
            PopulateDisksFromDriveInfo(snap);
        }
    }

    private static Dictionary<string, long> BuildLogicalDiskFreeMap()
    {
        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var ld in WmiHelper.Query("SELECT DeviceID, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3"))
            {
                var id   = WmiHelper.GetString(ld, "DeviceID");
                var free = WmiHelper.GetUInt64(ld, "FreeSpace");
                if (id is not null && free.HasValue)
                    map[id] = (long)free.Value;
            }
        }
        catch { /* non-fatal */ }
        return map;
    }

    private static string? GetFileSystem(string driveLetter)
    {
        try
        {
            var results = WmiHelper.Query(
                $"SELECT FileSystem FROM Win32_LogicalDisk WHERE DeviceID='{driveLetter}'");
            return results.Select(r => WmiHelper.GetString(r, "FileSystem")).FirstOrDefault();
        }
        catch { return null; }
    }

    private static Dictionary<string, List<string>> BuildPhysicalToLogicalMap()
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            // Walk Win32_DiskDrive → Win32_DiskDriveToDiskPartition → Win32_LogicalDiskToPartition → Win32_LogicalDisk
            foreach (var diskDrive in WmiHelper.Query("SELECT DeviceID FROM Win32_DiskDrive"))
            {
                var diskDeviceId = WmiHelper.GetString(diskDrive, "DeviceID");
                if (diskDeviceId is null) continue;

                var letters = new List<string>();
                foreach (var partition in WmiHelper.Associators(diskDeviceId, "Win32_DiskDriveToDiskPartition"))
                {
                    var partId = WmiHelper.GetString(partition, "DeviceID");
                    if (partId is null) continue;

                    foreach (var logicalDisk in WmiHelper.Associators(partId, "Win32_LogicalDiskToPartition"))
                    {
                        var id = WmiHelper.GetString(logicalDisk, "DeviceID");
                        if (id is not null)
                            letters.Add(id);
                    }
                }
                map[diskDeviceId] = letters;
            }
        }
        catch { /* non-fatal — map will be incomplete */ }
        return map;
    }

    private static bool IsSystemDisk(List<string> driveLetter)
        => driveLetter.Any(l => string.Equals(l, Environment.GetFolderPath(Environment.SpecialFolder.System)[..2],
                                               StringComparison.OrdinalIgnoreCase));

    private static void PopulateDisksFromDriveInfo(HardwareSnapshot snap)
    {
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
        {
            try
            {
                snap.Disks.Add(new DiskInfo(
                    Model:         drive.Name,
                    SerialNumber:  null,
                    MediaType:     null,
                    InterfaceType: null,
                    TotalBytes:    drive.TotalSize,
                    FreeBytes:     drive.AvailableFreeSpace,
                    DriveLetter:   drive.Name.TrimEnd('\\'),
                    FileSystem:    drive.DriveFormat,
                    IsSystemDisk:  drive.Name.StartsWith(
                        Environment.GetFolderPath(Environment.SpecialFolder.System)[..3],
                        StringComparison.OrdinalIgnoreCase)));
            }
            catch { /* skip inaccessible drives */ }
        }
    }

    private void PopulateGpus(HardwareSnapshot snap)
    {
        try
        {
            var gpus = WmiHelper.Query(
                "SELECT Name, AdapterRAM, DriverVersion, VideoProcessor, VideoModeDescription, CurrentRefreshRate " +
                "FROM Win32_VideoController");

            foreach (var gpu in gpus)
            {
                var adapterRamRaw = WmiHelper.GetUInt32(gpu, "AdapterRAM");
                snap.Gpus.Add(new GpuInfo(
                    Name:                 WmiHelper.GetString(gpu, "Name") ?? "Unknown GPU",
                    AdapterRamBytes:      adapterRamRaw.HasValue ? (long?)adapterRamRaw.Value : null,
                    DriverVersion:        WmiHelper.GetString(gpu, "DriverVersion"),
                    VideoProcessor:       WmiHelper.GetString(gpu, "VideoProcessor"),
                    VideoModeDescription: WmiHelper.GetString(gpu, "VideoModeDescription"),
                    CurrentRefreshRate:   WmiHelper.GetInt32(gpu, "CurrentRefreshRate")));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query GPUs via Win32_VideoController");
        }
    }

    private void PopulateNetworkAdapters(HardwareSnapshot snap)
    {
        try
        {
            var adapters = WmiHelper.Query(
                "SELECT Name, Description, MACAddress, AdapterType, Speed " +
                "FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True AND MACAddress IS NOT NULL");

            foreach (var adapter in adapters)
            {
                snap.NetworkAdapters.Add(new NetworkAdapterInfo(
                    Name:        WmiHelper.GetString(adapter, "Name") ?? "Unknown Adapter",
                    Description: WmiHelper.GetString(adapter, "Description"),
                    MacAddress:  WmiHelper.GetString(adapter, "MACAddress"),
                    AdapterType: WmiHelper.GetString(adapter, "AdapterType"),
                    Speed:       (long?)WmiHelper.GetUInt64(adapter, "Speed"),
                    IsPhysical:  true));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query network adapters via Win32_NetworkAdapter");
        }
    }

    private void PopulateBattery(HardwareSnapshot snap)
    {
        try
        {
            var batteries = WmiHelper.Query(
                "SELECT Name, DesignCapacity FROM Win32_Battery");
            var battery = batteries.FirstOrDefault();
            if (battery is null) return;

            snap.HasBattery               = true;
            snap.BatteryManufacturer      = WmiHelper.GetString(battery, "Name");
            var cap = WmiHelper.GetUInt32(battery, "DesignCapacity");
            snap.BatteryDesignCapacityMWh = cap.HasValue ? (int?)cap.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query battery via Win32_Battery");
        }
    }

    // ─── OS snapshot ─────────────────────────────────────────────────────────

    private OperatingSystemSnapshot BuildOsSnapshot(Guid machineProfileId)
    {
        var snap = new OperatingSystemSnapshot
        {
            Id               = Guid.NewGuid(),
            MachineProfileId = machineProfileId,
            CapturedAt       = DateTimeOffset.UtcNow,
            Architecture     = Environment.Is64BitOperatingSystem ? "x64" : "x86"
        };

        try
        {
            var osObjects = WmiHelper.Query(
                "SELECT Caption, Version, BuildNumber, CSDVersion, InstallDate, LastBootUpTime, " +
                "SystemDirectory, TotalVirtualMemorySize, NumberOfProcessors " +
                "FROM Win32_OperatingSystem");

            var os = osObjects.FirstOrDefault();
            if (os is null) return snap;

            snap.OsName       = WmiHelper.GetString(os, "Caption") ?? Environment.OSVersion.ToString();
            snap.OsVersion    = WmiHelper.GetString(os, "Version") ?? string.Empty;
            snap.OsBuildNumber = ParseBuildNumber(snap.OsVersion);
            snap.OsRevision    = GetWindowsRevision();
            snap.ServicePack   = WmiHelper.GetString(os, "CSDVersion");
            snap.InstallDate   = WmiHelper.ParseWmiDate(os, "InstallDate");
            snap.LastBootTime  = WmiHelper.ParseWmiDate(os, "LastBootUpTime");
            snap.SystemDirectory = WmiHelper.GetString(os, "SystemDirectory");
            snap.TimeZoneId    = TimeZoneInfo.Local.Id;
            snap.ProcessorCount = WmiHelper.GetInt32(os, "NumberOfProcessors");

            var pageSizeKB = WmiHelper.GetUInt64(os, "TotalVirtualMemorySize");
            snap.PageFileSizeBytes = pageSizeKB.HasValue ? (long)pageSizeKB.Value * 1024 : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query OS info via Win32_OperatingSystem");
            // Fallback to Environment APIs
            snap.OsName       = Environment.OSVersion.VersionString;
            snap.OsVersion    = Environment.OSVersion.Version.ToString();
        }

        return snap;
    }

    // ─── Machine ID ──────────────────────────────────────────────────────────

    private string BuildMachineId()
    {
        string? cpuId      = GetCpuProcessorId();
        string? mbSerial   = GetMotherboardSerial();
        string? macAddress = GetFirstPhysicalMac();

        return MachineIdGenerator.Generate(cpuId, mbSerial, macAddress);
    }

    private string? GetCpuProcessorId()
    {
        try
        {
            return WmiHelper.Query("SELECT ProcessorId FROM Win32_Processor")
                .Select(o => WmiHelper.GetString(o, "ProcessorId"))
                .FirstOrDefault(id => !string.IsNullOrWhiteSpace(id));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query CPU ProcessorId");
            return null;
        }
    }

    private string? GetMotherboardSerial()
    {
        try
        {
            return WmiHelper.Query("SELECT SerialNumber FROM Win32_BaseBoard")
                .Select(o => WmiHelper.GetString(o, "SerialNumber"))
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
        }
        catch { return null; }
    }

    private string? GetFirstPhysicalMac()
    {
        try
        {
            return WmiHelper.Query(
                    "SELECT MACAddress FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True AND MACAddress IS NOT NULL")
                .Select(o => WmiHelper.GetString(o, "MACAddress"))
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
        }
        catch { return null; }
    }

    // ─── Decode helpers ──────────────────────────────────────────────────────

    private static string? FormatCacheSize(uint? sizeKB) =>
        sizeKB is null or 0 ? null : $"{sizeKB} KB";

    private static string? DecodeMemoryType(int? code) => code switch
    {
        20 => "DDR",
        21 => "DDR2",
        22 => "DDR2 FB-DIMM",
        24 => "DDR3",
        26 => "DDR4",
        34 => "DDR5",
        _  => null
    };

    private static string? DecodeFormFactor(int? code) => code switch
    {
        8  => "DIMM",
        12 => "SO-DIMM",
        13 => "RIMM",
        14 => "DIMM",
        15 => "SO-RIMM",
        _  => null
    };

    private static int? ParseBankLabel(string? label)
    {
        if (label is null) return null;
        // e.g. "BANK 0" → 0
        var parts = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && int.TryParse(parts[^1], out var n) ? n : null;
    }

    private static string ParseBuildNumber(string version)
    {
        // version like "10.0.22621.3447" → build = "22621"
        var parts = version.Split('.');
        return parts.Length >= 3 ? parts[2] : version;
    }

    private static string GetWindowsRevision()
    {
        // Windows 11 revision is in the 4th octet of the OS version string (via Environment)
        var v = Environment.OSVersion.Version;
        return v.Build > 0 ? v.Build.ToString() : string.Empty;
    }
}
