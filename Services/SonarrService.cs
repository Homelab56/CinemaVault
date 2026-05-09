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
/// Service for interacting with Sonarr API.
/// </summary>
public class SonarrService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SonarrService> _logger;
    private readonly PluginConfiguration _config;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public SonarrService(IHttpClientFactory httpClientFactory, ILogger<SonarrService> logger, PluginConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Tests connection to Sonarr.
    /// </summary>
    public async Task<ConnectionStatusDto> TestConnectionAsync()
    {
        var status = new ConnectionStatusDto
        {
            Service = "Sonarr",
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
                var seriesResponse = await client.GetAsync("/api/v3/series").ConfigureAwait(false);
                if (seriesResponse.IsSuccessStatusCode)
                {
                    var seriesData = JsonSerializer.Deserialize<List<JsonElement>>(await seriesResponse.Content.ReadAsStringAsync().ConfigureAwait(false), _jsonOptions);
                    status.Data = new { 
                        seriesCount = seriesData?.Count ?? 0,
                        downloadedCount = seriesData?.Count(s => s.GetProperty("statistics").GetProperty("episodeFileCount").GetInt32() > 0) ?? 0
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
            _logger.LogError(ex, "Error testing Sonarr connection");
            status.Message = ex.Message;
        }

        return status;
    }

    /// <summary>
    /// Gets all series from Sonarr.
    /// </summary>
    public async Task<List<SonarrSeriesDto>> GetSeriesAsync()
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v3/series").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<SonarrSeriesDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var series = JsonSerializer.Deserialize<List<SonarrSeriesDto>>(content, _jsonOptions);
            
            return series ?? new List<SonarrSeriesDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting series from Sonarr");
            return new List<SonarrSeriesDto>();
        }
    }

    /// <summary>
    /// Gets series by TVDB ID.
    /// </summary>
    public async Task<SonarrSeriesDto?> GetSeriesByTvdbIdAsync(int tvdbId)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"/api/v3/series/lookup/tvdb?tvdbId={tvdbId}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var series = JsonSerializer.Deserialize<SonarrSeriesDto>(content, _jsonOptions);
            
            return series;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting series by TVDB ID {TvdbId}", tvdbId);
            return null;
        }
    }

    /// <summary>
    /// Adds a series to Sonarr.
    /// </summary>
    public async Task<SonarrSeriesDto?> AddSeriesAsync(int tvdbId, int qualityProfileId, string rootFolder, List<int> seasons, bool is4K = false)
    {
        try
        {
            // First lookup the series
            var lookupResult = await GetSeriesByTvdbIdAsync(tvdbId).ConfigureAwait(false);
            if (lookupResult == null)
            {
                return null;
            }

            var client = CreateHttpClient();
            
            var series = new SonarrSeriesDto
            {
                Title = lookupResult.Title,
                Year = lookupResult.Year,
                TvdbId = lookupResult.TvdbId,
                ImdbId = lookupResult.ImdbId,
                QualityProfileId = qualityProfileId,
                RootFolderPath = rootFolder,
                Monitored = true,
                SeasonFolder = true,
                SeriesType = "standard",
                AddOptions = new SonarrAddOptionsDto
                {
                    SearchForMissingEpisodes = true
                }
            };

            if (is4K)
            {
                // For 4K, we might need to use a different quality profile
                series.QualityProfileId = Get4KQualityProfileId();
            }

            // Set monitored seasons
            if (seasons.Any())
            {
                series.Seasons = lookupResult.Seasons?.Select(s => new SonarrSeasonDto
                {
                    SeasonNumber = s.SeasonNumber,
                    Monitored = seasons.Contains(s.SeasonNumber)
                }).ToList() ?? new List<SonarrSeasonDto>();
            }

            var response = await client.PostAsJsonAsync("/api/v3/series", series).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var addedSeries = JsonSerializer.Deserialize<SonarrSeriesDto>(content, _jsonOptions);
            
            return addedSeries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding series with TVDB ID {TvdbId}", tvdbId);
            return null;
        }
    }

    /// <summary>
    /// Gets download queue.
    /// </summary>
    public async Task<List<SonarrQueueItemDto>> GetQueueAsync()
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v3/queue").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<SonarrQueueItemDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var queueData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            var queue = new List<SonarrQueueItemDto>();
            if (queueData.TryGetProperty("records", out var records))
            {
                foreach (var item in records.EnumerateArray())
                {
                    queue.Add(JsonSerializer.Deserialize<SonarrQueueItemDto>(item.GetRawText(), _jsonOptions)!);
                }
            }

            return queue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue from Sonarr");
            return new List<SonarrQueueItemDto>();
        }
    }

    /// <summary>
    /// Gets quality profiles.
    /// </summary>
    public async Task<List<SonarrQualityProfileDto>> GetQualityProfilesAsync()
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v3/qualityprofile").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<SonarrQualityProfileDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var profiles = JsonSerializer.Deserialize<List<SonarrQualityProfileDto>>(content, _jsonOptions);
            
            return profiles ?? new List<SonarrQualityProfileDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quality profiles from Sonarr");
            return new List<SonarrQualityProfileDto>();
        }
    }

    /// <summary>
    /// Gets root folders.
    /// </summary>
    public async Task<List<SonarrRootFolderDto>> GetRootFoldersAsync()
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync("/api/v3/rootfolder").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<SonarrRootFolderDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var folders = JsonSerializer.Deserialize<List<SonarrRootFolderDto>>(content, _jsonOptions);
            
            return folders ?? new List<SonarrRootFolderDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root folders from Sonarr");
            return new List<SonarrRootFolderDto>();
        }
    }

    /// <summary>
    /// Searches for episodes of a series.
    /// </summary>
    public async Task<bool> SearchSeriesAsync(int seriesId)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.PostAsync($"/api/v3/command", 
                JsonContent.Create(new { name = "SeriesSearch", seriesId = seriesId })).ConfigureAwait(false);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for series {SeriesId}", seriesId);
            return false;
        }
    }

    /// <summary>
    /// Gets episodes for a series.
    /// </summary>
    public async Task<List<SonarrEpisodeDto>> GetEpisodesAsync(int seriesId)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"/api/v3/episode?seriesId={seriesId}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<SonarrEpisodeDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var episodes = JsonSerializer.Deserialize<List<SonarrEpisodeDto>>(content, _jsonOptions);
            
            return episodes ?? new List<SonarrEpisodeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting episodes for series {SeriesId}", seriesId);
            return new List<SonarrEpisodeDto>();
        }
    }

    /// <summary>
    /// Gets episode file information.
    /// </summary>
    public async Task<SonarrEpisodeFileDto?> GetEpisodeFileAsync(int episodeId)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"/api/v3/episodefile?episodeId={episodeId}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var files = JsonSerializer.Deserialize<List<SonarrEpisodeFileDto>>(content, _jsonOptions);
            
            return files?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting episode file for episode {EpisodeId}", episodeId);
            return null;
        }
    }

    /// <summary>
    /// Gets download status for TMDB IDs.
    /// Note: Sonarr uses TVDB IDs, so we need to map TMDB to TVDB first.
    /// </summary>
    public async Task<Dictionary<int, string>> GetDownloadStatusAsync(IEnumerable<int> tmdbIds)
    {
        var statusDict = new Dictionary<int, string>();
        
        try
        {
            var series = await GetSeriesAsync().ConfigureAwait(false);
            var queue = await GetQueueAsync().ConfigureAwait(false);
            
            var tmdbIdList = tmdbIds.ToList();
            
            foreach (var serie in series)
            {
                // We would need to map TVDB to TMDB here
                // For now, we'll use the TVDB ID as a proxy
                if (serie.Statistics?.EpisodeFileCount > 0)
                {
                    // statusDict[tmdbId] = "available"; // Would need proper mapping
                }
                else if (serie.Statistics?.TotalEpisodeCount > 0)
                {
                    // statusDict[tmdbId] = "downloading"; // Would need proper mapping
                }
                else
                {
                    // statusDict[tmdbId] = "pending"; // Would need proper mapping
                }
            }

            // Check queue for downloading items
            foreach (var queueItem in queue)
            {
                if (queueItem.Series?.TvdbId.HasValue == true)
                {
                    var progress = queueItem.Sizeleft > 0 && queueItem.Size > 0 
                        ? (1.0 - (double)queueItem.Sizeleft / queueItem.Size) * 100 
                        : 0.0;
                    // statusDict[tmdbId] = $"downloading:{progress:F0}"; // Would need proper mapping
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download status");
        }

        return statusDict;
    }

    /// <summary>
    /// Gets missing episodes for a series.
    /// </summary>
    public async Task<List<SonarrEpisodeDto>> GetMissingEpisodesAsync(int seriesId)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"/api/v3/wanted/missing?seriesId={seriesId}").ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<SonarrEpisodeDto>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var missingData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            
            var episodes = new List<SonarrEpisodeDto>();
            if (missingData.TryGetProperty("records", out var records))
            {
                foreach (var item in records.EnumerateArray())
                {
                    episodes.Add(JsonSerializer.Deserialize<SonarrEpisodeDto>(item.GetRawText(), _jsonOptions)!);
                }
            }

            return episodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting missing episodes for series {SeriesId}", seriesId);
            return new List<SonarrEpisodeDto>();
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_config.SonarrUrl);
        client.DefaultRequestHeaders.Add("X-Api-Key", _config.SonarrApiKey);
        client.DefaultRequestHeaders.Add("User-Agent", "CinemaVault/1.0.0");
        return client;
    }

    private int Get4KQualityProfileId()
    {
        // This would ideally be configurable, for now return a common 4K profile ID
        // In a real implementation, you'd get this from the quality profiles API
        return _config.SonarrQualityProfileId + 1; // Assume 4K profile is next to regular
    }
}

/// <summary>
/// Sonarr series DTO.
/// </summary>
public class SonarrSeriesDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int TvdbId { get; set; }
    public string? ImdbId { get; set; }
    public int QualityProfileId { get; set; }
    public string RootFolderPath { get; set; } = string.Empty;
    public bool Monitored { get; set; }
    public bool SeasonFolder { get; set; }
    public string SeriesType { get; set; } = string.Empty;
    public SonarrStatisticsDto? Statistics { get; set; }
    public List<SonarrSeasonDto>? Seasons { get; set; }
    public SonarrAddOptionsDto? AddOptions { get; set; }
    public List<SonarrEpisodeDto>? Episodes { get; set; }
}

/// <summary>
/// Sonarr season DTO.
/// </summary>
public class SonarrSeasonDto
{
    public int SeasonNumber { get; set; }
    public bool Monitored { get; set; }
    public int Statistics { get; set; }
}

/// <summary>
/// Sonarr statistics DTO.
/// </summary>
public class SonarrStatisticsDto
{
    public int SeasonCount { get; set; }
    public int EpisodeFileCount { get; set; }
    public int EpisodeCount { get; set; }
    public int TotalEpisodeCount { get; set; }
    public long SizeOnDisk { get; set; }
}

/// <summary>
/// Sonarr add options DTO.
/// </summary>
public class SonarrAddOptionsDto
{
    public bool SearchForMissingEpisodes { get; set; }
}

/// <summary>
/// Sonarr queue item DTO.
/// </summary>
public class SonarrQueueItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long Size { get; set; }
    public long Sizeleft { get; set; }
    public string? Status { get; set; }
    public string? TrackedDownloadStatus { get; set; }
    public SonarrSeriesDto? Series { get; set; }
    public SonarrEpisodeDto? Episode { get; set; }
}

/// <summary>
/// Sonarr episode DTO.
/// </summary>
public class SonarrEpisodeDto
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public DateTime? AirDate { get; set; }
    public bool HasFile { get; set; }
    public int? EpisodeFileId { get; set; }
    public SonarrEpisodeFileDto? EpisodeFile { get; set; }
}

/// <summary>
/// Sonarr episode file DTO.
/// </summary>
public class SonarrEpisodeFileDto
{
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public int SeasonNumber { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Quality { get; set; } = string.Empty;
    public List<string> CustomFormats { get; set; } = new();
}

/// <summary>
/// Sonarr quality profile DTO.
/// </summary>
public class SonarrQualityProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? UpgradeAllowed { get; set; }
    public int? Cutoff { get; set; }
    public List<SonarrQualityItemDto> Items { get; set; } = new();
}

/// <summary>
/// Sonarr quality item DTO.
/// </summary>
public class SonarrQualityItemDto
{
    public string Quality { get; set; } = string.Empty;
    public bool Allowed { get; set; }
}

/// <summary>
/// Sonarr root folder DTO.
/// </summary>
public class SonarrRootFolderDto
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public long FreeSpace { get; set; }
    public long TotalSpace { get; set; }
}
