using System.Text.Json.Serialization;

namespace Permitted.SDK.Models;

/// <summary>
/// License status values.
/// </summary>
public enum LicenseStatus
{
    /// <summary>License is active and valid.</summary>
    Active,
    /// <summary>License has expired.</summary>
    Expired,
    /// <summary>License has been suspended by the seller.</summary>
    Suspended,
    /// <summary>License has been permanently revoked.</summary>
    Revoked
}

/// <summary>
/// Represents a validated license.
/// </summary>
public sealed class License
{
    /// <summary>Unique license identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Current license status.</summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required LicenseStatus Status { get; init; }

    /// <summary>When the license was created.</summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the license expires. Null for lifetime licenses.</summary>
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Whether this is a lifetime license.</summary>
    [JsonPropertyName("is_lifetime")]
    public required bool IsLifetime { get; init; }

    /// <summary>Seconds remaining until expiration. Null for lifetime licenses.</summary>
    [JsonPropertyName("time_remaining_seconds")]
    public int? TimeRemainingSeconds { get; init; }

    /// <summary>Associated tier, if any.</summary>
    [JsonPropertyName("tier")]
    public LicenseTier? Tier { get; init; }

    /// <summary>Associated product.</summary>
    [JsonPropertyName("product")]
    public required LicenseProduct Product { get; init; }

    /// <summary>Associated customer, if any.</summary>
    [JsonPropertyName("customer")]
    public LicenseCustomer? Customer { get; init; }

    /// <summary>Custom metadata attached to the license.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// License tier information.
/// </summary>
public sealed class LicenseTier
{
    /// <summary>Tier identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Tier display name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Tier description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

/// <summary>
/// License product information.
/// </summary>
public sealed class LicenseProduct
{
    /// <summary>Product identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Product name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Product description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

/// <summary>
/// License customer information.
/// </summary>
public sealed class LicenseCustomer
{
    /// <summary>Customer identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Customer email.</summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>Customer name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
