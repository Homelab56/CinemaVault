/**
 * CinemaVault Hero Banner Component
 * Handles the rotating hero banner with featured content
 */

window.CinemaVaultHero = (function() {
    'use strict';

    let container = null;
    let items = [];
    let currentIndex = 0;
    let rotationInterval = null;
    let isPaused = false;
    let isTransitioning = false;
    let indicators = null;
    let prevBtn = null;
    let nextBtn = null;
    let pauseBtn = null;

    const ROTATION_INTERVAL = 8000; // 8 seconds
    const TRANSITION_DURATION = 1000; // 1 second

    /**
     * Initialize hero banner
     */
    async function init(parentContainer) {
        container = document.createElement('div');
        container.className = 'cv-hero';
        container.innerHTML = getHeroHTML();
        
        parentContainer.appendChild(container);
        
        // Load hero content
        await loadHeroContent();
        
        if (items.length === 0) {
            container.style.display = 'none';
            return;
        }
        
        // Initialize components
        initializeControls();
        initializeIndicators();
        
        // Start rotation
        startRotation();
        
        // Show first item
        showItem(0);
    }

    /**
     * Get hero HTML
     */
    function getHeroHTML() {
        return `
            <div class="cv-hero-container">
                <div class="cv-hero-content" id="cvHeroContent">
                    <!-- Hero items will be inserted here -->
                </div>
                
                <div class="cv-hero-overlay">
                    <div class="cv-hero-gradient"></div>
                </div>
                
                <div class="cv-hero-info">
                    <div class="cv-hero-info-content">
                        <div class="cv-hero-badges" id="cvHeroBadges"></div>
                        <h1 class="cv-hero-title" id="cvHeroTitle"></h1>
                        <p class="cv-hero-tagline" id="cvHeroTagline"></p>
                        <div class="cv-hero-meta" id="cvHeroMeta"></div>
                        <p class="cv-hero-description" id="cvHeroDescription"></p>
                        <div class="cv-hero-actions" id="cvHeroActions"></div>
                    </div>
                </div>
                
                <div class="cv-hero-controls">
                    <button class="cv-hero-btn cv-hero-prev" id="cvHeroPrev">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <polyline points="15 18 9 12 15 6"></polyline>
                        </svg>
                    </button>
                    <button class="cv-hero-btn cv-hero-next" id="cvHeroNext">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <polyline points="9 18 15 12 9 6"></polyline>
                        </svg>
                    </button>
                    <button class="cv-hero-btn cv-hero-pause" id="cvHeroPause">
                        <svg class="cv-pause-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <rect x="6" y="4" width="4" height="16"></rect>
                            <rect x="14" y="4" width="4" height="16"></rect>
                        </svg>
                        <svg class="cv-play-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" style="display: none;">
                            <polygon points="5 3 19 12 5 21 5 3"></polygon>
                        </svg>
                    </button>
                </div>
                
                <div class="cv-hero-indicators" id="cvHeroIndicators">
                    <!-- Indicators will be inserted here -->
                </div>
            </div>
        `;
    }

    /**
     * Load hero content
     */
    async function loadHeroContent() {
        try {
            const heroItems = await CinemaVaultAPI.hero.get();
            items = heroItems || [];
        } catch (error) {
            console.error('Error loading hero content:', error);
            items = [];
        }
    }

    /**
     * Initialize controls
     */
    function initializeControls() {
        prevBtn = document.getElementById('cvHeroPrev');
        nextBtn = document.getElementById('cvHeroNext');
        pauseBtn = document.getElementById('cvHeroPause');
        
        if (prevBtn) {
            prevBtn.addEventListener('click', () => previousItem());
        }
        
        if (nextBtn) {
            nextBtn.addEventListener('click', () => nextItem());
        }
        
        if (pauseBtn) {
            pauseBtn.addEventListener('click', togglePause);
        }
        
        // Keyboard navigation
        document.addEventListener('keydown', (e) => {
            if (e.key === 'ArrowLeft') {
                previousItem();
            } else if (e.key === 'ArrowRight') {
                nextItem();
            } else if (e.key === ' ') {
                e.preventDefault();
                togglePause();
            }
        });
        
        // Touch/swipe support
        let touchStartX = 0;
        let touchEndX = 0;
        
        container.addEventListener('touchstart', (e) => {
            touchStartX = e.changedTouches[0].screenX;
        });
        
        container.addEventListener('touchend', (e) => {
            touchEndX = e.changedTouches[0].screenX;
            handleSwipe();
        });
        
        function handleSwipe() {
            const swipeThreshold = 50;
            const diff = touchStartX - touchEndX;
            
            if (Math.abs(diff) > swipeThreshold) {
                if (diff > 0) {
                    nextItem();
                } else {
                    previousItem();
                }
            }
        }
    }

    /**
     * Initialize indicators
     */
    function initializeIndicators() {
        indicators = document.getElementById('cvHeroIndicators');
        if (!indicators || items.length === 0) return;
        
        const indicatorsHTML = items.map((_, index) => `
            <button class="cv-hero-indicator ${index === 0 ? 'cv-hero-indicator-active' : ''}" 
                    data-index="${index}">
                <span class="cv-sr-only">Slide ${index + 1}</span>
            </button>
        `).join('');
        
        indicators.innerHTML = indicatorsHTML;
        
        // Add click handlers
        indicators.querySelectorAll('.cv-hero-indicator').forEach(indicator => {
            indicator.addEventListener('click', () => {
                const index = parseInt(indicator.dataset.index);
                showItem(index);
            });
        });
    }

    /**
     * Show specific item
     */
    function showItem(index) {
        if (isTransitioning || items.length === 0) return;
        
        isTransitioning = true;
        
        const item = items[index];
        if (!item) {
            isTransitioning = false;
            return;
        }
        
        // Update content
        updateHeroContent(item);
        
        // Update indicators
        updateIndicators(index);
        
        // Update background
        updateBackground(item);
        
        // Update current index
        currentIndex = index;
        
        // Reset transition flag
        setTimeout(() => {
            isTransitioning = false;
        }, TRANSITION_DURATION);
    }

    /**
     * Update hero content
     */
    function updateHeroContent(item) {
        const badges = document.getElementById('cvHeroBadges');
        const title = document.getElementById('cvHeroTitle');
        const tagline = document.getElementById('cvHeroTagline');
        const meta = document.getElementById('cvHeroMeta');
        const description = document.getElementById('cvHeroDescription');
        const actions = document.getElementById('cvHeroActions');
        
        // Update badges
        if (badges) {
            badges.innerHTML = item.qualityBadges.map(badge => 
                `<span class="cv-quality-badge">${badge}</span>`
            ).join('');
        }
        
        // Update title
        if (title) title.textContent = item.title;
        
        // Update tagline
        if (tagline) {
            tagline.textContent = item.tagline || '';
            tagline.style.display = item.tagline ? 'block' : 'none';
        }
        
        // Update meta
        if (meta) {
            const metaItems = [];
            if (item.year) metaItems.push(item.year);
            if (item.runtime) metaItems.push(CinemaVaultAPI.utils.formatRuntime(item.runtime));
            if (item.genres && item.genres.length > 0) {
                metaItems.push(item.genres.slice(0, 3).join(' • '));
            }
            
            meta.innerHTML = metaItems.join(' • ');
        }
        
        // Update description
        if (description) {
            description.textContent = item.overview || '';
        }
        
        // Update actions
        if (actions) {
            actions.innerHTML = getActionsHTML(item);
            
            // Add action handlers
            const primaryBtn = actions.querySelector('.cv-hero-btn-primary');
            const secondaryBtn = actions.querySelector('.cv-hero-btn-secondary');
            
            if (primaryBtn) {
                primaryBtn.addEventListener('click', () => handlePrimaryAction(item));
            }
            
            if (secondaryBtn) {
                secondaryBtn.addEventListener('click', () => {
                    CinemaVaultModal.showDetails(item.tmdbId, item.type);
                });
            }
        }
    }

    /**
     * Get actions HTML
     */
    function getActionsHTML(item) {
        const primaryText = item.primaryAction;
        const primaryIcon = item.primaryActionType === 'play' ? '▶' : '＋';
        
        return `
            <button class="cv-hero-btn cv-hero-btn-primary">
                <span class="cv-hero-btn-icon">${primaryIcon}</span>
                <span class="cv-hero-btn-text">${primaryText}</span>
            </button>
            <button class="cv-hero-btn cv-hero-btn-secondary">
                <span class="cv-hero-btn-icon">ⓘ</span>
                <span class="cv-hero-btn-text">More Info</span>
            </button>
        `;
    }

    /**
     * Handle primary action
     */
    async function handlePrimaryAction(item) {
        try {
            if (item.primaryActionType === 'play' && item.jellyfinId) {
                // Play in Jellyfin
                await CinemaVaultJellyfin.playItem(item.jellyfinId);
            } else if (item.primaryActionType === 'request') {
                // Request content
                const success = await CinemaVaultAPI.requests.create({
                    tmdbId: item.tmdbId,
                    type: item.type
                });
                
                if (success) {
                    CinemaVaultToast.show('Request sent successfully!', 'success');
                    // Update button state
                    updateRequestButton(item.tmdbId, item.type, 'requested');
                } else {
                    CinemaVaultToast.show('Failed to send request', 'error');
                }
            }
        } catch (error) {
            console.error('Error handling primary action:', error);
            CinemaVaultToast.show('An error occurred', 'error');
        }
    }

    /**
     * Update request button state
     */
    function updateRequestButton(tmdbId, type, status) {
        const actions = document.getElementById('cvHeroActions');
        if (!actions) return;
        
        const primaryBtn = actions.querySelector('.cv-hero-btn-primary');
        if (!primaryBtn) return;
        
        const currentItem = items.find(item => item.tmdbId === tmdbId && item.type === type);
        if (!currentItem) return;
        
        if (status === 'requested') {
            currentItem.primaryAction = 'Requested';
            currentItem.primaryActionType = 'requested';
            primaryBtn.querySelector('.cv-hero-btn-icon').textContent = '✓';
            primaryBtn.querySelector('.cv-hero-btn-text').textContent = 'Requested';
            primaryBtn.disabled = true;
            primaryBtn.classList.add('cv-hero-btn-disabled');
        }
    }

    /**
     * Update background
     */
    function updateBackground(item) {
        const content = document.getElementById('cvHeroContent');
        if (!content) return;
        
        const backdropUrl = CinemaVaultAPI.utils.getBackdropUrl(item.backdropPath, 'w1280');
        if (!backdropUrl) return;
        
        // Create new background element
        const newBg = document.createElement('div');
        newBg.className = 'cv-hero-background';
        newBg.style.backgroundImage = `url(${backdropUrl})`;
        
        // Add to content
        content.appendChild(newBg);
        
        // Fade in
        requestAnimationFrame(() => {
            newBg.style.opacity = '1';
        });
        
        // Remove old background after transition
        setTimeout(() => {
            const oldBg = content.querySelector('.cv-hero-background:not(:last-child)');
            if (oldBg) {
                oldBg.remove();
            }
        }, TRANSITION_DURATION);
    }

    /**
     * Update indicators
     */
    function updateIndicators(index) {
        if (!indicators) return;
        
        const indicatorItems = indicators.querySelectorAll('.cv-hero-indicator');
        indicatorItems.forEach((item, i) => {
            if (i === index) {
                item.classList.add('cv-hero-indicator-active');
            } else {
                item.classList.remove('cv-hero-indicator-active');
            }
        });
    }

    /**
     * Show next item
     */
    function nextItem() {
        const nextIndex = (currentIndex + 1) % items.length;
        showItem(nextIndex);
        resetRotation();
    }

    /**
     * Show previous item
     */
    function previousItem() {
        const prevIndex = (currentIndex - 1 + items.length) % items.length;
        showItem(prevIndex);
        resetRotation();
    }

    /**
     * Toggle pause/play
     */
    function togglePause() {
        isPaused = !isPaused;
        
        if (isPaused) {
            stopRotation();
            pauseBtn.querySelector('.cv-pause-icon').style.display = 'none';
            pauseBtn.querySelector('.cv-play-icon').style.display = 'block';
            pauseBtn.classList.add('cv-hero-pause-playing');
        } else {
            startRotation();
            pauseBtn.querySelector('.cv-pause-icon').style.display = 'block';
            pauseBtn.querySelector('.cv-play-icon').style.display = 'none';
            pauseBtn.classList.remove('cv-hero-pause-playing');
        }
    }

    /**
     * Start rotation
     */
    function startRotation() {
        if (rotationInterval) {
            clearInterval(rotationInterval);
        }
        
        rotationInterval = setInterval(() => {
            if (!isPaused && !isTransitioning) {
                nextItem();
            }
        }, ROTATION_INTERVAL);
    }

    /**
     * Stop rotation
     */
    function stopRotation() {
        if (rotationInterval) {
            clearInterval(rotationInterval);
            rotationInterval = null;
        }
    }

    /**
     * Reset rotation timer
     */
    function resetRotation() {
        if (!isPaused) {
            stopRotation();
            startRotation();
        }
    }

    /**
     * Pause on hover
     */
    function pauseOnHover() {
        container.addEventListener('mouseenter', () => {
            if (!isPaused) {
                stopRotation();
            }
        });
        
        container.addEventListener('mouseleave', () => {
            if (!isPaused) {
                startRotation();
            }
        });
    }

    /**
     * Destroy hero banner
     */
    function destroy() {
        stopRotation();
        
        if (container) {
            container.remove();
            container = null;
        }
        
        items = [];
        currentIndex = 0;
        isPaused = false;
        isTransitioning = false;
    }

    // Public API
    return {
        init,
        destroy,
        nextItem,
        previousItem,
        togglePause
    };
})();
