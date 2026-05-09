using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.CinemaVault.Api.Dtos;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaVault.Services;

/// <summary>
/// Service for syncing with Jellyfin library.
/// </summary>
public class JellyfinSyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JellyfinSyncService> _logger;
    private readonly IJsonSerializer _jsonSerializer;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public JellyfinSyncService(IHttpClientFactory httpClientFactory, ILogger<JellyfinSyncService> logger, IJsonSerializer jsonSerializer)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jsonSerializer = jsonSerializer;
    }

    /// <summary>
    /// Gets recently added items from Jellyfin.
    /// </summary>
    public async Task<List<ContentItemDto>> GetRecentlyAddedAsync(int limit = 20, string? userId = null)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = "/Users/{userId}/Items/Latest";
            if (!string.IsNullOrEmpty(userId))
            {
                endpoint = endpoint.Replace("{userId}", userId);
            }
            else
            {
                // Get current user if not specified
                var userResponse = await client.GetAsync("/Users/Me").ConfigureAwait(false);
                if (userResponse.IsSuccessStatusCode)
                {
                    var userContent = await userResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var userData = JsonSerializer.Deserialize<JsonElement>(userContent, _jsonOptions);
                    var currentUserId = userData.GetProperty("Id").GetString();
                    endpoint = endpoint.Replace("{userId}", currentUserId ?? string.Empty);
                }
            }

            endpoint += $"?Limit={limit}&IncludeItemTypes=Movie,Episode&Recursive=true&SortBy=DateCreated&SortOrder=Descending";
            
            var response = await client.GetAsync(endpoint).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<ContentItemDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var items = JsonSerializer.Deserialize<List<JsonElement>>(content, _jsonOptions);
            
            var result = new List<ContentItemDto>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    var contentItem = await MapJellyfinItemToContentItemAsync(item, client).ConfigureAwait(false);
                    if (contentItem != null)
                    {
                        result.Add(contentItem);
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently added items from Jellyfin");
            return new List<ContentItemDto>();
        }
    }

    /// <summary>
    /// Gets continue watching items for a user.
    /// </summary>
    public async Task<List<ContentItemDto>> GetContinueWatchingAsync(string userId)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = $"/Users/{userId}/Items/Resume?IncludeItemTypes=Movie,Episode&Recursive=true&Limit=20";
            
            var response = await client.GetAsync(endpoint).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<ContentItemDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var items = JsonSerializer.Deserialize<List<JsonElement>>(content, _jsonOptions);
            
            var result = new List<ContentItemDto>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    var contentItem = await MapJellyfinItemToContentItemAsync(item, client).ConfigureAwait(false);
                    if (contentItem != null)
                    {
                        // Set last episode info for TV shows
                        if (item.TryGetProperty("Type", out var type) && type.GetString() == "Episode")
                        {
                            if (item.TryGetProperty("UserData", out var userData) && 
                                userData.TryGetProperty("PlaybackPositionTicks", out var position) &&
                                userData.TryGetProperty("RunTimeTicks", out var runtime))
                            {
                                var positionMs = position.GetInt64() / 10000;
                                var runtimeMs = runtime.GetInt64() / 10000;
                                var watchedPercentage = runtimeMs > 0 ? (double)positionMs / runtimeMs * 100 : 0;
                                
                                contentItem.LastEpisode = new LastEpisodeDto
                                {
                                    Season = item.TryGetProperty("ParentIndexNumber", out var season) ? season.GetInt32() : 0,
                                    Episode = item.TryGetProperty("IndexNumber", out var episode) ? episode.GetInt32() : 0,
                                    Title = item.TryGetProperty("Name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                                    WatchedPercentage = watchedPercentage
                                };
                            }
                        }
                        
                        result.Add(contentItem);
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting continue watching items for user {UserId}", userId);
            return new List<ContentItemDto>();
        }
    }

    /// <summary>
    /// Gets library status for multiple TMDB IDs.
    /// </summary>
    public async Task<Dictionary<int, string>> GetLibraryStatusAsync(IEnumerable<int> tmdbIds)
    {
        var statusDict = new Dictionary<int, string>();
        
        try
        {
            var client = CreateHttpClient();
            var tmdbIdList = tmdbIds.ToList();
            
            // Get all movies
            var moviesResponse = await client.GetAsync("/Items?IncludeItemTypes=Movie&Recursive=true&Fields=ProviderIds").ConfigureAwait(false);
            if (moviesResponse.IsSuccessStatusCode)
            {
                var moviesContent = await moviesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var moviesData = JsonSerializer.Deserialize<JsonElement>(moviesContent, _jsonOptions);
                
                if (moviesData.TryGetProperty("Items", out var movieItems))
                {
                    foreach (var movie in movieItems.EnumerateArray())
                    {
                        if (movie.TryGetProperty("ProviderIds", out var providerIds) &&
                            providerIds.TryGetProperty("Tmdb", out var tmdbId))
                        {
                            var tmdbIdValue = tmdbId.GetInt32();
                            if (tmdbIdList.Contains(tmdbIdValue))
                            {
                                statusDict[tmdbIdValue] = "available";
                            }
                        }
                    }
                }
            }

            // Get all TV series
            var seriesResponse = await client.GetAsync("/Items?IncludeItemTypes=Series&Recursive=true&Fields=ProviderIds").ConfigureAwait(false);
            if (seriesResponse.IsSuccessStatusCode)
            {
                var seriesContent = await seriesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var seriesData = JsonSerializer.Deserialize<JsonElement>(seriesContent, _jsonOptions);
                
                if (seriesData.TryGetProperty("Items", out var seriesItems))
                {
                    foreach (var series in seriesItems.EnumerateArray())
                    {
                        if (series.TryGetProperty("ProviderIds", out var providerIds) &&
                            providerIds.TryGetProperty("Tmdb", out var tmdbId))
                        {
                            var tmdbIdValue = tmdbId.GetInt32();
                            if (tmdbIdList.Contains(tmdbIdValue))
                            {
                                statusDict[tmdbIdValue] = "available";
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting library status");
        }

        return statusDict;
    }

    /// <summary>
    /// Gets item details by Jellyfin ID.
    /// </summary>
    public async Task<ContentItemDto?> GetItemDetailsAsync(string itemId, string? userId = null)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = $"/Items/{itemId}";
            if (!string.IsNullOrEmpty(userId))
            {
                endpoint += $"?userId={userId}";
            }
            
            var response = await client.GetAsync(endpoint).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var item = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            return await MapJellyfinItemToContentItemAsync(item, client).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item details for {ItemId}", itemId);
            return null;
        }
    }

    /// <summary>
    /// Gets similar items.
    /// </summary>
    public async Task<List<ContentItemDto>> GetSimilarItemsAsync(string itemId, string? userId = null, int limit = 10)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = $"/Items/{itemId}/Similar?Limit={limit}";
            if (!string.IsNullOrEmpty(userId))
            {
                endpoint += $"?userId={userId}";
            }
            
            var response = await client.GetAsync(endpoint).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<ContentItemDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var items = JsonSerializer.Deserialize<List<JsonElement>>(content, _jsonOptions);
            
            var result = new List<ContentItemDto>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    var contentItem = await MapJellyfinItemToContentItemAsync(item, client).ConfigureAwait(false);
                    if (contentItem != null)
                    {
                        result.Add(contentItem);
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar items for {ItemId}", itemId);
            return new List<ContentItemDto>();
        }
    }

    /// <summary>
    /// Gets library statistics.
    /// </summary>
    public async Task<LibraryStatsDto> GetLibraryStatsAsync()
    {
        var stats = new LibraryStatsDto();
        
        try
        {
            var client = CreateHttpClient();
            
            // Get movie count
            var moviesResponse = await client.GetAsync("/Items/Counts?IncludeItemTypes=Movie").ConfigureAwait(false);
            if (moviesResponse.IsSuccessStatusCode)
            {
                var moviesContent = await moviesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var moviesData = JsonSerializer.Deserialize<JsonElement>(moviesContent, _jsonOptions);
                stats.TotalMovies = moviesData.TryGetProperty("MovieCount", out var movieCount) ? movieCount.GetInt32() : 0;
            }

            // Get series count
            var seriesResponse = await client.GetAsync("/Items/Counts?IncludeItemTypes=Series").ConfigureAwait(false);
            if (seriesResponse.IsSuccessStatusCode)
            {
                var seriesContent = await seriesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var seriesData = JsonSerializer.Deserialize<JsonElement>(seriesContent, _jsonOptions);
                stats.TotalShows = seriesData.TryGetProperty("SeriesCount", out var seriesCount) ? seriesCount.GetInt32() : 0;
            }

            // Get episode count
            var episodesResponse = await client.GetAsync("/Items/Counts?IncludeItemTypes=Episode").ConfigureAwait(false);
            if (episodesResponse.IsSuccessStatusCode)
            {
                var episodesContent = await episodesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var episodesData = JsonSerializer.Deserialize<JsonElement>(episodesContent, _jsonOptions);
                stats.TotalEpisodes = episodesData.TryGetProperty("EpisodeCount", out var episodeCount) ? episodeCount.GetInt32() : 0;
            }

            // Get recently added
            stats.RecentlyAdded = await GetRecentlyAddedAsync(5).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting library statistics");
        }

        return stats;
    }

    /// <summary>
    /// Searches the Jellyfin library.
    /// </summary>
    public async Task<List<ContentItemDto>> SearchLibraryAsync(string query, string? userId = null, int limit = 20)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = $"/Search/Hints?SearchTerm={Uri.EscapeDataString(query)}&Limit={limit}&IncludeItemTypes=Movie,Series";
            if (!string.IsNullOrEmpty(userId))
            {
                endpoint += $"&UserId={userId}";
            }
            
            var response = await client.GetAsync(endpoint).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<ContentItemDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var hints = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            var result = new List<ContentItemDto>();
            if (hints.TryGetProperty("SearchHints", out var searchHints))
            {
                foreach (var hint in searchHints.EnumerateArray())
                {
                    var itemId = hint.GetProperty("ItemId").GetString();
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        var item = await GetItemDetailsAsync(itemId, userId).ConfigureAwait(false);
                        if (item != null)
                        {
                            result.Add(item);
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching library with query: {Query}", query);
            return new List<ContentItemDto>();
        }
    }

    /// <summary>
    /// Gets user information.
    /// </summary>
    public async Task<JellyfinUserDto?> GetCurrentUserAsync()
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/Users/Me").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var user = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            return new JellyfinUserDto
            {
                Id = user.GetProperty("Id").GetString() ?? string.Empty,
                Name = user.GetProperty("Name").GetString() ?? string.Empty,
                HasPassword = user.TryGetProperty("HasPassword", out var hasPassword) && hasPassword.GetBoolean(),
                LastLoginDate = user.TryGetProperty("LastLoginDate", out var lastLogin) ? lastLogin.GetDateTime() : null,
                LastActivityDate = user.TryGetProperty("LastActivityDate", out var lastActivity) ? lastActivity.GetDateTime() : null,
                Configuration = user.TryGetProperty("Configuration", out var config) ? 
                    new JellyfinUserConfigDto
                    {
                        AudioLanguagePreference = config.TryGetProperty("AudioLanguagePreference", out var audioLang) ? audioLang.GetString() : null,
                        PlayDefaultAudioTrack = config.TryGetProperty("PlayDefaultAudioTrack", out var defaultAudio) && defaultAudio.GetBoolean(),
                        SubtitleLanguagePreference = config.TryGetProperty("SubtitleLanguagePreference", out var subLang) ? subLang.GetString() : null,
                        DisplayMissingEpisodes = config.TryGetProperty("DisplayMissingEpisodes", out var missingEpisodes) && missingEpisodes.GetBoolean()
                    } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        // Use relative URLs since we're calling the same Jellyfin instance
        client.BaseAddress = null;
        client.DefaultRequestHeaders.Add("User-Agent", "CinemaVault/1.0.0");
        return client;
    }

    private async Task<ContentItemDto?> MapJellyfinItemToContentItemAsync(JsonElement item, HttpClient client)
    {
        try
        {
            var type = item.TryGetProperty("Type", out var typeProp) ? typeProp.GetString() : "Unknown";
            var isMovie = type == "Movie";
            var isSeries = type == "Series";
            var isEpisode = type == "Episode";

            var contentItem = new ContentItemDto
            {
                JellyfinId = item.GetProperty("Id").GetString() ?? string.Empty,
                Title = item.TryGetProperty("Name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                Overview = item.TryGetProperty("Overview", out var overview) ? overview.GetString() ?? string.Empty : string.Empty,
                Status = "available"
            };

            // Extract TMDB ID from ProviderIds
            if (item.TryGetProperty("ProviderIds", out var providerIds))
            {
                if (providerIds.TryGetProperty("Tmdb", out var tmdbId))
                {
                    contentItem.TmdbId = tmdbId.GetInt32();
                }
            }

            // Set type
            if (isMovie)
            {
                contentItem.Type = "movie";
            }
            else if (isSeries || isEpisode)
            {
                contentItem.Type = "tv";
            }

            // Set dates and year
            if (item.TryGetProperty("PremiereDate", out var premiereDate) || 
                item.TryGetProperty("CreationDate", out var creationDate))
            {
                var dateStr = premiereDate.GetString() ?? creationDate.GetString();
                if (DateTime.TryParse(dateStr, out var date))
                {
                    contentItem.ReleaseDate = date.ToString("yyyy-MM-dd");
                    contentItem.Year = date.Year;
                }
            }

            // Set runtime for movies
            if (isMovie && item.TryGetProperty("RunTimeTicks", out var runtimeTicks))
            {
                var runtimeMs = runtimeTicks.GetInt64() / 10000;
                contentItem.Runtime = (int)(runtimeMs / 60000); // Convert to minutes
            }

            // Set ratings
            if (item.TryGetProperty("CommunityRating", out var rating))
            {
                contentItem.VoteAverage = rating.GetDouble();
            }

            // Set image paths
            if (item.TryGetProperty("ImageTags", out var imageTags))
            {
                if (imageTags.TryGetProperty("Primary", out var primaryTag))
                {
                    var tag = primaryTag.GetString();
                    if (!string.IsNullOrEmpty(tag))
                    {
                        contentItem.PosterPath = $"/Items/{contentItem.JellyfinId}/Images/Primary?tag={tag}";
                    }
                }

                if (imageTags.TryGetProperty("Backdrop", out var backdropTag))
                {
                    var tag = backdropTag.GetString();
                    if (!string.IsNullOrEmpty(tag))
                    {
                        contentItem.BackdropPath = $"/Items/{contentItem.JellyfinId}/Images/Backdrop?tag={tag}";
                    }
                }
            }

            // Set genres
            if (item.TryGetProperty("Genres", out var genres))
            {
                foreach (var genre in genres.EnumerateArray())
                {
                    contentItem.Genres.Add(genre.GetString() ?? string.Empty);
                }
            }

            // For TV series, get season/episode counts
            if (isSeries)
            {
                // Get series info with season counts
                var seriesResponse = await client.GetAsync($"/Items/{contentItem.JellyfinId}?Fields=Seasons").ConfigureAwait(false);
                if (seriesResponse.IsSuccessStatusCode)
                {
                    var seriesContent = await seriesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var seriesData = JsonSerializer.Deserialize<JsonElement>(seriesContent, _jsonOptions);
                    
                    if (seriesData.TryGetProperty("Seasons", out var seasons))
                    {
                        contentItem.NumberOfSeasons = 0;
                        contentItem.NumberOfEpisodes = 0;
                        
                        foreach (var season in seasons.EnumerateArray())
                        {
                            contentItem.NumberOfSeasons++;
                            if (season.TryGetProperty("Statistics", out var stats) &&
                                stats.TryGetProperty("EpisodeCount", out var episodeCount))
                            {
                                contentItem.NumberOfEpisodes += episodeCount.GetInt32();
                            }
                        }
                    }
                }
            }

            // Set media info and quality
            if (item.TryGetProperty("MediaStreams", out var mediaStreams))
            {
                var videoStreams = new List<JsonElement>();
                var audioStreams = new List<JsonElement>();
                
                foreach (var stream in mediaStreams.EnumerateArray())
                {
                    if (stream.TryGetProperty("Type", out var streamType))
                    {
                        var type = streamType.GetString();
                        if (type == "Video")
                        {
                            videoStreams.Add(stream);
                        }
                        else if (type == "Audio")
                        {
                            audioStreams.Add(stream);
                        }
                    }
                }

                // Set quality info from video stream
                if (videoStreams.Any())
                {
                    var videoStream = videoStreams.First();
                    if (videoStream.TryGetProperty("Width", out var width) && 
                        videoStream.TryGetProperty("Height", out var height))
                    {
                        var widthVal = width.GetInt32();
                        var heightVal = height.GetInt32();
                        
                        if (widthVal >= 3840)
                        {
                            contentItem.Is4K = true;
                            contentItem.Quality = "4K";
                        }
                        else if (widthVal >= 1920)
                        {
                            contentItem.Quality = "1080p";
                        }
                        else if (widthVal >= 1280)
                        {
                            contentItem.Quality = "720p";
                        }
                        else
                        {
                            contentItem.Quality = "SD";
                        }

                        // Check for HDR
                        if (videoStream.TryGetProperty("VideoRange", out var videoRange))
                        {
                            var range = videoRange.GetString();
                            if (range == "HDR" || range == "HDR10")
                            {
                                contentItem.HasHDR = true;
                            }
                        }

                        // Check for Dolby Vision
                        if (videoStream.TryGetProperty("DvVersionMajor", out var dvVersion))
                        {
                            contentItem.HasDolbyVision = true;
                        }
                    }
                }

                // Set media info
                var mediaInfo = new List<string>();
                if (!string.IsNullOrEmpty(contentItem.Quality))
                {
                    mediaInfo.Add(contentItem.Quality);
                }
                if (contentItem.HasHDR)
                {
                    mediaInfo.Add("HDR");
                }
                if (contentItem.HasDolbyVision)
                {
                    mediaInfo.Add("Dolby Vision");
                }
                
                contentItem.MediaInfo = string.Join(" • ", mediaInfo);
            }

            return contentItem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping Jellyfin item to content item");
            return null;
        }
    }
}

/// <summary>
/// Jellyfin user DTO.
/// </summary>
public class JellyfinUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool HasPassword { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public JellyfinUserConfigDto? Configuration { get; set; }
}

/// <summary>
/// Jellyfin user configuration DTO.
/// </summary>
public class JellyfinUserConfigDto
{
    public string? AudioLanguagePreference { get; set; }
    public bool PlayDefaultAudioTrack { get; set; }
    public string? SubtitleLanguagePreference { get; set; }
    public bool DisplayMissingEpisodes { get; set; }
}
