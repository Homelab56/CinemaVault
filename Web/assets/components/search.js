/**
 * CinemaVault Search Component
 * Handles search functionality with real-time results
 */

window.CinemaVaultSearch = (function() {
    'use strict';

    let container = null;
    let searchInput = null;
    let resultsContainer = null;
    let searchTimeout = null;
    let currentQuery = '';
    let isLoading = false;
    let selectedIndex = -1;
    let searchHistory = [];

    const SEARCH_DELAY = 300;
    const MIN_QUERY_LENGTH = 2;
    const MAX_HISTORY_ITEMS = 10;

    /**
     * Initialize search component
     */
    function init(parentContainer) {
        container = document.createElement('div');
        container.className = 'cv-search';
        container.innerHTML = getSearchHTML();
        
        parentContainer.appendChild(container);
        
        // Initialize components
        initializeSearch();
        loadSearchHistory();
    }

    /**
     * Get search HTML
     */
    function getSearchHTML() {
        return `
            <div class="cv-search-container">
                <div class="cv-search-header">
                    <svg class="cv-search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <circle cx="11" cy="11" r="8"></circle>
                        <path d="m21 21-4.35-4.35"></path>
                    </svg>
                    <input type="text" 
                           class="cv-search-input" 
                           id="cvSearchInput"
                           placeholder="Search for movies, TV shows..." 
                           autocomplete="off"
                           autofocus>
                    <button class="cv-search-clear" id="cvSearchClear" style="display: none;">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <line x1="18" y1="6" x2="6" y2="18"></line>
                            <line x1="6" y1="6" x2="18" y2="18"></line>
                        </svg>
                    </button>
                </div>
                
                <div class="cv-search-results" id="cvSearchResults">
                    <div class="cv-search-empty">
                        <svg class="cv-search-empty-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <circle cx="11" cy="11" r="8"></circle>
                            <path d="m21 21-4.35-4.35"></path>
                        </svg>
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
     * Initialize search functionality
     */
    function initializeSearch() {
        searchInput = document.getElementById('cvSearchInput');
        resultsContainer = document.getElementById('cvSearchResults');
        const clearBtn = document.getElementById('cvSearchClear');
        
        if (!searchInput || !resultsContainer || !clearBtn) return;
        
        // Input events
        searchInput.addEventListener('input', handleSearchInput);
        searchInput.addEventListener('keydown', handleSearchKeydown);
        searchInput.addEventListener('focus', handleSearchFocus);
        
        // Clear button
        clearBtn.addEventListener('click', clearSearch);
        
        // Global keyboard shortcut (Ctrl+K or Cmd+K)
        document.addEventListener('keydown', (e) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                openSearch();
            }
        });
    }

    /**
     * Handle search input
     */
    function handleSearchInput(e) {
        const query = e.target.value.trim();
        const clearBtn = document.getElementById('cvSearchClear');
        
        // Update clear button visibility
        if (clearBtn) {
            clearBtn.style.display = query ? 'block' : 'none';
        }
        
        if (query === currentQuery) return;
        currentQuery = query;

        // Clear existing timeout
        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        if (query.length < MIN_QUERY_LENGTH) {
            showEmptyState();
            return;
        }

        // Set new timeout
        searchTimeout = setTimeout(async () => {
            await performSearch(query);
        }, SEARCH_DELAY);
    }

    /**
     * Handle search keydown
     */
    function handleSearchKeydown(e) {
        const items = resultsContainer.querySelectorAll('.cv-search-item');
        
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                selectNextItem(items);
                break;
            case 'ArrowUp':
                e.preventDefault();
                selectPreviousItem(items);
                break;
            case 'Enter':
                e.preventDefault();
                if (selectedIndex >= 0 && items[selectedIndex]) {
                    items[selectedIndex].click();
                } else if (currentQuery.length >= MIN_QUERY_LENGTH) {
                    performSearch(currentQuery);
                }
                break;
            case 'Escape':
                clearSearch();
                break;
        }
    }

    /**
     * Handle search focus
     */
    function handleSearchFocus() {
        if (currentQuery.length === 0) {
            showSearchHistory();
        }
    }

    /**
     * Perform search
     */
    async function performSearch(query) {
        if (isLoading) return;
        
        isLoading = true;
        showLoading();
        
        try {
            const results = await CinemaVaultAPI.search.search(query);
            
            if (results.libraryResults.length === 0 && results.discoverResults.length === 0) {
                showNoResults(query);
                return;
            }

            renderResults(results);
            addToSearchHistory(query);
            
        } catch (error) {
            console.error('Search error:', error);
            showError();
        } finally {
            isLoading = false;
        }
    }

    /**
     * Render search results
     */
    function renderResults(results) {
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
        
        resultsContainer.innerHTML = html;
        
        // Add click handlers
        initializeResultItems();
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
     * Initialize result items
     */
    function initializeResultItems() {
        const items = resultsContainer.querySelectorAll('.cv-search-item');
        
        items.forEach((item, index) => {
            item.addEventListener('click', () => {
                const tmdbId = parseInt(item.dataset.tmdbId);
                const type = item.dataset.type;
                CinemaVaultModal.showDetails(tmdbId, type);
            });
            
            item.addEventListener('mouseenter', () => {
                selectedIndex = index;
                updateSelection(items);
            });
        });
    }

    /**
     * Select next item
     */
    function selectNextItem(items) {
        if (items.length === 0) return;
        selectedIndex = (selectedIndex + 1) % items.length;
        updateSelection(items);
    }

    /**
     * Select previous item
     */
    function selectPreviousItem(items) {
        if (items.length === 0) return;
        selectedIndex = selectedIndex <= 0 ? items.length - 1 : selectedIndex - 1;
        updateSelection(items);
    }

    /**
     * Update selection
     */
    function updateSelection(items) {
        items.forEach((item, index) => {
            if (index === selectedIndex) {
                item.classList.add('cv-search-selected');
                item.scrollIntoView({ block: 'nearest' });
            } else {
                item.classList.remove('cv-search-selected');
            }
        });
    }

    /**
     * Show empty state
     */
    function showEmptyState() {
        resultsContainer.innerHTML = `
            <div class="cv-search-empty">
                <svg class="cv-search-empty-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <circle cx="11" cy="11" r="8"></circle>
                    <path d="m21 21-4.35-4.35"></path>
                </svg>
                <p>Start typing to search...</p>
            </div>
        `;
        selectedIndex = -1;
    }

    /**
     * Show no results
     */
    function showNoResults(query) {
        resultsContainer.innerHTML = `
            <div class="cv-search-empty">
                <svg class="cv-search-empty-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <circle cx="11" cy="11" r="8"></circle>
                    <path d="m21 21-4.35-4.35"></path>
                </svg>
                <p>No results found for "${query}"</p>
                <button class="cv-search-suggestion" onclick="window.CinemaVaultSearch.clearSearch()">
                    Try a different search
                </button>
            </div>
        `;
        selectedIndex = -1;
    }

    /**
     * Show loading state
     */
    function showLoading() {
        resultsContainer.innerHTML = `
            <div class="cv-search-loading">
                <div class="cv-spinner"></div>
                <p>Searching...</p>
            </div>
        `;
        selectedIndex = -1;
    }

    /**
     * Show error state
     */
    function showError() {
        resultsContainer.innerHTML = `
            <div class="cv-search-error">
                <svg class="cv-error-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <circle cx="12" cy="12" r="10"></circle>
                    <line x1="12" y1="8" x2="12" y2="12"></line>
                    <line x1="12" y1="16" x2="12.01" y2="16"></line>
                </svg>
                <p>Search failed. Please try again.</p>
                <button class="cv-search-suggestion" onclick="window.CinemaVaultSearch.performSearch('${currentQuery}')">
                    Try again
                </button>
            </div>
        `;
        selectedIndex = -1;
    }

    /**
     * Show search history
     */
    function showSearchHistory() {
        if (searchHistory.length === 0) {
            showEmptyState();
            return;
        }
        
        const historyHTML = `
            <div class="cv-search-section">
                <h3 class="cv-search-section-title">Recent Searches</h3>
                <div class="cv-search-history">
                    ${searchHistory.map(query => `
                        <button class="cv-search-history-item" data-query="${query}">
                            <svg class="cv-history-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <circle cx="12" cy="12" r="10"></circle>
                                <polyline points="12 6 12 12 16 14"></polyline>
                            </svg>
                            ${query}
                        </button>
                    `).join('')}
                </div>
            </div>
        `;
        
        resultsContainer.innerHTML = historyHTML;
        
        // Add click handlers
        const historyItems = resultsContainer.querySelectorAll('.cv-search-history-item');
        historyItems.forEach(item => {
            item.addEventListener('click', () => {
                const query = item.dataset.query;
                searchInput.value = query;
                handleSearchInput({ target: { value: query } });
            });
        });
    }

    /**
     * Load search history
     */
    function loadSearchHistory() {
        try {
            const stored = localStorage.getItem('cinemavault_search_history');
            if (stored) {
                searchHistory = JSON.parse(stored);
            }
        } catch (error) {
            console.error('Error loading search history:', error);
            searchHistory = [];
        }
    }

    /**
     * Save search history
     */
    function saveSearchHistory() {
        try {
            localStorage.setItem('cinemavault_search_history', JSON.stringify(searchHistory));
        } catch (error) {
            console.error('Error saving search history:', error);
        }
    }

    /**
     * Add to search history
     */
    function addToSearchHistory(query) {
        // Remove existing entry
        searchHistory = searchHistory.filter(item => item !== query);
        
        // Add to beginning
        searchHistory.unshift(query);
        
        // Limit to max items
        searchHistory = searchHistory.slice(0, MAX_HISTORY_ITEMS);
        
        // Save
        saveSearchHistory();
    }

    /**
     * Clear search
     */
    function clearSearch() {
        searchInput.value = '';
        currentQuery = '';
        selectedIndex = -1;
        
        if (searchTimeout) {
            clearTimeout(searchTimeout);
            searchTimeout = null;
        }
        
        showEmptyState();
        searchInput.focus();
        
        // Update clear button
        const clearBtn = document.getElementById('cvSearchClear');
        if (clearBtn) {
            clearBtn.style.display = 'none';
        }
    }

    /**
     * Open search
     */
    function openSearch() {
        if (searchInput) {
            searchInput.focus();
        }
    }

    /**
     * Close search
     */
    function closeSearch() {
        clearSearch();
        if (searchInput) {
            searchInput.blur();
        }
    }

    /**
     * Get current query
     */
    function getCurrentQuery() {
        return currentQuery;
    }

    /**
     * Set query
     */
    function setQuery(query) {
        if (searchInput) {
            searchInput.value = query;
            handleSearchInput({ target: { value: query } });
        }
    }

    /**
     * Destroy search component
     */
    function destroy() {
        if (searchTimeout) {
            clearTimeout(searchTimeout);
            searchTimeout = null;
        }
        
        if (container) {
            container.remove();
            container = null;
        }
        
        searchInput = null;
        resultsContainer = null;
        currentQuery = '';
        isLoading = false;
        selectedIndex = -1;
    }

    // Public API
    return {
        init,
        performSearch,
        clearSearch,
        openSearch,
        closeSearch,
        getCurrentQuery,
        setQuery,
        destroy
    };
})();
