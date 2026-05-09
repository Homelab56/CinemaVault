using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.CinemaVault.Api.Dtos;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaVault.Services;

/// <summary>
/// Service for interacting with Seerr/Jellyseerr API.
/// </summary>
public class SeerrService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SeerrService> _logger;
    private readonly PluginConfiguration _config;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SeerrService(IHttpClientFactory httpClientFactory, ILogger<SeerrService> logger, PluginConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Tests connection to Seerr.
    /// </summary>
    public async Task<ConnectionStatusDto> TestConnectionAsync()
    {
        var status = new ConnectionStatusDto
        {
            Service = "Seerr",
            Connected = false,
            Message = "Connection failed"
        };

        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v1/status").ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var statusData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
                
                status.Connected = true;
                status.Message = "Connected successfully";
                status.Version = statusData.GetProperty("version").GetString();
                status.ResponseTime = response.Headers.Date != null 
                    ? (long)(DateTime.UtcNow - response.Headers.Date.Value).TotalMilliseconds 
                    : 0;

                // Get additional stats
                var usersResponse = await client.GetAsync("/api/v1/user/count").ConfigureAwait(false);
                if (usersResponse.IsSuccessStatusCode)
                {
                    var usersData = JsonSerializer.Deserialize<JsonElement>(await usersResponse.Content.ReadAsStringAsync().ConfigureAwait(false), _jsonOptions);
                    status.Data = new { userCount = usersData.GetProperty("count").GetInt32() };
                }
            }
            else
            {
                status.Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Seerr connection");
            status.Message = ex.Message;
        }

        return status;
    }

    /// <summary>
    /// Gets trending content.
    /// </summary>
    public async Task<SearchResultDto> GetTrendingAsync(string type = "movie", int page = 1)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = type.ToLowerInvariant() == "movie" ? "/api/v1/discover/movies" : "/api/v1/discover/tv";
            var response = await client.GetAsync($"{endpoint}?sortBy=trending&page={page}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new SearchResultDto();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<SeerrResponseDto>(content, _jsonOptions);
            
            return MapToSearchResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending content for {Type}", type);
            return new SearchResultDto();
        }
    }

    /// <summary>
    /// Gets popular content.
    /// </summary>
    public async Task<SearchResultDto> GetPopularAsync(string type = "movie", int page = 1)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = type.ToLowerInvariant() == "movie" ? "/api/v1/discover/movies" : "/api/v1/discover/tv";
            var response = await client.GetAsync($"{endpoint}?sortBy=popularity&page={page}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new SearchResultDto();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<SeerrResponseDto>(content, _jsonOptions);
            
            return MapToSearchResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular content for {Type}", type);
            return new SearchResultDto();
        }
    }

    /// <summary>
    /// Gets top rated content.
    /// </summary>
    public async Task<SearchResultDto> GetTopRatedAsync(string type = "movie", int page = 1)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = type.ToLowerInvariant() == "movie" ? "/api/v1/discover/movies" : "/api/v1/discover/tv";
            var response = await client.GetAsync($"{endpoint}?sortBy=ratings&page={page}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new SearchResultDto();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<SeerrResponseDto>(content, _jsonOptions);
            
            return MapToSearchResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top rated content for {Type}", type);
            return new SearchResultDto();
        }
    }

    /// <summary>
    /// Gets now playing movies.
    /// </summary>
    public async Task<SearchResultDto> GetNowPlayingAsync(int page = 1)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"/api/v1/discover/movies?sortBy=nowPlaying&page={page}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new SearchResultDto();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<SeerrResponseDto>(content, _jsonOptions);
            
            return MapToSearchResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting now playing movies");
            return new SearchResultDto();
        }
    }

    /// <summary>
    /// Gets content by genre.
    /// </summary>
    public async Task<SearchResultDto> GetByGenreAsync(int genreId, string type = "movie", int page = 1)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = type.ToLowerInvariant() == "movie" ? "/api/v1/discover/movies" : "/api/v1/discover/tv";
            var response = await client.GetAsync($"{endpoint}?genreId={genreId}&page={page}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new SearchResultDto();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<SeerrResponseDto>(content, _jsonOptions);
            
            return MapToSearchResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content by genre {GenreId} for {Type}", genreId, type);
            return new SearchResultDto();
        }
    }

    /// <summary>
    /// Searches for content.
    /// </summary>
    public async Task<SearchResultDto> SearchAsync(string query, int page = 1)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"/api/v1/search?query={Uri.EscapeDataString(query)}&page={page}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new SearchResultDto();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<SeerrResponseDto>(content, _jsonOptions);
            
            return MapToSearchResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for content with query: {Query}", query);
            return new SearchResultDto();
        }
    }

    /// <summary>
    /// Gets content details.
    /// </summary>
    public async Task<ContentItemDto?> GetDetailsAsync(int tmdbId, string type)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = type.ToLowerInvariant() == "movie" ? $"/api/v1/movie/{tmdbId}" : $"/api/v1/tv/{tmdbId}";
            var response = await client.GetAsync(endpoint).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            return MapToContentItem(data, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting details for TMDB ID {TmdbId} type {Type}", tmdbId, type);
            return null;
        }
    }

    /// <summary>
    /// Gets recommendations.
    /// </summary>
    public async Task<SearchResultDto> GetRecommendationsAsync(int tmdbId, string type, int page = 1)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = type.ToLowerInvariant() == "movie" ? $"/api/v1/movie/{tmdbId}/recommendations" : $"/api/v1/tv/{tmdbId}/recommendations";
            var response = await client.GetAsync($"{endpoint}?page={page}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new SearchResultDto();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<SeerrResponseDto>(content, _jsonOptions);
            
            return MapToSearchResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for TMDB ID {TmdbId} type {Type}", tmdbId, type);
            return new SearchResultDto();
        }
    }

    /// <summary>
    /// Gets videos/trailers.
    /// </summary>
    public async Task<List<VideoDto>> GetVideosAsync(int tmdbId, string type)
    {
        try
        {
            var client = CreateHttpClient();
            var endpoint = type.ToLowerInvariant() == "movie" ? $"/api/v1/movie/{tmdbId}/videos" : $"/api/v1/tv/{tmdbId}/videos";
            var response = await client.GetAsync(endpoint).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<VideoDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            var videos = new List<VideoDto>();
            if (data.TryGetProperty("results", out var results))
            {
                foreach (var video in results.EnumerateArray())
                {
                    videos.Add(new VideoDto
                    {
                        Id = video.GetProperty("id").GetString() ?? string.Empty,
                        Key = video.GetProperty("key").GetString() ?? string.Empty,
                        Name = video.GetProperty("name").GetString() ?? string.Empty,
                        Site = video.GetProperty("site").GetString() ?? string.Empty,
                        Type = video.GetProperty("type").GetString() ?? string.Empty,
                        Size = video.TryGetProperty("size", out var size) ? size.GetInt32() : 0
                    });
                }
            }

            return videos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting videos for TMDB ID {TmdbId} type {Type}", tmdbId, type);
            return new List<VideoDto>();
        }
    }

    /// <summary>
    /// Creates a request.
    /// </summary>
    public async Task<RequestDto?> CreateRequestAsync(CreateRequestDto request, string userId)
    {
        try
        {
            var client = CreateHttpClient();
            
            var payload = new
            {
                mediaType = request.Type,
                mediaId = request.TmdbId,
                is4k = request.Is4K,
                seasons = request.Seasons ?? new List<int>()
            };

            var response = await client.PostAsJsonAsync("/api/v1/request", payload).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            return MapToRequest(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating request for TMDB ID {TmdbId}", request.TmdbId);
            return null;
        }
    }

    /// <summary>
    /// Gets user requests.
    /// </summary>
    public async Task<List<RequestDto>> GetRequestsAsync(string? userId = null, string? status = null, int take = 20, int skip = 0)
    {
        try
        {
            var client = CreateHttpClient();
            var url = $"/api/v1/request?take={take}&skip={skip}";
            
            if (!string.IsNullOrEmpty(status))
                url += $"&filter={status}";
            
            if (!string.IsNullOrEmpty(userId))
                url += $"&userId={userId}";

            var response = await client.GetAsync(url).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<RequestDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            var requests = new List<RequestDto>();
            if (data.TryGetProperty("results", out var results))
            {
                foreach (var request in results.EnumerateArray())
                {
                    requests.Add(MapToRequest(request));
                }
            }

            return requests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting requests");
            return new List<RequestDto>();
        }
    }

    /// <summary>
    /// Deletes a request.
    /// </summary>
    public async Task<bool> DeleteRequestAsync(int requestId)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.DeleteAsync($"/api/v1/request/{requestId}").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting request {RequestId}", requestId);
            return false;
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_config.SeerrUrl);
        client.DefaultRequestHeaders.Add("X-Api-Key", _config.SeerrApiKey);
        client.DefaultRequestHeaders.Add("User-Agent", "CinemaVault/1.0.0");
        return client;
    }

    private SearchResultDto MapToSearchResult(SeerrResponseDto? data)
    {
        if (data == null) return new SearchResultDto();

        return new SearchResultDto
        {
            Page = data.Page,
            TotalPages = data.TotalPages,
            TotalResults = data.TotalResults,
            Results = data.Results?.Select(MapToContentItem).ToList() ?? new List<ContentItemDto>()
        };
    }

    private ContentItemDto MapToContentItem(JsonElement data, string type)
    {
        var item = new ContentItemDto
        {
            TmdbId = data.GetProperty("id").GetInt32(),
            Type = type,
            Title = data.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : 
                    data.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
            Overview = data.TryGetProperty("overview", out var overview) ? overview.GetString() ?? string.Empty : string.Empty,
            ReleaseDate = data.TryGetProperty("release_date", out var releaseDate) ? releaseDate.GetString() : 
                         data.TryGetProperty("first_air_date", out var firstAirDate) ? firstAirDate.GetString() : null,
            VoteAverage = data.TryGetProperty("vote_average", out var voteAverage) ? voteAverage.GetDouble() : 0,
            VoteCount = data.TryGetProperty("vote_count", out var voteCount) ? voteCount.GetInt32() : 0,
            Popularity = data.TryGetProperty("popularity", out var popularity) ? popularity.GetDouble() : 0,
            PosterPath = data.TryGetProperty("poster_path", out var posterPath) ? posterPath.GetString() : null,
            BackdropPath = data.TryGetProperty("backdrop_path", out var backdropPath) ? backdropPath.GetString() : null
        };

        if (DateTime.TryParse(item.ReleaseDate, out var date))
        {
            item.Year = date.Year;
        }

        if (data.TryGetProperty("genres", out var genres))
        {
            foreach (var genre in genres.EnumerateArray())
            {
                if (genre.TryGetProperty("name", out var genreName))
                {
                    item.Genres.Add(genreName.GetString() ?? string.Empty);
                }
            }
        }

        if (type == "tv")
        {
            item.FirstAirDate = data.TryGetProperty("first_air_date", out var firstAirDate) ? firstAirDate.GetString() : null;
            item.NumberOfSeasons = data.TryGetProperty("number_of_seasons", out var seasons) ? seasons.GetInt32() : null;
            item.NumberOfEpisodes = data.TryGetProperty("number_of_episodes", out var episodes) ? episodes.GetInt32() : null;
        }

        return item;
    }

    private RequestDto MapToRequest(JsonElement data)
    {
        var request = new RequestDto
        {
            Id = data.GetProperty("id").GetInt32(),
            TmdbId = data.GetProperty("media").GetProperty("tmdbId").GetInt32(),
            Type = data.GetProperty("media").GetProperty("mediaType").GetString() ?? string.Empty,
            Title = data.GetProperty("media").GetProperty("title").GetString() ?? string.Empty,
            Status = data.GetProperty("status").GetString() ?? "pending",
            RequestDate = data.GetProperty("createdAt").GetDateTime(),
            ModifiedDate = data.GetProperty("updatedAt").GetDateTime(),
            Is4K = data.TryGetProperty("is4k", out var is4k) && is4k.GetBoolean()
        };

        if (data.TryGetProperty("requestedBy", out var requestedBy))
        {
            request.UserId = requestedBy.GetProperty("id").GetString() ?? string.Empty;
            request.UserName = requestedBy.GetProperty("displayName").GetString() ?? string.Empty;
        }

        if (data.TryGetProperty("media", out var media))
        {
            request.PosterPath = media.TryGetProperty("posterPath", out var posterPath) ? posterPath.GetString() : null;
        }

        if (data.TryGetProperty("seasons", out var seasons))
        {
            foreach (var season in seasons.EnumerateArray())
            {
                request.Seasons.Add(season.GetInt32());
            }
        }

        return request;
    }
}

/// <summary>
/// Seerr API response wrapper.
/// </summary>
public class SeerrResponseDto
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
    public List<JsonElement>? Results { get; set; }
}

/// <summary>
/// Video/trailer DTO.
/// </summary>
public class VideoDto
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Size { get; set; }
}
