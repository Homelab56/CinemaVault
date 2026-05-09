# CinemaVault 🎬

[![Release](https://img.shields.io/github/release/CinemaVault/CinemaVault.svg)](https://github.com/CinemaVault/CinemaVault/releases)
[![License](https://img.shields.io/github/license/CinemaVault/CinemaVault.svg)](LICENSE)
[![Jellyfin](https://img.shields.io/badge/Jellyfin-10.11.x-blue.svg)](https://jellyfin.org/)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)

> Transform your Jellyfin experience into a Netflix/Disney+-style interface with native Seerr integration for content discovery and requesting.

![CinemaVault Preview](https://github.com/CinemaVault/CinemaVault/raw/main/assets/preview.png)

## ✨ Features

### 🎨 Netflix-Style Interface
- **Hero Banner**: Rotating featured content with auto-play
- **Carousels**: Horizontal scrolling content rows (Netflix-style)
- **Responsive Design**: Optimized for desktop, tablet, and mobile
- **Dark Theme**: Beautiful dark theme with customizable accent colors
- **Smooth Animations**: Modern transitions and micro-interactions

### 🔍 Content Discovery
- **Advanced Search**: Real-time search with suggestions and filters
- **Content Details**: Rich modal with trailers, cast, and recommendations
- **Genre Browsing**: Browse by genre with smart filtering
- **Trending Content**: Discover what's popular and trending

### 📺 Seerr Integration
- **Native Integration**: Seamless Seerr/Jellyseerr integration
- **Content Requests**: Request movies and TV shows directly from the UI
- **Request Tracking**: Monitor request status in real-time
- **User Permissions**: Granular control over who can request content

### 🎬 Media Management
- **Radarr Integration**: Automatic movie management
- **Sonarr Integration**: Automatic TV show management
- **Download Status**: Track download progress and availability
- **Quality Profiles**: Support for multiple quality profiles

### 👤 User Features
- **Personal Watchlist**: "My List" functionality
- **Continue Watching**: Pick up where you left off
- **User Profiles**: Individual user preferences and watchlists
- **Request History**: Track all your requests

## 🚀 Quick Start

### Prerequisites
- **Jellyfin**: 10.11.x or later
- **.NET Runtime**: 9.0 or later
- **Seerr/Jellyseerr**: Instance for content discovery and requests
- **Radarr**: Optional, for movie management
- **Sonarr**: Optional, for TV show management

### Installation

#### Option 1: Automatic Installation (Recommended)
1. Go to **Dashboard → Plugins → Catalog**
2. Search for "CinemaVault"
3. Click **Install** and restart Jellyfin

#### Option 2: Manual Installation
1. Download the latest release from [Releases](https://github.com/CinemaVault/CinemaVault/releases)
2. Extract the zip file to your Jellyfin plugins directory:
   ```
   /jellyfin/config/plugins/CinemaVault/
   ```
3. Restart Jellyfin
4. Configure in **Dashboard → Plugins → CinemaVault**

### Initial Configuration

1. **Seerr Connection**:
   - URL: `http://your-seerr-server:5056`
   - API Key: Get from Seerr settings → General

2. **Radarr/Sonarr** (Optional):
   - URLs: `http://localhost:7878` (Radarr), `http://localhost:8989` (Sonarr)
   - API Keys: Get from respective service settings
   - Quality Profiles: Select appropriate profiles

3. **User Permissions**:
   - Enable/disable user requests
   - Set request limits
   - Configure 4K request options

## ⚙️ Configuration

### Seerr Settings
| Setting | Description | Default |
|---------|-------------|----------|
| URL | Seerr server URL | `http://localhost:5056` |
| API Key | Seerr API key | Required |
| Auto-approve | Automatically approve requests | `false` |

### Radarr Settings
| Setting | Description | Default |
|---------|-------------|----------|
| URL | Radarr server URL | `http://localhost:7878` |
| API Key | Radarr API key | Required |
| Quality Profile | Default quality profile | `1` |
| Root Folder | Download location | `/media/movies` |

### Sonarr Settings
| Setting | Description | Default |
|---------|-------------|----------|
| URL | Sonarr server URL | `http://localhost:8989` |
| API Key | Sonarr API key | Required |
| Quality Profile | Default quality profile | `1` |
| Root Folder | Download location | `/media/series` |

### Display Settings
| Setting | Description | Default |
|---------|-------------|----------|
| Accent Color | UI accent color | `#6c63ff` |
| Hero Banner | Enable hero banner | `true` |
| Auto-play | Auto-play hero content | `true` |
| Adult Content | Show adult content | `false` |

## 🏗️ Architecture

### Backend (C#)
- **Plugin Framework**: Jellyfin SDK with .NET 9
- **API Controller**: RESTful API under `/CinemaVault/` prefix
- **Services**: Modular services for Seerr, Radarr, Sonarr, and Jellyfin
- **Data Layer**: SQLite repository for requests and watchlists
- **Dependency Injection**: Modern DI pattern with IHttpClientFactory

### Frontend (JavaScript)
- **Vanilla JS**: No frameworks, pure JavaScript
- **Component System**: Modular component architecture
- **CSS Grid/Flexbox**: Modern layout techniques
- **Intersection Observer**: Lazy loading and performance optimization
- **Service Worker**: Caching and offline support

### Key Components
```
├── Backend (C#)
│   ├── Controllers/
│   │   └── CinemaVaultController.cs
│   ├── Services/
│   │   ├── SeerrService.cs
│   │   ├── RadarrService.cs
│   │   ├── SonarrService.cs
│   │   ├── JellyfinSyncService.cs
│   │   ├── WatchlistService.cs
│   │   └── PollingService.cs
│   ├── Data/
│   │   └── CinemaVaultRepository.cs
│   └── DTOs/
│       ├── ContentItemDto.cs
│       ├── RequestDto.cs
│       ├── SearchResultDto.cs
│       ├── HeroItemDto.cs
│       └── StatusDto.cs
└── Frontend (JavaScript)
    ├── Utils/
    │   ├── api.js
    │   ├── jellyfin.js
    │   ├── cache.js
    │   └── router.js
    ├── Components/
    │   ├── navbar.js
    │   ├── hero.js
    │   ├── carousel.js
    │   ├── card.js
    │   ├── modal.js
    │   ├── search.js
    │   └── toast.js
    └── Assets/
        ├── cinemavault.js
        ├── cinemavault.css
        └── bootstrap.js
```

## 🌐 API Reference

### Discovery Endpoints
- `GET /CinemaVault/discover/trending` - Get trending content
- `GET /CinemaVault/discover/popular` - Get popular content
- `GET /CinemaVault/discover/toprated` - Get top-rated content
- `GET /CinemaVault/discover/nowplaying` - Get now playing movies
- `GET /CinemaVault/discover/genre/{id}` - Get content by genre

### Search Endpoints
- `GET /CinemaVault/search/{query}` - Search for content
- `GET /CinemaVault/search/combined/{query}` - Combined search results

### Content Endpoints
- `GET /CinemaVault/content/details/{id}` - Get content details
- `GET /CinemaVault/content/recommendations/{id}` - Get recommendations
- `GET /CinemaVault/content/videos/{id}` - Get videos/trailers

### Library Endpoints
- `GET /CinemaVault/library/status/{ids}` - Get library status
- `GET /CinemaVault/library/recent` - Get recently added
- `GET /CinemaVault/library/resume` - Get continue watching

### Request Endpoints
- `POST /CinemaVault/requests/create` - Create new request
- `GET /CinemaVault/requests/all` - Get all requests
- `PUT /CinemaVault/requests/update/{id}` - Update request
- `DELETE /CinemaVault/requests/delete/{id}` - Delete request

### Watchlist Endpoints
- `GET /CinemaVault/watchlist/get` - Get user watchlist
- `POST /CinemaVault/watchlist/add` - Add to watchlist
- `DELETE /CinemaVault/watchlist/remove/{id}` - Remove from watchlist

## 🔧 Development

### Building from Source

#### Prerequisites
- .NET 9 SDK
- Node.js (for development tools)
- Git

#### Steps
```bash
# Clone the repository
git clone https://github.com/CinemaVault/CinemaVault.git
cd CinemaVault

# Build the plugin
dotnet build -c Release

# Run tests
dotnet test

# Package for release
dotnet pack -c Release
```

### Project Structure
```
CinemaVault/
├── CinemaVault.csproj          # Project file
├── Plugin.cs                   # Main plugin class
├── PluginConfiguration.cs       # Configuration model
├── Api/                       # API layer
│   ├── Controllers/
│   └── DTOs/
├── Services/                   # Business logic
├── Data/                      # Data access
├── Configuration/              # Configuration UI
└── Web/                      # Frontend assets
    ├── assets/
    │   ├── utils/
    │   ├── components/
    │   └── css/
    ├── bootstrap.js
    └── index.html
```

### Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

### Code Style
- **C#**: Follow Microsoft coding conventions
- **JavaScript**: Use modern ES6+ features, semicolons required
- **CSS**: BEM methodology for class names
- **Documentation**: XML comments for C#, JSDoc for JavaScript

## 🐛 Troubleshooting

### Common Issues

#### Plugin Not Loading
- **Check**: Jellyfin version compatibility (10.11.x required)
- **Verify**: .NET 9 runtime is installed
- **Check**: Plugin directory permissions
- **Solution**: Restart Jellyfin service

#### Seerr Connection Failed
- **Check**: Seerr server URL and API key
- **Verify**: Network connectivity between servers
- **Check**: Seerr API version compatibility
- **Solution**: Test connection in plugin settings

#### Radarr/Sonarr Not Working
- **Check**: Service URLs and API keys
- **Verify**: Quality profiles exist
- **Check**: Root folder permissions
- **Solution**: Test connections individually

#### UI Not Loading
- **Check**: Browser console for JavaScript errors
- **Verify**: CSS files are loading properly
- **Check**: Network connectivity
- **Solution**: Clear browser cache and restart

### Debug Mode
Enable debug logging in Jellyfin:
1. Go to **Dashboard → Logging**
2. Set log level to **Debug**
3. Filter by **CinemaVault**
4. Restart Jellyfin
5. Check logs in `/jellyfin/log/`

### Performance Optimization
- **Lazy Loading**: Images and content loaded as needed
- **Caching**: API responses cached for 5 minutes
- **Debouncing**: Search and scroll events debounced
- **Virtual Scrolling**: Large lists use virtual scrolling

## 📚 API Documentation

### Authentication
All API endpoints require Jellyfin authentication. Include the Jellyfin authentication token in requests.

### Rate Limiting
- **Seerr API**: 100 requests per minute
- **Radarr/Sonarr**: 60 requests per minute
- **Jellyfin API**: No explicit limit

### Error Handling
```json
{
  "error": {
    "code": "SEERR_CONNECTION_FAILED",
    "message": "Failed to connect to Seerr server",
    "details": "Connection timeout after 30 seconds"
  }
}
```

### Response Format
```json
{
  "data": {
    "results": [...],
    "total": 100,
    "page": 1,
    "totalPages": 10
  },
  "success": true,
  "message": "Request successful"
}
```

## 🔒 Security

### API Security
- **Authentication**: All endpoints require Jellyfin user authentication
- **Authorization**: Admin-only endpoints protected
- **Input Validation**: All inputs validated and sanitized
- **Rate Limiting**: Built-in rate limiting for external APIs

### Data Privacy
- **Local Storage**: All data stored locally in SQLite
- **No Tracking**: No analytics or tracking
- **Minimal Data**: Only essential data collected
- **GDPR Compliant**: Data handling follows GDPR principles

### Security Recommendations
- Use HTTPS for all external services
- Rotate API keys regularly
- Limit user permissions appropriately
- Keep Jellyfin updated
- Monitor access logs

## 🚀 Deployment

### Docker Deployment
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY ./bin/Release/net9.0 ./
EXPOSE 80
ENTRYPOINT ["dotnet", "CinemaVault.dll"]
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: cinemavault
spec:
  replicas: 1
  selector:
    matchLabels:
      app: cinemavault
  template:
    metadata:
      labels:
        app: cinemavault
    spec:
      containers:
      - name: cinemavault
        image: cinemavault:latest
        ports:
        - containerPort: 80
```

### Environment Variables
```bash
# Seerr Configuration
SEERR_URL=http://seerr:5056
SEERR_API_KEY=your-api-key

# Radarr Configuration
RADARR_URL=http://radarr:7878
RADARR_API_KEY=your-api-key

# Sonarr Configuration
SONARR_URL=http://sonarr:8989
SONARR_API_KEY=your-api-key
```

## 📊 Monitoring

### Health Checks
- `GET /CinemaVault/health` - Plugin health status
- `GET /CinemaVault/health/services` - External service status

### Metrics
- Request count and response times
- External API connection status
- Database performance metrics
- User activity statistics

### Logging
- Structured logging with Serilog
- Configurable log levels
- File-based logging
- Integration with Jellyfin logging

## 🤝 Support

### Getting Help
- **Documentation**: [Wiki](https://github.com/CinemaVault/CinemaVault/wiki)
- **Issues**: [GitHub Issues](https://github.com/CinemaVault/CinemaVault/issues)
- **Discussions**: [GitHub Discussions](https://github.com/CinemaVault/CinemaVault/discussions)
- **Community**: [Discord](https://discord.gg/cinemavault)

### Reporting Bugs
1. Check existing issues first
2. Use the bug report template
3. Include system information
4. Provide logs and screenshots
5. Steps to reproduce required

### Feature Requests
1. Check existing feature requests
2. Use the feature request template
3. Describe use case clearly
4. Consider implementation complexity
5. Provide mockups if possible

## 📄 License

This project is licensed under the **GPLv3 License** - see the [LICENSE](LICENSE) file for details.

### Third-Party Licenses
- **Jellyfin SDK**: GPL v3
- **Font Awesome**: CC BY 4.0
- **Material Icons**: Apache 2.0

## 🙏 Acknowledgments

- **Jellyfin Team**: For the amazing media server
- **Seerr Team**: For the content request system
- **Radarr/Sonarr Teams**: For media management
- **Netflix/Disney+**: UI design inspiration
- **Community**: For feedback and contributions

## 🗺️ Roadmap

### Version 1.1.0 (Planned)
- [ ] Mobile app support
- [ ] Advanced filtering options
- [ ] User themes and customization
- [ ] Performance improvements
- [ ] More language support

### Version 1.2.0 (Future)
- [ ] AI-powered recommendations
- [ ] Social features (sharing, reviews)
- [ ] Advanced analytics dashboard
- [ ] Plugin marketplace integration
- [ ] Voice search support

## 📈 Changelog

### v1.0.0.0 (2026-05-09)
- 🎉 Initial release
- ✨ Complete Netflix-style UI
- 🔍 Seerr integration
- 📺 Radarr/Sonarr support
- 👤 User management
- 🎨 Responsive design
- 📱 Mobile support
- 🔧 Configuration interface
- 📚 Comprehensive documentation

---

<div align="center">
  <p>Made with ❤️ by the CinemaVault team</p>
  <p>Transform your media experience today!</p>
  <p>
    <a href="#installation">Install Now</a> •
    <a href="https://github.com/CinemaVault/CinemaVault">GitHub</a> •
    <a href="https://discord.gg/cinemavault">Discord</a>
  </p>
</div>
