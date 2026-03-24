using System.Security.Cryptography;
using System.Text;

namespace FullBenchmark.Core.Utilities;

/// <summary>
/// Generates a stable, reproducible machine identifier.
/// Combines CPU processor ID, motherboard serial, and first physical MAC address,
/// then SHA-256 hashes to a compact hex string.
/// Callers provide the raw component strings; OS-specific retrieval is in Telemetry.Windows.
/// </summary>
public static class MachineIdGenerator
{
    /// <summary>
    /// Derives a stable 32-character hex machine ID from supplied hardware identifiers.
    /// Gracefully handles nulls — uses "unknown" for missing components.
    /// </summary>
    public static string Generate(
        string? cpuProcessorId,
        string? motherboardSerial,
        string? firstMacAddress)
    {
        var raw = string.Concat(
            Normalize(cpuProcessorId),
            "|",
            Normalize(motherboardSerial),
            "|",
            Normalize(firstMacAddress));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..32].ToLowerInvariant();
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToUpperInvariant();
}
