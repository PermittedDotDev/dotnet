using System.Text.Json.Serialization;

namespace Permitted.SDK.Models;

/// <summary>
/// API status check result.
/// </summary>
public sealed class StatusResult
{
    /// <summary>API status. "operational" when available.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Current server time.</summary>
    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Unix timestamp.</summary>
    [JsonPropertyName("timestamp_unix")]
    public required long TimestampUnix { get; init; }

    /// <summary>Product API status, if requested.</summary>
    [JsonPropertyName("product")]
    public ProductStatus? Product { get; init; }

    /// <summary>Whether the API is operational.</summary>
    public bool IsOperational => Status == "operational";
}

/// <summary>
/// Product-specific API status.
/// </summary>
public sealed class ProductStatus
{
    /// <summary>Product identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Whether API access is enabled for this product.</summary>
    [JsonPropertyName("api_enabled")]
    public required bool ApiEnabled { get; init; }
}
