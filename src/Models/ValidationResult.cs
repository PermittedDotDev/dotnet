using System.Text.Json.Serialization;

namespace Permitted.SDK.Models;

/// <summary>
/// Result of a license validation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>Authentication token for subsequent API calls.</summary>
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    /// <summary>When the token expires.</summary>
    [JsonPropertyName("expires_at")]
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Unix timestamp when the token expires.</summary>
    [JsonPropertyName("expires_at_unix")]
    public required long ExpiresAtUnix { get; init; }

    /// <summary>The validated license information.</summary>
    [JsonPropertyName("license")]
    public required ValidationLicense License { get; init; }
}

/// <summary>
/// License information returned from validation.
/// </summary>
public sealed class ValidationLicense
{
    /// <summary>License identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>License status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>When the license was created.</summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the license expires. Null for lifetime.</summary>
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Whether this is a lifetime license.</summary>
    [JsonPropertyName("is_lifetime")]
    public required bool IsLifetime { get; init; }

    /// <summary>Tier information.</summary>
    [JsonPropertyName("tier")]
    public LicenseTier? Tier { get; init; }

    /// <summary>Product information.</summary>
    [JsonPropertyName("product")]
    public required LicenseProduct Product { get; init; }
}
