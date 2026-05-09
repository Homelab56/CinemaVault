/**
 * CinemaVault Bootstrap Script
 * Injects CinemaVault into Jellyfin Web UI
 */

(function() {
    'use strict';
    
    // Wait for Jellyfin's main UI to load
    function waitForJellyfin(callback, maxWait = 10000) {
        const start = Date.now();
        const check = () => {
            // Check for various Jellyfin UI indicators
            if (document.getElementById('reactRoot') || 
                document.querySelector('.skinBody') ||
                document.querySelector('.mainAnimatedPages') ||
                window.ApiClient ||
                window.Emby) {
                callback();
            } else if (Date.now() - start < maxWait) {
                requestAnimationFrame(check);
            } else {
                console.warn('CinemaVault: Jellyfin UI not detected after timeout');
            }
        };
        requestAnimationFrame(check);
    }

    // Intercept Jellyfin's routing
    function interceptRouting() {
        const originalHash = window.location.hash;
        
        // Handle hash changes
        window.addEventListener('hashchange', (e) => {
            const hash = window.location.hash;
            if (shouldInterceptHash(hash)) {
                e.preventDefault();
                loadCinemaVault();
                return false;
            }
        });
        
        // Override Jellyfin's internal navigation if available
        if (window.Emby && window.Emby.Page) {
            const originalShow = window.Emby.Page.show;
            window.Emby.Page.show = function(path, ...args) {
                if (shouldInterceptPath(path)) {
                    loadCinemaVault();
                    return;
                }
                return originalShow.call(this, path, ...args);
            };
        }
        
        // Override Emby Router if available
        if (window.Emby && window.Emby.Router) {
            const originalGo = window.Emby.Router.go;
            window.Emby.Router.go = function(path, ...args) {
                if (shouldInterceptPath(path)) {
                    loadCinemaVault();
                    return;
                }
                return originalGo.call(this, path, ...args);
            };
        }
        
        // Check initial hash
        if (shouldInterceptHash(originalHash)) {
            loadCinemaVault();
        }
    }

    // Check if hash should be intercepted
    function shouldInterceptHash(hash) {
        return hash === '#/home.html' || 
               hash === '#/' || 
               hash === '' ||
               hash.startsWith('#/cinemavault');
    }

    // Check if path should be intercepted
    function shouldInterceptPath(path) {
        return path && (
            path.includes('home.html') || 
            path === '/' ||
            path.startsWith('/cinemavault')
        );
    }

    // Load CinemaVault
    function loadCinemaVault() {
        try {
            console.log('CinemaVault: Loading UI...');
            
            // Get or create the CinemaVault container
            let container = document.getElementById('cinemavault-root');
            if (!container) {
                container = document.createElement('div');
                container.id = 'cinemavault-root';
                container.style.display = 'none';
                document.body.appendChild(container);
            }

            // Hide Jellyfin's default home content
            hideJellyfinHome();

            // Load CinemaVault CSS
            loadCSS('/CinemaVault/Web/assets/cinemavault.css', () => {
                // Load main CinemaVault script
                loadScript('/CinemaVault/Web/assets/cinemavault.js', () => {
                    // Show CinemaVault container
                    container.style.display = 'block';
                    container.className = 'cv-root';
                    
                    console.log('CinemaVault: UI loaded successfully');
                });
            });

        } catch (error) {
            console.error('CinemaVault: Error loading UI:', error);
        }
    }

    // Hide Jellyfin's default home content
    function hideJellyfinHome() {
        const selectors = [
            '.homePage',
            '.pageWithAbsoluteTabs',
            '#indexPage',
            '.dashboardHome',
            '.mainAnimatedPages',
            '.libraryPage:not(.cinemavault-processed)'
        ];
        
        selectors.forEach(selector => {
            try {
                const elements = document.querySelectorAll(selector);
                elements.forEach(el => {
                    if (el.offsetParent !== null) { // Only hide visible elements
                        el.style.display = 'none';
                        el.setAttribute('data-cinemavault-hidden', 'true');
                        el.classList.add('cinemavault-processed');
                    }
                });
            } catch (error) {
                console.warn('CinemaVault: Error hiding element:', selector, error);
            }
        });
    }

    // Show Jellyfin's default home content
    function showJellyfinHome() {
        const elements = document.querySelectorAll('[data-cinemavault-hidden="true"]');
        elements.forEach(el => {
            try {
                el.style.display = '';
                el.removeAttribute('data-cinemavault-hidden');
            } catch (error) {
                console.warn('CinemaVault: Error showing element:', error);
            }
        });
    }

    // Load CSS file
    function loadCSS(url, callback) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = url;
        link.onload = callback;
        link.onerror = () => {
            console.error('CinemaVault: Failed to load CSS:', url);
            callback();
        };
        
        // Add to head
        const head = document.head || document.getElementsByTagName('head')[0];
        if (head) {
            head.appendChild(link);
        }
    }

    // Load JavaScript file
    function loadScript(url, callback) {
        const script = document.createElement('script');
        script.src = url;
        script.onload = callback;
        script.onerror = () => {
            console.error('CinemaVault: Failed to load script:', url);
            callback();
        };
        
        // Add to head
        const head = document.head || document.getElementsByTagName('head')[0];
        if (head) {
            head.appendChild(script);
        }
    }

    // Initialize CinemaVault bootstrap
    function init() {
        console.log('CinemaVault: Bootstrap initializing...');
        
        waitForJellyfin(() => {
            console.log('CinemaVault: Jellyfin detected, setting up interception...');
            interceptRouting();
        });
    }

    // Expose some functions globally for debugging
    window.CinemaVaultBootstrap = {
        loadCinemaVault,
        hideJellyfinHome,
        showJellyfinHome,
        init
    };

    // Auto-initialize
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        // DOM already loaded, initialize after a short delay
        setTimeout(init, 500);
    }

})();
