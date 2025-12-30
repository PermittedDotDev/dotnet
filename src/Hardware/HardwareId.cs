using System.Security.Cryptography;
using System.Text;

namespace Permitted.SDK.Hardware;

/// <summary>
/// Generates hardware identifiers for license binding.
/// Collects real hardware serial numbers for robust device identification.
/// </summary>
public static class HardwareId
{
    /// <summary>
    /// Gets the hardware ID for the current device.
    /// Combines multiple hardware sources and produces a SHA-256 hash.
    /// </summary>
    /// <returns>A 64-character hex string representing the device.</returns>
    public static string Get()
    {
        var collector = GetPlatformCollector();
        var components = collector.CollectAll();
        return HashComponents(components);
    }

    /// <summary>
    /// Gets individual hardware components for debugging.
    /// </summary>
    /// <returns>Dictionary of component names and their values.</returns>
    public static Dictionary<string, string?> GetComponents()
    {
        var collector = GetPlatformCollector();
        return collector.CollectAll();
    }

    private static IHardwareCollector GetPlatformCollector()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsHardwareCollector();
        }
        else if (OperatingSystem.IsMacOS())
        {
            return new MacOsHardwareCollector();
        }
        else if (OperatingSystem.IsLinux())
        {
            return new LinuxHardwareCollector();
        }
        else
        {
            return new FallbackHardwareCollector();
        }
    }

    private static string HashComponents(Dictionary<string, string?> components)
    {
        // Sort keys for consistent ordering
        var sortedKeys = components.Keys.OrderBy(k => k).ToList();

        var sb = new StringBuilder();
        foreach (var key in sortedKeys)
        {
            var value = components[key];
            if (!string.IsNullOrEmpty(value))
            {
                sb.Append(key);
                sb.Append(':');
                sb.Append(value);
                sb.Append('|');
            }
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Interface for platform-specific hardware collection.
/// </summary>
internal interface IHardwareCollector
{
    /// <summary>
    /// Collects all available hardware identifiers.
    /// </summary>
    Dictionary<string, string?> CollectAll();
}
