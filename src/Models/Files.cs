using System.Text.Json.Serialization;

namespace Permitted.SDK.Models;

/// <summary>
/// Result of listing files.
/// </summary>
public sealed class FilesResult
{
    /// <summary>List of available files.</summary>
    [JsonPropertyName("files")]
    public required IReadOnlyList<FileInfo> Files { get; init; }
}

/// <summary>
/// Information about a downloadable file.
/// </summary>
public sealed class FileInfo
{
    /// <summary>Unique file identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Display name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Original filename.</summary>
    [JsonPropertyName("file_name")]
    public required string FileName { get; init; }

    /// <summary>File size in bytes.</summary>
    [JsonPropertyName("size")]
    public required long Size { get; init; }

    /// <summary>Human-readable file size.</summary>
    [JsonPropertyName("size_formatted")]
    public required string SizeFormatted { get; init; }

    /// <summary>When the file was uploaded.</summary>
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Result of requesting a file download.
/// </summary>
public sealed class DownloadResult
{
    /// <summary>Signed download URL. Expires in 15 minutes.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>When the URL expires.</summary>
    [JsonPropertyName("expires_at")]
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Unix timestamp when the URL expires.</summary>
    [JsonPropertyName("expires_at_unix")]
    public required long ExpiresAtUnix { get; init; }
}
