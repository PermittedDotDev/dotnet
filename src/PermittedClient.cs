namespace Permitted.SDK;

/// <summary>
/// Main client for interacting with the Permitted API.
/// </summary>
/// <example>
/// <code>
/// var client = new PermittedClient();
///
/// var license = await client.ValidateAsync("XXXX-XXXX-XXXX-XXXX", GetHardwareId());
///
/// if (license.Status == LicenseStatus.Active)
/// {
///     var config = await client.GetConfigAsync();
///     Console.WriteLine($"Welcome! Max projects: {config.Variables["max_projects"]}");
/// }
/// </code>
/// </example>
public class PermittedClient : IDisposable
{
    /// <summary>
    /// SDK version.
    /// </summary>
    public const string Version = "0.0.1";

    private readonly HttpClient _httpClient;
    private bool _disposed;

    /// <summary>
    /// Creates a new Permitted client.
    /// </summary>
    /// <param name="options">Optional client configuration.</param>
    public PermittedClient(PermittedClientOptions? options = null)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options?.BaseUrl ?? "https://api.permitted.io/v1/")
        };
    }

    // TODO: Implement SDK methods
    // See API_V1_SPECIFICATION.md for endpoint details

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the client resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }
}

/// <summary>
/// Configuration options for the Permitted client.
/// </summary>
public class PermittedClientOptions
{
    /// <summary>
    /// Base URL for the Permitted API. Defaults to https://api.permitted.io/v1/
    /// </summary>
    public string? BaseUrl { get; set; }
}
