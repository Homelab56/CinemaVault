using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CinemaVault;

/// <summary>
/// Plugin configuration class for CinemaVault.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the Seerr URL.
    /// </summary>
    public string SeerrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Seerr API key.
    /// </summary>
    public string SeerrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Radarr URL.
    /// </summary>
    public string RadarrUrl { get; set; } = "http://localhost:7878";

    /// <summary>
    /// Gets or sets the Radarr API key.
    /// </summary>
    public string RadarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Radarr quality profile ID.
    /// </summary>
    public int RadarrQualityProfileId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the Radarr root folder.
    /// </summary>
    public string RadarrRootFolder { get; set; } = "/media/movies";

    /// <summary>
    /// Gets or sets the Sonarr URL.
    /// </summary>
    public string SonarrUrl { get; set; } = "http://localhost:8989";

    /// <summary>
    /// Gets or sets the Sonarr API key.
    /// </summary>
    public string SonarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Sonarr quality profile ID.
    /// </summary>
    public int SonarrQualityProfileId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the Sonarr root folder.
    /// </summary>
    public string SonarrRootFolder { get; set; } = "/media/series";

    /// <summary>
    /// Gets or sets a value indicating whether 4K requests are enabled.
    /// </summary>
    public bool Enable4KRequests { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether user requests are allowed.
    /// </summary>
    public bool AllowUserRequests { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum requests per user.
    /// </summary>
    public int MaxRequestsPerUser { get; set; } = 20;

    /// <summary>
    /// Gets or sets the polling interval in minutes.
    /// </summary>
    public int PollingIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether adult content is shown.
    /// </summary>
    public bool ShowAdultContent { get; set; } = false;

    /// <summary>
    /// Gets or sets the default language.
    /// </summary>
    public string DefaultLanguage { get; set; } = "en-US";

    /// <summary>
    /// Gets or sets a value indicating whether the hero banner is enabled.
    /// </summary>
    public bool EnableHeroBanner { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether auto-play is enabled for hero banner.
    /// </summary>
    public bool EnableAutoPlay { get; set; } = true;

    /// <summary>
    /// Gets or sets the hero rotation seconds.
    /// </summary>
    public int HeroRotationSeconds { get; set; } = 8;

    /// <summary>
    /// Gets or sets the accent color.
    /// </summary>
    public string AccentColor { get; set; } = "#6c63ff";
}
