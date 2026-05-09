/**
 * CinemaVault Navigation Bar Component
 * Handles the top navigation bar with search, user menu, etc.
 */

window.CinemaVaultNavbar = (function() {
    'use strict';

    let container = null;
    let searchOverlay = null;
    let userMenu = null;
    let isScrolled = false;
    let searchInput = null;
    let searchResults = null;
    let searchTimeout = null;
    let currentQuery = '';

    /**
     * Initialize navbar
     */
    async function init(parentContainer) {
        container = document.createElement('nav');
        container.className = 'cv-navbar';
        container.innerHTML = getNavbarHTML();
        
        parentContainer.appendChild(container);
        
        // Initialize components
        initializeSearch();
        initializeUserMenu();
        initializeScrollEffects();
        
        // Load user info
        await loadUserInfo();
    }

    /**
     * Get navbar HTML
     */
    function getNavbarHTML() {
        return `
            <div class="cv-navbar-container">
                <div class="cv-navbar-left">
                    <div class="cv-navbar-logo">
                        <span class="cv-logo-icon">🎬</span>
                        <span class="cv-logo-text">CinemaVault</span>
                    </div>
                    <div class="cv-navbar-nav">
                        <a href="/" class="cv-nav-link cv-nav-active" data-route="home">Home</a>
                        <a href="/movies" class="cv-nav-link" data-route="movies">Movies</a>
                        <a href="/tv" class="cv-nav-link" data-route="tv">TV Shows</a>
                        <a href="/requests" class="cv-nav-link" data-route="requests">Requests</a>
                        <a href="/mylist" class="cv-nav-link" data-route="mylist">My List</a>
                    </div>
                </div>
                
                <div class="cv-navbar-right">
                    <div class="cv-navbar-search">
                        <button class="cv-search-btn" id="cvSearchBtn">
                            <svg class="cv-search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <circle cx="11" cy="11" r="8"></circle>
                                <path d="m21 21-4.35-4.35"></path>
                            </svg>
                        </button>
                    </div>
                    
                    <div class="cv-navbar-user">
                        <button class="cv-user-btn" id="cvUserBtn">
                            <img class="cv-user-avatar" id="cvUserAvatar" src="" alt="User">
                            <span class="cv-user-name" id="cvUserName">User</span>
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Initialize search functionality
     */
    function initializeSearch() {
        const searchBtn = document.getElementById('cvSearchBtn');
        if (!searchBtn) return;

        searchBtn.addEventListener('click', toggleSearch);
        
        // Create search overlay
        searchOverlay = document.createElement('div');
        searchOverlay.className = 'cv-search-overlay';
        searchOverlay.innerHTML = getSearchOverlayHTML();
        document.body.appendChild(searchOverlay);

        // Initialize search components
        searchInput = searchOverlay.querySelector('.cv-search-input');
        searchResults = searchOverlay.querySelector('.cv-search-results');

        // Search input events
        searchInput.addEventListener('input', handleSearchInput);
        searchInput.addEventListener('keydown', handleSearchKeydown);
        
        // Close search on overlay click
        searchOverlay.addEventListener('click', (e) => {
            if (e.target === searchOverlay) {
                closeSearch();
            }
        });

        // Close search on escape
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && searchOverlay.classList.contains('cv-search-active')) {
                closeSearch();
            }
        });
    }

    /**
     * Get search overlay HTML
     */
    function getSearchOverlayHTML() {
        return `
            <div class="cv-search-container">
                <div class="cv-search-header">
                    <input type="text" class="cv-search-input" placeholder="Search for movies, TV shows..." autocomplete="off">
                    <button class="cv-search-close" id="cvSearchClose">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <line x1="18" y1="6" x2="6" y2="18"></line>
                            <line x1="6" y1="6" x2="18" y2="18"></line>
                        </svg>
                    </button>
                </div>
                
                <div class="cv-search-results" id="cvSearchResults">
                    <div class="cv-search-empty">
                        <p>Start typing to search...</p>
                    </div>
                </div>
                
                <div class="cv-search-footer">
                    <div class="cv-search-shortcuts">
                        <span class="cv-search-shortcut">
                            <kbd>↑</kbd><kbd>↓</kbd> Navigate
                        </span>
                        <span class="cv-search-shortcut">
                            <kbd>Enter</kbd> Select
                        </span>
                        <span class="cv-search-shortcut">
                            <kbd>Esc</kbd> Close
                        </span>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Toggle search overlay
     */
    function toggleSearch() {
        if (searchOverlay.classList.contains('cv-search-active')) {
            closeSearch();
        } else {
            openSearch();
        }
    }

    /**
     * Open search overlay
     */
    function openSearch() {
        searchOverlay.classList.add('cv-search-active');
        searchInput.value = '';
        searchInput.focus();
        currentQuery = '';
        
        // Show initial popular searches
        showPopularSearches();
    }

    /**
     * Close search overlay
     */
    function closeSearch() {
        searchOverlay.classList.remove('cv-search-active');
        searchInput.value = '';
        searchResults.innerHTML = '<div class="cv-search-empty"><p>Start typing to search...</p></div>';
        currentQuery = '';
        
        if (searchTimeout) {
            clearTimeout(searchTimeout);
            searchTimeout = null;
        }
    }

    /**
     * Handle search input
     */
    function handleSearchInput(e) {
        const query = e.target.value.trim();
        
        if (query === currentQuery) return;
        currentQuery = query;

        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        if (query.length < 2) {
            showPopularSearches();
            return;
        }

        searchTimeout = setTimeout(async () => {
            await performSearch(query);
        }, 300);
    }

    /**
     * Handle search keydown
     */
    function handleSearchKeydown(e) {
        const items = searchResults.querySelectorAll('.cv-search-item');
        const currentIndex = Array.from(items).findIndex(item => item.classList.contains('cv-search-selected'));
        
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                selectSearchItem(items, currentIndex + 1);
                break;
            case 'ArrowUp':
                e.preventDefault();
                selectSearchItem(items, currentIndex - 1);
                break;
            case 'Enter':
                e.preventDefault();
                if (currentIndex >= 0 && items[currentIndex]) {
                    items[currentIndex].click();
                }
                break;
        }
    }

    /**
     * Select search item
     */
    function selectSearchItem(items, index) {
        // Remove previous selection
        items.forEach(item => item.classList.remove('cv-search-selected'));
        
        // Select new item
        if (index >= 0 && index < items.length) {
            items[index].classList.add('cv-search-selected');
            items[index].scrollIntoView({ block: 'nearest' });
        }
    }

    /**
     * Perform search
     */
    async function performSearch(query) {
        try {
            searchResults.innerHTML = '<div class="cv-search-loading"><div class="cv-spinner"></div> Searching...</div>';
            
            const results = await CinemaVaultAPI.search.search(query);
            
            if (results.libraryResults.length === 0 && results.discoverResults.length === 0) {
                searchResults.innerHTML = `
                    <div class="cv-search-empty">
                        <p>No results found for "${query}"</p>
                    </div>
                `;
                return;
            }

            let html = '';
            
            // Library results
            if (results.libraryResults.length > 0) {
                html += `
                    <div class="cv-search-section">
                        <h3 class="cv-search-section-title">In Your Library</h3>
                        <div class="cv-search-grid">
                            ${results.libraryResults.map(item => createSearchResultItem(item, 'library')).join('')}
                        </div>
                    </div>
                `;
            }
            
            // Discover results
            if (results.discoverResults.length > 0) {
                html += `
                    <div class="cv-search-section">
                        <h3 class="cv-search-section-title">Discover</h3>
                        <div class="cv-search-grid">
                            ${results.discoverResults.map(item => createSearchResultItem(item, 'discover')).join('')}
                        </div>
                    </div>
                `;
            }
            
            searchResults.innerHTML = html;
            
            // Add click handlers
            searchResults.querySelectorAll('.cv-search-item').forEach(item => {
                item.addEventListener('click', () => {
                    const tmdbId = parseInt(item.dataset.tmdbId);
                    const type = item.dataset.type;
                    closeSearch();
                    CinemaVaultModal.showDetails(tmdbId, type);
                });
            });
            
        } catch (error) {
            console.error('Search error:', error);
            searchResults.innerHTML = `
                <div class="cv-search-error">
                    <p>Search failed. Please try again.</p>
                </div>
            `;
        }
    }

    /**
     * Create search result item
     */
    function createSearchResultItem(item, source) {
        const posterUrl = CinemaVaultAPI.utils.getImageUrl(item.posterPath, 'w154');
        const isInLibrary = source === 'library';
        const statusBadge = isInLibrary ? '<span class="cv-status-badge cv-status-available">In Library</span>' : '';
        
        return `
            <div class="cv-search-item" data-tmdb-id="${item.tmdbId}" data-type="${item.type}">
                <div class="cv-search-poster">
                    <img src="${posterUrl || '/assets/images/placeholder.png'}" alt="${item.title}" loading="lazy">
                    ${statusBadge}
                </div>
                <div class="cv-search-info">
                    <h4 class="cv-search-title">${item.title}</h4>
                    <p class="cv-search-meta">${item.year || ''} • ${item.type}</p>
                    <p class="cv-search-overview">${item.overview ? item.overview.substring(0, 100) + '...' : ''}</p>
                </div>
            </div>
        `;
    }

    /**
     * Show popular searches
     */
    function showPopularSearches() {
        const popularSearches = ['Action', 'Comedy', 'Drama', 'Horror', 'Sci-Fi', 'Thriller'];
        
        const html = `
            <div class="cv-search-popular">
                <h3 class="cv-search-section-title">Popular Searches</h3>
                <div class="cv-search-tags">
                    ${popularSearches.map(term => `
                        <button class="cv-search-tag" data-query="${term}">${term}</button>
                    `).join('')}
                </div>
            </div>
        `;
        
        searchResults.innerHTML = html;
        
        // Add click handlers
        searchResults.querySelectorAll('.cv-search-tag').forEach(tag => {
            tag.addEventListener('click', () => {
                const query = tag.dataset.query;
                searchInput.value = query;
                handleSearchInput({ target: { value: query } });
            });
        });
    }

    /**
     * Initialize user menu
     */
    function initializeUserMenu() {
        const userBtn = document.getElementById('cvUserBtn');
        if (!userBtn) return;

        userBtn.addEventListener('click', toggleUserMenu);
        
        // Create user menu
        userMenu = document.createElement('div');
        userMenu.className = 'cv-user-menu';
        userMenu.innerHTML = getUserMenuHTML();
        document.body.appendChild(userMenu);

        // Close menu on outside click
        document.addEventListener('click', (e) => {
            if (!userBtn.contains(e.target) && !userMenu.contains(e.target)) {
                closeUserMenu();
            }
        });

        // Add menu item handlers
        userMenu.querySelectorAll('.cv-user-menu-item').forEach(item => {
            item.addEventListener('click', handleUserMenuClick);
        });
    }

    /**
     * Get user menu HTML
     */
    function getUserMenuHTML() {
        return `
            <div class="cv-user-menu-content">
                <div class="cv-user-menu-header">
                    <img class="cv-user-menu-avatar" id="cvUserMenuAvatar" src="" alt="User">
                    <div class="cv-user-menu-info">
                        <div class="cv-user-menu-name" id="cvUserMenuName">User</div>
                        <div class="cv-user-menu-role" id="cvUserMenuRole">Member</div>
                    </div>
                </div>
                
                <div class="cv-user-menu-items">
                    <a href="/profile" class="cv-user-menu-item" data-action="profile">
                        <svg class="cv-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                            <circle cx="12" cy="7" r="4"></circle>
                        </svg>
                        Profile
                    </a>
                    <a href="/settings" class="cv-user-menu-item" data-action="settings">
                        <svg class="cv-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <circle cx="12" cy="12" r="3"></circle>
                            <path d="M12 1v6m0 6v6m4.22-13.22l4.24 4.24M1.54 9.96l4.24 4.24m14.44 0l4.24 4.24M1.54 14.04l4.24-4.24"></path>
                        </svg>
                        Settings
                    </a>
                    <div class="cv-user-menu-divider"></div>
                    <a href="/help" class="cv-user-menu-item" data-action="help">
                        <svg class="cv-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <circle cx="12" cy="12" r="10"></circle>
                            <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"></path>
                            <line x1="12" y1="17" x2="12.01" y2="17"></line>
                        </svg>
                        Help
                    </a>
                    <a href="/logout" class="cv-user-menu-item" data-action="logout">
                        <svg class="cv-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
                            <polyline points="16 17 21 12 16 7"></polyline>
                            <line x1="21" y1="12" x2="9" y2="12"></line>
                        </svg>
                        Sign Out
                    </a>
                </div>
            </div>
        `;
    }

    /**
     * Toggle user menu
     */
    function toggleUserMenu() {
        if (userMenu.classList.contains('cv-user-menu-active')) {
            closeUserMenu();
        } else {
            openUserMenu();
        }
    }

    /**
     * Open user menu
     */
    function openUserMenu() {
        userMenu.classList.add('cv-user-menu-active');
    }

    /**
     * Close user menu
     */
    function closeUserMenu() {
        userMenu.classList.remove('cv-user-menu-active');
    }

    /**
     * Handle user menu click
     */
    function handleUserMenuClick(e) {
        e.preventDefault();
        const action = e.currentTarget.dataset.action;
        
        switch (action) {
            case 'profile':
                CinemaVaultRouter.navigate('/profile');
                break;
            case 'settings':
                CinemaVaultRouter.navigate('/settings');
                break;
            case 'help':
                window.open('/help', '_blank');
                break;
            case 'logout':
                window.location.href = '/#!/logout.html';
                break;
        }
        
        closeUserMenu();
    }

    /**
     * Initialize scroll effects
     */
    function initializeScrollEffects() {
        let lastScrollY = window.scrollY;
        
        window.addEventListener('scroll', CinemaVaultAPI.utils.throttle(() => {
            const currentScrollY = window.scrollY;
            
            // Add scrolled class when scrolled down
            if (currentScrollY > 50) {
                if (!isScrolled) {
                    container.classList.add('cv-navbar-scrolled');
                    isScrolled = true;
                }
            } else {
                if (isScrolled) {
                    container.classList.remove('cv-navbar-scrolled');
                    isScrolled = false;
                }
            }
            
            lastScrollY = currentScrollY;
        }, 16));
    }

    /**
     * Load user information
     */
    async function loadUserInfo() {
        try {
            const user = await CinemaVaultJellyfin.getCurrentUser();
            if (user) {
                updateUserDisplay(user);
            }
        } catch (error) {
            console.error('Error loading user info:', error);
        }
    }

    /**
     * Update user display
     */
    function updateUserDisplay(user) {
        const userAvatar = document.getElementById('cvUserAvatar');
        const userName = document.getElementById('cvUserName');
        const userMenuAvatar = document.getElementById('cvUserMenuAvatar');
        const userMenuName = document.getElementById('cvUserMenuName');
        const userMenuRole = document.getElementById('cvUserMenuRole');
        
        const avatarUrl = CinemaVaultJellyfin.getUserImageUrl(user.Id, 'Primary', user.PrimaryImageTag);
        
        if (userAvatar) userAvatar.src = avatarUrl;
        if (userName) userName.textContent = user.Name;
        if (userMenuAvatar) userMenuAvatar.src = avatarUrl;
        if (userMenuName) userMenuName.textContent = user.Name;
        if (userMenuRole) userMenuRole.textContent = user.Policy?.IsAdministrator ? 'Administrator' : 'Member';
    }

    /**
     * Update active navigation link
     */
    function updateActiveLink(route) {
        const navLinks = container.querySelectorAll('.cv-nav-link');
        navLinks.forEach(link => {
            link.classList.remove('cv-nav-active');
            if (link.dataset.route === route) {
                link.classList.add('cv-nav-active');
            }
        });
    }

    /**
     * Destroy navbar
     */
    function destroy() {
        if (container) {
            container.remove();
            container = null;
        }
        
        if (searchOverlay) {
            searchOverlay.remove();
            searchOverlay = null;
        }
        
        if (userMenu) {
            userMenu.remove();
            userMenu = null;
        }
        
        if (searchTimeout) {
            clearTimeout(searchTimeout);
            searchTimeout = null;
        }
    }

    // Public API
    return {
        init,
        updateActiveLink,
        destroy,
        openSearch,
        closeSearch
    };
})();
