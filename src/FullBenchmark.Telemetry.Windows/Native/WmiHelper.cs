using System.Management;

namespace FullBenchmark.Telemetry.Windows.Native;

/// <summary>
/// Utility helpers for safe WMI property extraction.
/// All methods return null rather than throwing when a property is absent or malformed.
/// </summary>
internal static class WmiHelper
{
    public static string? GetString(ManagementBaseObject obj, string property)
    {
        try { return obj[property]?.ToString()?.Trim(); }
        catch { return null; }
    }

    public static int? GetInt32(ManagementBaseObject obj, string property)
    {
        try
        {
            var v = obj[property];
            return v is null ? null : Convert.ToInt32(v);
        }
        catch { return null; }
    }

    public static uint? GetUInt32(ManagementBaseObject obj, string property)
    {
        try
        {
            var v = obj[property];
            return v is null ? null : Convert.ToUInt32(v);
        }
        catch { return null; }
    }

    public static long? GetInt64(ManagementBaseObject obj, string property)
    {
        try
        {
            var v = obj[property];
            return v is null ? null : Convert.ToInt64(v);
        }
        catch { return null; }
    }

    public static ulong? GetUInt64(ManagementBaseObject obj, string property)
    {
        try
        {
            var v = obj[property];
            return v is null ? null : Convert.ToUInt64(v);
        }
        catch { return null; }
    }

    public static bool? GetBool(ManagementBaseObject obj, string property)
    {
        try
        {
            var v = obj[property];
            return v is null ? null : Convert.ToBoolean(v);
        }
        catch { return null; }
    }

    /// <summary>
    /// Parses a WMI datetime string (e.g., "20210614000000.000000+000") to DateTimeOffset.
    /// Returns null for any parse failure or placeholder values containing asterisks.
    /// </summary>
    public static DateTimeOffset? ParseWmiDate(ManagementBaseObject obj, string property)
    {
        try
        {
            var raw = GetString(obj, property);
            if (raw is null || raw.Contains('*')) return null;

            // ManagementDateTimeConverter returns DateTime in local time
            var local = ManagementDateTimeConverter.ToDateTime(raw);
            return new DateTimeOffset(local);
        }
        catch { return null; }
    }

    /// <summary>Executes a WMI SELECT query and returns results. Returns empty on any error.</summary>
    public static IEnumerable<ManagementObject> Query(string wql)
    {
        using var searcher = new ManagementObjectSearcher(wql);
        return [..searcher.Get().Cast<ManagementObject>()];
    }

    /// <summary>
    /// Returns ASSOCIATORS OF a given WMI object path, filtered by association class.
    /// Escapes backslashes in the object path automatically.
    /// </summary>
    public static IEnumerable<ManagementObject> Associators(string objectPath, string assocClass)
    {
        var safePath = objectPath.Replace(@"\", @"\\");
        using var searcher = new ManagementObjectSearcher(
            $"ASSOCIATORS OF {{{safePath}}} WHERE AssocClass={assocClass}");
        return [..searcher.Get().Cast<ManagementObject>()];
    }

    /// <summary>
    /// Maps Win32_Processor.Architecture numeric value to a display string.
    /// https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-processor
    /// </summary>
    public static string GetCpuArchitectureName(ushort arch) => arch switch
    {
        0  => "x86",
        1  => "MIPS",
        2  => "Alpha",
        3  => "PowerPC",
        5  => "ARM",
        6  => "IA-64",
        9  => "x64",
        12 => "ARM64",
        _  => $"Unknown ({arch})"
    };
}
