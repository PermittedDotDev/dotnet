using System.Diagnostics;
using System.Runtime.Versioning;

namespace Permitted.SDK.Hardware;

/// <summary>
/// Collects hardware identifiers on Windows using WMI.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsHardwareCollector : IHardwareCollector
{
    public Dictionary<string, string?> CollectAll()
    {
        var components = new Dictionary<string, string?>
        {
            ["cpu_id"] = GetWmiValue("Win32_Processor", "ProcessorId"),
            ["baseboard_serial"] = GetWmiValue("Win32_BaseBoard", "SerialNumber"),
            ["bios_serial"] = GetWmiValue("Win32_BIOS", "SerialNumber"),
            ["disk_serial"] = GetPrimaryDiskSerial(),
            ["machine_guid"] = GetMachineGuid(),
        };

        return components;
    }

    private static string? GetWmiValue(string wmiClass, string property)
    {
        try
        {
            // Use PowerShell to query WMI - works on all .NET versions
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"(Get-WmiObject -Class {wmiClass}).{property}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);

            // Filter out common placeholder values
            if (string.IsNullOrWhiteSpace(output) ||
                output.Equals("To Be Filled By O.E.M.", StringComparison.OrdinalIgnoreCase) ||
                output.Equals("Default string", StringComparison.OrdinalIgnoreCase) ||
                output.Equals("None", StringComparison.OrdinalIgnoreCase) ||
                output.All(c => c == '0'))
            {
                return null;
            }

            return output;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetPrimaryDiskSerial()
    {
        try
        {
            // Get the first physical disk serial
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"(Get-WmiObject -Class Win32_DiskDrive | Select-Object -First 1).SerialNumber\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);

            return string.IsNullOrWhiteSpace(output) ? null : output;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetMachineGuid()
    {
        try
        {
            // Read the Windows Machine GUID from registry
            var psi = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = "query HKLM\\SOFTWARE\\Microsoft\\Cryptography /v MachineGuid",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            // Parse the registry output
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("MachineGuid", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(new[] { "REG_SZ" }, StringSplitOptions.RemoveEmptyEntries);
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
