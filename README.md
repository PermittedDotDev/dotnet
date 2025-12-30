# Permitted SDK for .NET

Official .NET SDK for the [Permitted](https://permitted.dev) licensing platform.

## Installation

```bash
dotnet add package Permitted
```

Or via NuGet Package Manager:

```
Install-Package Permitted
```

## Quick Start

```csharp
using Permitted.SDK;

// Create client with your product's API key (from product settings)
using var client = new PermittedClient(new PermittedClientOptions
{
    ApiKey = "pk_live_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
});

// Validate license - automatically uses device hardware ID
var result = await client.ValidateAsync("XXXX-XXXX-XXXX-XXXX");

if (result.License.Status == "active")
{
    Console.WriteLine($"Welcome! License valid until: {result.License.ExpiresAt}");

    // Get remote config
    var config = await client.GetConfigAsync();
    var maxProjects = config.GetInt("max_projects", 5);

    // List available files
    var files = await client.GetFilesAsync();
    foreach (var file in files.Files)
    {
        Console.WriteLine($"Available: {file.Name} ({file.Size} bytes)");
    }
}
```

## Features

- **Automatic Device ID**: Collects real hardware serial numbers (CPU ID, baseboard serial, disk serial, etc.)
- **Session Management**: Automatic token refresh before expiration
- **Remote Configuration**: Fetch tier-specific config with typed getters
- **Secure File Downloads**: Time-limited signed URLs for protected files
- **Async/Await**: Full async support with CancellationToken
- **Multi-platform**: Windows, macOS, and Linux support

## Device Identifier Collection

The SDK automatically collects hardware identifiers for device binding:

| Platform | Sources |
|----------|---------|
| Windows  | CPU ProcessorId, Baseboard Serial, BIOS Serial, Disk Serial, MachineGuid |
| macOS    | IOPlatformUUID, Serial Number, Hardware UUID |
| Linux    | /etc/machine-id, Product UUID, Board Serial, Disk Serial |

```csharp
// Get device ID manually (optional)
var deviceId = PermittedClient.GetDeviceId();
Console.WriteLine($"Device ID: {deviceId}");

// Debug: see individual components
var components = PermittedClient.GetHardwareComponents();
foreach (var (key, value) in components)
{
    Console.WriteLine($"  {key}: {value ?? "N/A"}");
}

// Or use a custom identifier (account-based binding)
var result = await client.ValidateAsync("XXXX-XXXX-XXXX-XXXX", user.Id);
```

## API Reference

### Validation

```csharp
// Validate with auto-detected hardware ID
var result = await client.ValidateAsync("XXXX-XXXX-XXXX-XXXX");

// Or provide explicit device identifier
var result = await client.ValidateAsync("XXXX-XXXX-XXXX-XXXX", customIdentifier);

// Access validation result
Console.WriteLine($"Token: {result.Token}");
Console.WriteLine($"Expires: {result.ExpiresAt}");
Console.WriteLine($"License Status: {result.License.Status}");
Console.WriteLine($"Tier: {result.License.Tier?.Name}");
```

### Session Management

```csharp
// Check if authenticated
if (client.IsAuthenticated)
{
    Console.WriteLine($"Token expires: {client.ExpiresAt}");
}

// Ensure session is valid (refreshes if needed)
await client.EnsureValidSessionAsync();

// Manual refresh
var session = await client.RefreshAsync();

// Ping to verify session
var ping = await client.PingAsync();
Console.WriteLine($"Session valid: {ping.Valid}");
```

### License Info

```csharp
// Get detailed license information
var license = await client.GetLicenseAsync();

Console.WriteLine($"Key: {license.Key}");
Console.WriteLine($"Status: {license.Status}");
Console.WriteLine($"Email: {license.Email}");
Console.WriteLine($"Created: {license.CreatedAt}");
Console.WriteLine($"Expires: {license.ExpiresAt}");
Console.WriteLine($"Tier: {license.Tier?.Name}");
```

### Remote Configuration

```csharp
var config = await client.GetConfigAsync();

// Typed getters with defaults
var maxProjects = config.GetInt("max_projects", 5);
var apiEnabled = config.GetBool("api_enabled", false);
var threshold = config.GetDouble("threshold", 0.5);
var message = config.GetString("welcome_message", "Hello!");

// Raw access
if (config.Variables.TryGetValue("custom_key", out var value))
{
    Console.WriteLine($"Custom value: {value}");
}
```

### File Downloads

```csharp
// List available files
var files = await client.GetFilesAsync();

foreach (var file in files.Files)
{
    Console.WriteLine($"{file.Name}: {file.Size} bytes");
}

// Get signed download URL
var download = await client.GetDownloadUrlAsync("file_xxx");
Console.WriteLine($"Download: {download.Url}");
Console.WriteLine($"Expires: {download.ExpiresAt}");

// Download directly to disk with progress
await client.DownloadFileAsync(
    "file_xxx",
    "/path/to/destination",
    progress: (downloaded, total) =>
    {
        var percent = total.HasValue ? (downloaded * 100 / total.Value) : 0;
        Console.Write($"\rDownloading: {percent}%");
    }
);
```

### API Status

```csharp
// Check API status
var status = await client.GetStatusAsync();
Console.WriteLine($"API Status: {status.Status}");

// Check status for specific product
var productStatus = await client.GetStatusAsync("prod_xxx");
```

## Error Handling

The SDK throws specific exceptions for different error conditions:

```csharp
try
{
    var result = await client.ValidateAsync(licenseKey);
}
catch (InvalidLicenseException)
{
    Console.WriteLine("License key not found");
}
catch (LicenseExpiredException)
{
    Console.WriteLine("License has expired");
}
catch (LicenseSuspendedException)
{
    Console.WriteLine("License is suspended");
}
catch (LicenseRevokedException)
{
    Console.WriteLine("License has been revoked");
}
catch (IdentifierMismatchException)
{
    Console.WriteLine("Device mismatch - license bound to different device");
}
catch (TokenException ex)
{
    Console.WriteLine($"Session error: {ex.Code}");
}
catch (RateLimitedException ex)
{
    Console.WriteLine($"Rate limited. Retry after: {ex.RetryAfterSeconds}s");
}
catch (PermittedException ex)
{
    Console.WriteLine($"API error [{ex.Code}]: {ex.Message}");
}
```

### Exception Hierarchy

| Exception | Description |
|-----------|-------------|
| `PermittedException` | Base exception for all API errors |
| `InvalidLicenseException` | License key not found |
| `LicenseExpiredException` | License validity period ended |
| `LicenseSuspendedException` | License temporarily suspended |
| `LicenseRevokedException` | License permanently revoked |
| `IdentifierMismatchException` | Device doesn't match bound identifier |
| `TokenException` | Session token invalid/expired |
| `RateLimitedException` | Too many requests |
| `ApiDisabledException` | API disabled for product |
| `FileNotFoundException` | Requested file not found |
| `FileAccessDeniedException` | Tier doesn't have file access |
| `DownloadRateLimitedException` | Download rate limit exceeded |

## Configuration

```csharp
// Full configuration options
var client = new PermittedClient(new PermittedClientOptions
{
    // Required: Your product's API key (from product settings > Developer)
    ApiKey = "pk_live_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",

    // Optional: Custom API base URL (for self-hosted instances)
    BaseUrl = "https://your-instance.permitted.dev/api/v1"
});
```

## Requirements

- .NET 6.0, 7.0, or 8.0
- Windows, macOS, or Linux

## Documentation

See the [Permitted Documentation](https://permitted.dev/docs) for complete API reference and guides.

## License

MIT
