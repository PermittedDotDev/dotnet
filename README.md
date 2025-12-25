# Permitted

Official Permitted SDK for .NET.

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

// Initialize the client
var client = new PermittedClient();

// Validate a license
var license = await client.ValidateAsync(
    licenseKey: "XXXX-XXXX-XXXX-XXXX",
    hwid: GetHardwareId() // Your hardware ID implementation
);

if (license.Status == LicenseStatus.Active)
{
    Console.WriteLine($"License valid until: {license.ExpiresAt}");

    // Get remote config
    var config = await client.GetConfigAsync();
    Console.WriteLine($"Max projects: {config.Variables["max_projects"]}");

    // List available files
    var files = await client.GetFilesAsync();
    foreach (var file in files)
    {
        Console.WriteLine($"Available: {file.Name}");
    }
}
```

## Features

- License validation with HWID binding
- Automatic session refresh
- Remote configuration
- File downloads with signed URLs
- Supports .NET 6, 7, and 8

## Documentation

See the [Permitted Documentation](https://permitted.io/docs) for complete API reference and guides.

## License

MIT
