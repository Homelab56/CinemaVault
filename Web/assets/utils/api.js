/**
 * CinemaVault API Client
 * Handles all communication with the CinemaVault backend
 */

window.CinemaVaultAPI = (function() {
    'use strict';

    const API_BASE = '/CinemaVault';
    const CACHE_TTL = 5 * 60 * 1000; // 5 minutes
    const cache = new Map();

    /**
     * Generic API request method with caching
     */
    async function request(endpoint, options = {}) {
        const url = `${API_BASE}${endpoint}`;
        const cacheKey = `${url}:${JSON.stringify(options)}`;
        
        // Check cache for GET requests
        if (!options.method || options.method === 'GET') {
            const cached = cache.get(cacheKey);
            if (cached && Date.now() - cached.timestamp < CACHE_TTL) {
                return cached.data;
            }
        }

        try {
            const response = await fetch(url, {
                method: options.method || 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                body: options.body ? JSON.stringify(options.body) : undefined,
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const data = await response.json();

            // Cache successful GET requests
            if (!options.method || options.method === 'GET') {
                cache.set(cacheKey, {
                    data: data,
                    timestamp: Date.now()
                });
            }

            return data;
        } catch (error) {
            console.error('API request failed:', error);
            throw error;
        }
    }

    /**
     * Clear cache for a specific endpoint or all cache
     */
    function clearCache(endpoint) {
        if (endpoint) {
            const keysToDelete = [];
            for (const key of cache.keys()) {
                if (key.startsWith(endpoint)) {
                    keysToDelete.push(key);
                }
            }
            keysToDelete.forEach(key => cache.delete(key));
        } else {
            cache.clear();
        }
    }

    /**
     * Discovery endpoints
     */
    const discovery = {
        /**
         * Get trending content
         */
        async getTrending(type = 'movie', page = 1) {
            return await request(`/discover/trending?type=${type}&page=${page}`);
        },

        /**
         * Get popular content
         */
        async getPopular(type = 'movie', page = 1) {
            return await request(`/discover/popular?type=${type}&page=${page}`);
        },

        /**
         * Get top rated content
         */
        async getTopRated(type = 'movie', page = 1) {
            return await request(`/discover/toprated?type=${type}&page=${page}`);
        },

        /**
         * Get now playing movies
         */
        async getNowPlaying(page = 1) {
            return await request(`/discover/nowplaying?page=${page}`);
        },

        /**
         * Get content by genre
         */
        async getByGenre(genreId, type = 'movie', page = 1) {
            return await request(`/discover/genre?genreId=${genreId}&type=${type}&page=${page}`);
        }
    };

    /**
     * Search endpoints
     */
    const search = {
        /**
         * Search for content
         */
        async search(query, page = 1) {
            return await request(`/search?query=${encodeURIComponent(query)}&page=${page}`);
        }
    };

    /**
     * Content detail endpoints
     */
    const content = {
        /**
         * Get content details
         */
        async getDetails(tmdbId, type) {
            return await request(`/detail?tmdbId=${tmdbId}&type=${type}`);
        },

        /**
         * Get recommendations
         */
        async getRecommendations(tmdbId, type, page = 1) {
            return await request(`/recommendations?tmdbId=${tmdbId}&type=${type}&page=${page}`);
        },

        /**
         * Get videos/trailers
         */
        async getVideos(tmdbId, type) {
            return await request(`/videos?tmdbId=${tmdbId}&type=${type}`);
        }
    };

    /**
     * Library endpoints
     */
    const library = {
        /**
         * Get library status for TMDB IDs
         */
        async getStatus(tmdbIds) {
            const tmdbIdsParam = Array.isArray(tmdbIds) ? tmdbIds.join(',') : tmdbIds;
            return await request(`/library/status?tmdbIds=${tmdbIdsParam}`);
        },

        /**
         * Get recently added items
         */
        async getRecent(limit = 20) {
            return await request(`/library/recent?limit=${limit}`);
        },

        /**
         * Get continue watching items
         */
        async getResume() {
            return await request(`/library/resume`);
        }
    };

    /**
     * Request endpoints
     */
    const requests = {
        /**
         * Create a new request
         */
        async create(requestData) {
            clearCache('/requests');
            return await request('/request', {
                method: 'POST',
                body: requestData
            });
        },

        /**
         * Get requests
         */
        async getAll(userId = null, status = null) {
            let endpoint = '/requests';
            const params = new URLSearchParams();
            
            if (userId) params.append('userId', userId);
            if (status) params.append('status', status);
            
            if (params.toString()) {
                endpoint += '?' + params.toString();
            }
            
            return await request(endpoint);
        },

        /**
         * Delete a request
         */
        async delete(requestId) {
            clearCache('/requests');
            return await request(`/request/${requestId}`, {
                method: 'DELETE'
            });
        },

        /**
         * Get request status
         */
        async getStatus(tmdbId, type) {
            return await request(`/request/status/${tmdbId}?type=${type}`);
        }
    };

    /**
     * Watchlist endpoints
     */
    const watchlist = {
        /**
         * Get user watchlist
         */
        async get(page = 1) {
            return await request(`/watchlist?page=${page}`);
        },

        /**
         * Add item to watchlist
         */
        async add(tmdbId, type, title, posterPath = null) {
            clearCache('/watchlist');
            return await request('/watchlist', {
                method: 'POST',
                body: {
                    tmdbId: tmdbId,
                    type: type,
                    title: title,
                    posterPath: posterPath
                }
            });
        },

        /**
         * Remove item from watchlist
         */
        async remove(tmdbId, type) {
            clearCache('/watchlist');
            return await request(`/watchlist/${tmdbId}?type=${type}`, {
                method: 'DELETE'
            });
        }
    };

    /**
     * Config endpoints
     */
    const config = {
        /**
         * Get configuration
         */
        async get() {
            return await request('/config');
        },

        /**
         * Update configuration
         */
        async update(configData) {
            return await request('/config', {
                method: 'POST',
                body: configData
            });
        },

        /**
         * Test Seerr connection
         */
        async testSeerr() {
            return await request('/config/test/seerr');
        },

        /**
         * Test Radarr connection
         */
        async testRadarr() {
            return await request('/config/test/radarr');
        },

        /**
         * Test Sonarr connection
         */
        async testSonarr() {
            return await request('/config/test/sonarr');
        }
    };

    /**
     * Hero content endpoint
     */
    const hero = {
        /**
         * Get hero content
         */
        async get() {
            return await request('/hero');
        }
    };

    /**
     * Utility functions
     */
    const utils = {
        /**
         * Get image URL with proper sizing
         */
        getImageUrl(path, size = 'w342') {
            if (!path) return null;
            if (path.startsWith('/')) {
                return path; // Jellyfin local image
            }
            return `https://image.tmdb.org/t/p/${size}${path}`;
        },

        /**
         * Get backdrop URL
         */
        getBackdropUrl(path, size = 'w1280') {
            if (!path) return null;
            if (path.startsWith('/')) {
                return path; // Jellyfin local image
            }
            return `https://image.tmdb.org/t/p/${size}${path}`;
        },

        /**
         * Format runtime
         */
        formatRuntime(minutes) {
            if (!minutes) return '';
            const hours = Math.floor(minutes / 60);
            const mins = minutes % 60;
            return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
        },

        /**
         * Format date
         */
        formatDate(dateString) {
            if (!dateString) return '';
            const date = new Date(dateString);
            return date.toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric'
            });
        },

        /**
         * Format vote average
         */
        formatVoteAverage(rating) {
            if (!rating) return '';
            return rating.toFixed(1);
        },

        /**
         * Get status color
         */
        getStatusColor(status) {
            const colors = {
                'available': '#00d4aa',
                'downloading': '#ffa733',
                'pending': '#6c63ff',
                'requested': '#6c63ff',
                'processing': '#ffa733',
                'approved': '#6c63ff',
                'declined': '#ff4757',
                'unknown': '#6b6b8a'
            };
            return colors[status] || colors.unknown;
        },

        /**
         * Get status text
         */
        getStatusText(status) {
            const texts = {
                'available': 'Available',
                'downloading': 'Downloading',
                'pending': 'Pending',
                'requested': 'Requested',
                'processing': 'Processing',
                'approved': 'Approved',
                'declined': 'Declined',
                'unknown': 'Unknown'
            };
            return texts[status] || texts.unknown;
        },

        /**
         * Debounce function
         */
        debounce(func, wait) {
            let timeout;
            return function executedFunction(...args) {
                const later = () => {
                    clearTimeout(timeout);
                    func(...args);
                };
                clearTimeout(timeout);
                timeout = setTimeout(later, wait);
            };
        },

        /**
         * Throttle function
         */
        throttle(func, limit) {
            let inThrottle;
            return function() {
                const args = arguments;
                const context = this;
                if (!inThrottle) {
                    func.apply(context, args);
                    inThrottle = true;
                    setTimeout(() => inThrottle = false, limit);
                }
            };
        }
    };

    // Public API
    return {
        // Modules
        discovery,
        search,
        content,
        library,
        requests,
        watchlist,
        config,
        hero,
        utils,
        
        // Core methods
        request,
        clearCache
    };
})();
