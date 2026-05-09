/**
 * CinemaVault Router
 * Handles client-side routing and navigation
 */

window.CinemaVaultRouter = (function() {
    'use strict';

    const routes = new Map();
    let currentRoute = null;
    let previousRoute = null;
    let notFoundHandler = null;

    /**
     * Route class
     */
    class Route {
        constructor(path, handler, options = {}) {
            this.path = path;
            this.handler = handler;
            this.params = {};
            this.queryParams = {};
            this.options = {
                title: options.title || 'CinemaVault',
                auth: options.auth !== false, // Default to requiring auth
                cache: options.cache !== false, // Default to caching
                ...options
            };
        }

        /**
         * Check if this route matches the given path
         */
        matches(path) {
            // Convert route path to regex
            const routeRegex = this.pathToRegex(this.path);
            const match = path.match(routeRegex);
            
            if (!match) return false;

            // Extract parameters
            this.params = this.extractParams(this.path, match);
            
            // Extract query parameters
            const [pathWithoutQuery, queryString] = path.split('?');
            this.queryParams = this.parseQueryString(queryString || '');
            
            return true;
        }

        /**
         * Convert route path to regex
         */
        pathToRegex(path) {
            const regexPath = path
                .replace(/:[^\/]+/g, '([^/]+)') // Replace :param with capture group
                .replace(/\*/g, '(.*)') // Replace * with wildcard
                .replace(/\//g, '\\/'); // Escape slashes
            
            return new RegExp(`^${regexPath}$`);
        }

        /**
         * Extract parameters from route match
         */
        extractParams(routePath, match) {
            const params = {};
            const paramNames = (routePath.match(/:[^\/]+/g) || [])
                .map(name => name.substring(1)); // Remove ':' prefix
            
            paramNames.forEach((name, index) => {
                params[name] = match[index + 1]; // Skip full match
            });
            
            return params;
        }

        /**
         * Parse query string
         */
        parseQueryString(queryString) {
            const params = {};
            const pairs = queryString.split('&');
            
            pairs.forEach(pair => {
                const [key, value] = pair.split('=');
                if (key) {
                    params[decodeURIComponent(key)] = decodeURIComponent(value || '');
                }
            });
            
            return params;
        }

        /**
         * Execute the route handler
         */
        async execute() {
            try {
                // Check authentication if required
                if (this.options.auth) {
                    const user = await CinemaVaultJellyfin.getCurrentUser();
                    if (!user) {
                        // Redirect to login or show auth required
                        this.showAuthRequired();
                        return false;
                    }
                }

                // Set page title
                document.title = this.options.title;

                // Execute handler
                await this.handler(this.params, this.queryParams);
                
                return true;
            } catch (error) {
                console.error('Route execution error:', error);
                this.showError(error);
                return false;
            }
        }

        /**
         * Show authentication required message
         */
        showAuthRequired() {
            const container = document.getElementById('cinemavault-root');
            if (container) {
                container.innerHTML = `
                    <div class="cv-auth-required">
                        <div class="cv-auth-card">
                            <h2>Authentication Required</h2>
                            <p>Please sign in to access CinemaVault.</p>
                            <button class="cv-btn cv-btn-primary" onclick="window.location.href='/#!/login.html'">
                                Sign In
                            </button>
                        </div>
                    </div>
                `;
            }
        }

        /**
         * Show error message
         */
        showError(error) {
            const container = document.getElementById('cinemavault-root');
            if (container) {
                container.innerHTML = `
                    <div class="cv-error">
                        <div class="cv-error-card">
                            <h2>Error</h2>
                            <p>${error.message || 'An unexpected error occurred'}</p>
                            <button class="cv-btn cv-btn-secondary" onclick="window.CinemaVaultRouter.navigate('/')">
                                Go Home
                            </button>
                        </div>
                    </div>
                `;
            }
        }
    }

    /**
     * Register a new route
     */
    function register(path, handler, options = {}) {
        const route = new Route(path, handler, options);
        routes.set(path, route);
        return route;
    }

    /**
     * Navigate to a specific route
     */
    async function navigate(path, replace = false) {
        // Normalize path
        path = path.startsWith('/') ? path : '/' + path;
        
        // Find matching route
        let matchedRoute = null;
        for (const route of routes.values()) {
            if (route.matches(path)) {
                matchedRoute = route;
                break;
            }
        }

        if (!matchedRoute) {
            if (notFoundHandler) {
                await notFoundHandler(path);
            } else {
                showNotFound(path);
            }
            return false;
        }

        // Update browser history
        if (replace) {
            history.replaceState({ path }, '', path);
        } else {
            history.pushState({ path }, '', path);
        }

        // Execute route
        previousRoute = currentRoute;
        currentRoute = matchedRoute;
        
        const success = await matchedRoute.execute();
        
        // Scroll to top
        window.scrollTo(0, 0);
        
        return success;
    }

    /**
     * Go back to previous route
     */
    function back() {
        history.back();
    }

    /**
     * Go forward
     */
    function forward() {
        history.forward();
    }

    /**
     * Reload current route
     */
    async function reload() {
        if (currentRoute) {
            await currentRoute.execute();
        }
    }

    /**
     * Get current route
     */
    function getCurrentRoute() {
        return currentRoute;
    }

    /**
     * Get current path
     */
    function getCurrentPath() {
        return window.location.pathname + window.location.search;
    }

    /**
     * Get route parameters
     */
    function getParams() {
        return currentRoute ? currentRoute.params : {};
    }

    /**
     * Get query parameters
     */
    function getQueryParams() {
        return currentRoute ? currentRoute.queryParams : {};
    }

    /**
     * Set 404 handler
     */
    function setNotFoundHandler(handler) {
        notFoundHandler = handler;
    }

    /**
     * Show 404 page
     */
    function showNotFound(path) {
        const container = document.getElementById('cinemavault-root');
        if (container) {
            container.innerHTML = `
                <div class="cv-404">
                    <div class="cv-404-card">
                        <h1>404</h1>
                        <h2>Page Not Found</h2>
                        <p>The page <code>${path}</code> could not be found.</p>
                        <div class="cv-404-actions">
                            <button class="cv-btn cv-btn-primary" onclick="window.CinemaVaultRouter.navigate('/')">
                                Go Home
                            </button>
                            <button class="cv-btn cv-btn-secondary" onclick="window.history.back()">
                                Go Back
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }
    }

    /**
     * Initialize router
     */
    function init() {
        // Handle browser navigation
        window.addEventListener('popstate', async (event) => {
            const path = getCurrentPath();
            await navigate(path, true);
        });

        // Handle initial route
        const initialPath = getCurrentPath();
        navigate(initialPath, true);

        // Handle link clicks for SPA navigation
        document.addEventListener('click', (event) => {
            const link = event.target.closest('a');
            if (!link) return;

            const href = link.getAttribute('href');
            if (!href) return;

            // Only handle internal links
            if (href.startsWith('/') && !href.startsWith('//')) {
                event.preventDefault();
                navigate(href);
            }
        });
    }

    /**
     * Generate URL for route with parameters
     */
    function generateUrl(routeName, params = {}, queryParams = {}) {
        const route = routes.get(routeName);
        if (!route) {
            throw new Error(`Route '${routeName}' not found`);
        }

        let url = route.path;

        // Replace route parameters
        Object.entries(params).forEach(([key, value]) => {
            url = url.replace(`:${key}`, encodeURIComponent(value));
        });

        // Add query parameters
        const queryString = Object.entries(queryParams)
            .filter(([_, value]) => value !== undefined && value !== null)
            .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
            .join('&');

        if (queryString) {
            url += '?' + queryString;
        }

        return url;
    }

    /**
     * Check if route exists
     */
    function hasRoute(path) {
        return routes.has(path);
    }

    /**
     * Get all registered routes
     */
    function getRoutes() {
        return Array.from(routes.keys());
    }

    /**
     * Clear all routes
     */
    function clearRoutes() {
        routes.clear();
        currentRoute = null;
        previousRoute = null;
    }

    /**
     * Add route guard
     */
    function addGuard(path, guard) {
        const route = routes.get(path);
        if (route) {
            const originalHandler = route.handler;
            route.handler = async (params, queryParams) => {
                const canProceed = await guard(params, queryParams);
                if (canProceed) {
                    return await originalHandler(params, queryParams);
                }
                return false;
            };
        }
    }

    /**
     * Add middleware
     */
    function addMiddleware(middleware) {
        const originalHandlers = new Map();
        
        routes.forEach((route, path) => {
            originalHandlers.set(path, route.handler);
            route.handler = async (params, queryParams) => {
                await middleware(params, queryParams, () => {
                    return originalHandlers.get(path)(params, queryParams);
                });
            };
        });
    }

    // Public API
    return {
        register,
        navigate,
        back,
        forward,
        reload,
        getCurrentRoute,
        getCurrentPath,
        getParams,
        getQueryParams,
        setNotFoundHandler,
        generateUrl,
        hasRoute,
        getRoutes,
        clearRoutes,
        addGuard,
        addMiddleware,
        init
    };
})();

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        CinemaVaultRouter.init();
    });
} else {
    CinemaVaultRouter.init();
}
