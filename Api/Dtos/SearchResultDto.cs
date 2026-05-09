using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CinemaVault.Api.Dtos;

/// <summary>
/// Represents a paginated search result.
/// </summary>
public class SearchResultDto
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the total number of results.
    /// </summary>
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    /// <summary>
    /// Gets or sets the list of results.
    /// </summary>
    [JsonPropertyName("results")]
    public List<ContentItemDto> Results { get; set; } = new();
}

/// <summary>
/// Represents a combined search result from multiple sources.
/// </summary>
public class CombinedSearchResultDto
{
    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets results from the user's library.
    /// </summary>
    [JsonPropertyName("libraryResults")]
    public List<ContentItemDto> LibraryResults { get; set; } = new();

    /// <summary>
    /// Gets or sets results from Seerr/TMDB discovery.
    /// </summary>
    [JsonPropertyName("discoverResults")]
    public List<ContentItemDto> DiscoverResults { get; set; } = new();

    /// <summary>
    /// Gets or sets popular search suggestions.
    /// </summary>
    [JsonPropertyName="popularSearches"]
    public List<string> PopularSearches { get; set; } = new();

    /// <summary>
    /// Gets or sets recent searches from the user.
    /// </summary>
    [JsonPropertyName="recentSearches"]
    public List<string> RecentSearches { get; set; } = new();
}

/// <summary>
/// Represents a person search result (actor, director, etc.).
/// </summary>
public class PersonSearchResultDto
{
    /// <summary>
    /// Gets or sets the person ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the person's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile image path.
    /// </summary>
    [JsonPropertyName("profilePath")]
    public string? ProfilePath { get; set; }

    /// <summary>
    /// Gets or sets the known for department.
    /// </summary>
    [JsonPropertyName("knownForDepartment")]
    public string? KnownForDepartment { get; set; }

    /// <summary>
    /// Gets or sets the popularity score.
    /// </summary>
    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    /// <summary>
    /// Gets or sets the list of known for content.
    /// </summary>
    [JsonPropertyName("knownFor")]
    public List<ContentItemDto> KnownFor { get; set; } = new();
}

/// <summary>
/// Represents a genre search result.
/// </summary>
public class GenreSearchResultDto
{
    /// <summary>
    /// Gets or sets the genre ID.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the genre name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type (movie/tv).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sample content in this genre.
    /// </summary>
    [JsonPropertyName="sampleContent"]
    public List<ContentItemDto> SampleContent { get; set; } = new();
}
