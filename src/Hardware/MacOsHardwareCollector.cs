using System.Diagnostics;
using System.Runtime.Versioning;

namespace Permitted.SDK.Hardware;

/// <summary>
/// Collects hardware identifiers on macOS using system_profiler and IOKit.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOsHardwareCollector : IHardwareCollector
{
    public Dictionary<string, string?> CollectAll()
    {
        var components = new Dictionary<string, string?>
        {
            ["platform_uuid"] = GetPlatformUuid(),
            ["serial_number"] = GetSerialNumber(),
            ["hardware_uuid"] = GetHardwareUuid(),
        };

        return components;
    }

    private static string? GetPlatformUuid()
    {
        try
        {
            // IOPlatformUUID - the most reliable hardware identifier on macOS
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/ioreg",
                Arguments = "-rd1 -c IOPlatformExpertDevice",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            // Parse the output for IOPlatformUUID
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("IOPlatformUUID"))
                {
                    var start = line.IndexOf('"', line.IndexOf('=')) + 1;
                    var end = line.LastIndexOf('"');
                    if (start > 0 && end > start)
                    {
                        return line.Substring(start, end - start);
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetSerialNumber()
    {
        try
        {
            // Get Mac serial number
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/ioreg",
                Arguments = "-rd1 -c IOPlatformExpertDevice",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            // Parse for IOPlatformSerialNumber
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("IOPlatformSerialNumber"))
                {
                    var start = line.IndexOf('"', line.IndexOf('=')) + 1;
                    var end = line.LastIndexOf('"');
                    if (start > 0 && end > start)
                    {
                        return line.Substring(start, end - start);
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetHardwareUuid()
    {
        try
        {
            // Alternative method using system_profiler
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/system_profiler",
                Arguments = "SPHardwareDataType",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(10000);

            // Parse for Hardware UUID
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("Hardware UUID:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 2)
                    {
                        return parts[1].Trim();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
