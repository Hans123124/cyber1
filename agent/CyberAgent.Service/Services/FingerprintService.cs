using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace CyberAgent.Service.Services;

/// <summary>
/// Generates a stable hardware fingerprint for workstation identification.
/// Uses CPU ID + BIOS serial + board serial.
/// </summary>
public static class FingerprintService
{
    public static string GetFingerprint()
    {
        var parts = new List<string>
        {
            GetWmiValue("Win32_Processor", "ProcessorId"),
            GetWmiValue("Win32_BIOS", "SerialNumber"),
            GetWmiValue("Win32_BaseBoard", "SerialNumber"),
            Environment.MachineName
        };

        var combined = string.Join("|", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash);
    }

    private static string GetWmiValue(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            foreach (var obj in searcher.Get())
            {
                var val = obj[property]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }
        }
        catch
        {
            // WMI not available
        }
        return string.Empty;
    }
}
