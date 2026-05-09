using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.CinemaVault.Api.Dtos;
using Jellyfin.Plugin.CinemaVault.Data;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaVault.Services;

/// <summary>
/// Service for managing user watchlists (My List).
/// </summary>
public class WatchlistService
{
    private readonly ICinemaVaultRepository _repository;
    private readonly ILogger<WatchlistService> _logger;

    public WatchlistService(ICinemaVaultRepository repository, ILogger<WatchlistService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Gets a user's watchlist.
    /// </summary>
    public async Task<List<WatchlistItemDto>> GetUserWatchlistAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var items = await _repository.GetWatchlistItemsAsync(userId, page, pageSize).ConfigureAwait(false);
            return items.Select(MapToWatchlistItem).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting watchlist for user {UserId}", userId);
            return new List<WatchlistItemDto>();
        }
    }

    /// <summary>
    /// Adds an item to a user's watchlist.
    /// </summary>
    public async Task<bool> AddToWatchlistAsync(string userId, int tmdbId, string type, string title, string? posterPath = null)
    {
        try
        {
            var existingItem = await _repository.GetWatchlistItemAsync(userId, tmdbId, type).ConfigureAwait(false);
            if (existingItem != null)
            {
                return true; // Already in watchlist
            }

            var watchlistItem = new WatchlistItem
            {
                UserId = userId,
                TmdbId = tmdbId,
                Type = type,
                Title = title,
                PosterPath = posterPath,
                AddedDate = DateTime.UtcNow,
                Order = await GetNextOrderAsync(userId).ConfigureAwait(false)
            };

            await _repository.AddWatchlistItemAsync(watchlistItem).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to watchlist for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Removes an item from a user's watchlist.
    /// </summary>
    public async Task<bool> RemoveFromWatchlistAsync(string userId, int tmdbId, string type)
    {
        try
        {
            var item = await _repository.GetWatchlistItemAsync(userId, tmdbId, type).ConfigureAwait(false);
            if (item == null)
            {
                return false; // Item not found
            }

            await _repository.DeleteWatchlistItemAsync(item.Id).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from watchlist for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Checks if an item is in a user's watchlist.
    /// </summary>
    public async Task<bool> IsInWatchlistAsync(string userId, int tmdbId, string type)
    {
        try
        {
            var item = await _repository.GetWatchlistItemAsync(userId, tmdbId, type).ConfigureAwait(false);
            return item != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking watchlist status for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Updates watchlist item order.
    /// </summary>
    public async Task<bool> UpdateWatchlistOrderAsync(string userId, List<WatchlistOrderDto> orders)
    {
        try
        {
            foreach (var order in orders)
            {
                var item = await _repository.GetWatchlistItemAsync(userId, order.TmdbId, order.Type).ConfigureAwait(false);
                if (item != null)
                {
                    item.Order = order.Order;
                    await _repository.UpdateWatchlistItemAsync(item).ConfigureAwait(false);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating watchlist order for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Gets watchlist statistics for a user.
    /// </summary>
    public async Task<WatchlistStatsDto> GetWatchlistStatsAsync(string userId)
    {
        try
        {
            var items = await _repository.GetWatchlistItemsAsync(userId, 1, int.MaxValue).ConfigureAwait(false);
            
            var stats = new WatchlistStatsDto
            {
                TotalItems = items.Count,
                Movies = items.Count(i => i.Type == "movie"),
                TvShows = items.Count(i => i.Type == "tv"),
                AddedThisWeek = items.Count(i => i.AddedDate >= DateTime.UtcNow.AddDays(-7)),
                AddedThisMonth = items.Count(i => i.AddedDate >= DateTime.UtcNow.AddDays(-30))
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting watchlist stats for user {UserId}", userId);
            return new WatchlistStatsDto();
        }
    }

    /// <summary>
    /// Exports a user's watchlist to CSV format.
    /// </summary>
    public async Task<string> ExportWatchlistToCsvAsync(string userId)
    {
        try
        {
            var items = await _repository.GetWatchlistItemsAsync(userId, 1, int.MaxValue).ConfigureAwait(false);
            
            var csv = new List<string>
            {
                "Title,Type,TMDB ID,Added Date,Order"
            };

            foreach (var item in items.OrderBy(i => i.Order))
            {
                csv.Add($"\"{item.Title}\",{item.Type},{item.TmdbId},{item.AddedDate:yyyy-MM-dd HH:mm:ss},{item.Order}");
            }

            return string.Join("\n", csv);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting watchlist for user {UserId}", userId);
            return string.Empty;
        }
    }

    /// <summary>
    /// Imports watchlist from CSV format.
    /// </summary>
    public async Task<WatchlistImportResultDto> ImportWatchlistFromCsvAsync(string userId, string csvContent)
    {
        var result = new WatchlistImportResultDto();
        
        try
        {
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                result.ErrorMessage = "CSV file is empty or invalid";
                return result;
            }

            // Skip header line
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var parts = line.Split(',');
                if (parts.Length >= 4)
                {
                    var title = parts[0].Trim('"');
                    var type = parts[1].Trim();
                    var tmdbIdStr = parts[2].Trim();
                    var addedDateStr = parts[3].Trim();

                    if (int.TryParse(tmdbIdStr, out var tmdbId) && 
                        (type == "movie" || type == "tv"))
                    {
                        var success = await AddToWatchlistAsync(userId, tmdbId, type, title).ConfigureAwait(false);
                        if (success)
                        {
                            result.ImportedCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                        }
                    }
                    else
                    {
                        result.SkippedCount++;
                    }
                }
                else
                {
                    result.SkippedCount++;
                }
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing watchlist for user {UserId}", userId);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Gets recently added items to watchlist across all users (for admin).
    /// </summary>
    public async Task<List<WatchlistItemDto>> GetRecentlyAddedWatchlistItemsAsync(int limit = 20)
    {
        try
        {
            var items = await _repository.GetRecentlyAddedWatchlistItemsAsync(limit).ConfigureAwait(false);
            return items.Select(MapToWatchlistItem).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently added watchlist items");
            return new List<WatchlistItemDto>();
        }
    }

    /// <summary>
    /// Gets popular items across all watchlists (for admin).
    /// </summary>
    public async Task<List<WatchlistPopularItemDto>> GetPopularWatchlistItemsAsync(int limit = 20)
    {
        try
        {
            var items = await _repository.GetPopularWatchlistItemsAsync(limit).ConfigureAwait(false);
            return items.Select(i => new WatchlistPopularItemDto
            {
                TmdbId = i.TmdbId,
                Type = i.Type,
                Title = i.Title,
                PosterPath = i.PosterPath,
                UserCount = i.UserCount
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular watchlist items");
            return new List<WatchlistPopularItemDto>();
        }
    }

    /// <summary>
    /// Cleans up old watchlist items for a user (removes items older than specified days).
    /// </summary>
    public async Task<int> CleanupWatchlistAsync(string userId, int olderThanDays = 365)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            return await _repository.CleanupWatchlistAsync(userId, cutoffDate).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up watchlist for user {UserId}", userId);
            return 0;
        }
    }

    private async Task<int> GetNextOrderAsync(string userId)
    {
        try
        {
            var items = await _repository.GetWatchlistItemsAsync(userId, 1, int.MaxValue).ConfigureAwait(false);
            return items.Any() ? items.Max(i => i.Order) + 1 : 1;
        }
        catch
        {
            return 1;
        }
    }

    private WatchlistItemDto MapToWatchlistItem(WatchlistItem item)
    {
        return new WatchlistItemDto
        {
            Id = item.Id,
            UserId = item.UserId,
            TmdbId = item.TmdbId,
            Type = item.Type,
            Title = item.Title,
            PosterPath = item.PosterPath,
            AddedDate = item.AddedDate,
            Order = item.Order
        };
    }
}

/// <summary>
/// Watchlist item DTO.
/// </summary>
public class WatchlistItemDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int TmdbId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public DateTime AddedDate { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// Watchlist order DTO.
/// </summary>
public class WatchlistOrderDto
{
    public int TmdbId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
}

/// <summary>
/// Watchlist statistics DTO.
/// </summary>
public class WatchlistStatsDto
{
    public int TotalItems { get; set; }
    public int Movies { get; set; }
    public int TvShows { get; set; }
    public int AddedThisWeek { get; set; }
    public int AddedThisMonth { get; set; }
}

/// <summary>
/// Watchlist import result DTO.
/// </summary>
public class WatchlistImportResultDto
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Popular watchlist item DTO.
/// </summary>
public class WatchlistPopularItemDto
{
    public int TmdbId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public int UserCount { get; set; }
}
