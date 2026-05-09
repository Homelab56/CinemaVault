using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.CinemaVault.Api.Dtos;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CinemaVault.Data;

/// <summary>
/// Repository interface for CinemaVault data operations.
/// </summary>
public interface ICinemaVaultRepository
{
    // Request operations
    Task<List<Request>> GetAllRequestsAsync();
    Task<List<Request>> GetRequestsByUserIdAsync(string userId);
    Task<List<Request>> GetRequestsByStatusAsync(string status);
    Task<List<Request>> GetRequestsByTypeAsync(string type);
    Task<Request?> GetRequestAsync(int id);
    Task<Request?> GetRequestByTmdbIdAsync(int tmdbId, string type);
    Task AddRequestAsync(Request request);
    Task UpdateRequestAsync(Request request);
    Task DeleteRequestAsync(int id);
    Task<int> CleanupOldRequestsAsync(DateTime cutoffDate);

    // Watchlist operations
    Task<List<WatchlistItem>> GetWatchlistItemsAsync(string userId, int page, int pageSize);
    Task<WatchlistItem?> GetWatchlistItemAsync(string userId, int tmdbId, string type);
    Task AddWatchlistItemAsync(WatchlistItem item);
    Task UpdateWatchlistItemAsync(WatchlistItem item);
    Task DeleteWatchlistItemAsync(int id);
    Task<List<WatchlistItem>> GetRecentlyAddedWatchlistItemsAsync(int limit);
    Task<List<WatchlistPopularItem>> GetPopularWatchlistItemsAsync(int limit);
    Task<int> CleanupWatchlistAsync(string userId, DateTime cutoffDate);

    // System operations
    Task InitializeAsync();
    DateTime GetStartTime();
    DateTime GetLastSyncTime();
}

/// <summary>
/// SQLite repository implementation for CinemaVault.
/// </summary>
public class CinemaVaultRepository : ICinemaVaultRepository
{
    private readonly string _connectionString;
    private readonly ILogger<CinemaVaultRepository> _logger;
    private readonly DateTime _startTime;

    public CinemaVaultRepository(string dbPath, ILogger<CinemaVaultRepository> logger)
    {
        _connectionString = $"Data Source={dbPath}";
        _logger = logger;
        _startTime = DateTime.UtcNow;
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        var createRequestsTable = @"
            CREATE TABLE IF NOT EXISTS Requests (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ExternalId INTEGER,
                TmdbId INTEGER NOT NULL,
                Type TEXT NOT NULL,
                Title TEXT NOT NULL,
                UserId TEXT NOT NULL,
                UserName TEXT NOT NULL,
                Status TEXT NOT NULL DEFAULT 'pending',
                RequestDate TEXT NOT NULL,
                ModifiedDate TEXT NOT NULL,
                Is4K INTEGER NOT NULL DEFAULT 0,
                PosterPath TEXT,
                Seasons TEXT,
                RootFolder TEXT,
                QualityProfileId INTEGER,
                DownloadProgress REAL DEFAULT 0,
                JellyfinId TEXT,
                ApprovedBy TEXT,
                ApprovedDate TEXT,
                DeclineReason TEXT,
                EstimatedCompletion TEXT
            )";

        var createWatchlistTable = @"
            CREATE TABLE IF NOT EXISTS WatchlistItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                TmdbId INTEGER NOT NULL,
                Type TEXT NOT NULL,
                Title TEXT NOT NULL,
                PosterPath TEXT,
                AddedDate TEXT NOT NULL,
                [Order] INTEGER NOT NULL,
                UNIQUE(UserId, TmdbId, Type)
            )";

        var createIndexes = @"
            CREATE INDEX IF NOT EXISTS idx_requests_tmdbid ON Requests(TmdbId, Type);
            CREATE INDEX IF NOT EXISTS idx_requests_userid ON Requests(UserId);
            CREATE INDEX IF NOT EXISTS idx_requests_status ON Requests(Status);
            CREATE INDEX IF NOT EXISTS idx_watchlist_userid ON WatchlistItems(UserId);
            CREATE INDEX IF NOT EXISTS idx_watchlist_order ON WatchlistItems(UserId, [Order]);";

        await using var transaction = connection.BeginTransaction();
        
        try
        {
            await connection.ExecuteAsync(createRequestsTable).ConfigureAwait(false);
            await connection.ExecuteAsync(createWatchlistTable).ConfigureAwait(false);
            await connection.ExecuteAsync(createIndexes).ConfigureAwait(false);
            
            await transaction.CommitAsync().ConfigureAwait(false);
            _logger.LogInformation("CinemaVault database initialized successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            _logger.LogError(ex, "Error initializing CinemaVault database");
            throw;
        }
    }

    // Request operations
    public async Task<List<Request>> GetAllRequestsAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "SELECT * FROM Requests ORDER BY RequestDate DESC";
        var requests = await connection.QueryAsync<Request>(sql).ConfigureAwait(false);
        
        return requests.ToList();
    }

    public async Task<List<Request>> GetRequestsByUserIdAsync(string userId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "SELECT * FROM Requests WHERE UserId = @UserId ORDER BY RequestDate DESC";
        var requests = await connection.QueryAsync<Request>(sql, new { UserId = userId }).ConfigureAwait(false);
        
        return requests.ToList();
    }

    public async Task<List<Request>> GetRequestsByStatusAsync(string status)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "SELECT * FROM Requests WHERE Status = @Status ORDER BY RequestDate DESC";
        var requests = await connection.QueryAsync<Request>(sql, new { Status = status }).ConfigureAwait(false);
        
        return requests.ToList();
    }

    public async Task<List<Request>> GetRequestsByTypeAsync(string type)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "SELECT * FROM Requests WHERE Type = @Type ORDER BY RequestDate DESC";
        var requests = await connection.QueryAsync<Request>(sql, new { Type = type }).ConfigureAwait(false);
        
        return requests.ToList();
    }

    public async Task<Request?> GetRequestAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "SELECT * FROM Requests WHERE Id = @Id";
        var request = await connection.QueryFirstOrDefaultAsync<Request>(sql, new { Id = id }).ConfigureAwait(false);
        
        return request;
    }

    public async Task<Request?> GetRequestByTmdbIdAsync(int tmdbId, string type)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "SELECT * FROM Requests WHERE TmdbId = @TmdbId AND Type = @Type";
        var request = await connection.QueryFirstOrDefaultAsync<Request>(sql, new { TmdbId = tmdbId, Type = type }).ConfigureAwait(false);
        
        return request;
    }

    public async Task AddRequestAsync(Request request)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = @"
            INSERT INTO Requests (
                ExternalId, TmdbId, Type, Title, UserId, UserName, Status,
                RequestDate, ModifiedDate, Is4K, PosterPath, Seasons,
                RootFolder, QualityProfileId, DownloadProgress, JellyfinId,
                ApprovedBy, ApprovedDate, DeclineReason, EstimatedCompletion
            ) VALUES (
                @ExternalId, @TmdbId, @Type, @Title, @UserId, @UserName, @Status,
                @RequestDate, @ModifiedDate, @Is4K, @PosterPath, @Seasons,
                @RootFolder, @QualityProfileId, @DownloadProgress, @JellyfinId,
                @ApprovedBy, @ApprovedDate, @DeclineReason, @EstimatedCompletion
            )";

        await connection.ExecuteAsync(sql, request).ConfigureAwait(false);
    }

    public async Task UpdateRequestAsync(Request request)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = @"
            UPDATE Requests SET
                Status = @Status,
                ModifiedDate = @ModifiedDate,
                DownloadProgress = @DownloadProgress,
                JellyfinId = @JellyfinId,
                ApprovedBy = @ApprovedBy,
                ApprovedDate = @ApprovedDate,
                DeclineReason = @DeclineReason,
                EstimatedCompletion = @EstimatedCompletion
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, request).ConfigureAwait(false);
    }

    public async Task DeleteRequestAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "DELETE FROM Requests WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id }).ConfigureAwait(false);
    }

    public async Task<int> CleanupOldRequestsAsync(DateTime cutoffDate)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "DELETE FROM Requests WHERE Status IN ('available', 'declined') AND ModifiedDate < @CutoffDate";
        var result = await connection.ExecuteAsync(sql, new { CutoffDate = cutoffDate.ToString("yyyy-MM-dd HH:mm:ss") }).ConfigureAwait(false);
        
        return result;
    }

    // Watchlist operations
    public async Task<List<WatchlistItem>> GetWatchlistItemsAsync(string userId, int page, int pageSize)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = @"
            SELECT * FROM WatchlistItems 
            WHERE UserId = @UserId 
            ORDER BY [Order] ASC, AddedDate DESC
            LIMIT @Limit OFFSET @Offset";

        var offset = (page - 1) * pageSize;
        var items = await connection.QueryAsync<WatchlistItem>(sql, new 
        { 
            UserId = userId, 
            Limit = pageSize, 
            Offset = offset 
        }).ConfigureAwait(false);
        
        return items.ToList();
    }

    public async Task<WatchlistItem?> GetWatchlistItemAsync(string userId, int tmdbId, string type)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "SELECT * FROM WatchlistItems WHERE UserId = @UserId AND TmdbId = @TmdbId AND Type = @Type";
        var item = await connection.QueryFirstOrDefaultAsync<WatchlistItem>(sql, new 
        { 
            UserId = userId, 
            TmdbId = tmdbId, 
            Type = type 
        }).ConfigureAwait(false);
        
        return item;
    }

    public async Task AddWatchlistItemAsync(WatchlistItem item)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = @"
            INSERT INTO WatchlistItems (UserId, TmdbId, Type, Title, PosterPath, AddedDate, [Order])
            VALUES (@UserId, @TmdbId, @Type, @Title, @PosterPath, @AddedDate, @Order)";

        await connection.ExecuteAsync(sql, item).ConfigureAwait(false);
    }

    public async Task UpdateWatchlistItemAsync(WatchlistItem item)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "UPDATE WatchlistItems SET [Order] = @Order WHERE Id = @Id";
        await connection.ExecuteAsync(sql, item).ConfigureAwait(false);
    }

    public async Task DeleteWatchlistItemAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = "DELETE FROM WatchlistItems WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id }).ConfigureAwait(false);
    }

    public async Task<List<WatchlistItem>> GetRecentlyAddedWatchlistItemsAsync(int limit)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = @"
            SELECT * FROM WatchlistItems 
            ORDER BY AddedDate DESC 
            LIMIT @Limit";

        var items = await connection.QueryAsync<WatchlistItem>(sql, new { Limit = limit }).ConfigureAwait(false);
        
        return items.ToList();
    }

    public async Task<List<WatchlistPopularItem>> GetPopularWatchlistItemsAsync(int limit)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        const string sql = @"
            SELECT TmdbId, Type, Title, PosterPath, COUNT(*) as UserCount
            FROM WatchlistItems
            GROUP BY TmdbId, Type, Title, PosterPath
            ORDER BY UserCount DESC
            LIMIT @Limit";

        var items = await connection.QueryAsync<WatchlistPopularItem>(sql, new { Limit = limit }).ConfigureAwait(false);
        
        return items.ToList();
    }

    public async Task<int> CleanupWatchlistAsync(string userId, DateTime cutoffDate)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        var sql = "DELETE FROM WatchlistItems WHERE AddedDate < @CutoffDate";
        var parameters = new { CutoffDate = cutoffDate.ToString("yyyy-MM-dd HH:mm:ss") };

        if (!string.IsNullOrEmpty(userId))
        {
            sql = "DELETE FROM WatchlistItems WHERE UserId = @UserId AND AddedDate < @CutoffDate";
            parameters = new { UserId = userId, CutoffDate = cutoffDate.ToString("yyyy-MM-dd HH:mm:ss") };
        }

        var result = await connection.ExecuteAsync(sql, parameters).ConfigureAwait(false);
        return result;
    }

    // System operations
    public DateTime GetStartTime()
    {
        return _startTime;
    }

    public DateTime GetLastSyncTime()
    {
        // In a real implementation, this would be stored in the database
        // For now, return the start time
        return _startTime;
    }
}

/// <summary>
/// Request entity model.
/// </summary>
public class Request
{
    public int Id { get; set; }
    public int? ExternalId { get; set; }
    public int TmdbId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime RequestDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool Is4K { get; set; }
    public string? PosterPath { get; set; }
    public string? Seasons { get; set; }
    public string? RootFolder { get; set; }
    public int? QualityProfileId { get; set; }
    public double DownloadProgress { get; set; }
    public string? JellyfinId { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? DeclineReason { get; set; }
    public DateTime? EstimatedCompletion { get; set; }
}

/// <summary>
/// Watchlist item entity model.
/// </summary>
public class WatchlistItem
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
/// Popular watchlist item entity model.
/// </summary>
public class WatchlistPopularItem
{
    public int TmdbId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public int UserCount { get; set; }
}

/// <summary>
/// Extension methods for SQLite operations.
/// </summary>
public static class SqliteExtensions
{
    public static async Task<int> ExecuteAsync(this SqliteConnection connection, string sql, object? parameters = null)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        if (parameters != null)
        {
            foreach (var prop in parameters.GetType().GetProperties())
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{prop.Name}";
                parameter.Value = prop.GetValue(parameters) ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public static async Task<T?> QueryFirstOrDefaultAsync<T>(this SqliteConnection connection, string sql, object? parameters = null)
    {
        var results = await QueryAsync<T>(connection, sql, parameters).ConfigureAwait(false);
        return results.FirstOrDefault();
    }

    public static async Task<IEnumerable<T>> QueryAsync<T>(this SqliteConnection connection, string sql, object? parameters = null)
    {
        var results = new List<T>();
        
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        if (parameters != null)
        {
            foreach (var prop in parameters.GetType().GetProperties())
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{prop.Name}";
                parameter.Value = prop.GetValue(parameters) ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var item = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties();
            
            foreach (var prop in properties)
            {
                if (!reader.IsDBNull(reader.GetOrdinal(prop.Name)))
                {
                    var value = reader[prop.Name];
                    if (value != DBNull.Value)
                    {
                        prop.SetValue(item, value);
                    }
                }
            }
            
            results.Add(item);
        }

        return results;
    }
}
