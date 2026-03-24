using System.Runtime.InteropServices;

namespace FullBenchmark.Telemetry.Windows.Native;

/// <summary>
/// P/Invoke wrappers for Windows memory and power APIs.
/// GlobalMemoryStatusEx is preferred over PerformanceCounters for RAM readings
/// as it is faster, more accurate, and requires no special privileges.
/// </summary>
internal static class NativeMemoryReader
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpStatus);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint   dwLength;
        public uint   dwMemoryLoad;
        public ulong  ullTotalPhys;
        public ulong  ullAvailPhys;
        public ulong  ullTotalPageFile;
        public ulong  ullAvailPageFile;
        public ulong  ullTotalVirtual;
        public ulong  ullAvailVirtual;
        public ulong  ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte  ACLineStatus;          // 0=offline,1=online,255=unknown
        public byte  BatteryFlag;           // 0=high,1=low,2=critical,4=charging,8=no,128=no battery,255=unknown
        public byte  BatteryLifePercent;    // 0–100, or 255 if unknown
        public byte  SystemStatusFlag;
        public uint  BatteryLifeTime;
        public uint  BatteryFullLifeTime;
    }

    public readonly record struct MemoryStatus(
        long TotalPhysicalBytes,
        long AvailablePhysicalBytes,
        long TotalPageFileBytes,
        long AvailablePageFileBytes);

    public readonly record struct PowerStatus(
        bool IsOnAcPower,
        double? BatteryChargePercent);

    public static MemoryStatus? ReadMemory()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status)) return null;

        return new MemoryStatus(
            TotalPhysicalBytes:     (long)status.ullTotalPhys,
            AvailablePhysicalBytes: (long)status.ullAvailPhys,
            TotalPageFileBytes:     (long)status.ullTotalPageFile,
            AvailablePageFileBytes: (long)status.ullAvailPageFile);
    }

    public static PowerStatus ReadPower()
    {
        if (!GetSystemPowerStatus(out var p))
            return new PowerStatus(true, null);

        bool onAc  = p.ACLineStatus == 1;
        double? pct = p.BatteryLifePercent == 255 ? null : (double)p.BatteryLifePercent;

        return new PowerStatus(onAc, pct);
    }
}
