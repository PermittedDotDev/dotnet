namespace Permitted.SDK.Exceptions;

/// <summary>
/// Base exception for all Permitted SDK errors.
/// </summary>
public class PermittedException : Exception
{
    /// <summary>
    /// The error code from the API.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// HTTP status code, if applicable.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Creates a new Permitted exception.
    /// </summary>
    public PermittedException(string code, string message, int? statusCode = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Creates a new Permitted exception with an inner exception.
    /// </summary>
    public PermittedException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}

/// <summary>
/// Thrown when the license key is invalid or not found.
/// </summary>
public class InvalidLicenseException : PermittedException
{
    /// <summary>Creates a new invalid license exception.</summary>
    public InvalidLicenseException(string message = "License key not found")
        : base("INVALID_LICENSE", message, 401) { }
}

/// <summary>
/// Thrown when the license has expired.
/// </summary>
public class LicenseExpiredException : PermittedException
{
    /// <summary>Creates a new license expired exception.</summary>
    public LicenseExpiredException(string message = "License has expired")
        : base("LICENSE_EXPIRED", message, 401) { }
}

/// <summary>
/// Thrown when the license has been suspended.
/// </summary>
public class LicenseSuspendedException : PermittedException
{
    /// <summary>Creates a new license suspended exception.</summary>
    public LicenseSuspendedException(string message = "License has been suspended")
        : base("LICENSE_SUSPENDED", message, 403) { }
}

/// <summary>
/// Thrown when the license has been revoked.
/// </summary>
public class LicenseRevokedException : PermittedException
{
    /// <summary>Creates a new license revoked exception.</summary>
    public LicenseRevokedException(string message = "License has been revoked")
        : base("LICENSE_REVOKED", message, 403) { }
}

/// <summary>
/// Thrown when the identifier doesn't match the bound device.
/// </summary>
public class IdentifierMismatchException : PermittedException
{
    /// <summary>Creates a new identifier mismatch exception.</summary>
    public IdentifierMismatchException(string message = "License is bound to a different device")
        : base("IDENTIFIER_MISMATCH", message, 403) { }
}

/// <summary>
/// Thrown when the identifier doesn't match the bound device.
/// </summary>
[Obsolete("Use IdentifierMismatchException instead. This class will be removed in a future version.")]
public class HwidMismatchException : IdentifierMismatchException
{
    /// <summary>Creates a new HWID mismatch exception.</summary>
    public HwidMismatchException(string message = "License is bound to a different device")
        : base(message) { }
}

/// <summary>
/// Thrown when the token is invalid, expired, or revoked.
/// </summary>
public class TokenException : PermittedException
{
    /// <summary>Creates a new token exception.</summary>
    public TokenException(string code, string message)
        : base(code, message, 401) { }
}

/// <summary>
/// Thrown when API access is disabled for the product.
/// </summary>
public class ApiDisabledException : PermittedException
{
    /// <summary>Creates a new API disabled exception.</summary>
    public ApiDisabledException(string message = "API access is not enabled for this product")
        : base("API_DISABLED", message, 403) { }
}

/// <summary>
/// Thrown when rate limited.
/// </summary>
public class RateLimitedException : PermittedException
{
    /// <summary>Seconds to wait before retrying.</summary>
    public int? RetryAfter { get; }

    /// <summary>Creates a new rate limited exception.</summary>
    public RateLimitedException(string message = "Too many requests", int? retryAfter = null)
        : base("RATE_LIMITED", message, 429)
    {
        RetryAfter = retryAfter;
    }
}

/// <summary>
/// Thrown when a file is not found.
/// </summary>
public class FileNotFoundException : PermittedException
{
    /// <summary>Creates a new file not found exception.</summary>
    public FileNotFoundException(string message = "File not found")
        : base("FILE_NOT_FOUND", message, 404) { }
}

/// <summary>
/// Thrown when access to a file is denied.
/// </summary>
public class FileAccessDeniedException : PermittedException
{
    /// <summary>Creates a new file access denied exception.</summary>
    public FileAccessDeniedException(string message = "Access to this file is denied")
        : base("FILE_ACCESS_DENIED", message, 403) { }
}

/// <summary>
/// Thrown when download rate limit is exceeded.
/// </summary>
public class DownloadRateLimitedException : PermittedException
{
    /// <summary>Creates a new download rate limited exception.</summary>
    public DownloadRateLimitedException(string message = "Download rate limit exceeded")
        : base("DOWNLOAD_RATE_LIMITED", message, 429) { }
}

/// <summary>
/// Thrown for network or connection errors.
/// </summary>
public class NetworkException : PermittedException
{
    /// <summary>Creates a new network exception.</summary>
    public NetworkException(string message, Exception innerException)
        : base("NETWORK_ERROR", message, innerException) { }
}
