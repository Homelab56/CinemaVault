using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CinemaVault.Api.Dtos;

/// <summary>
/// Represents status information.
/// </summary>
public class StatusDto
{
    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status type (success/error/info/warning).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "info";

    /// <summary>
    /// Gets or sets the status code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets additional data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents connection status for external services.
/// </summary>
public class ConnectionStatusDto
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the connection is successful.
    /// </summary>
    [JsonPropertyName("connected")]
    public bool Connected { get; set; }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the version information.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets additional service-specific data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// Represents system status and statistics.
/// </summary>
public class SystemStatusDto
{
    /// <summary>
    /// Gets or sets the plugin version.
    /// </summary>
    [JsonPropertyName("pluginVersion")]
    public string PluginVersion { get; set; } = "1.0.0.0";

    /// <summary>
    /// Gets or sets the Jellyfin version.
    /// </summary>
    [JsonPropertyName("jellyfinVersion")]
    public string JellyfinVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection statuses.
    /// </summary>
    [JsonPropertyName("connections")]
    public List<ConnectionStatusDto> Connections { get; set; } = new();

    /// <summary>
    /// Gets or sets the request statistics.
    /// </summary>
    [JsonPropertyName("requestStats")]
    public RequestStatsDto RequestStats { get; set; } = new();

    /// <summary>
    /// Gets or sets the library statistics.
    /// </summary>
    [JsonPropertyName("libraryStats")]
    public LibraryStatsDto LibraryStats { get; set; } = new();

    /// <summary>
    /// Gets or sets the user statistics.
    /// </summary>
    [JsonPropertyName("userStats")]
    public UserStatsDto UserStats { get; set; } = new();

    /// <summary>
    /// Gets or sets the system uptime.
    /// </summary>
    [JsonPropertyName("uptime")]
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets or sets the last background sync time.
    /// </summary>
    [JsonPropertyName("lastSync")]
    public DateTime? LastSync { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled sync time.
    /// </summary>
    [JsonPropertyName="nextSync"]
    public DateTime? NextSync { get; set; }
}

/// <summary>
/// Represents request statistics.
/// </summary>
public class RequestStatsDto
{
    /// <summary>
    /// Gets or sets the total number of requests.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the number of pending requests.
    /// </summary>
    [JsonPropertyName("pending")]
    public int Pending { get; set; }

    /// <summary>
    /// Gets or sets the number of approved requests.
    /// </summary>
    [JsonPropertyName("approved")]
    public int Approved { get; set; }

    /// <summary>
    /// Gets or sets the number of processing requests.
    /// </summary>
    [JsonPropertyName("processing")]
    public int Processing { get; set; }

    /// <summary>
    /// Gets or sets the number of available requests.
    /// </summary>
    [JsonPropertyName("available")]
    public int Available { get; set; }

    /// <summary>
    /// Gets or sets the number of declined requests.
    /// </summary>
    [JsonPropertyName("declined")]
    public int Declined { get; set; }

    /// <summary>
    /// Gets or sets the number of requests this month.
    /// </summary>
    [JsonPropertyName("thisMonth")]
    public int ThisMonth { get; set; }

    /// <summary>
    /// Gets or sets the average fulfillment time.
    /// </summary>
    [JsonPropertyName("averageFulfillmentTime")]
    public TimeSpan? AverageFulfillmentTime { get; set; }
}

/// <summary>
/// Represents library statistics.
/// </summary>
public class LibraryStatsDto
{
    /// <summary>
    /// Gets or sets the total number of movies.
    /// </summary>
    [JsonPropertyName("totalMovies")]
    public int TotalMovies { get; set; }

    /// <summary>
    /// Gets or sets the total number of TV shows.
    /// </summary>
    [JsonPropertyName("totalShows")]
    public int TotalShows { get; set; }

    /// <summary>
    /// Gets or sets the total number of episodes.
    /// </summary>
    [JsonPropertyName("totalEpisodes")]
    public int TotalEpisodes { get; set; }

    /// <summary>
    /// Gets or sets the total library size in GB.
    /// </summary>
    [JsonPropertyName="totalSizeGB"]
    public double TotalSizeGB { get; set; }

    /// <summary>
    /// Gets or sets the number of items added this week.
    /// </summary>
    [JsonPropertyName="addedThisWeek"]
    public int AddedThisWeek { get; set; }

    /// <summary>
    /// Gets or sets the number of items added this month.
    /// </summary>
    [JsonPropertyName="addedThisMonth"]
    public int AddedThisMonth { get; set; }

    /// <summary>
    /// Gets or sets the most recently added items.
    /// </summary>
    [JsonPropertyName="recentlyAdded"]
    public List<ContentItemDto> RecentlyAdded { get; set; } = new();
}

/// <summary>
/// Represents user statistics.
/// </summary>
public class UserStatsDto
{
    /// <summary>
    /// Gets or sets the total number of active users.
    /// </summary>
    [JsonPropertyName("activeUsers")]
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Gets or sets the number of users with requests.
    /// </summary>
    [JsonPropertyName("usersWithRequests")]
    public int UsersWithRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of users with watchlists.
    /// </summary>
    [JsonPropertyName("usersWithWatchlists")]
    public int UsersWithWatchlists { get; set; }

    /// <summary>
    /// Gets or sets the top requesters.
    /// </summary>
    [JsonPropertyName="topRequesters"]
    public List<TopUserDto> TopRequesters { get; set; } = new();

    /// <summary>
    /// Gets or sets the most active users.
    /// </summary>
    [JsonPropertyName="mostActive")]
    public List<TopUserDto> MostActive { get; set; } = new();
}

/// <summary>
/// Represents a top user statistic.
/// </summary>
public class TopUserDto
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets the user avatar.
    /// </summary>
    [JsonPropertyName="avatar")]
    public string? Avatar { get; set; }
}
