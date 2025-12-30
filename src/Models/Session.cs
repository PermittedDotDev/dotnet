using System.Text.Json.Serialization;

namespace Permitted.SDK.Models;

/// <summary>
/// Result of a session refresh.
/// </summary>
public sealed class SessionRefreshResult
{
    /// <summary>New authentication token.</summary>
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    /// <summary>When the new token expires.</summary>
    [JsonPropertyName("expires_at")]
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Unix timestamp when the token expires.</summary>
    [JsonPropertyName("expires_at_unix")]
    public required long ExpiresAtUnix { get; init; }
}

/// <summary>
/// Result of a ping request.
/// </summary>
public sealed class PingResult
{
    /// <summary>Session status. Always "valid" on success.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>When the token expires.</summary>
    [JsonPropertyName("expires_at")]
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Unix timestamp when the token expires.</summary>
    [JsonPropertyName("expires_at_unix")]
    public required long ExpiresAtUnix { get; init; }
}
