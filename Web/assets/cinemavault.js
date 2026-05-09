/**
 * CinemaVault Main Application
 * Netflix-style UI for Jellyfin with Seerr integration
 */

window.CinemaVault = (function() {
    'use strict';

    let container = null;
    let user = null;
    let config = {};
    let isInitialized = false;
    let originalTheme = null;

    /**
     * Initialize CinemaVault
     */
    async function init() {
        if (isInitialized) return;
        
        try {
            console.log('Initializing CinemaVault...');
            
            // Wait for Jellyfin to be ready
            await waitForJellyfin();
            
            // Initialize components
            await initializeComponents();
            
            // Set up routing
            setupRouting();
            
            // Apply theme
            originalTheme = CinemaVaultJellyfin.applyTheme();
            
            isInitialized = true;
            console.log('CinemaVault initialized successfully');
            
        } catch (error) {
            console.error('Failed to initialize CinemaVault:', error);
        }
    }

    /**
     * Wait for Jellyfin to be ready
     */
    function waitForJellyfin() {
        return new Promise((resolve) => {
            const check = () => {
                if (document.getElementById('reactRoot') || 
                    document.querySelector('.skinBody') ||
                    window.ApiClient) {
                    resolve();
                } else {
                    requestAnimationFrame(check);
                }
            };
            requestAnimationFrame(check);
        });
    }

    /**
     * Initialize all components
     */
    async function initializeComponents() {
        // Get current user
        user = await CinemaVaultJellyfin.getCurrentUser();
        if (!user) {
            throw new Error('User not authenticated');
        }

        // Load configuration
        try {
            config = await CinemaVaultAPI.config.get();
        } catch (error) {
            console.warn('Failed to load config, using defaults:', error);
            config = getDefaultConfig();
        }

        // Initialize toast system
        CinemaVaultToast.init();

        // Initialize modal system
        CinemaVaultModal.init();
    }

    /**
     * Set up routing
     */
    function setupRouting() {
        // Register routes
        CinemaVaultRouter.register('/', renderHome, { title: 'CinemaVault' });
        CinemaVaultRouter.register('/movies', renderMovies, { title: 'Movies - CinemaVault' });
        CinemaVaultRouter.register('/tv', renderTV, { title: 'TV Shows - CinemaVault' });
        CinemaVaultRouter.register('/requests', renderRequests, { title: 'Requests - CinemaVault' });
        CinemaVaultRouter.register('/mylist', renderMyList, { title: 'My List - CinemaVault' });
        CinemaVaultRouter.register('/browse', renderBrowse, { title: 'Browse - CinemaVault' });
        CinemaVaultRouter.register('/profile', renderProfile, { title: 'Profile - CinemaVault' });
        CinemaVaultRouter.register('/settings', renderSettings, { title: 'Settings - CinemaVault' });

        // Set up navigation interception
        interceptNavigation();
    }

    /**
     * Intercept Jellyfin navigation
     */
    function interceptNavigation() {
        // Intercept hash changes
        window.addEventListener('hashchange', (e) => {
            const hash = window.location.hash;
            if (shouldInterceptRoute(hash)) {
                e.preventDefault();
                CinemaVaultRouter.navigate(hash.replace('#', '') || '/');
            }
        });

        // Override Jellyfin navigation if available
        if (window.Emby && window.Emby.Page) {
            const originalShow = window.Emby.Page.show;
            window.Emby.Page.show = function(path, ...args) {
                if (shouldInterceptPath(path)) {
                    CinemaVaultRouter.navigate(path);
                    return;
                }
                return originalShow.call(this, path, ...args);
            };
        }
    }

    /**
     * Check if route should be intercepted
     */
    function shouldInterceptRoute(hash) {
        return hash === '#/home.html' || 
               hash === '#/' || 
               hash === '' ||
               hash.startsWith('#/cinemavault');
    }

    /**
     * Check if path should be intercepted
     */
    function shouldInterceptPath(path) {
        return path.includes('home.html') || 
               path === '/' ||
               path.startsWith('/cinemavault');
    }

    /**
     * Render CinemaVault UI
     */
    async function render() {
        try {
            // Create or get container
            container = document.getElementById('cinemavault-root');
            if (!container) {
                container = document.createElement('div');
                container.id = 'cinemavault-root';
                document.body.appendChild(container);
            }

            // Hide Jellyfin's default content
            CinemaVaultJellyfin.hideJellyfinHome();

            // Show CinemaVault container
            container.style.display = 'block';
            container.className = 'cv-root';

            // Clear existing content
            container.innerHTML = '';

            // Render navigation
            await CinemaVaultNavbar.init(container);

            // Render main content
            const mainContent = document.createElement('main');
            mainContent.className = 'cv-main-content';
            container.appendChild(mainContent);

            // Check current route and render accordingly
            const currentPath = CinemaVaultRouter.getCurrentPath();
            if (currentPath === '/' || CinemaVaultJellyfin.isHomePage()) {
                await renderHomeContent(mainContent);
            } else {
                // Let router handle other routes
                CinemaVaultRouter.navigate(currentPath, true);
            }

        } catch (error) {
            console.error('Error rendering CinemaVault:', error);
            showErrorState(error.message);
        }
    }

    /**
     * Render home page
     */
    async function renderHome() {
        await render();
    }

    /**
     * Render home content
     */
    async function renderHomeContent(container) {
        try {
            // Check if hero banner is enabled
            if (config.enableHeroBanner !== false) {
                await CinemaVaultHero.init(container);
            }

            // Render carousels
            await renderCarousels(container);

        } catch (error) {
            console.error('Error rendering home content:', error);
            showErrorState('Failed to load content');
        }
    }

    /**
     * Render carousels
     */
    async function renderCarousels(container) {
        const carouselSection = document.createElement('div');
        carouselSection.className = 'cv-carousels';
        container.appendChild(carouselSection);

        // Define carousel configurations
        const carouselConfigs = [
            {
                title: 'Continue Watching',
                subtitle: 'Pick up where you left off',
                type: 'resume',
                endpoint: '/CinemaVault/library/resume'
            },
            {
                title: '✨ Trending This Week',
                subtitle: 'What everyone is watching',
                type: 'trending',
                endpoint: '/CinemaVault/discover/trending?type=movie'
            },
            {
                title: '🎬 New to CinemaVault',
                subtitle: 'Recently added to your library',
                type: 'recent',
                endpoint: '/CinemaVault/library/recent'
            },
            {
                title: '📺 Popular TV Shows',
                subtitle: 'Trending television series',
                type: 'popular',
                endpoint: '/CinemaVault/discover/popular?type=tv'
            },
            {
                title: '⭐ Top Rated Movies',
                subtitle: 'Highest rated films',
                type: 'toprated',
                endpoint: '/CinemaVault/discover/toprated?type=movie'
            }
        ];

        // Render carousels in parallel for better performance
        const carouselPromises = carouselConfigs.map(async (config, index) => {
            try {
                await CinemaVaultCarousel.init(carouselSection, config);
            } catch (error) {
                console.error(`Error loading carousel "${config.title}":`, error);
                // Continue with other carousels even if one fails
            }
        });

        await Promise.allSettled(carouselPromises);
    }

    /**
     * Render movies page
     */
    async function renderMovies(params, queryParams) {
        await render();
        const container = document.querySelector('.cv-main-content');
        if (container) {
            container.innerHTML = `
                <div class="cv-page-header">
                    <h1>Movies</h1>
                    <div class="cv-page-filters">
                        <select class="cv-filter-select" id="movieSort">
                            <option value="trending">Trending</option>
                            <option value="popular">Popular</option>
                            <option value="toprated">Top Rated</option>
                            <option value="nowplaying">Now Playing</option>
                        </select>
                        <select class="cv-filter-select" id="movieGenre">
                            <option value="">All Genres</option>
                        </select>
                    </div>
                </div>
                <div class="cv-movies-grid" id="moviesGrid">
                    <div class="cv-loading-state">
                        <div class="cv-spinner"></div>
                        <p>Loading movies...</p>
                    </div>
                </div>
            `;

            await loadMoviesContent();
            setupMovieFilters();
        }
    }

    /**
     * Render TV shows page
     */
    async function renderTV(params, queryParams) {
        await render();
        const container = document.querySelector('.cv-main-content');
        if (container) {
            container.innerHTML = `
                <div class="cv-page-header">
                    <h1>TV Shows</h1>
                    <div class="cv-page-filters">
                        <select class="cv-filter-select" id="tvSort">
                            <option value="trending">Trending</option>
                            <option value="popular">Popular</option>
                            <option value="toprated">Top Rated</option>
                        </select>
                        <select class="cv-filter-select" id="tvGenre">
                            <option value="">All Genres</option>
                        </select>
                    </div>
                </div>
                <div class="cv-tv-grid" id="tvGrid">
                    <div class="cv-loading-state">
                        <div class="cv-spinner"></div>
                        <p>Loading TV shows...</p>
                    </div>
                </div>
            `;

            await loadTVContent();
            setupTVFilters();
        }
    }

    /**
     * Render requests page
     */
    async function renderRequests(params, queryParams) {
        await render();
        const container = document.querySelector('.cv-main-content');
        if (container) {
            container.innerHTML = `
                <div class="cv-page-header">
                    <h1>Requests</h1>
                    <div class="cv-page-tabs">
                        <button class="cv-tab cv-tab-active" data-tab="my">My Requests</button>
                        ${user.Policy?.IsAdministrator ? '<button class="cv-tab" data-tab="all">All Requests</button>' : ''}
                        <button class="cv-tab" data-tab="available">Available</button>
                    </div>
                </div>
                <div class="cv-requests-content" id="requestsContent">
                    <div class="cv-loading-state">
                        <div class="cv-spinner"></div>
                        <p>Loading requests...</p>
                    </div>
                </div>
            `;

            await loadRequestsContent('my');
            setupRequestTabs();
        }
    }

    /**
     * Render watchlist page
     */
    async function renderMyList(params, queryParams) {
        await render();
        const container = document.querySelector('.cv-main-content');
        if (container) {
            container.innerHTML = `
                <div class="cv-page-header">
                    <h1>My List</h1>
                    <div class="cv-page-actions">
                        <button class="cv-btn cv-btn-secondary" id="exportList">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                                <polyline points="7 10 12 15 17 10"></polyline>
                                <line x1="12" y1="15" x2="12" y2="3"></line>
                            </svg>
                            Export
                        </button>
                    </div>
                </div>
                <div class="cv-watchlist-grid" id="watchlistGrid">
                    <div class="cv-loading-state">
                        <div class="cv-spinner"></div>
                        <p>Loading your watchlist...</p>
                    </div>
                </div>
            `;

            await loadWatchlistContent();
            setupWatchlistActions();
        }
    }

    /**
     * Render browse page
     */
    async function renderBrowse(params, queryParams) {
        await render();
        const container = document.querySelector('.cv-main-content');
        if (container) {
            const endpoint = queryParams.endpoint || '/CinemaVault/discover/trending?type=movie';
            container.innerHTML = `
                <div class="cv-page-header">
                    <h1>Browse</h1>
                    <p>Browse our entire collection</p>
                </div>
                <div class="cv-browse-content" id="browseContent">
                    <div class="cv-loading-state">
                        <div class="cv-spinner"></div>
                        <p>Loading content...</p>
                    </div>
                </div>
            `;

            await loadBrowseContent(endpoint);
        }
    }

    /**
     * Render profile page
     */
    async function renderProfile(params, queryParams) {
        await render();
        const container = document.querySelector('.cv-main-content');
        if (container) {
            container.innerHTML = `
                <div class="cv-page-header">
                    <h1>Profile</h1>
                </div>
                <div class="cv-profile-content">
                    <div class="cv-profile-card">
                        <div class="cv-profile-avatar">
                            <img src="${CinemaVaultJellyfin.getUserImageUrl(user.Id, 'Primary', user.PrimaryImageTag)}" alt="${user.Name}">
                        </div>
                        <div class="cv-profile-info">
                            <h2>${user.Name}</h2>
                            <p class="cv-profile-role">${user.Policy?.IsAdministrator ? 'Administrator' : 'Member'}</p>
                            <p class="cv-profile-lastseen">Last seen: ${user.LastActivityDate ? new Date(user.LastActivityDate).toLocaleDateString() : 'Never'}</p>
                        </div>
                    </div>
                    <div class="cv-profile-stats">
                        <h3>Your Statistics</h3>
                        <div class="cv-stats-grid">
                            <div class="cv-stat-item">
                                <div class="cv-stat-value" id="requestCount">-</div>
                                <div class="cv-stat-label">Requests</div>
                            </div>
                            <div class="cv-stat-item">
                                <div class="cv-stat-value" id="watchlistCount">-</div>
                                <div class="cv-stat-label">Watchlist Items</div>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            await loadProfileStats();
        }
    }

    /**
     * Render settings page
     */
    async function renderSettings(params, queryParams) {
        // Only allow administrators
        if (!user.Policy?.IsAdministrator) {
            CinemaVaultToast.error('Access denied. Administrator privileges required.');
            CinemaVaultRouter.navigate('/');
            return;
        }

        await render();
        const container = document.querySelector('.cv-main-content');
        if (container) {
            container.innerHTML = `
                <div class="cv-page-header">
                    <h1>Settings</h1>
                </div>
                <div class="cv-settings-content">
                    <div class="cv-settings-section">
                        <h2>Seerr Connection</h2>
                        <div class="cv-settings-form">
                            <div class="cv-form-group">
                                <label for="seerrUrl">Seerr URL</label>
                                <input type="url" id="seerrUrl" class="cv-form-input" placeholder="http://localhost:5056">
                            </div>
                            <div class="cv-form-group">
                                <label for="seerrApiKey">API Key</label>
                                <input type="password" id="seerrApiKey" class="cv-form-input" placeholder="Enter API key">
                            </div>
                            <button class="cv-btn cv-btn-primary" id="testSeerr">Test Connection</button>
                        </div>
                    </div>
                    
                    <div class="cv-settings-section">
                        <h2>Display Settings</h2>
                        <div class="cv-settings-form">
                            <div class="cv-form-group">
                                <label for="accentColor">Accent Color</label>
                                <input type="color" id="accentColor" class="cv-form-input" value="#6c63ff">
                            </div>
                            <div class="cv-form-group">
                                <label class="cv-checkbox-label">
                                    <input type="checkbox" id="enableHeroBanner">
                                    <span class="cv-checkbox"></span>
                                    Enable Hero Banner
                                </label>
                            </div>
                            <div class="cv-form-group">
                                <label class="cv-checkbox-label">
                                    <input type="checkbox" id="enableAutoPlay">
                                    <span class="cv-checkbox"></span>
                                    Auto-play Hero Banner
                                </label>
                            </div>
                        </div>
                    </div>
                    
                    <div class="cv-settings-actions">
                        <button class="cv-btn cv-btn-primary" id="saveSettings">Save Settings</button>
                        <button class="cv-btn cv-btn-secondary" id="resetSettings">Reset to Defaults</button>
                    </div>
                </div>
            `;

            await loadSettings();
            setupSettingsActions();
        }
    }

    /**
     * Load movies content
     */
    async function loadMoviesContent() {
        try {
            const sortSelect = document.getElementById('movieSort');
            const sort = sortSelect?.value || 'trending';
            
            let response;
            switch (sort) {
                case 'trending':
                    response = await CinemaVaultAPI.discovery.getTrending('movie');
                    break;
                case 'popular':
                    response = await CinemaVaultAPI.discovery.getPopular('movie');
                    break;
                case 'toprated':
                    response = await CinemaVaultAPI.discovery.getTopRated('movie');
                    break;
                case 'nowplaying':
                    response = await CinemaVaultAPI.discovery.getNowPlaying();
                    break;
                default:
                    response = { results: [] };
            }

            renderMovieGrid(response.results || []);

        } catch (error) {
            console.error('Error loading movies:', error);
            showErrorState('Failed to load movies');
        }
    }

    /**
     * Load TV content
     */
    async function loadTVContent() {
        try {
            const sortSelect = document.getElementById('tvSort');
            const sort = sortSelect?.value || 'trending';
            
            let response;
            switch (sort) {
                case 'trending':
                    response = await CinemaVaultAPI.discovery.getTrending('tv');
                    break;
                case 'popular':
                    response = await CinemaVaultAPI.discovery.getPopular('tv');
                    break;
                case 'toprated':
                    response = await CinemaVaultAPI.discovery.getTopRated('tv');
                    break;
                default:
                    response = { results: [] };
            }

            renderTVGrid(response.results || []);

        } catch (error) {
            console.error('Error loading TV shows:', error);
            showErrorState('Failed to load TV shows');
        }
    }

    /**
     * Load requests content
     */
    async function loadRequestsContent(tab) {
        try {
            let response;
            switch (tab) {
                case 'my':
                    response = await CinemaVaultAPI.requests.getAll(user.Id);
                    break;
                case 'all':
                    response = await CinemaVaultAPI.requests.getAll();
                    break;
                case 'available':
                    response = await CinemaVaultAPI.requests.getAll('', 'available');
                    break;
                default:
                    response = [];
            }

            renderRequestsGrid(response || []);

        } catch (error) {
            console.error('Error loading requests:', error);
            showErrorState('Failed to load requests');
        }
    }

    /**
     * Load watchlist content
     */
    async function loadWatchlistContent() {
        try {
            const response = await CinemaVaultAPI.watchlist.get();
            renderWatchlistGrid(response || []);

        } catch (error) {
            console.error('Error loading watchlist:', error);
            showErrorState('Failed to load watchlist');
        }
    }

    /**
     * Load browse content
     */
    async function loadBrowseContent(endpoint) {
        try {
            const response = await CinemaVaultAPI.request(endpoint);
            renderBrowseGrid(response.results || []);

        } catch (error) {
            console.error('Error loading browse content:', error);
            showErrorState('Failed to load content');
        }
    }

    /**
     * Load profile statistics
     */
    async function loadProfileStats() {
        try {
            const [requests, watchlist] = await Promise.allSettled([
                CinemaVaultAPI.requests.getAll(user.Id),
                CinemaVaultAPI.watchlist.get()
            ]);

            const requestCount = requests.status === 'fulfilled' ? requests.value.length : 0;
            const watchlistCount = watchlist.status === 'fulfilled' ? watchlist.value.length : 0;

            const requestCountEl = document.getElementById('requestCount');
            const watchlistCountEl = document.getElementById('watchlistCount');

            if (requestCountEl) requestCountEl.textContent = requestCount;
            if (watchlistCountEl) watchlistCountEl.textContent = watchlistCount;

        } catch (error) {
            console.error('Error loading profile stats:', error);
        }
    }

    /**
     * Load settings
     */
    async function loadSettings() {
        try {
            // Populate form fields
            const seerrUrl = document.getElementById('seerrUrl');
            const seerrApiKey = document.getElementById('seerrApiKey');
            const accentColor = document.getElementById('accentColor');
            const enableHeroBanner = document.getElementById('enableHeroBanner');
            const enableAutoPlay = document.getElementById('enableAutoPlay');

            if (seerrUrl) seerrUrl.value = config.seerrUrl || '';
            if (seerrApiKey) seerrApiKey.value = config.seerrApiKey || '';
            if (accentColor) accentColor.value = config.accentColor || '#6c63ff';
            if (enableHeroBanner) enableHeroBanner.checked = config.enableHeroBanner !== false;
            if (enableAutoPlay) enableAutoPlay.checked = config.enableAutoPlay !== false;

        } catch (error) {
            console.error('Error loading settings:', error);
        }
    }

    /**
     * Render movie grid
     */
    function renderMovieGrid(movies) {
        const grid = document.getElementById('moviesGrid');
        if (!grid) return;

        if (movies.length === 0) {
            grid.innerHTML = '<div class="cv-empty-state"><p>No movies found</p></div>';
            return;
        }

        const cards = CinemaVaultCard.createMultiple(movies);
        const gridElement = CinemaVaultCard.createGrid(movies);
        grid.innerHTML = '';
        grid.appendChild(gridElement);
    }

    /**
     * Render TV grid
     */
    function renderTVGrid(shows) {
        const grid = document.getElementById('tvGrid');
        if (!grid) return;

        if (shows.length === 0) {
            grid.innerHTML = '<div class="cv-empty-state"><p>No TV shows found</p></div>';
            return;
        }

        const gridElement = CinemaVaultCard.createGrid(shows);
        grid.innerHTML = '';
        grid.appendChild(gridElement);
    }

    /**
     * Render requests grid
     */
    function renderRequestsGrid(requests) {
        const content = document.getElementById('requestsContent');
        if (!content) return;

        if (requests.length === 0) {
            content.innerHTML = '<div class="cv-empty-state"><p>No requests found</p></div>';
            return;
        }

        const gridElement = CinemaVaultCard.createGrid(requests, { showProgress: true });
        content.innerHTML = '';
        content.appendChild(gridElement);
    }

    /**
     * Render watchlist grid
     */
    function renderWatchlistGrid(items) {
        const grid = document.getElementById('watchlistGrid');
        if (!grid) return;

        if (items.length === 0) {
            grid.innerHTML = '<div class="cv-empty-state"><p>Your watchlist is empty</p></div>';
            return;
        }

        const gridElement = CinemaVaultCard.createGrid(items);
        grid.innerHTML = '';
        grid.appendChild(gridElement);
    }

    /**
     * Render browse grid
     */
    function renderBrowseGrid(items) {
        const content = document.getElementById('browseContent');
        if (!content) return;

        if (items.length === 0) {
            content.innerHTML = '<div class="cv-empty-state"><p>No content found</p></div>';
            return;
        }

        const gridElement = CinemaVaultCard.createGrid(items);
        content.innerHTML = '';
        content.appendChild(gridElement);
    }

    /**
     * Setup movie filters
     */
    function setupMovieFilters() {
        const sortSelect = document.getElementById('movieSort');
        if (sortSelect) {
            sortSelect.addEventListener('change', loadMoviesContent);
        }
    }

    /**
     * Setup TV filters
     */
    function setupTVFilters() {
        const sortSelect = document.getElementById('tvSort');
        if (sortSelect) {
            sortSelect.addEventListener('change', loadTVContent);
        }
    }

    /**
     * Setup request tabs
     */
    function setupRequestTabs() {
        const tabs = document.querySelectorAll('.cv-tab');
        tabs.forEach(tab => {
            tab.addEventListener('click', () => {
                // Update active tab
                tabs.forEach(t => t.classList.remove('cv-tab-active'));
                tab.classList.add('cv-tab-active');
                
                // Load content for selected tab
                loadRequestsContent(tab.dataset.tab);
            });
        });
    }

    /**
     * Setup watchlist actions
     */
    function setupWatchlistActions() {
        const exportBtn = document.getElementById('exportList');
        if (exportBtn) {
            exportBtn.addEventListener('click', async () => {
                try {
                    const csv = await CinemaVaultAPI.watchlist.export();
                    const blob = new Blob([csv], { type: 'text/csv' });
                    const url = URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.href = url;
                    a.download = 'cinemavault-watchlist.csv';
                    a.click();
                    URL.revokeObjectURL(url);
                    
                    CinemaVaultToast.success('Watchlist exported successfully');
                } catch (error) {
                    console.error('Error exporting watchlist:', error);
                    CinemaVaultToast.error('Failed to export watchlist');
                }
            });
        }
    }

    /**
     * Setup settings actions
     */
    function setupSettingsActions() {
        const testSeerrBtn = document.getElementById('testSeerr');
        const saveBtn = document.getElementById('saveSettings');
        const resetBtn = document.getElementById('resetSettings');

        if (testSeerrBtn) {
            testSeerrBtn.addEventListener('click', async () => {
                try {
                    const result = await CinemaVaultAPI.config.testSeerr();
                    if (result.connected) {
                        CinemaVaultToast.success('Seerr connection successful');
                    } else {
                        CinemaVaultToast.error(`Seerr connection failed: ${result.message}`);
                    }
                } catch (error) {
                    CinemaVaultToast.error('Failed to test Seerr connection');
                }
            });
        }

        if (saveBtn) {
            saveBtn.addEventListener('click', async () => {
                try {
                    const newConfig = {
                        seerrUrl: document.getElementById('seerrUrl').value,
                        seerrApiKey: document.getElementById('seerrApiKey').value,
                        accentColor: document.getElementById('accentColor').value,
                        enableHeroBanner: document.getElementById('enableHeroBanner').checked,
                        enableAutoPlay: document.getElementById('enableAutoPlay').checked
                    };

                    await CinemaVaultAPI.config.update(newConfig);
                    CinemaVaultToast.success('Settings saved successfully');
                    
                    // Update config and apply theme changes
                    config = { ...config, ...newConfig };
                    if (newConfig.accentColor) {
                        document.documentElement.style.setProperty('--cv-accent', newConfig.accentColor);
                    }

                } catch (error) {
                    CinemaVaultToast.error('Failed to save settings');
                }
            });
        }

        if (resetBtn) {
            resetBtn.addEventListener('click', () => {
                if (confirm('Are you sure you want to reset all settings to defaults?')) {
                    const defaults = getDefaultConfig();
                    loadSettings();
                    CinemaVaultToast.info('Settings reset to defaults');
                }
            });
        }
    }

    /**
     * Show error state
     */
    function showErrorState(message) {
        if (container) {
            container.innerHTML = `
                <div class="cv-error-state">
                    <svg class="cv-error-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <circle cx="12" cy="12" r="10"></circle>
                        <line x1="12" y1="8" x2="12" y2="12"></line>
                        <line x1="12" y1="16" x2="12.01" y2="16"></line>
                    </svg>
                    <h2>Oops! Something went wrong</h2>
                    <p>${message}</p>
                    <button class="cv-btn cv-btn-primary" onclick="window.CinemaVault.render()">
                        Try Again
                    </button>
                </div>
            `;
        }
    }

    /**
     * Get default configuration
     */
    function getDefaultConfig() {
        return {
            seerrUrl: '',
            seerrApiKey: '',
            accentColor: '#6c63ff',
            enableHeroBanner: true,
            enableAutoPlay: true
        };
    }

    /**
     * Destroy CinemaVault
     */
    function destroy() {
        if (!isInitialized) return;

        // Restore original theme
        if (originalTheme) {
            CinemaVaultJellyfin.restoreTheme(originalTheme);
        }

        // Show Jellyfin content
        CinemaVaultJellyfin.showJellyfinHome();

        // Remove container
        if (container) {
            container.remove();
            container = null;
        }

        // Destroy components
        if (typeof CinemaVaultNavbar !== 'undefined') {
            CinemaVaultNavbar.destroy();
        }
        if (typeof CinemaVaultHero !== 'undefined') {
            CinemaVaultHero.destroy();
        }
        if (typeof CinemaVaultModal !== 'undefined') {
            CinemaVaultModal.destroy();
        }
        if (typeof CinemaVaultToast !== 'undefined') {
            CinemaVaultToast.destroy();
        }

        isInitialized = false;
    }

    /**
     * Get current user
     */
    function getUser() {
        return user;
    }

    /**
     * Get current configuration
     */
    function getConfig() {
        return config;
    }

    // Public API
    return {
        init,
        render,
        destroy,
        getUser,
        getConfig
    };
})();

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        // Wait a bit for Jellyfin to load
        setTimeout(() => {
            CinemaVault.init();
        }, 1000);
    });
} else {
    // DOM already loaded, initialize after a short delay
    setTimeout(() => {
        CinemaVault.init();
    }, 1000);
}
