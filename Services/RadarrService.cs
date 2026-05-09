using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.CinemaVault.Api.Dtos;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaVault.Services;

/// <summary>
/// Service for interacting with Radarr API.
/// </summary>
public class RadarrService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RadarrService> _logger;
    private readonly PluginConfiguration _config;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RadarrService(IHttpClientFactory httpClientFactory, ILogger<RadarrService> logger, PluginConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Tests connection to Radarr.
    /// </summary>
    public async Task<ConnectionStatusDto> TestConnectionAsync()
    {
        var status = new ConnectionStatusDto
        {
            Service = "Radarr",
            Connected = false,
            Message = "Connection failed"
        };

        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v3/system/status").ConfigureAwait(false);
            
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
                var moviesResponse = await client.GetAsync("/api/v3/movie").ConfigureAwait(false);
                if (moviesResponse.IsSuccessStatusCode)
                {
                    var moviesData = JsonSerializer.Deserialize<List<JsonElement>>(await moviesResponse.Content.ReadAsStringAsync().ConfigureAwait(false), _jsonOptions);
                    status.Data = new { 
                        movieCount = moviesData?.Count ?? 0,
                        downloadedCount = moviesData?.Count(m => m.GetProperty("hasFile").GetBoolean()) ?? 0
                    };
                }
            }
            else
            {
                status.Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Radarr connection");
            status.Message = ex.Message;
        }

        return status;
    }

    /// <summary>
    /// Gets all movies from Radarr.
    /// </summary>
    public async Task<List<RadarrMovieDto>> GetMoviesAsync()
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v3/movie").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<RadarrMovieDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var movies = JsonSerializer.Deserialize<List<RadarrMovieDto>>(content, _jsonOptions);
            
            return movies ?? new List<RadarrMovieDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movies from Radarr");
            return new List<RadarrMovieDto>();
        }
    }

    /// <summary>
    /// Gets movie by TMDB ID.
    /// </summary>
    public async Task<RadarrMovieDto?> GetMovieByTmdbIdAsync(int tmdbId)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"/api/v3/movie/lookup/tmdb?tmdbId={tmdbId}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var movie = JsonSerializer.Deserialize<RadarrMovieDto>(content, _jsonOptions);
            
            return movie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movie by TMDB ID {TmdbId}", tmdbId);
            return null;
        }
    }

    /// <summary>
    /// Adds a movie to Radarr.
    /// </summary>
    public async Task<RadarrMovieDto?> AddMovieAsync(int tmdbId, int qualityProfileId, string rootFolder, bool is4K = false)
    {
        try
        {
            // First lookup the movie
            var lookupResult = await GetMovieByTmdbIdAsync(tmdbId).ConfigureAwait(false);
            if (lookupResult == null)
            {
                return null;
            }

            var client = CreateHttpClient();
            
            var movie = new RadarrMovieDto
            {
                Title = lookupResult.Title,
                Year = lookupResult.Year,
                TmdbId = lookupResult.TmdbId,
                ImdbId = lookupResult.ImdbId,
                QualityProfileId = qualityProfileId,
                RootFolderPath = rootFolder,
                Monitored = true,
                MinimumAvailability = "released",
                AddOptions = new RadarrAddOptionsDto
                {
                    SearchForMovie = true
                }
            };

            if (is4K)
            {
                // For 4K, we might need to use a different quality profile
                movie.QualityProfileId = Get4KQualityProfileId();
            }

            var response = await client.PostAsJsonAsync("/api/v3/movie", movie).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var addedMovie = JsonSerializer.Deserialize<RadarrMovieDto>(content, _jsonOptions);
            
            return addedMovie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding movie with TMDB ID {TmdbId}", tmdbId);
            return null;
        }
    }

    /// <summary>
    /// Gets download queue.
    /// </summary>
    public async Task<List<RadarrQueueItemDto>> GetQueueAsync()
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v3/queue").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<RadarrQueueItemDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var queueData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            var queue = new List<RadarrQueueItemDto>();
            if (queueData.TryGetProperty("records", out var records))
            {
                foreach (var item in records.EnumerateArray())
                {
                    queue.Add(JsonSerializer.Deserialize<RadarrQueueItemDto>(item.GetRawText(), _jsonOptions)!);
                }
            }

            return queue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue from Radarr");
            return new List<RadarrQueueItemDto>();
        }
    }

    /// <summary>
    /// Gets quality profiles.
    /// </summary>
    public async Task<List<RadarrQualityProfileDto>> GetQualityProfilesAsync()
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v3/qualityprofile").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<RadarrQualityProfileDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var profiles = JsonSerializer.Deserialize<List<RadarrQualityProfileDto>>(content, _jsonOptions);
            
            return profiles ?? new List<RadarrQualityProfileDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quality profiles from Radarr");
            return new List<RadarrQualityProfileDto>();
        }
    }

    /// <summary>
    /// Gets root folders.
    /// </summary>
    public async Task<List<RadarrRootFolderDto>> GetRootFoldersAsync()
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v3/rootfolder").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<RadarrRootFolderDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var folders = JsonSerializer.Deserialize<List<RadarrRootFolderDto>>(content, _jsonOptions);
            
            return folders ?? new List<RadarrRootFolderDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root folders from Radarr");
            return new List<RadarrRootFolderDto>();
        }
    }

    /// <summary>
    /// Searches for a movie.
    /// </summary>
    public async Task<bool> SearchMovieAsync(int movieId)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.PostAsync($"/api/v3/command", 
                JsonContent.Create(new { name = "MoviesSearch", movieIds = new[] { movieId } })).ConfigureAwait(false);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for movie {MovieId}", movieId);
            return false;
        }
    }

    /// <summary>
    /// Gets movie file information.
    /// </summary>
    public async Task<RadarrMovieFileDto?> GetMovieFileAsync(int movieId)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"/api/v3/moviefile?movieId={movieId}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var files = JsonSerializer.Deserialize<List<RadarrMovieFileDto>>(content, _jsonOptions);
            
            return files?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movie file for movie {MovieId}", movieId);
            return null;
        }
    }

    /// <summary>
    /// Gets download status for TMDB IDs.
    /// </summary>
    public async Task<Dictionary<int, string>> GetDownloadStatusAsync(IEnumerable<int> tmdbIds)
    {
        var statusDict = new Dictionary<int, string>();
        
        try
        {
            var movies = await GetMoviesAsync().ConfigureAwait(false);
            var queue = await GetQueueAsync().ConfigureAwait(false);
            
            var tmdbIdList = tmdbIds.ToList();
            
            foreach (var movie in movies.Where(m => tmdbIdList.Contains(m.TmdbId)))
            {
                if (movie.HasFile)
                {
                    statusDict[movie.TmdbId] = "available";
                }
                else if (movie.IsAvailable)
                {
                    statusDict[movie.TmdbId] = "downloading";
                }
                else
                {
                    statusDict[movie.TmdbId] = "pending";
                }
            }

            // Check queue for downloading items
            foreach (var queueItem in queue)
            {
                if (queueItem.Movie?.TmdbId.HasValue == true && tmdbIdList.Contains(queueItem.Movie.TmdbId.Value))
                {
                    var progress = queueItem.Sizeleft > 0 && queueItem.Size > 0 
                        ? (1.0 - (double)queueItem.Sizeleft / queueItem.Size) * 100 
                        : 0.0;
                    statusDict[queueItem.Movie.TmdbId.Value] = $"downloading:{progress:F0}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download status");
        }

        return statusDict;
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_config.RadarrUrl);
        client.DefaultRequestHeaders.Add("X-Api-Key", _config.RadarrApiKey);
        client.DefaultRequestHeaders.Add("User-Agent", "CinemaVault/1.0.0");
        return client;
    }

    private int Get4KQualityProfileId()
    {
        // This would ideally be configurable, for now return a common 4K profile ID
        // In a real implementation, you'd get this from the quality profiles API
        return _config.RadarrQualityProfileId + 1; // Assume 4K profile is next to regular
    }
}

/// <summary>
/// Radarr movie DTO.
/// </summary>
public class RadarrMovieDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int TmdbId { get; set; }
    public string? ImdbId { get; set; }
    public int QualityProfileId { get; set; }
    public string RootFolderPath { get; set; } = string.Empty;
    public bool Monitored { get; set; }
    public string MinimumAvailability { get; set; } = string.Empty;
    public bool HasFile { get; set; }
    public bool IsAvailable { get; set; }
    public RadarrAddOptionsDto? AddOptions { get; set; }
    public List<RadarrMovieFileDto>? MovieFiles { get; set; }
}

/// <summary>
/// Radarr add options DTO.
/// </summary>
public class RadarrAddOptionsDto
{
    public bool SearchForMovie { get; set; }
}

/// <summary>
/// Radarr queue item DTO.
/// </summary>
public class RadarrQueueItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long Size { get; set; }
    public long Sizeleft { get; set; }
    public string? Status { get; set; }
    public string? TrackedDownloadStatus { get; set; }
    public RadarrMovieDto? Movie { get; set; }
}

/// <summary>
/// Radarr movie file DTO.
/// </summary>
public class RadarrMovieFileDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Quality { get; set; } = string.Empty;
    public List<string> CustomFormats { get; set; } = new();
}

/// <summary>
/// Radarr quality profile DTO.
/// </summary>
public class RadarrQualityProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? UpgradeAllowed { get; set; }
    public int? Cutoff { get; set; }
    public List<RadarrQualityItemDto> Items { get; set; } = new();
}

/// <summary>
/// Radarr quality item DTO.
/// </summary>
public class RadarrQualityItemDto
{
    public string Quality { get; set; } = string.Empty;
    public bool Allowed { get; set; }
}

/// <summary>
/// Radarr root folder DTO.
/// </summary>
public class RadarrRootFolderDto
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public long FreeSpace { get; set; }
    public long TotalSpace { get; set; }
}
