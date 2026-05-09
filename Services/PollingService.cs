using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.CinemaVault.Data;
using Jellyfin.Plugin.CinemaVault.Api.Dtos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaVault.Services;

/// <summary>
/// Background service for polling external services and updating request statuses.
/// </summary>
public class PollingService : BackgroundService
{
    private readonly ILogger<PollingService> _logger;
    private readonly ICinemaVaultRepository _repository;
    private readonly SeerrService _seerrService;
    private readonly RadarrService _radarrService;
    private readonly SonarrService _sonarrService;
    private readonly JellyfinSyncService _jellyfinSyncService;
    private readonly PluginConfiguration _config;

    private readonly TimeSpan _pollingInterval;

    public PollingService(
        ILogger<PollingService> logger,
        ICinemaVaultRepository repository,
        SeerrService seerrService,
        RadarrService radarrService,
        SonarrService sonarrService,
        JellyfinSyncService jellyfinSyncService,
        PluginConfiguration config)
    {
        _logger = logger;
        _repository = repository;
        _seerrService = seerrService;
        _radarrService = radarrService;
        _sonarrService = sonarrService;
        _jellyfinSyncService = jellyfinSyncService;
        _config = config;
        _pollingInterval = TimeSpan.FromMinutes(_config.PollingIntervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CinemaVault polling service started with interval: {Interval}", _pollingInterval);

        // Initial sync after startup
        await PerformFullSyncAsync().ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformFullSyncAsync().ConfigureAwait(false);
                await Task.Delay(_pollingInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during polling service execution");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("CinemaVault polling service stopped");
    }

    /// <summary>
    /// Performs a full sync of all external services.
    /// </summary>
    private async Task PerformFullSyncAsync()
    {
        _logger.LogDebug("Performing full sync at {Time}", DateTime.UtcNow);

        var tasks = new List<Task>
        {
            SyncSeerrRequestsAsync(),
            SyncRadarrStatusAsync(),
            SyncSonarrStatusAsync(),
            SyncJellyfinLibraryAsync(),
            CleanupOldDataAsync()
        };

        await Task.WhenAll(tasks).ConfigureAwait(false);

        _logger.LogDebug("Full sync completed at {Time}", DateTime.UtcNow);
    }

    /// <summary>
    /// Syncs requests from Seerr.
    /// </summary>
    private async Task SyncSeerrRequestsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_config.SeerrUrl) || string.IsNullOrEmpty(_config.SeerrApiKey))
            {
                return;
            }

            var seerrRequests = await _seerrService.GetRequestsAsync().ConfigureAwait(false);
            var existingRequests = await _repository.GetAllRequestsAsync().ConfigureAwait(false);

            foreach (var seerrRequest in seerrRequests)
            {
                var existingRequest = existingRequests.FirstOrDefault(r => r.ExternalId == seerrRequest.Id);
                
                if (existingRequest == null)
                {
                    // New request, add to database
                    var newRequest = new Request
                    {
                        ExternalId = seerrRequest.Id,
                        TmdbId = seerrRequest.TmdbId,
                        Type = seerrRequest.Type,
                        Title = seerrRequest.Title,
                        UserId = seerrRequest.UserId,
                        UserName = seerrRequest.UserName,
                        Status = MapSeerrStatus(seerrRequest.Status),
                        RequestDate = seerrRequest.RequestDate,
                        ModifiedDate = seerrRequest.ModifiedDate,
                        Is4K = seerrRequest.Is4K,
                        PosterPath = seerrRequest.PosterPath,
                        Seasons = string.Join(",", seerrRequest.Seasons)
                    };

                    await _repository.AddRequestAsync(newRequest).ConfigureAwait(false);
                    _logger.LogDebug("Added new request: {Title} ({TmdbId})", seerrRequest.Title, seerrRequest.TmdbId);
                }
                else
                {
                    // Update existing request if status changed
                    var newStatus = MapSeerrStatus(seerrRequest.Status);
                    if (existingRequest.Status != newStatus)
                    {
                        existingRequest.Status = newStatus;
                        existingRequest.ModifiedDate = DateTime.UtcNow;
                        await _repository.UpdateRequestAsync(existingRequest).ConfigureAwait(false);
                        _logger.LogDebug("Updated request status: {Title} -> {Status}", seerrRequest.Title, newStatus);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Seerr requests");
        }
    }

    /// <summary>
    /// Syncs download status from Radarr.
    /// </summary>
    private async Task SyncRadarrStatusAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_config.RadarrUrl) || string.IsNullOrEmpty(_config.RadarrApiKey))
            {
                return;
            }

            var movieRequests = await _repository.GetRequestsByTypeAsync("movie").ConfigureAwait(false);
            if (!movieRequests.Any())
            {
                return;
            }

            var tmdbIds = movieRequests.Select(r => r.TmdbId).ToList();
            var downloadStatus = await _radarrService.GetDownloadStatusAsync(tmdbIds).ConfigureAwait(false);

            foreach (var request in movieRequests)
            {
                if (downloadStatus.TryGetValue(request.TmdbId, out var status))
                {
                    var newStatus = MapDownloadStatus(status);
                    if (request.Status != newStatus)
                    {
                        request.Status = newStatus;
                        request.ModifiedDate = DateTime.UtcNow;

                        // Extract progress if downloading
                        if (status.StartsWith("downloading:"))
                        {
                            var progressStr = status.Substring(12); // Remove "downloading:" prefix
                            if (double.TryParse(progressStr, out var progress))
                            {
                                request.DownloadProgress = progress;
                            }
                        }

                        await _repository.UpdateRequestAsync(request).ConfigureAwait(false);
                        _logger.LogDebug("Updated movie request status: {Title} -> {Status}", request.Title, newStatus);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Radarr status");
        }
    }

    /// <summary>
    /// Syncs download status from Sonarr.
    /// </summary>
    private async Task SyncSonarrStatusAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_config.SonarrUrl) || string.IsNullOrEmpty(_config.SonarrApiKey))
            {
                return;
            }

            var tvRequests = await _repository.GetRequestsByTypeAsync("tv").ConfigureAwait(false);
            if (!tvRequests.Any())
            {
                return;
            }

            var tmdbIds = tvRequests.Select(r => r.TmdbId).ToList();
            var downloadStatus = await _sonarrService.GetDownloadStatusAsync(tmdbIds).ConfigureAwait(false);

            foreach (var request in tvRequests)
            {
                if (downloadStatus.TryGetValue(request.TmdbId, out var status))
                {
                    var newStatus = MapDownloadStatus(status);
                    if (request.Status != newStatus)
                    {
                        request.Status = newStatus;
                        request.ModifiedDate = DateTime.UtcNow;

                        // Extract progress if downloading
                        if (status.StartsWith("downloading:"))
                        {
                            var progressStr = status.Substring(12); // Remove "downloading:" prefix
                            if (double.TryParse(progressStr, out var progress))
                            {
                                request.DownloadProgress = progress;
                            }
                        }

                        await _repository.UpdateRequestAsync(request).ConfigureAwait(false);
                        _logger.LogDebug("Updated TV request status: {Title} -> {Status}", request.Title, newStatus);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Sonarr status");
        }
    }

    /// <summary>
    /// Syncs with Jellyfin library to mark available content.
    /// </summary>
    private async Task SyncJellyfinLibraryAsync()
    {
        try
        {
            var pendingRequests = await _repository.GetRequestsByStatusAsync("pending").ConfigureAwait(false);
            if (!pendingRequests.Any())
            {
                return;
            }

            var tmdbIds = pendingRequests.Select(r => r.TmdbId).ToList();
            var libraryStatus = await _jellyfinSyncService.GetLibraryStatusAsync(tmdbIds).ConfigureAwait(false);

            foreach (var request in pendingRequests)
            {
                if (libraryStatus.ContainsKey(request.TmdbId))
                {
                    request.Status = "available";
                    request.ModifiedDate = DateTime.UtcNow;
                    request.DownloadProgress = 100;
                    
                    await _repository.UpdateRequestAsync(request).ConfigureAwait(false);
                    _logger.LogDebug("Marked request as available: {Title}", request.Title);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Jellyfin library");
        }
    }

    /// <summary>
    /// Cleans up old data.
    /// </summary>
    private async Task CleanupOldDataAsync()
    {
        try
        {
            // Clean up old completed requests (older than 30 days)
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var cleanedCount = await _repository.CleanupOldRequestsAsync(cutoffDate).ConfigureAwait(false);
            
            if (cleanedCount > 0)
            {
                _logger.LogDebug("Cleaned up {Count} old requests", cleanedCount);
            }

            // Clean up old watchlist items (older than 1 year)
            var watchlistCutoffDate = DateTime.UtcNow.AddDays(-365);
            var watchlistCleanedCount = await _repository.CleanupWatchlistAsync(string.Empty, watchlistCutoffDate).ConfigureAwait(false);
            
            if (watchlistCleanedCount > 0)
            {
                _logger.LogDebug("Cleaned up {Count} old watchlist items", watchlistCleanedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
    }

    /// <summary>
    /// Maps Seerr status to internal status.
    /// </summary>
    private string MapSeerrStatus(string seerrStatus)
    {
        return seerrStatus.ToLowerInvariant() switch
        {
            "pending" => "pending",
            "approved" => "approved",
            "processing" => "downloading",
            "available" => "available",
            "declined" => "declined",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Maps download status to internal status.
    /// </summary>
    private string MapDownloadStatus(string downloadStatus)
    {
        if (downloadStatus == "available")
        {
            return "available";
        }
        else if (downloadStatus == "downloading" || downloadStatus.StartsWith("downloading:"))
        {
            return "downloading";
        }
        else if (downloadStatus == "pending")
        {
            return "pending";
        }
        else
        {
            return "unknown";
        }
    }

    /// <summary>
    /// Gets system status for monitoring.
    /// </summary>
    public async Task<SystemStatusDto> GetSystemStatusAsync()
    {
        var status = new SystemStatusDto
        {
            PluginVersion = "1.0.0.0",
            Uptime = DateTime.UtcNow - _repository.GetStartTime(),
            LastSync = _repository.GetLastSyncTime(),
            NextSync = DateTime.UtcNow.Add(_pollingInterval)
        };

        try
        {
            // Test connections
            var connectionTasks = new List<Task<ConnectionStatusDto>>();

            if (!string.IsNullOrEmpty(_config.SeerrUrl) && !string.IsNullOrEmpty(_config.SeerrApiKey))
            {
                connectionTasks.Add(_seerrService.TestConnectionAsync());
            }

            if (!string.IsNullOrEmpty(_config.RadarrUrl) && !string.IsNullOrEmpty(_config.RadarrApiKey))
            {
                connectionTasks.Add(_radarrService.TestConnectionAsync());
            }

            if (!string.IsNullOrEmpty(_config.SonarrUrl) && !string.IsNullOrEmpty(_config.SonarrApiKey))
            {
                connectionTasks.Add(_sonarrService.TestConnectionAsync());
            }

            var connectionResults = await Task.WhenAll(connectionTasks).ConfigureAwait(false);
            status.Connections.AddRange(connectionResults);

            // Get request statistics
            var allRequests = await _repository.GetAllRequestsAsync().ConfigureAwait(false);
            status.RequestStats.Total = allRequests.Count;
            status.RequestStats.Pending = allRequests.Count(r => r.Status == "pending");
            status.RequestStats.Approved = allRequests.Count(r => r.Status == "approved");
            status.RequestStats.Processing = allRequests.Count(r => r.Status == "downloading");
            status.RequestStats.Available = allRequests.Count(r => r.Status == "available");
            status.RequestStats.Declined = allRequests.Count(r => r.Status == "declined");
            status.RequestStats.ThisMonth = allRequests.Count(r => r.RequestDate >= DateTime.UtcNow.AddDays(-30));

            // Calculate average fulfillment time
            var completedRequests = allRequests.Where(r => r.Status == "available" && r.RequestDate != default).ToList();
            if (completedRequests.Any())
            {
                var totalFulfillmentTime = completedRequests.Sum(r => (r.ModifiedDate - r.RequestDate).TotalHours);
                status.RequestStats.AverageFulfillmentTime = TimeSpan.FromHours(totalFulfillmentTime / completedRequests.Count);
            }

            // Get library statistics
            status.LibraryStats = await _jellyfinSyncService.GetLibraryStatsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
        }

        return status;
    }
}
