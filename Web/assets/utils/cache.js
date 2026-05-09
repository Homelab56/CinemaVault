/**
 * CinemaVault Cache Utility
 * Handles client-side caching with TTL and storage management
 */

window.CinemaVaultCache = (function() {
    'use strict';

    const STORAGE_PREFIX = 'cinemavault_cache_';
    const DEFAULT_TTL = 5 * 60 * 1000; // 5 minutes
    const MAX_CACHE_SIZE = 50 * 1024 * 1024; // 50MB
    const CLEANUP_THRESHOLD = 0.8; // Clean up when 80% full

    let memoryCache = new Map();
    let storageAvailable = false;

    // Check if localStorage is available
    try {
        const test = '__cinemavault_test__';
        localStorage.setItem(test, test);
        localStorage.removeItem(test);
        storageAvailable = true;
    } catch (e) {
        console.warn('localStorage not available, using memory cache only');
    }

    /**
     * Cache entry structure
     */
    class CacheEntry {
        constructor(data, ttl = DEFAULT_TTL) {
            this.data = data;
            this.timestamp = Date.now();
            this.ttl = ttl;
            this.accessCount = 0;
            this.lastAccessed = Date.now();
        }

        isExpired() {
            return Date.now() - this.timestamp > this.ttl;
        }

        isValid() {
            return !this.isExpired();
        }

        touch() {
            this.accessCount++;
            this.lastAccessed = Date.now();
        }
    }

    /**
     * Get cache key
     */
    function getCacheKey(key) {
        return `${STORAGE_PREFIX}${key}`;
    }

    /**
     * Calculate cache size
     */
    function calculateCacheSize() {
        let size = 0;
        
        // Memory cache size
        memoryCache.forEach((entry, key) => {
            size += key.length * 2; // UTF-16 characters
            size += JSON.stringify(entry.data).length * 2;
            size += 64; // Entry metadata
        });
        
        // localStorage size
        if (storageAvailable) {
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith(STORAGE_PREFIX)) {
                    size += key.length * 2;
                    size += (localStorage.getItem(key) || '').length * 2;
                }
            }
        }
        
        return size;
    }

    /**
     * Clean up expired entries
     */
    function cleanupExpired() {
        // Clean memory cache
        for (const [key, entry] of memoryCache.entries()) {
            if (entry.isExpired()) {
                memoryCache.delete(key);
            }
        }
        
        // Clean localStorage
        if (storageAvailable) {
            const keysToRemove = [];
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith(STORAGE_PREFIX)) {
                    try {
                        const entry = JSON.parse(localStorage.getItem(key) || '{}');
                        if (new CacheEntry(entry.data, entry.ttl).isExpired()) {
                            keysToRemove.push(key);
                        }
                    } catch (e) {
                        keysToRemove.push(key); // Remove corrupted entries
                    }
                }
            }
            
            keysToRemove.forEach(key => localStorage.removeItem(key));
        }
    }

    /**
     * Clean up least recently used entries
     */
    function cleanupLRU(count = 10) {
        // Sort by last accessed time
        const sortedEntries = Array.from(memoryCache.entries())
            .sort((a, b) => a[1].lastAccessed - b[1].lastAccessed);
        
        // Remove oldest entries
        for (let i = 0; i < Math.min(count, sortedEntries.length); i++) {
            memoryCache.delete(sortedEntries[i][0]);
        }
        
        // Similar cleanup for localStorage
        if (storageAvailable) {
            const entries = [];
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith(STORAGE_PREFIX)) {
                    try {
                        const entry = JSON.parse(localStorage.getItem(key) || '{}');
                        entries.push({ key, entry });
                    } catch (e) {
                        localStorage.removeItem(key);
                    }
                }
            }
            
            entries.sort((a, b) => a.entry.lastAccessed - b.entry.lastAccessed);
            
            for (let i = 0; i < Math.min(count, entries.length); i++) {
                localStorage.removeItem(entries[i].key);
            }
        }
    }

    /**
     * Check if cleanup is needed
     */
    function checkCleanupNeeded() {
        const currentSize = calculateCacheSize();
        
        if (currentSize > MAX_CACHE_SIZE * CLEANUP_THRESHOLD) {
            cleanupExpired();
            
            // Still too big? Remove LRU entries
            const newSize = calculateCacheSize();
            if (newSize > MAX_CACHE_SIZE * CLEANUP_THRESHOLD) {
                cleanupLRU(Math.ceil(memoryCache.size * 0.3)); // Remove 30% of entries
            }
        }
    }

    /**
     * Set cache entry
     */
    function set(key, data, ttl = DEFAULT_TTL, useStorage = false) {
        const entry = new CacheEntry(data, ttl);
        
        // Always store in memory cache
        memoryCache.set(key, entry);
        
        // Optionally store in localStorage
        if (useStorage && storageAvailable) {
            try {
                localStorage.setItem(getCacheKey(key), JSON.stringify({
                    data: entry.data,
                    timestamp: entry.timestamp,
                    ttl: entry.ttl,
                    accessCount: entry.accessCount,
                    lastAccessed: entry.lastAccessed
                }));
            } catch (e) {
                console.warn('Failed to store in localStorage:', e);
            }
        }
        
        checkCleanupNeeded();
    }

    /**
     * Get cache entry
     */
    function get(key) {
        // Check memory cache first
        let entry = memoryCache.get(key);
        
        if (!entry && storageAvailable) {
            // Try localStorage
            try {
                const stored = localStorage.getItem(getCacheKey(key));
                if (stored) {
                    const parsed = JSON.parse(stored);
                    entry = new CacheEntry(parsed.data, parsed.ttl);
                    entry.timestamp = parsed.timestamp;
                    entry.accessCount = parsed.accessCount || 0;
                    entry.lastAccessed = parsed.lastAccessed || parsed.timestamp;
                    
                    // Move to memory cache if still valid
                    if (entry.isValid()) {
                        memoryCache.set(key, entry);
                    } else {
                        localStorage.removeItem(getCacheKey(key));
                        return null;
                    }
                }
            } catch (e) {
                console.warn('Failed to retrieve from localStorage:', e);
                localStorage.removeItem(getCacheKey(key));
            }
        }
        
        if (entry && entry.isValid()) {
            entry.touch();
            return entry.data;
        }
        
        if (entry) {
            memoryCache.delete(key);
            if (storageAvailable) {
                localStorage.removeItem(getCacheKey(key));
            }
        }
        
        return null;
    }

    /**
     * Check if key exists and is valid
     */
    function has(key) {
        const entry = memoryCache.get(key);
        if (entry && entry.isValid()) {
            return true;
        }
        
        if (storageAvailable) {
            try {
                const stored = localStorage.getItem(getCacheKey(key));
                if (stored) {
                    const parsed = JSON.parse(stored);
                    const cacheEntry = new CacheEntry(parsed.data, parsed.ttl);
                    cacheEntry.timestamp = parsed.timestamp;
                    return cacheEntry.isValid();
                }
            } catch (e) {
                localStorage.removeItem(getCacheKey(key));
            }
        }
        
        return false;
    }

    /**
     * Remove cache entry
     */
    function remove(key) {
        memoryCache.delete(key);
        if (storageAvailable) {
            localStorage.removeItem(getCacheKey(key));
        }
    }

    /**
     * Clear all cache
     */
    function clear() {
        memoryCache.clear();
        
        if (storageAvailable) {
            const keysToRemove = [];
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith(STORAGE_PREFIX)) {
                    keysToRemove.push(key);
                }
            }
            keysToRemove.forEach(key => localStorage.removeItem(key));
        }
    }

    /**
     * Clear expired entries
     */
    function clearExpired() {
        cleanupExpired();
    }

    /**
     * Get cache statistics
     */
    function getStats() {
        const memoryEntries = memoryCache.size;
        let storageEntries = 0;
        
        if (storageAvailable) {
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith(STORAGE_PREFIX)) {
                    storageEntries++;
                }
            }
        }
        
        let totalAccesses = 0;
        memoryCache.forEach(entry => {
            totalAccesses += entry.accessCount;
        });
        
        return {
            memoryEntries,
            storageEntries,
            totalEntries: memoryEntries + storageEntries,
            totalSize: calculateCacheSize(),
            maxSize: MAX_CACHE_SIZE,
            usagePercentage: (calculateCacheSize() / MAX_CACHE_SIZE) * 100,
            totalAccesses
        };
    }

    /**
     * Export cache data (for backup)
     */
    function exportCache() {
        const data = {
            memoryCache: Array.from(memoryCache.entries()).map(([key, entry]) => ({
                key,
                data: entry.data,
                timestamp: entry.timestamp,
                ttl: entry.ttl,
                accessCount: entry.accessCount,
                lastAccessed: entry.lastAccessed
            })),
            storageCache: {}
        };
        
        if (storageAvailable) {
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.startsWith(STORAGE_PREFIX)) {
                    const cleanKey = key.replace(STORAGE_PREFIX, '');
                    try {
                        data.storageCache[cleanKey] = JSON.parse(localStorage.getItem(key) || '{}');
                    } catch (e) {
                        console.warn('Failed to export cache entry:', key);
                    }
                }
            }
        }
        
        return data;
    }

    /**
     * Import cache data (for restore)
     */
    function importCache(data) {
        clear();
        
        // Import memory cache
        if (data.memoryCache) {
            data.memoryCache.forEach(item => {
                const entry = new CacheEntry(item.data, item.ttl);
                entry.timestamp = item.timestamp;
                entry.accessCount = item.accessCount;
                entry.lastAccessed = item.lastAccessed;
                memoryCache.set(item.key, entry);
            });
        }
        
        // Import storage cache
        if (data.storageCache && storageAvailable) {
            Object.entries(data.storageCache).forEach(([key, value]) => {
                try {
                    localStorage.setItem(getCacheKey(key), JSON.stringify(value));
                } catch (e) {
                    console.warn('Failed to import cache entry:', key);
                }
            });
        }
        
        checkCleanupNeeded();
    }

    /**
     * Initialize cache with cleanup
     */
    function init() {
        cleanupExpired();
        
        // Run cleanup every 5 minutes
        setInterval(cleanupExpired, 5 * 60 * 1000);
    }

    // Auto-initialize
    init();

    // Public API
    return {
        set,
        get,
        has,
        remove,
        clear,
        clearExpired,
        getStats,
        export: exportCache,
        import: importCache,
        
        // Constants
        DEFAULT_TTL,
        MAX_CACHE_SIZE
    };
})();
