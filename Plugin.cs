using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Jellyfin.Plugin.CinemaVault.Services;
using Jellyfin.Plugin.CinemaVault.Data;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.CinemaVault;

/// <summary>
/// Main plugin class for CinemaVault.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "CinemaVault";

    /// <inheritdoc />
    public override Guid Id => new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    /// <inheritdoc />
    public override string Description => "Netflix-style media discovery with Seerr integration";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "CinemaVault",
                EmbeddedResourcePath = "Jellyfin.Plugin.CinemaVault.Web.index.html"
            }
        };
    }

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection serviceCollection)
    {
        // Register repository
        serviceCollection.AddSingleton<ICinemaVaultRepository>(provider =>
        {
            var config = provider.GetRequiredService<IApplicationPaths>();
            var dbPath = Path.Combine(config.DataPath, "cinemavault.db");
            return new CinemaVaultRepository(dbPath);
        });

        // Register HTTP client factory
        serviceCollection.AddHttpClient();

        // Register services
        serviceCollection.AddSingleton<SeerrService>();
        serviceCollection.AddSingleton<RadarrService>();
        serviceCollection.AddSingleton<SonarrService>();
        serviceCollection.AddSingleton<JellyfinSyncService>();
        serviceCollection.AddSingleton<WatchlistService>();

        // Register background polling service
        serviceCollection.AddHostedService<PollingService>();
    }
}
