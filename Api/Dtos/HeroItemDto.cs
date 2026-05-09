using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CinemaVault.Api.Dtos;

/// <summary>
/// Represents a hero banner item.
/// </summary>
public class HeroItemDto
{
    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the content type (movie/tv).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tagline.
    /// </summary>
    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    /// <summary>
    /// Gets or sets the overview/synopsis.
    /// </summary>
    [JsonPropertyName("overview")]
    public string Overview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the backdrop path (full size).
    /// </summary>
    [JsonPropertyName("backdropPath")]
    public string BackdropPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the poster path.
    /// </summary>
    [JsonPropertyName("posterPath")]
    public string? PosterPath { get; set; }

    /// <summary>
    /// Gets or sets the logo path.
    /// </summary>
    [JsonPropertyName("logoPath")]
    public string? LogoPath { get; set; }

    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the runtime in minutes.
    /// </summary>
    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    /// <summary>
    /// Gets or sets the vote average.
    /// </summary>
    [JsonPropertyName("voteAverage")]
    public double VoteAverage { get; set; }

    /// <summary>
    /// Gets or sets the vote count.
    /// </summary>
    [JsonPropertyName("voteCount")]
    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    /// <summary>
    /// Gets or sets the quality badges.
    /// </summary>
    [JsonPropertyName("qualityBadges")]
    public List<string> QualityBadges { get; set; } = new();

    /// <summary>
    /// Gets or sets the status in library.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "unknown";

    /// <summary>
    /// Gets or sets the primary action button text.
    /// </summary>
    [JsonPropertyName("primaryAction")]
    public string PrimaryAction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary action type (play/request).
    /// </summary>
    [JsonPropertyName("primaryActionType")]
    public string PrimaryActionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Jellyfin item ID if available.
    /// </summary>
    [JsonPropertyName("jellyfinId")]
    public string? JellyfinId { get; set; }

    /// <summary>
    /// Gets or sets the trailer URL.
    /// </summary>
    [JsonPropertyName("trailerUrl")]
    public string? TrailerUrl { get; set; }

    /// <summary>
    /// Gets or sets the priority for hero rotation (lower = higher priority).
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether this item is featured.
    /// </summary>
    [JsonPropertyName("isFeatured")]
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Gets or sets the last episode info (for TV shows).
    /// </summary>
    [JsonPropertyName("lastEpisode")]
    public LastEpisodeDto? LastEpisode { get; set; }

    /// <summary>
    /// Gets or sets the release date string.
    /// </summary>
    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the content rating.
    /// </summary>
    [JsonPropertyName("contentRating")]
    public string? ContentRating { get; set; }

    /// <summary>
    /// Gets or sets the country of origin.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// Gets or sets the spoken languages.
    /// </summary>
    [JsonPropertyName("languages")]
    public List<string> Languages { get; set; } = new();
}

/// <summary>
/// Represents a hero banner configuration.
/// </summary>
public class HeroConfigDto
{
    /// <summary>
    /// Gets or sets whether the hero banner is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether auto-rotation is enabled.
    /// </summary>
    [JsonPropertyName("autoRotate")]
    public bool AutoRotate { get; set; } = true;

    /// <summary>
    /// Gets or sets the rotation interval in seconds.
    /// </summary>
    [JsonPropertyName("rotationInterval")]
    public int RotationInterval { get; set; } = 8;

    /// <summary>
    /// Gets or sets the maximum number of items.
    /// </summary>
    [JsonPropertyName("maxItems")]
    public int MaxItems { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether to include continue watching.
    /// </summary>
    [JsonPropertyName("includeContinueWatching")]
    public bool IncludeContinueWatching { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include trending.
    /// </summary>
    [JsonPropertyName("includeTrending")]
    public bool IncludeTrending { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include new releases.
    /// </summary>
    [JsonPropertyName("includeNewReleases")]
    public bool IncludeNewReleases { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include top rated.
    /// </summary>
    [JsonPropertyName("includeTopRated")]
    public bool IncludeTopRated { get; set; } = false;
}
