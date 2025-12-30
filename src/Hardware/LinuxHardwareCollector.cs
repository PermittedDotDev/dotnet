using System.Runtime.Versioning;

namespace Permitted.SDK.Hardware;

/// <summary>
/// Collects hardware identifiers on Linux using system files.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxHardwareCollector : IHardwareCollector
{
    public Dictionary<string, string?> CollectAll()
    {
        var components = new Dictionary<string, string?>
        {
            ["machine_id"] = GetMachineId(),
            ["product_uuid"] = GetProductUuid(),
            ["board_serial"] = GetBoardSerial(),
            ["disk_serial"] = GetPrimaryDiskSerial(),
        };

        return components;
    }

    private static string? GetMachineId()
    {
        // /etc/machine-id is the standard location
        // /var/lib/dbus/machine-id is a fallback
        var paths = new[]
        {
            "/etc/machine-id",
            "/var/lib/dbus/machine-id"
        };

        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(content))
                    {
                        return content;
                    }
                }
            }
            catch
            {
                // Continue to next path
            }
        }

        return null;
    }

    private static string? GetProductUuid()
    {
        // DMI product UUID (requires root or appropriate permissions)
        try
        {
            const string path = "/sys/class/dmi/id/product_uuid";
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path).Trim();
                if (!string.IsNullOrEmpty(content) && content != "00000000-0000-0000-0000-000000000000")
                {
                    return content;
                }
            }
        }
        catch
        {
            // May require elevated permissions
        }

        return null;
    }

    private static string? GetBoardSerial()
    {
        try
        {
            const string path = "/sys/class/dmi/id/board_serial";
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path).Trim();
                if (!string.IsNullOrEmpty(content) &&
                    content != "None" &&
                    content != "To Be Filled By O.E.M.")
                {
                    return content;
                }
            }
        }
        catch
        {
            // May require elevated permissions
        }

        return null;
    }

    private static string? GetPrimaryDiskSerial()
    {
        try
        {
            // Find the first block device
            var blockDevices = Directory.GetDirectories("/sys/block");
            foreach (var device in blockDevices)
            {
                var name = Path.GetFileName(device);

                // Skip loop devices, ram disks, etc.
                if (name.StartsWith("loop") ||
                    name.StartsWith("ram") ||
                    name.StartsWith("dm-"))
                {
                    continue;
                }

                // Try to get the serial
                var serialPath = Path.Combine(device, "device", "serial");
                if (File.Exists(serialPath))
                {
                    var serial = File.ReadAllText(serialPath).Trim();
                    if (!string.IsNullOrEmpty(serial))
                    {
                        return serial;
                    }
                }

                // Alternative: WWN (World Wide Name)
                var wwnPath = Path.Combine(device, "device", "wwid");
                if (File.Exists(wwnPath))
                {
                    var wwn = File.ReadAllText(wwnPath).Trim();
                    if (!string.IsNullOrEmpty(wwn))
                    {
                        return wwn;
                    }
                }
            }
        }
        catch
        {
            // May require elevated permissions
        }

        return null;
    }
}
