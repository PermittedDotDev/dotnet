namespace Permitted.SDK.Hardware;

/// <summary>
/// Fallback hardware collector for unsupported platforms.
/// Uses less reliable but universally available identifiers.
/// </summary>
internal sealed class FallbackHardwareCollector : IHardwareCollector
{
    public Dictionary<string, string?> CollectAll()
    {
        // On unsupported platforms, we use what's available from .NET
        // These are less reliable but provide some level of identification
        var components = new Dictionary<string, string?>
        {
            ["machine_name"] = Environment.MachineName,
            ["user_name"] = Environment.UserName,
            ["os_version"] = Environment.OSVersion.ToString(),
            ["processor_count"] = Environment.ProcessorCount.ToString(),
            ["is_64bit"] = Environment.Is64BitOperatingSystem.ToString(),
        };

        return components;
    }
}
