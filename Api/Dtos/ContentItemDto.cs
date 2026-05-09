using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CinemaVault.Api.Dtos;

/// <summary>
/// Represents a content item (movie or TV show).
/// </summary>
public class ContentItemDto
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
    /// Gets or sets the original title.
    /// </summary>
    [JsonPropertyName("originalTitle")]
    public string? OriginalTitle { get; set; }

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
    /// Gets or sets the release date.
    /// </summary>
    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the year.
    /// </summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    /// <summary>
    /// Gets or sets the runtime in minutes.
    /// </summary>
    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    /// <summary>
    /// Gets or sets the vote average (0-10).
    /// </summary>
    [JsonPropertyName("voteAverage")]
    public double VoteAverage { get; set; }

    /// <summary>
    /// Gets or sets the vote count.
    /// </summary>
    [JsonPropertyName("voteCount")]
    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the popularity score.
    /// </summary>
    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    /// <summary>
    /// Gets or sets the poster path.
    /// </summary>
    [JsonPropertyName("posterPath")]
    public string? PosterPath { get; set; }

    /// <summary>
    /// Gets or sets the backdrop path.
    /// </summary>
    [JsonPropertyName("backdropPath")]
    public string? BackdropPath { get; set; }

    /// <summary>
    /// Gets or sets the logo path.
    /// </summary>
    [JsonPropertyName("logoPath")]
    public string? LogoPath { get; set; }

    /// <summary>
    /// Gets or sets the genres.
    /// </summary>
    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    /// <summary>
    /// Gets or sets the status in library.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "unknown"; // unknown, available, requested, downloading

    /// <summary>
    /// Gets or sets the download progress percentage.
    /// </summary>
    [JsonPropertyName("downloadProgress")]
    public double DownloadProgress { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin item ID.
    /// </summary>
    [JsonPropertyName("jellyfinId")]
    public string? JellyfinId { get; set; }

    /// <summary>
    /// Gets or sets the quality information.
    /// </summary>
    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    /// <summary>
    /// Gets or sets the media info (resolution, codec, etc.).
    /// </summary>
    [JsonPropertyName("mediaInfo")]
    public string? MediaInfo { get; set; }

    /// <summary>
    /// Gets or sets whether this is 4K content.
    /// </summary>
    [JsonPropertyName("is4K")]
    public bool Is4K { get; set; }

    /// <summary>
    /// Gets or sets whether this has HDR.
    /// </summary>
    [JsonPropertyName("hasHDR")]
    public bool HasHDR { get; set; }

    /// <summary>
    /// Gets or sets whether this has Dolby Vision.
    /// </summary>
    [JsonPropertyName("hasDolbyVision")]
    public bool HasDolbyVision { get; set; }

    /// <summary>
    /// Gets or sets the first air date (for TV shows).
    /// </summary>
    [JsonPropertyName("firstAirDate")]
    public string? FirstAirDate { get; set; }

    /// <summary>
    /// Gets or sets the number of seasons (for TV shows).
    /// </summary>
    [JsonPropertyName("numberOfSeasons")]
    public int? NumberOfSeasons { get; set; }

    /// <summary>
    /// Gets or sets the number of episodes (for TV shows).
    /// </summary>
    [JsonPropertyName("numberOfEpisodes")]
    public int? NumberOfEpisodes { get; set; }

    /// <summary>
    /// Gets or sets the last episode info (for continue watching).
    /// </summary>
    [JsonPropertyName("lastEpisode")]
    public LastEpisodeDto? LastEpisode { get; set; }
}

/// <summary>
/// Represents the last watched episode for TV shows.
/// </summary>
public class LastEpisodeDto
{
    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    [JsonPropertyName("season")]
    public int Season { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    [JsonPropertyName("episode")]
    public int Episode { get; set; }

    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the watched percentage.
    /// </summary>
    [JsonPropertyName="watchedPercentage"]
    public double WatchedPercentage { get; set; }
}
