using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CinemaVault.Api.Dtos;

/// <summary>
/// Represents a media request.
/// </summary>
public class RequestDto
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the media type (movie/tv).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the poster path.
    /// </summary>
    [JsonPropertyName("posterPath")]
    public string? PosterPath { get; set; }

    /// <summary>
    /// Gets or sets the requesting user ID.
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the requesting user name.
    /// </summary>
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending"; // pending, approved, processing, available, declined

    /// <summary>
    /// Gets or sets the request date.
    /// </summary>
    [JsonPropertyName("requestDate")]
    public DateTime RequestDate { get; set; }

    /// <summary>
    /// Gets or sets the last modified date.
    /// </summary>
    [JsonPropertyName("modifiedDate")]
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Gets or sets whether this is a 4K request.
    /// </summary>
    [JsonPropertyName("is4K")]
    public bool Is4K { get; set; }

    /// <summary>
    /// Gets or sets the requested seasons (for TV shows).
    /// </summary>
    [JsonPropertyName("seasons")]
    public List<int> Seasons { get; set; } = new();

    /// <summary>
    /// Gets or sets the root folder for the request.
    /// </summary>
    [JsonPropertyName("rootFolder")]
    public string? RootFolder { get; set; }

    /// <summary>
    /// Gets or sets the quality profile ID.
    /// </summary>
    [JsonPropertyName("qualityProfileId")]
    public int QualityProfileId { get; set; }

    /// <summary>
    /// Gets or sets the download progress percentage.
    /// </summary>
    [JsonPropertyName("downloadProgress")]
    public double DownloadProgress { get; set; }

    /// <summary>
    /// Gets or sets the estimated completion date.
    /// </summary>
    [JsonPropertyName("estimatedCompletion")]
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin item ID when available.
    /// </summary>
    [JsonPropertyName("jellyfinId")]
    public string? JellyfinId { get; set; }

    /// <summary>
    /// Gets or sets the approval user ID.
    /// </summary>
    [JsonPropertyName("approvedBy")]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Gets or sets the approval date.
    /// </summary>
    [JsonPropertyName("approvedDate")]
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Gets or sets the decline reason.
    /// </summary>
    [JsonPropertyName("declineReason")]
    public string? DeclineReason { get; set; }

    /// <summary>
    /// Gets or sets the external request ID (from Seerr).
    /// </summary>
    [JsonPropertyName("externalId")]
    public int? ExternalId { get; set; }

    /// <summary>
    /// Gets or sets the media details.
    /// </summary>
    [JsonPropertyName("media")]
    public ContentItemDto? Media { get; set; }
}

/// <summary>
/// Represents a request creation payload.
/// </summary>
public class CreateRequestDto
{
    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the media type (movie/tv).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is a 4K request.
    /// </summary>
    [JsonPropertyName("is4K")]
    public bool Is4K { get; set; }

    /// <summary>
    /// Gets or sets the requested seasons (for TV shows).
    /// </summary>
    [JsonPropertyName("seasons")]
    public List<int>? Seasons { get; set; }

    /// <summary>
    /// Gets or sets the root folder override.
    /// </summary>
    [JsonPropertyName("rootFolder")]
    public string? RootFolder { get; set; }

    /// <summary>
    /// Gets or sets the quality profile ID override.
    /// </summary>
    [JsonPropertyName("qualityProfileId")]
    public int? QualityProfileId { get; set; }
}

/// <summary>
/// Represents a request update payload.
/// </summary>
public class UpdateRequestDto
{
    /// <summary>
    /// Gets or sets the new status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the decline reason (if declining).
    /// </summary>
    [JsonPropertyName("declineReason")]
    public string? DeclineReason { get; set; }

    /// <summary>
    /// Gets or sets the download progress.
    /// </summary>
    [JsonPropertyName("downloadProgress")]
    public double? DownloadProgress { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin item ID.
    /// </summary>
    [JsonPropertyName("jellyfinId")]
    public string? JellyfinId { get; set; }
}
