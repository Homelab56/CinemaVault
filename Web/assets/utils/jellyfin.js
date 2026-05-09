/**
 * Jellyfin Integration Utilities
 * Handles integration with Jellyfin's existing API and UI
 */

window.CinemaVaultJellyfin = (function() {
    'use strict';

    /**
     * Get current Jellyfin user
     */
    async function getCurrentUser() {
        try {
            if (window.ApiClient) {
                return await ApiClient.getCurrentUser();
            }
            
            // Fallback to direct API call
            const response = await fetch('/Users/Me', {
                credentials: 'same-origin'
            });
            
            if (response.ok) {
                return await response.json();
            }
        } catch (error) {
            console.error('Error getting current user:', error);
        }
        
        return null;
    }

    /**
     * Get user image URL
     */
    function getUserImageUrl(userId, type = 'Primary', tag = null) {
        let url = `/Users/${userId}/Images/${type}`;
        if (tag) {
            url += `?tag=${tag}`;
        }
        return url;
    }

    /**
     * Play media item in Jellyfin
     */
    async function playItem(itemId, startPositionTicks = 0) {
        try {
            if (window.ApiClient) {
                return await ApiClient.play(itemId, startPositionTicks);
            }
            
            // Fallback: navigate to player
            const playUrl = `/#!/item?id=${itemId}&serverId=${window.ApiClient?.serverId() || ''}&context=`;
            window.location.href = playUrl;
        } catch (error) {
            console.error('Error playing item:', error);
            throw error;
        }
    }

    /**
     * Get item details from Jellyfin
     */
    async function getItemDetails(itemId, userId = null) {
        try {
            let url = `/Items/${itemId}`;
            if (userId) {
                url += `?userId=${userId}`;
            }
            
            const response = await fetch(url, {
                credentials: 'same-origin'
            });
            
            if (response.ok) {
                return await response.json();
            }
        } catch (error) {
            console.error('Error getting item details:', error);
        }
        
        return null;
    }

    /**
     * Get similar items from Jellyfin
     */
    async function getSimilarItems(itemId, userId = null, limit = 10) {
        try {
            let url = `/Items/${itemId}/Similar?Limit=${limit}`;
            if (userId) {
                url += `&userId=${userId}`;
            }
            
            const response = await fetch(url, {
                credentials: 'same-origin'
            });
            
            if (response.ok) {
                return await response.json();
            }
        } catch (error) {
            console.error('Error getting similar items:', error);
        }
        
        return [];
    }

    /**
     * Get user's playback progress for an item
     */
    async function getPlaybackProgress(itemId, userId = null) {
        try {
            let url = `/Users/${userId || 'Me'}/PlayedItems/${itemId}`;
            
            const response = await fetch(url, {
                credentials: 'same-origin'
            });
            
            if (response.ok) {
                const userData = await response.json();
                return userData.PlaybackPositionTicks || 0;
            }
        } catch (error) {
            console.error('Error getting playback progress:', error);
        }
        
        return 0;
    }

    /**
     * Update user's playback progress for an item
     */
    async function updatePlaybackProgress(itemId, positionTicks, userId = null) {
        try {
            const url = `/Users/${userId || 'Me'}/PlayingItems/${itemId}/Progress`;
            
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    PositionTicks: positionTicks
                }),
                credentials: 'same-origin'
            });
            
            return response.ok;
        } catch (error) {
            console.error('Error updating playback progress:', error);
            return false;
        }
    }

    /**
     * Mark item as played/unplayed
     */
    async function markAsPlayed(itemId, played = true, userId = null) {
        try {
            const url = `/Users/${userId || 'Me'}/PlayedItems/${itemId}`;
            const method = played ? 'POST' : 'DELETE';
            
            const response = await fetch(url, {
                method: method,
                credentials: 'same-origin'
            });
            
            return response.ok;
        } catch (error) {
            console.error('Error marking item as played:', error);
            return false;
        }
    }

    /**
     * Add/remove item from favorites
     */
    async function toggleFavorite(itemId, isFavorite = null, userId = null) {
        try {
            // First get current favorite status if not provided
            if (isFavorite === null) {
                const userData = await getUserData(itemId, userId);
                isFavorite = !userData.IsFavorite;
            }
            
            const url = `/Users/${userId || 'Me'}/FavoriteItems/${itemId}`;
            const method = isFavorite ? 'POST' : 'DELETE';
            
            const response = await fetch(url, {
                method: method,
                credentials: 'same-origin'
            });
            
            return response.ok;
        } catch (error) {
            console.error('Error toggling favorite:', error);
            return false;
        }
    }

    /**
     * Get user data for an item
     */
    async function getUserData(itemId, userId = null) {
        try {
            let url = `/Users/${userId || 'Me'}/Items/${itemId}/UserData`;
            
            const response = await fetch(url, {
                credentials: 'same-origin'
            });
            
            if (response.ok) {
                return await response.json();
            }
        } catch (error) {
            console.error('Error getting user data:', error);
        }
        
        return {
            IsFavorite: false,
            Played: false,
            PlaybackPositionTicks: 0
        };
    }

    /**
     * Get Jellyfin server info
     */
    async function getServerInfo() {
        try {
            const response = await fetch('/System/Info/Public', {
                credentials: 'same-origin'
            });
            
            if (response.ok) {
                return await response.json();
            }
        } catch (error) {
            console.error('Error getting server info:', error);
        }
        
        return null;
    }

    /**
     * Check if user has permission for an action
     */
    async function hasPermission(permission) {
        try {
            const user = await getCurrentUser();
            if (!user) return false;
            
            // Check policy permissions
            if (user.Policy) {
                switch (permission) {
                    case 'admin':
                        return user.Policy.IsAdministrator;
                    case 'delete':
                        return user.Policy.EnableContentDeletion;
                    case 'download':
                        return user.Policy.EnableContentDownloading;
                    case 'manage':
                        return user.Policy.EnableContentManagement;
                    default:
                        return true;
                }
            }
            
            return false;
        } catch (error) {
            console.error('Error checking permission:', error);
            return false;
        }
    }

    /**
     * Get image URL for an item
     */
    function getItemImageUrl(itemId, imageType = 'Primary', tag = null, maxWidth = null, maxHeight = null) {
        let url = `/Items/${itemId}/Images/${imageType}`;
        const params = new URLSearchParams();
        
        if (tag) params.append('tag', tag);
        if (maxWidth) params.append('maxWidth', maxWidth);
        if (maxHeight) params.append('maxHeight', maxHeight);
        
        if (params.toString()) {
            url += '?' + params.toString();
        }
        
        return url;
    }

    /**
     * Get backdrop URL for an item
     */
    function getBackdropUrl(itemId, tag = null, maxWidth = null, maxHeight = null) {
        return getItemImageUrl(itemId, 'Backdrop', tag, maxWidth, maxHeight);
    }

    /**
     * Get logo URL for an item
     */
    function getLogoUrl(itemId, tag = null, maxWidth = null, maxHeight = null) {
        return getItemImageUrl(itemId, 'Logo', tag, maxWidth, maxHeight);
    }

    /**
     * Navigate to Jellyfin item page
     */
    function navigateToItem(itemId) {
        const url = `#!/item?id=${itemId}`;
        window.location.hash = url;
    }

    /**
     * Navigate to Jellyfin user page
     */
    function navigateToUser(userId) {
        const url = `#!/userprofile.html?userId=${userId}`;
        window.location.hash = url;
    }

    /**
     * Show Jellyfin toast notification
     */
    function showToast(message, type = 'info') {
        if (window.require && window.require('toast')) {
            const toast = window.require('toast');
            toast.show(message, type);
        } else if (window.Dashboard && window.Dashboard.alert) {
            // Fallback for older Jellyfin versions
            if (type === 'error') {
                window.Dashboard.alert(message);
            } else {
                window.Dashboard.notify(message, type);
            }
        } else {
            // Last resort - use console
            console.log(`[${type.toUpperCase()}] ${message}`);
        }
    }

    /**
     * Show Jellyfin confirmation dialog
     */
    async function showConfirm(message, title = 'Confirm') {
        if (window.Dashboard && window.Dashboard.confirm) {
            return await window.Dashboard.confirm(message, title);
        }
        
        // Fallback - use native confirm
        return confirm(`${title}\n\n${message}`);
    }

    /**
     * Get Jellyfin CSS variables for theming
     */
    function getThemeVariables() {
        const styles = getComputedStyle(document.documentElement);
        return {
            backgroundColor: styles.getPropertyValue('--background-color').trim(),
            textColor: styles.getPropertyValue('--text-color').trim(),
            accentColor: styles.getPropertyValue('--accent-color').trim(),
            cardBackgroundColor: styles.getPropertyValue('--card-background-color').trim(),
            cardTextColor: styles.getPropertyValue('--card-text-color').trim()
        };
    }

    /**
     * Apply CinemaVault theme while preserving Jellyfin variables
     */
    function applyTheme() {
        const root = document.documentElement;
        
        // Store original Jellyfin variables
        const originalVars = {};
        for (let i = 0; i < root.style.length; i++) {
            const property = root.style[i];
            if (property.startsWith('--')) {
                originalVars[property] = root.style.getPropertyValue(property);
            }
        }
        
        // Apply CinemaVault variables
        root.style.setProperty('--cv-bg-primary', '#0a0a0f');
        root.style.setProperty('--cv-bg-secondary', '#12121a');
        root.style.setProperty('--cv-bg-card', '#1a1a2e');
        root.style.setProperty('--cv-bg-card-hover', '#22223a');
        root.style.setProperty('--cv-accent', '#6c63ff');
        root.style.setProperty('--cv-accent-hover', '#8b84ff');
        root.style.setProperty('--cv-accent-secondary', '#ff6b9d');
        root.style.setProperty('--cv-text-primary', '#ffffff');
        root.style.setProperty('--cv-text-secondary', '#b0b0c8');
        root.style.setProperty('--cv-text-muted', '#6b6b8a');
        root.style.setProperty('--cv-success', '#00d4aa');
        root.style.setProperty('--cv-warning', '#ffa733');
        root.style.setProperty('--cv-danger', '#ff4757');
        root.style.setProperty('--cv-gradient-hero', 'linear-gradient(135deg, #0a0a0f 0%, #1a1a2e 50%, #0a0a0f 100%)');
        
        return originalVars;
    }

    /**
     * Restore original Jellyfin theme
     */
    function restoreTheme(originalVars) {
        const root = document.documentElement;
        
        // Clear CinemaVault variables
        root.style.removeProperty('--cv-bg-primary');
        root.style.removeProperty('--cv-bg-secondary');
        root.style.removeProperty('--cv-bg-card');
        root.style.removeProperty('--cv-bg-card-hover');
        root.style.removeProperty('--cv-accent');
        root.style.removeProperty('--cv-accent-hover');
        root.style.removeProperty('--cv-accent-secondary');
        root.style.removeProperty('--cv-text-primary');
        root.style.removeProperty('--cv-text-secondary');
        root.style.removeProperty('--cv-text-muted');
        root.style.removeProperty('--cv-success');
        root.style.removeProperty('--cv-warning');
        root.style.removeProperty('--cv-danger');
        root.style.removeProperty('--cv-gradient-hero');
        
        // Restore original variables
        if (originalVars) {
            Object.entries(originalVars).forEach(([property, value]) => {
                root.style.setProperty(property, value);
            });
        }
    }

    /**
     * Wait for Jellyfin to be ready
     */
    function waitForJellyfin(callback, maxWait = 10000) {
        const start = Date.now();
        const check = () => {
            if (window.ApiClient || document.querySelector('.skinBody') || document.getElementById('reactRoot')) {
                callback();
            } else if (Date.now() - start < maxWait) {
                requestAnimationFrame(check);
            }
        };
        requestAnimationFrame(check);
    }

    /**
     * Check if we're on the home page
     */
    function isHomePage() {
        const hash = window.location.hash;
        const path = window.location.pathname;
        
        return hash === '#/home.html' || 
               hash === '#/' || 
               hash === '' ||
               path === '/' ||
               path === '/home.html';
    }

    /**
     * Hide Jellyfin's default home content
     */
    function hideJellyfinHome() {
        const selectors = [
            '.homePage',
            '.pageWithAbsoluteTabs',
            '#indexPage',
            '.dashboardHome',
            '.mainAnimatedPages',
            '.libraryPage'
        ];
        
        selectors.forEach(selector => {
            const elements = document.querySelectorAll(selector);
            elements.forEach(el => {
                if (el.offsetParent !== null) { // Only hide visible elements
                    el.style.display = 'none';
                    el.setAttribute('data-cinemavault-hidden', 'true');
                }
            });
        });
    }

    /**
     * Show Jellyfin's default home content
     */
    function showJellyfinHome() {
        const elements = document.querySelectorAll('[data-cinemavault-hidden="true"]');
        elements.forEach(el => {
            el.style.display = '';
            el.removeAttribute('data-cinemavault-hidden');
        });
    }

    // Public API
    return {
        // User and auth
        getCurrentUser,
        hasPermission,
        
        // Media playback
        playItem,
        getItemDetails,
        getSimilarItems,
        getPlaybackProgress,
        updatePlaybackProgress,
        
        // User actions
        markAsPlayed,
        toggleFavorite,
        getUserData,
        
        // Images and navigation
        getUserImageUrl,
        getItemImageUrl,
        getBackdropUrl,
        getLogoUrl,
        navigateToItem,
        navigateToUser,
        
        // UI and theming
        showToast,
        showConfirm,
        getThemeVariables,
        applyTheme,
        restoreTheme,
        
        // Page management
        waitForJellyfin,
        isHomePage,
        hideJellyfinHome,
        showJellyfinHome,
        
        // Server info
        getServerInfo
    };
})();
