/**
 * CinemaVault Card Component
 * Handles individual content cards with hover effects and actions
 */

window.CinemaVaultCard = (function() {
    'use strict';

    /**
     * Create a content card
     */
    function create(item, options = {}) {
        const card = document.createElement('div');
        card.className = 'cv-card';
        card.dataset.tmdbId = item.tmdbId;
        card.dataset.type = item.type;
        
        // Apply custom options
        const cardOptions = {
            showProgress: options.showProgress !== false,
            showStatus: options.showStatus !== false,
            showRating: options.showRating !== false,
            showRuntime: options.showRuntime !== false,
            lazyLoad: options.lazyLoad !== false,
            size: options.size || 'normal', // normal, small, large
            ...options
        };
        
        card.innerHTML = getCardHTML(item, cardOptions);
        
        // Initialize card functionality
        initializeCard(card, item, cardOptions);
        
        return card;
    }

    /**
     * Get card HTML
     */
    function getCardHTML(item, options) {
        const posterUrl = CinemaVaultAPI.utils.getImageUrl(item.posterPath, 'w342');
        const statusColor = CinemaVaultAPI.utils.getStatusColor(item.status);
        const statusText = CinemaVaultAPI.utils.getStatusText(item.status);
        const progressPercent = item.lastEpisode ? item.lastEpisode.watchedPercentage : 0;
        const sizeClass = `cv-card-${options.size}`;
        
        return `
            <div class="cv-card-poster ${sizeClass}">
                <img src="${options.lazyLoad ? '/assets/images/placeholder.png' : posterUrl || '/assets/images/placeholder.png'}" 
                     ${options.lazyLoad ? `data-src="${posterUrl}"` : ''}
                     alt="${item.title}" 
                     loading="lazy"
                     class="cv-card-image">
                
                ${options.showStatus && item.status && item.status !== 'unknown' ? `
                    <div class="cv-card-status" style="background-color: ${statusColor}">
                        ${statusText}
                    </div>
                ` : ''}
                
                ${options.showProgress && progressPercent > 0 && progressPercent < 100 ? `
                    <div class="cv-card-progress">
                        <div class="cv-card-progress-bar" style="width: ${progressPercent}%"></div>
                    </div>
                ` : ''}
                
                ${item.lastEpisode ? `
                    <div class="cv-card-episode-info">
                        <span class="cv-card-episode">S${item.lastEpisode.season} E${item.lastEpisode.episode}</span>
                    </div>
                ` : ''}
            </div>
            
            <div class="cv-card-overlay">
                <div class="cv-card-content">
                    <h3 class="cv-card-title">${item.title}</h3>
                    <div class="cv-card-meta">
                        ${item.year ? `<span class="cv-card-year">${item.year}</span>` : ''}
                        ${options.showRating && item.voteAverage ? `
                            <span class="cv-card-rating">
                                <svg class="cv-star-icon" viewBox="0 0 24 24" fill="currentColor">
                                    <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon>
                                </svg>
                                ${CinemaVaultAPI.utils.formatVoteAverage(item.voteAverage)}
                            </span>
                        ` : ''}
                        ${options.showRuntime && item.runtime ? `
                            <span class="cv-card-runtime">${CinemaVaultAPI.utils.formatRuntime(item.runtime)}</span>
                        ` : ''}
                    </div>
                    <p class="cv-card-overview">${item.overview ? item.overview.substring(0, 120) + '...' : ''}</p>
                    <div class="cv-card-genres">
                        ${item.genres ? item.genres.slice(0, 2).map(genre => 
                            `<span class="cv-card-genre">${genre}</span>`
                        ).join('') : ''}
                    </div>
                    <div class="cv-card-actions">
                        <button class="cv-card-btn cv-card-btn-primary" data-action="play">
                            ${item.status === 'available' ? '▶ Play' : '＋ Request'}
                        </button>
                        <button class="cv-card-btn cv-card-btn-secondary" data-action="info">
                            ⓘ More Info
                        </button>
                        <button class="cv-card-btn cv-card-btn-tertiary" data-action="watchlist">
                            ${item.inWatchlist ? '✓ In List' : '＋ Add to List'}
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Initialize card functionality
     */
    function initializeCard(card, item, options) {
        // Card hover effects
        card.addEventListener('mouseenter', () => {
            card.classList.add('cv-card-hover');
        });
        
        card.addEventListener('mouseleave', () => {
            card.classList.remove('cv-card-hover');
        });
        
        // Action buttons
        const actionButtons = card.querySelectorAll('.cv-card-btn');
        actionButtons.forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                handleCardAction(btn.dataset.action, item, card);
            });
        });
        
        // Card click
        card.addEventListener('click', () => {
            CinemaVaultModal.showDetails(item.tmdbId, item.type);
        });
        
        // Touch support for mobile
        initializeTouchSupport(card);
        
        // Lazy loading
        if (options.lazyLoad) {
            initializeLazyLoading(card);
        }
        
        // Keyboard support
        card.setAttribute('tabindex', '0');
        card.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                CinemaVaultModal.showDetails(item.tmdbId, item.type);
            }
        });
    }

    /**
     * Initialize touch support
     */
    function initializeTouchSupport(card) {
        let touchStartY = 0;
        let touchEndY = 0;
        let touchStartTime = 0;
        
        card.addEventListener('touchstart', (e) => {
            touchStartY = e.touches[0].clientY;
            touchStartTime = Date.now();
        });
        
        card.addEventListener('touchend', (e) => {
            touchEndY = e.changedTouches[0].clientY;
            const touchDuration = Date.now() - touchStartTime;
            const diff = Math.abs(touchStartY - touchEndY);
            
            // If it's a quick tap (not a scroll), show overlay
            if (diff < 10 && touchDuration < 200) {
                card.classList.add('cv-card-hover');
                setTimeout(() => {
                    card.classList.remove('cv-card-hover');
                }, 3000);
            }
        });
    }

    /**
     * Initialize lazy loading
     */
    function initializeLazyLoading(card) {
        const img = card.querySelector('.cv-card-image[data-src]');
        if (!img) return;
        
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const src = img.dataset.src;
                    if (src) {
                        img.src = src;
                        img.removeAttribute('data-src');
                        imageObserver.unobserve(img);
                    }
                }
            });
        }, {
            rootMargin: '50px'
        });
        
        imageObserver.observe(img);
    }

    /**
     * Handle card action
     */
    async function handleCardAction(action, item, card) {
        try {
            switch (action) {
                case 'play':
                    await handlePlayAction(item, card);
                    break;
                case 'info':
                    CinemaVaultModal.showDetails(item.tmdbId, item.type);
                    break;
                case 'watchlist':
                    await handleWatchlistAction(item, card);
                    break;
            }
        } catch (error) {
            console.error('Error handling card action:', error);
            CinemaVaultToast.show('An error occurred', 'error');
        }
    }

    /**
     * Handle play action
     */
    async function handlePlayAction(item, card) {
        if (item.jellyfinId) {
            // Play in Jellyfin
            await CinemaVaultJellyfin.playItem(item.jellyfinId);
        } else {
            // Request content
            const success = await CinemaVaultAPI.requests.create({
                tmdbId: item.tmdbId,
                type: item.type
            });
            
            if (success) {
                CinemaVaultToast.show('Request sent successfully!', 'success');
                updateCardStatus(card, 'requested');
            } else {
                CinemaVaultToast.show('Failed to send request', 'error');
            }
        }
    }

    /**
     * Handle watchlist action
     */
    async function handleWatchlistAction(item, card) {
        if (item.inWatchlist) {
            await CinemaVaultAPI.watchlist.remove(item.tmdbId, item.type);
            CinemaVaultToast.show('Removed from watchlist', 'info');
            updateCardWatchlist(card, false);
        } else {
            await CinemaVaultAPI.watchlist.add(item.tmdbId, item.type, item.title, item.posterPath);
            CinemaVaultToast.show('Added to watchlist', 'success');
            updateCardWatchlist(card, true);
        }
    }

    /**
     * Update card status
     */
    function updateCardStatus(card, status) {
        const statusElement = card.querySelector('.cv-card-status');
        const primaryBtn = card.querySelector('.cv-card-btn-primary');
        
        if (statusElement) {
            const statusColor = CinemaVaultAPI.utils.getStatusColor(status);
            const statusText = CinemaVaultAPI.utils.getStatusText(status);
            statusElement.style.backgroundColor = statusColor;
            statusElement.textContent = statusText;
        }
        
        if (primaryBtn) {
            if (status === 'requested') {
                primaryBtn.textContent = '✓ Requested';
                primaryBtn.disabled = true;
                primaryBtn.classList.add('cv-card-btn-disabled');
            }
        }
    }

    /**
     * Update card watchlist status
     */
    function updateCardWatchlist(card, inWatchlist) {
        const watchlistBtn = card.querySelector('[data-action="watchlist"]');
        if (watchlistBtn) {
            watchlistBtn.textContent = inWatchlist ? '✓ In List' : '＋ Add to List';
            watchlistBtn.classList.toggle('cv-card-btn-active', inWatchlist);
        }
    }

    /**
     * Create multiple cards
     */
    function createMultiple(items, options = {}) {
        return items.map(item => create(item, options));
    }

    /**
     * Create a grid of cards
     */
    function createGrid(items, options = {}) {
        const grid = document.createElement('div');
        grid.className = 'cv-card-grid';
        
        if (options.columns) {
            grid.style.setProperty('--cv-grid-columns', options.columns);
        }
        
        const cards = createMultiple(items, options);
        cards.forEach(card => grid.appendChild(card));
        
        return grid;
    }

    /**
     * Update card data
     */
    function updateCard(card, newData) {
        const tmdbId = parseInt(card.dataset.tmdbId);
        const type = card.dataset.type;
        
        if (newData.tmdbId !== tmdbId || newData.type !== type) {
            console.warn('Card data mismatch');
            return;
        }
        
        // Update status
        if (newData.status) {
            updateCardStatus(card, newData.status);
        }
        
        // Update watchlist status
        if (newData.inWatchlist !== undefined) {
            updateCardWatchlist(card, newData.inWatchlist);
        }
        
        // Update progress
        if (newData.lastEpisode) {
            const progressPercent = newData.lastEpisode.watchedPercentage;
            const progressBar = card.querySelector('.cv-card-progress-bar');
            if (progressBar) {
                progressBar.style.width = `${progressPercent}%`;
            }
        }
    }

    /**
     * Show card loading state
     */
    function showLoading(card) {
        const poster = card.querySelector('.cv-card-poster');
        if (poster) {
            poster.classList.add('cv-card-loading');
            poster.innerHTML = `
                <div class="cv-card-loading-spinner">
                    <div class="cv-spinner"></div>
                </div>
            `;
        }
    }

    /**
     * Hide card loading state
     */
    function hideLoading(card) {
        const poster = card.querySelector('.cv-card-poster');
        if (poster) {
            poster.classList.remove('cv-card-loading');
            // Restore original content would need to be stored
        }
    }

    /**
     * Show card error state
     */
    function showError(card, message = 'Failed to load') {
        const poster = card.querySelector('.cv-card-poster');
        if (poster) {
            poster.classList.add('cv-card-error');
            poster.innerHTML = `
                <div class="cv-card-error-content">
                    <svg class="cv-error-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <circle cx="12" cy="12" r="10"></circle>
                        <line x1="12" y1="8" x2="12" y2="12"></line>
                        <line x1="12" y1="16" x2="12.01" y2="16"></line>
                    </svg>
                    <p>${message}</p>
                </div>
            `;
        }
    }

    /**
     * Get card data from DOM element
     */
    function getCardData(card) {
        return {
            tmdbId: parseInt(card.dataset.tmdbId),
            type: card.dataset.type
        };
    }

    // Public API
    return {
        create,
        createMultiple,
        createGrid,
        updateCard,
        showLoading,
        hideLoading,
        showError,
        getCardData
    };
})();
