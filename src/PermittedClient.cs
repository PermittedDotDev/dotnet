using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Permitted.SDK.Exceptions;
using Permitted.SDK.Hardware;
using Permitted.SDK.Models;

namespace Permitted.SDK;

/// <summary>
/// Main client for interacting with the Permitted API.
/// Handles license validation, session management, remote config, and file downloads.
/// </summary>
/// <example>
/// <code>
/// using var client = new PermittedClient(new PermittedClientOptions
/// {
///     ApiKey = "pk_live_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
/// });
///
/// // Validate license (auto-detects device identifier)
/// var result = await client.ValidateAsync("XXXX-XXXX-XXXX-XXXX");
///
/// // Use the license
/// if (result.License.Status == "active")
/// {
///     var config = await client.GetConfigAsync();
///     var maxProjects = config.GetInt("max_projects", 5);
/// }
/// </code>
/// </example>
public sealed class PermittedClient : IDisposable
{
    /// <summary>
    /// SDK version.
    /// </summary>
    public const string Version = "0.1.0";

    private const string DefaultBaseUrl = "https://permitted.dev/api/v1";
    private const int RefreshMarginSeconds = 300; // 5 minutes

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _apiKey;
    private readonly object _tokenLock = new();

    private string? _token;
    private long _expiresAtUnix;
    private string? _licenseKey;
    private string? _identifier;
    private bool _disposed;

    /// <summary>
    /// Creates a new Permitted client.
    /// </summary>
    /// <param name="options">Client configuration with required API key.</param>
    /// <exception cref="ArgumentNullException">Options or API key is null.</exception>
    public PermittedClient(PermittedClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ApiKey, nameof(options.ApiKey));

        _apiKey = options.ApiKey;

        var baseUrl = options.BaseUrl ?? DefaultBaseUrl;
        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += '/';
        }

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"permitted-dotnet/{Version}");
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);

        // Add client IP headers for VPN detection
        var clientIps = GetLocalIpAddresses();
        if (clientIps.Count > 0)
        {
            _httpClient.DefaultRequestHeaders.Add("X-Client-IP", clientIps[0]);
            if (clientIps.Count > 1)
            {
                _httpClient.DefaultRequestHeaders.Add("X-Client-IPs", string.Join(",", clientIps));
            }
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
        };
    }

    /// <summary>
    /// Gets the device identifier for the current device.
    /// Uses real hardware serial numbers (CPU, baseboard, disk, etc.).
    /// </summary>
    /// <returns>A 64-character hex string representing the device.</returns>
    public static string GetDeviceId() => HardwareId.Get();

    /// <summary>
    /// Gets the hardware ID for the current device.
    /// </summary>
    /// <returns>A 64-character hex string representing the device.</returns>
    [Obsolete("Use GetDeviceId() instead. This method will be removed in a future version.")]
    public static string GetHardwareId() => GetDeviceId();

    /// <summary>
    /// Gets individual hardware components for debugging.
    /// </summary>
    /// <returns>Dictionary of component names and their values.</returns>
    public static Dictionary<string, string?> GetHardwareComponents() => HardwareId.GetComponents();

    /// <summary>
    /// Whether the client has a valid session.
    /// </summary>
    public bool IsAuthenticated
    {
        get
        {
            lock (_tokenLock)
            {
                return !string.IsNullOrEmpty(_token) && !IsTokenExpired();
            }
        }
    }

    /// <summary>
    /// The current token, if authenticated.
    /// </summary>
    public string? Token
    {
        get
        {
            lock (_tokenLock)
            {
                return _token;
            }
        }
    }

    /// <summary>
    /// When the current token expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt
    {
        get
        {
            lock (_tokenLock)
            {
                return _expiresAtUnix > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(_expiresAtUnix)
                    : null;
            }
        }
    }

    #region Status

    /// <summary>
    /// Checks API availability.
    /// </summary>
    /// <param name="productId">Optional product ID to check API status for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status result.</returns>
    public async Task<StatusResult> GetStatusAsync(
        string? productId = null,
        CancellationToken cancellationToken = default)
    {
        var url = "status";
        if (!string.IsNullOrEmpty(productId))
        {
            url += $"?product_id={Uri.EscapeDataString(productId)}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        return await HandleResponseAsync<StatusResult>(response, cancellationToken);
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates a license key and establishes a session.
    /// </summary>
    /// <param name="licenseKey">The license key to validate.</param>
    /// <param name="identifier">Device identifier for binding (hardware fingerprint, account ID, or installation ID).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with token and license info.</returns>
    /// <exception cref="InvalidLicenseException">License key not found.</exception>
    /// <exception cref="LicenseExpiredException">License has expired.</exception>
    /// <exception cref="LicenseSuspendedException">License is suspended.</exception>
    /// <exception cref="IdentifierMismatchException">Device doesn't match.</exception>
    public async Task<ValidationResult> ValidateAsync(
        string licenseKey,
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var request = new { license_key = licenseKey, identifier };
        var response = await _httpClient.PostAsJsonAsync(
            "license/validate",
            request,
            _jsonOptions,
            cancellationToken);

        var result = await HandleResponseAsync<ValidationResult>(response, cancellationToken);

        // Store session info
        lock (_tokenLock)
        {
            _token = result.Token;
            _expiresAtUnix = result.ExpiresAtUnix;
            _licenseKey = licenseKey;
            _identifier = identifier;
        }

        return result;
    }

    /// <summary>
    /// Validates a license key using the device's hardware fingerprint.
    /// </summary>
    /// <param name="licenseKey">The license key to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with token and license info.</returns>
    public Task<ValidationResult> ValidateAsync(
        string licenseKey,
        CancellationToken cancellationToken = default)
    {
        return ValidateAsync(licenseKey, GetDeviceId(), cancellationToken);
    }

    #endregion

    #region Session

    /// <summary>
    /// Refreshes the current session token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New session with extended expiry.</returns>
    /// <exception cref="InvalidOperationException">No active session.</exception>
    /// <exception cref="TokenException">Token is invalid or expired.</exception>
    public async Task<SessionRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        string currentToken;
        lock (_tokenLock)
        {
            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("No active session. Call ValidateAsync first.");
            }
            currentToken = _token;
        }

        var request = new { token = currentToken };
        var response = await _httpClient.PostAsJsonAsync(
            "session/refresh",
            request,
            _jsonOptions,
            cancellationToken);

        var result = await HandleResponseAsync<SessionRefreshResult>(response, cancellationToken);

        // Update session
        lock (_tokenLock)
        {
            _token = result.Token;
            _expiresAtUnix = result.ExpiresAtUnix;
        }

        return result;
    }

    /// <summary>
    /// Checks if the current session is valid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ping result with session status.</returns>
    public async Task<PingResult> PingAsync(CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await _httpClient.GetAsync("ping", cancellationToken);
        return await HandleResponseAsync<PingResult>(response, cancellationToken);
    }

    /// <summary>
    /// Ensures the session is valid, refreshing if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task EnsureValidSessionAsync(CancellationToken cancellationToken = default)
    {
        bool shouldRefresh;
        lock (_tokenLock)
        {
            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("No active session. Call ValidateAsync first.");
            }
            shouldRefresh = ShouldRefresh();
        }

        if (shouldRefresh)
        {
            try
            {
                await RefreshAsync(cancellationToken);
            }
            catch (TokenException)
            {
                // Token expired or invalid, need to revalidate
                if (!string.IsNullOrEmpty(_licenseKey) && !string.IsNullOrEmpty(_identifier))
                {
                    await ValidateAsync(_licenseKey, _identifier, cancellationToken);
                }
                else
                {
                    throw;
                }
            }
        }
    }

    #endregion

    #region License

    /// <summary>
    /// Gets detailed information about the current license.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full license details.</returns>
    public async Task<License> GetLicenseAsync(CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await _httpClient.GetAsync("license", cancellationToken);
        return await HandleResponseAsync<License>(response, cancellationToken);
    }

    #endregion

    #region Config

    /// <summary>
    /// Gets remote configuration values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Configuration with variables.</returns>
    public async Task<ConfigResult> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await _httpClient.GetAsync("config", cancellationToken);
        return await HandleResponseAsync<ConfigResult>(response, cancellationToken);
    }

    #endregion

    #region Files

    /// <summary>
    /// Lists available files for download.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available files.</returns>
    public async Task<FilesResult> GetFilesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await _httpClient.GetAsync("files", cancellationToken);
        return await HandleResponseAsync<FilesResult>(response, cancellationToken);
    }

    /// <summary>
    /// Gets a signed download URL for a file.
    /// </summary>
    /// <param name="fileId">The file ID to download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Download URL (expires in 15 minutes).</returns>
    /// <exception cref="Exceptions.FileNotFoundException">File not found.</exception>
    /// <exception cref="FileAccessDeniedException">Access denied.</exception>
    /// <exception cref="DownloadRateLimitedException">Rate limit exceeded.</exception>
    public async Task<DownloadResult> GetDownloadUrlAsync(
        string fileId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await _httpClient.GetAsync($"files/{Uri.EscapeDataString(fileId)}/download", cancellationToken);
        return await HandleResponseAsync<DownloadResult>(response, cancellationToken);
    }

    /// <summary>
    /// Downloads a file to the specified path.
    /// </summary>
    /// <param name="fileId">The file ID to download.</param>
    /// <param name="destinationPath">Where to save the file.</param>
    /// <param name="progress">Optional progress callback (bytes downloaded, total bytes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DownloadFileAsync(
        string fileId,
        string destinationPath,
        Action<long, long?>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var download = await GetDownloadUrlAsync(fileId, cancellationToken);

        using var response = await _httpClient.GetAsync(
            download.Url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;
            progress?.Invoke(totalRead, totalBytes);
        }
    }

    #endregion

    #region Private Methods

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        string? token;
        lock (_tokenLock)
        {
            token = _token;
        }

        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("No active session. Call ValidateAsync first.");
        }

        // Refresh if needed
        if (ShouldRefresh())
        {
            await RefreshAsync(cancellationToken);
        }

        // Set auth header
        lock (_tokenLock)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
        }
    }

    private bool IsTokenExpired()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return now >= _expiresAtUnix;
    }

    private bool ShouldRefresh()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return now >= _expiresAtUnix - RefreshMarginSeconds;
    }

    private async Task<T> HandleResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
            return result ?? throw new PermittedException("PARSE_ERROR", "Failed to parse response");
        }

        // Handle error response
        var statusCode = (int)response.StatusCode;
        ErrorResponse? error = null;

        try
        {
            error = await response.Content.ReadFromJsonAsync<ErrorResponse>(_jsonOptions, cancellationToken);
        }
        catch
        {
            // Could not parse error response
        }

        var code = error?.Error?.Code ?? "UNKNOWN_ERROR";
        var message = error?.Error?.Message ?? $"Request failed with status {statusCode}";

        throw code switch
        {
            "INVALID_LICENSE" => new InvalidLicenseException(message),
            "LICENSE_EXPIRED" => new LicenseExpiredException(message),
            "LICENSE_SUSPENDED" => new LicenseSuspendedException(message),
            "LICENSE_REVOKED" => new LicenseRevokedException(message),
            "IDENTIFIER_MISMATCH" => new IdentifierMismatchException(message),
            "TOKEN_MISSING" or "TOKEN_INVALID" or "TOKEN_EXPIRED" or "TOKEN_REVOKED"
                => new TokenException(code, message),
            "API_DISABLED" => new ApiDisabledException(message),
            "RATE_LIMITED" => new RateLimitedException(message, GetRetryAfter(response)),
            "FILE_NOT_FOUND" => new Exceptions.FileNotFoundException(message),
            "FILE_ACCESS_DENIED" => new FileAccessDeniedException(message),
            "DOWNLOAD_RATE_LIMITED" => new DownloadRateLimitedException(message),
            _ => new PermittedException(code, message, statusCode)
        };
    }

    private static int? GetRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var value = values.FirstOrDefault();
            if (int.TryParse(value, out var seconds))
            {
                return seconds;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the local IP addresses of the machine.
    /// Filters out loopback, link-local, and VPN adapter addresses where possible.
    /// </summary>
    private static List<string> GetLocalIpAddresses()
    {
        var ips = new List<string>();

        try
        {
            // Get all network interfaces
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                    && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var iface in interfaces)
            {
                // Skip common VPN adapter names (for honest reporting)
                var name = iface.Name.ToLowerInvariant();
                var description = iface.Description.ToLowerInvariant();

                // Get IPv4 addresses from this interface
                var ipProps = iface.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ip = addr.Address.ToString();

                        // Skip loopback and link-local
                        if (ip.StartsWith("127.") || ip.StartsWith("169.254."))
                            continue;

                        ips.Add(ip);
                    }
                }
            }
        }
        catch
        {
            // Fallback: try simple DNS lookup
            try
            {
                var hostName = Dns.GetHostName();
                var hostEntry = Dns.GetHostEntry(hostName);
                foreach (var addr in hostEntry.AddressList)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ip = addr.ToString();
                        if (!ip.StartsWith("127.") && !ip.StartsWith("169.254."))
                        {
                            ips.Add(ip);
                        }
                    }
                }
            }
            catch
            {
                // Could not determine IP addresses
            }
        }

        return ips;
    }

    #endregion

    #region Dispose

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
    }

    #endregion

    #region Internal Types

    private sealed class ErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorDetails? Error { get; set; }
    }

    private sealed class ErrorDetails
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    #endregion
}

/// <summary>
/// Configuration options for the Permitted client.
/// </summary>
public sealed class PermittedClientOptions
{
    /// <summary>
    /// Product API key (required). Format: pk_live_xxxxxxxx...
    /// Found in your product's Developer settings.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Base URL for the Permitted API. Defaults to https://permitted.dev/api/v1
    /// </summary>
    public string? BaseUrl { get; set; }
}
