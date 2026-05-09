using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.CinemaVault.Api.Dtos;
using Jellyfin.Plugin.CinemaVault.Data;
using Jellyfin.Plugin.CinemaVault.Services;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaVault.Api;

/// <summary>
/// Main API controller for CinemaVault.
/// </summary>
[Route("/CinemaVault")]
public class CinemaVaultController : IService, IRequiresRequest
{
    private readonly IServerConfigurationManager _configManager;
    private readonly ILogger<CinemaVaultController> _logger;
    private readonly ICinemaVaultRepository _repository;
    private readonly SeerrService _seerrService;
    private readonly RadarrService _radarrService;
    private readonly SonarrService _sonarrService;
    private readonly JellyfinSyncService _jellyfinSyncService;
    private readonly WatchlistService _watchlistService;
    private readonly PollingService _pollingService;
    private readonly IJsonSerializer _jsonSerializer;

    public IRequest Request { get; set; } = null!;

    public CinemaVaultController(
        IServerConfigurationManager configManager,
        ILogger<CinemaVaultController> logger,
        ICinemaVaultRepository repository,
        SeerrService seerrService,
        RadarrService radarrService,
        SonarrService sonarrService,
        JellyfinSyncService jellyfinSyncService,
        WatchlistService watchlistService,
        PollingService pollingService,
        IJsonSerializer jsonSerializer)
    {
        _configManager = configManager;
        _logger = logger;
        _repository = repository;
        _seerrService = seerrService;
        _radarrService = radarrService;
        _sonarrService = sonarrService;
        _jellyfinSyncService = jellyfinSyncService;
        _watchlistService = watchlistService;
        _pollingService = pollingService;
        _jsonSerializer = jsonSerializer;
    }

    // Discovery endpoints
    [HttpGet]
    [Route("/CinemaVault/discover/trending")]
    public async Task<object> GetTrendingAsync()
    {
        var type = GetQueryParameter("type", "movie");
        var page = GetQueryParameterInt("page", 1);
        
        try
        {
            var result = await _seerrService.GetTrendingAsync(type, page).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending content");
            return new SearchResultDto();
        }
    }

    [HttpGet]
    [Route("/CinemaVault/discover/popular")]
    public async Task<object> GetPopularAsync()
    {
        var type = GetQueryParameter("type", "movie");
        var page = GetQueryParameterInt("page", 1);
        
        try
        {
            var result = await _seerrService.GetPopularAsync(type, page).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular content");
            return new SearchResultDto();
        }
    }

    [HttpGet]
    [Route("/CinemaVault/discover/toprated")]
    public async Task<object> GetTopRatedAsync()
    {
        var type = GetQueryParameter("type", "movie");
        var page = GetQueryParameterInt("page", 1);
        
        try
        {
            var result = await _seerrService.GetTopRatedAsync(type, page).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top rated content");
            return new SearchResultDto();
        }
    }

    [HttpGet]
    [Route("/CinemaVault/discover/nowplaying")]
    public async Task<object> GetNowPlayingAsync()
    {
        var page = GetQueryParameterInt("page", 1);
        
        try
        {
            var result = await _seerrService.GetNowPlayingAsync(page).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting now playing movies");
            return new SearchResultDto();
        }
    }

    [HttpGet]
    [Route("/CinemaVault/discover/genre")]
    public async Task<object> GetByGenreAsync()
    {
        var genreId = GetQueryParameterInt("genreId", 0);
        var type = GetQueryParameter("type", "movie");
        var page = GetQueryParameterInt("page", 1);
        
        if (genreId == 0)
        {
            return new SearchResultDto();
        }

        try
        {
            var result = await _seerrService.GetByGenreAsync(genreId, type, page).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content by genre {GenreId}", genreId);
            return new SearchResultDto();
        }
    }

    [HttpGet]
    [Route("/CinemaVault/search")]
    public async Task<object> SearchAsync()
    {
        var query = GetQueryParameter("query", "");
        var page = GetQueryParameterInt("page", 1);
        
        if (string.IsNullOrEmpty(query))
        {
            return new CombinedSearchResultDto();
        }

        try
        {
            var userId = GetCurrentUserId();
            
            // Get discover results from Seerr
            var discoverResults = await _seerrService.SearchAsync(query, page).ConfigureAwait(false);
            
            // Get library results from Jellyfin
            var libraryResults = await _jellyfinSyncService.SearchLibraryAsync(query, userId, 20).ConfigureAwait(false);
            
            // Get popular searches (placeholder)
            var popularSearches = new List<string> { "Action", "Comedy", "Drama", "Horror", "Sci-Fi", "Thriller" };
            
            // Get recent searches from localStorage would be handled client-side
            
            return new CombinedSearchResultDto
            {
                Query = query,
                LibraryResults = libraryResults,
                DiscoverResults = discoverResults.Results,
                PopularSearches = popularSearches,
                RecentSearches = new List<string>() // Would come from client-side
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for content with query: {Query}", query);
            return new CombinedSearchResultDto();
        }
    }

    // Content detail endpoints
    [HttpGet]
    [Route("/CinemaVault/detail")]
    public async Task<object> GetDetailAsync()
    {
        var tmdbId = GetQueryParameterInt("tmdbId", 0);
        var type = GetQueryParameter("type", "movie");
        
        if (tmdbId == 0)
        {
            return null;
        }

        try
        {
            var result = await _seerrService.GetDetailsAsync(tmdbId, type).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting details for TMDB ID {TmdbId} type {Type}", tmdbId, type);
            return null;
        }
    }

    [HttpGet]
    [Route("/CinemaVault/recommendations")]
    public async Task<object> GetRecommendationsAsync()
    {
        var tmdbId = GetQueryParameterInt("tmdbId", 0);
        var type = GetQueryParameter("type", "movie");
        var page = GetQueryParameterInt("page", 1);
        
        if (tmdbId == 0)
        {
            return new SearchResultDto();
        }

        try
        {
            var result = await _seerrService.GetRecommendationsAsync(tmdbId, type, page).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for TMDB ID {TmdbId} type {Type}", tmdbId, type);
            return new SearchResultDto();
        }
    }

    [HttpGet]
    [Route("/CinemaVault/videos")]
    public async Task<object> GetVideosAsync()
    {
        var tmdbId = GetQueryParameterInt("tmdbId", 0);
        var type = GetQueryParameter("type", "movie");
        
        if (tmdbId == 0)
        {
            return new List<VideoDto>();
        }

        try
        {
            var result = await _seerrService.GetVideosAsync(tmdbId, type).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting videos for TMDB ID {TmdbId} type {Type}", tmdbId, type);
            return new List<VideoDto>();
        }
    }

    // Library sync endpoints
    [HttpGet]
    [Route("/CinemaVault/library/status")]
    public async Task<object> GetLibraryStatusAsync()
    {
        var tmdbIdsParam = GetQueryParameter("tmdbIds", "");
        
        if (string.IsNullOrEmpty(tmdbIdsParam))
        {
            return new Dictionary<int, string>();
        }

        try
        {
            var tmdbIds = tmdbIdsParam.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id.Trim(), out var tmdbId) ? tmdbId : 0)
                .Where(id => id > 0)
                .ToList();

            var result = await _jellyfinSyncService.GetLibraryStatusAsync(tmdbIds).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting library status for TMDB IDs: {TmdbIds}", tmdbIdsParam);
            return new Dictionary<int, string>();
        }
    }

    [HttpGet]
    [Route("/CinemaVault/library/recent")]
    public async Task<object> GetRecentAsync()
    {
        var limit = GetQueryParameterInt("limit", 20);
        var userId = GetCurrentUserId();
        
        try
        {
            var result = await _jellyfinSyncService.GetRecentlyAddedAsync(limit, userId).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently added items");
            return new List<ContentItemDto>();
        }
    }

    [HttpGet]
    [Route("/CinemaVault/library/resume")]
    public async Task<object> GetResumeAsync()
    {
        var userId = GetCurrentUserId();
        
        if (string.IsNullOrEmpty(userId))
        {
            return new List<ContentItemDto>();
        }

        try
        {
            var result = await _jellyfinSyncService.GetContinueWatchingAsync(userId).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting continue watching items for user {UserId}", userId);
            return new List<ContentItemDto>();
        }
    }

    // Request endpoints
    [HttpPost]
    [Route("/CinemaVault/request")]
    public async Task<object> CreateRequestAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return new StatusDto { Message = "User not authenticated", Type = "error", Code = 401 };
            }

            var config = Plugin.Instance!.Configuration;
            if (!config.AllowUserRequests && !IsAdmin())
            {
                return new StatusDto { Message = "User requests are not allowed", Type = "error", Code = 403 };
            }

            var requestDto = _jsonSerializer.DeserializeFromString<CreateRequestDto>(Request.RequestBody);
            if (requestDto == null)
            {
                return new StatusDto { Message = "Invalid request data", Type = "error", Code = 400 };
            }

            // Check if request already exists
            var existingRequest = await _repository.GetRequestByTmdbIdAsync(requestDto.TmdbId, requestDto.Type).ConfigureAwait(false);
            if (existingRequest != null)
            {
                return new StatusDto { Message = "Request already exists", Type = "warning", Code = 409 };
            }

            // Create request in Seerr
            var seerrRequest = await _seerrService.CreateRequestAsync(requestDto, userId).ConfigureAwait(false);
            if (seerrRequest == null)
            {
                return new StatusDto { Message = "Failed to create request in Seerr", Type = "error", Code = 500 };
            }

            // Add to our database
            var request = new Request
            {
                ExternalId = seerrRequest.Id,
                TmdbId = requestDto.TmdbId,
                Type = requestDto.Type,
                UserId = userId,
                UserName = GetCurrentUserName(),
                RequestDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Is4K = requestDto.Is4K,
                Seasons = requestDto.Seasons != null ? string.Join(",", requestDto.Seasons) : null
            };

            await _repository.AddRequestAsync(request).ConfigureAwait(false);

            return new StatusDto 
            { 
                Message = "Request created successfully", 
                Type = "success", 
                Code = 201,
                Data = new { requestId = request.Id }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating request");
            return new StatusDto { Message = "Internal server error", Type = "error", Code = 500 };
        }
    }

    [HttpGet]
    [Route("/CinemaVault/requests")]
    public async Task<object> GetRequestsAsync()
    {
        var userId = GetQueryParameter("userId", "");
        var status = GetQueryParameter("status", "");
        
        try
        {
            List<Request> requests;
            
            if (!string.IsNullOrEmpty(userId))
            {
                requests = await _repository.GetRequestsByUserIdAsync(userId).ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(status))
            {
                requests = await _repository.GetRequestsByStatusAsync(status).ConfigureAwait(false);
            }
            else
            {
                requests = await _repository.GetAllRequestsAsync().ConfigureAwait(false);
            }

            var result = requests.Select(r => new RequestDto
            {
                Id = r.Id,
                TmdbId = r.TmdbId,
                Type = r.Type,
                Title = r.Title,
                UserId = r.UserId,
                UserName = r.UserName,
                Status = r.Status,
                RequestDate = r.RequestDate,
                ModifiedDate = r.ModifiedDate,
                Is4K = r.Is4K,
                PosterPath = r.PosterPath,
                Seasons = r.Seasons?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList() ?? new List<int>(),
                DownloadProgress = r.DownloadProgress,
                JellyfinId = r.JellyfinId,
                ExternalId = r.ExternalId
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting requests");
            return new List<RequestDto>();
        }
    }

    [HttpDelete]
    [Route("/CinemaVault/request/{id}")]
    public async Task<object> DeleteRequestAsync()
    {
        var id = GetPathParameterInt("id", 0);
        
        if (id == 0)
        {
            return new StatusDto { Message = "Invalid request ID", Type = "error", Code = 400 };
        }

        try
        {
            var request = await _repository.GetRequestAsync(id).ConfigureAwait(false);
            if (request == null)
            {
                return new StatusDto { Message = "Request not found", Type = "error", Code = 404 };
            }

            // Check permissions
            var currentUserId = GetCurrentUserId();
            if (request.UserId != currentUserId && !IsAdmin())
            {
                return new StatusDto { Message = "Access denied", Type = "error", Code = 403 };
            }

            // Delete from Seerr if it has an external ID
            if (request.ExternalId.HasValue)
            {
                await _seerrService.DeleteRequestAsync(request.ExternalId.Value).ConfigureAwait(false);
            }

            // Delete from our database
            await _repository.DeleteRequestAsync(id).ConfigureAwait(false);

            return new StatusDto { Message = "Request deleted successfully", Type = "success", Code = 200 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting request {RequestId}", id);
            return new StatusDto { Message = "Internal server error", Type = "error", Code = 500 };
        }
    }

    [HttpGet]
    [Route("/CinemaVault/request/status/{tmdbId}")]
    public async Task<object> GetRequestStatusAsync()
    {
        var tmdbId = GetPathParameterInt("tmdbId", 0);
        var type = GetQueryParameter("type", "movie");
        
        if (tmdbId == 0)
        {
            return new StatusDto { Message = "Invalid TMDB ID", Type = "error", Code = 400 };
        }

        try
        {
            var request = await _repository.GetRequestByTmdbIdAsync(tmdbId, type).ConfigureAwait(false);
            
            if (request == null)
            {
                return new StatusDto { Message = "Request not found", Type = "info", Code = 404 };
            }

            return new StatusDto 
            { 
                Message = "Request found", 
                Type = "success", 
                Code = 200,
                Data = new 
                { 
                    status = request.Status,
                    downloadProgress = request.DownloadProgress,
                    jellyfinId = request.JellyfinId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting request status for TMDB ID {TmdbId} type {Type}", tmdbId, type);
            return new StatusDto { Message = "Internal server error", Type = "error", Code = 500 };
        }
    }

    // Watchlist endpoints
    [HttpGet]
    [Route("/CinemaVault/watchlist")]
    public async Task<object> GetWatchlistAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new List<WatchlistItemDto>();
        }

        var page = GetQueryParameterInt("page", 1);
        
        try
        {
            var result = await _watchlistService.GetUserWatchlistAsync(userId, page).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting watchlist for user {UserId}", userId);
            return new List<WatchlistItemDto>();
        }
    }

    [HttpPost]
    [Route("/CinemaVault/watchlist")]
    public async Task<object> AddToWatchlistAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return new StatusDto { Message = "User not authenticated", Type = "error", Code = 401 };
            }

            var data = _jsonSerializer.DeserializeFromString<Dictionary<string, object>>(Request.RequestBody);
            if (data == null)
            {
                return new StatusDto { Message = "Invalid request data", Type = "error", Code = 400 };
            }

            var tmdbId = Convert.ToInt32(data.GetValueOrDefault("tmdbId", 0));
            var type = data.GetValueOrDefault("type", "").ToString() ?? "";
            var title = data.GetValueOrDefault("title", "").ToString() ?? "";
            var posterPath = data.GetValueOrDefault("posterPath", "").ToString();

            if (tmdbId == 0 || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(title))
            {
                return new StatusDto { Message = "Missing required fields", Type = "error", Code = 400 };
            }

            var success = await _watchlistService.AddToWatchlistAsync(userId, tmdbId, type, title, posterPath).ConfigureAwait(false);
            
            if (success)
            {
                return new StatusDto { Message = "Added to watchlist", Type = "success", Code = 201 };
            }
            else
            {
                return new StatusDto { Message = "Failed to add to watchlist", Type = "error", Code = 500 };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to watchlist");
            return new StatusDto { Message = "Internal server error", Type = "error", Code = 500 };
        }
    }

    [HttpDelete]
    [Route("/CinemaVault/watchlist/{tmdbId}")]
    public async Task<object> RemoveFromWatchlistAsync()
    {
        var tmdbId = GetPathParameterInt("tmdbId", 0);
        var type = GetQueryParameter("type", "movie");
        
        if (tmdbId == 0)
        {
            return new StatusDto { Message = "Invalid TMDB ID", Type = "error", Code = 400 };
        }

        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return new StatusDto { Message = "User not authenticated", Type = "error", Code = 401 };
            }

            var success = await _watchlistService.RemoveFromWatchlistAsync(userId, tmdbId, type).ConfigureAwait(false);
            
            if (success)
            {
                return new StatusDto { Message = "Removed from watchlist", Type = "success", Code = 200 };
            }
            else
            {
                return new StatusDto { Message = "Item not found in watchlist", Type = "warning", Code = 404 };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from watchlist");
            return new StatusDto { Message = "Internal server error", Type = "error", Code = 500 };
        }
    }

    // Config endpoints
    [HttpGet]
    [Route("/CinemaVault/config")]
    public object GetConfig()
    {
        if (!IsAdmin())
        {
            return new StatusDto { Message = "Access denied", Type = "error", Code = 403 };
        }

        try
        {
            var config = Plugin.Instance!.Configuration;
            return new
            {
                config.SeerrUrl,
                config.SeerrApiKey,
                config.RadarrUrl,
                config.RadarrApiKey,
                config.RadarrQualityProfileId,
                config.RadarrRootFolder,
                config.SonarrUrl,
                config.SonarrApiKey,
                config.SonarrQualityProfileId,
                config.SonarrRootFolder,
                config.Enable4KRequests,
                config.AllowUserRequests,
                config.MaxRequestsPerUser,
                config.PollingIntervalMinutes,
                config.ShowAdultContent,
                config.DefaultLanguage,
                config.EnableHeroBanner,
                config.EnableAutoPlay,
                config.HeroRotationSeconds,
                config.AccentColor
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting config");
            return new StatusDto { Message = "Internal server error", Type = "error", Code = 500 };
        }
    }

    [HttpPost]
    [Route("/CinemaVault/config")]
    public async Task<object> UpdateConfigAsync()
    {
        if (!IsAdmin())
        {
            return new StatusDto { Message = "Access denied", Type = "error", Code = 403 };
        }

        try
        {
            var configData = _jsonSerializer.DeserializeFromString<Dictionary<string, object>>(Request.RequestBody);
            if (configData == null)
            {
                return new StatusDto { Message = "Invalid config data", Type = "error", Code = 400 };
            }

            var config = Plugin.Instance!.Configuration;
            
            // Update config properties
            foreach (var prop in typeof(PluginConfiguration).GetProperties())
            {
                if (configData.ContainsKey(prop.Name))
                {
                    var value = configData[prop.Name];
                    if (value != null)
                    {
                        // Handle type conversion
                        if (prop.PropertyType == typeof(string))
                        {
                            prop.SetValue(config, value.ToString());
                        }
                        else if (prop.PropertyType == typeof(int))
                        {
                            prop.SetValue(config, Convert.ToInt32(value));
                        }
                        else if (prop.PropertyType == typeof(bool))
                        {
                            prop.SetValue(config, Convert.ToBoolean(value));
                        }
                    }
                }
            }

            Plugin.Instance!.UpdateConfiguration(config);
            
            return new StatusDto { Message = "Configuration updated", Type = "success", Code = 200 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating config");
            return new StatusDto { Message = "Internal server error", Type = "error", Code = 500 };
        }
    }

    [HttpGet]
    [Route("/CinemaVault/config/test/seerr")]
    public async Task<object> TestSeerrConnectionAsync()
    {
        if (!IsAdmin())
        {
            return new StatusDto { Message = "Access denied", Type = "error", Code = 403 };
        }

        try
        {
            var result = await _seerrService.TestConnectionAsync().ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Seerr connection");
            return new ConnectionStatusDto 
            { 
                Service = "Seerr",
                Connected = false,
                Message = ex.Message,
                ResponseTime = 0
            };
        }
    }

    [HttpGet]
    [Route("/CinemaVault/config/test/radarr")]
    public async Task<object> TestRadarrConnectionAsync()
    {
        if (!IsAdmin())
        {
            return new StatusDto { Message = "Access denied", Type = "error", Code = 403 };
        }

        try
        {
            var result = await _radarrService.TestConnectionAsync().ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Radarr connection");
            return new ConnectionStatusDto 
            { 
                Service = "Radarr",
                Connected = false,
                Message = ex.Message,
                ResponseTime = 0
            };
        }
    }

    [HttpGet]
    [Route("/CinemaVault/config/test/sonarr")]
    public async Task<object> TestSonarrConnectionAsync()
    {
        if (!IsAdmin())
        {
            return new StatusDto { Message = "Access denied", Type = "error", Code = 403 };
        }

        try
        {
            var result = await _sonarrService.TestConnectionAsync().ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Sonarr connection");
            return new ConnectionStatusDto 
            { 
                Service = "Sonarr",
                Connected = false,
                Message = ex.Message,
                ResponseTime = 0
            };
        }
    }

    // Hero content endpoint
    [HttpGet]
    [Route("/CinemaVault/hero")]
    public async Task<object> GetHeroContentAsync()
    {
        var userId = GetCurrentUserId();
        
        try
        {
            var heroItems = new List<HeroItemDto>();
            
            // Get continue watching items
            var continueWatching = await _jellyfinSyncService.GetContinueWatchingAsync(userId).ConfigureAwait(false);
            foreach (var item in continueWatching.Take(2))
            {
                heroItems.Add(MapToHeroItem(item, "continue"));
            }

            // Get trending items
            var trending = await _seerrService.GetTrendingAsync("movie", 1).ConfigureAwait(false);
            foreach (var item in trending.Results.Take(2))
            {
                heroItems.Add(MapToHeroItem(item, "trending"));
            }

            // Get top rated items
            var topRated = await _seerrService.GetTopRatedAsync("movie", 1).ConfigureAwait(false);
            foreach (var item in topRated.Results.Take(1))
            {
                heroItems.Add(MapToHeroItem(item, "toprated"));
            }

            // Sort by priority and limit to 5
            return heroItems.OrderBy(i => i.Priority).Take(5).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hero content");
            return new List<HeroItemDto>();
        }
    }

    // Helper methods
    private string GetCurrentUserId()
    {
        // Get current user from Jellyfin session
        return Request?.User?.Id?.ToString() ?? string.Empty;
    }

    private string GetCurrentUserName()
    {
        return Request?.User?.Name ?? string.Empty;
    }

    private bool IsAdmin()
    {
        return Request?.User?.Policy?.IsAdministrator ?? false;
    }

    private string GetQueryParameter(string name, string defaultValue)
    {
        return Request?.QueryString[name] ?? defaultValue;
    }

    private int GetQueryParameterInt(string name, int defaultValue)
    {
        var value = GetQueryParameter(name, defaultValue.ToString());
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private int GetPathParameterInt(string name, int defaultValue)
    {
        var pathParts = Request?.PathInfo?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var index = Array.IndexOf(pathParts, name);
        if (index >= 0 && index + 1 < pathParts.Length)
        {
            return int.TryParse(pathParts[index + 1], out var result) ? result : defaultValue;
        }
        return defaultValue;
    }

    private HeroItemDto MapToHeroItem(ContentItemDto item, string source)
    {
        return new HeroItemDto
        {
            TmdbId = item.TmdbId,
            Type = item.Type,
            Title = item.Title,
            Overview = item.Overview,
            BackdropPath = item.BackdropPath ?? "",
            PosterPath = item.PosterPath,
            Year = item.Year,
            Runtime = item.Runtime,
            VoteAverage = item.VoteAverage,
            VoteCount = item.VoteCount,
            Genres = item.Genres,
            Status = item.Status,
            QualityBadges = GetQualityBadges(item),
            PrimaryAction = item.Status == "available" ? "Play" : "Request",
            PrimaryActionType = item.Status == "available" ? "play" : "request",
            JellyfinId = item.JellyfinId,
            Priority = source switch
            {
                "continue" => 1,
                "trending" => 2,
                "toprated" => 3,
                _ => 10
            },
            IsFeatured = source == "trending",
            LastEpisode = item.LastEpisode,
            ReleaseDate = item.ReleaseDate
        };
    }

    private List<string> GetQualityBadges(ContentItemDto item)
    {
        var badges = new List<string>();
        
        if (item.Is4K) badges.Add("4K");
        if (item.HasHDR) badges.Add("HDR");
        if (item.HasDolbyVision) badges.Add("Dolby Vision");
        if (!string.IsNullOrEmpty(item.Quality)) badges.Add(item.Quality);
        
        return badges;
    }
}
