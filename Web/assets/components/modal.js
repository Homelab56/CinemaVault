/**
 * CinemaVault Modal Component
 * Handles detail modals for content
 */

window.CinemaVaultModal = (function() {
    'use strict';

    let modal = null;
    let overlay = null;
    let currentContent = null;
    let isLoading = false;
    let trailerModal = null;

    /**
     * Initialize modal
     */
    function init() {
        createModal();
        bindEvents();
    }

    /**
     * Create modal elements
     */
    function createModal() {
        overlay = document.createElement('div');
        overlay.className = 'cv-modal-overlay';
        overlay.innerHTML = getModalHTML();
        document.body.appendChild(overlay);
        
        modal = overlay.querySelector('.cv-modal');
    }

    /**
     * Get modal HTML
     */
    function getModalHTML() {
        return `
            <div class="cv-modal">
                <div class="cv-modal-container">
                    <div class="cv-modal-close" id="cvModalClose">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <line x1="18" y1="6" x2="6" y2="18"></line>
                            <line x1="6" y1="6" x2="18" y2="18"></line>
                        </svg>
                    </div>
                    
                    <div class="cv-modal-content">
                        <div class="cv-modal-loading" id="cvModalLoading">
                            <div class="cv-spinner"></div>
                            <p>Loading details...</p>
                        </div>
                        
                        <div class="cv-modal-body" id="cvModalBody" style="display: none;">
                            <!-- Content will be inserted here -->
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Bind events
     */
    function bindEvents() {
        // Close button
        const closeBtn = document.getElementById('cvModalClose');
        if (closeBtn) {
            closeBtn.addEventListener('click', hide);
        }
        
        // Overlay click
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                hide();
            }
        });
        
        // Escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && isVisible()) {
                hide();
            }
        });
    }

    /**
     * Show details modal
     */
    async function showDetails(tmdbId, type) {
        if (isLoading) return;
        
        showLoading();
        show();
        
        try {
            // Load content details
            const [details, recommendations] = await Promise.allSettled([
                CinemaVaultAPI.content.getDetails(tmdbId, type),
                CinemaVaultAPI.content.getRecommendations(tmdbId, type)
            ]);
            
            const detailsData = details.status === 'fulfilled' ? details.value : null;
            const recommendationsData = recommendations.status === 'fulfilled' ? recommendations.value : { results: [] };
            
            if (!detailsData) {
                throw new Error('Failed to load content details');
            }
            
            currentContent = detailsData;
            
            // Render content
            await renderContent(detailsData, recommendationsData.results || []);
            
            // Show content
            hideLoading();
            
        } catch (error) {
            console.error('Error loading content details:', error);
            showError('Failed to load content details');
        }
    }

    /**
     * Render content
     */
    async function renderContent(content, recommendations = []) {
        const body = document.getElementById('cvModalBody');
        if (!body) return;
        
        // Check if content is available in library
        let libraryStatus = 'unknown';
        try {
            const statusMap = await CinemaVaultAPI.library.getStatus(content.tmdbId);
            libraryStatus = statusMap[content.tmdbId] || 'unknown';
        } catch (error) {
            console.error('Error checking library status:', error);
        }
        
        body.innerHTML = getContentHTML(content, libraryStatus, recommendations);
        
        // Initialize content interactions
        initializeContentInteractions(content, libraryStatus);
    }

    /**
     * Get content HTML
     */
    function getContentHTML(content, libraryStatus, recommendations) {
        const posterUrl = CinemaVaultAPI.utils.getImageUrl(content.posterPath, 'w500');
        const backdropUrl = CinemaVaultAPI.utils.getBackdropUrl(content.backdropPath, 'w1280');
        const statusColor = CinemaVaultAPI.utils.getStatusColor(libraryStatus);
        const statusText = CinemaVaultAPI.utils.getStatusText(libraryStatus);
        
        return `
            <div class="cv-modal-main">
                <div class="cv-modal-poster">
                    <img src="${posterUrl || '/assets/images/placeholder.png'}" alt="${content.title}">
                </div>
                
                <div class="cv-modal-info">
                    <div class="cv-modal-header">
                        <div class="cv-modal-quality-badges">
                            ${content.qualityBadges ? content.qualityBadges.map(badge => 
                                `<span class="cv-quality-badge">${badge}</span>`
                            ).join('') : ''}
                        </div>
                        
                        <h1 class="cv-modal-title">${content.title}</h1>
                        ${content.tagline ? `<p class="cv-modal-tagline">${content.tagline}</p>` : ''}
                        
                        <div class="cv-modal-meta">
                            ${content.voteAverage ? `
                                <div class="cv-modal-rating">
                                    <svg class="cv-star-icon" viewBox="0 0 24 24" fill="currentColor">
                                        <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon>
                                    </svg>
                                    <span>${CinemaVaultAPI.utils.formatVoteAverage(content.voteAverage)}</span>
                                    <span class="cv-modal-vote-count">(${content.voteCount})</span>
                                </div>
                            ` : ''}
                            ${content.year ? `<span class="cv-modal-year">${content.year}</span>` : ''}
                            ${content.runtime ? `<span class="cv-modal-runtime">${CinemaVaultAPI.utils.formatRuntime(content.runtime)}</span>` : ''}
                        </div>
                        
                        <div class="cv-modal-genres">
                            ${content.genres ? content.genres.map(genre => 
                                `<span class="cv-modal-genre">${genre}</span>`
                            ).join('') : ''}
                        </div>
                    </div>
                    
                    <div class="cv-modal-actions">
                        <button class="cv-modal-btn cv-modal-btn-primary" id="cvModalPrimaryBtn">
                            ${libraryStatus === 'available' ? '▶ Play Now' : '＋ Request'}
                        </button>
                        <button class="cv-modal-btn cv-modal-btn-secondary" id="cvModalWatchlistBtn">
                            ${content.inWatchlist ? '✓ In My List' : '＋ Add to My List'}
                        </button>
                        <button class="cv-modal-btn cv-modal-btn-tertiary" id="cvModalShareBtn">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <circle cx="18" cy="5" r="3"></circle>
                                <circle cx="6" cy="12" r="3"></circle>
                                <circle cx="18" cy="19" r="3"></circle>
                                <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"></line>
                                <line x1="15.41" y1="6.51" x2="8.59" y2="10.49"></line>
                            </svg>
                            Share
                        </button>
                    </div>
                    
                    <div class="cv-modal-overview">
                        <h3>Overview</h3>
                        <p>${content.overview || 'No overview available.'}</p>
                    </div>
                    
                    ${content.cast && content.cast.length > 0 ? `
                        <div class="cv-modal-cast">
                            <h3>Cast</h3>
                            <div class="cv-modal-cast-grid">
                                ${content.cast.slice(0, 8).map(person => `
                                    <div class="cv-modal-cast-member">
                                        <img src="${CinemaVaultAPI.utils.getImageUrl(person.profilePath, 'w185') || '/assets/images/avatar-placeholder.png'}" 
                                             alt="${person.name}" 
                                             class="cv-modal-cast-photo">
                                        <div class="cv-modal-cast-info">
                                            <div class="cv-modal-cast-name">${person.name}</div>
                                            <div class="cv-modal-cast-character">${person.character}</div>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                    ` : ''}
                </div>
            </div>
            
            ${recommendations.length > 0 ? `
                <div class="cv-modal-recommendations">
                    <h3>Similar Titles</h3>
                    <div class="cv-modal-recommendations-grid">
                        ${recommendations.slice(0, 6).map(item => `
                            <div class="cv-modal-recommendation-card" 
                                 data-tmdb-id="${item.tmdbId}" 
                                 data-type="${item.type}">
                                <img src="${CinemaVaultAPI.utils.getImageUrl(item.posterPath, 'w154') || '/assets/images/placeholder.png'}" 
                                     alt="${item.title}" 
                                     class="cv-modal-recommendation-poster">
                                <div class="cv-modal-recommendation-info">
                                    <h4>${item.title}</h4>
                                    <p>${item.year || ''}</p>
                                </div>
                            </div>
                        `).join('')}
                    </div>
                </div>
            ` : ''}
        `;
    }

    /**
     * Initialize content interactions
     */
    function initializeContentInteractions(content, libraryStatus) {
        // Primary action button
        const primaryBtn = document.getElementById('cvModalPrimaryBtn');
        if (primaryBtn) {
            primaryBtn.addEventListener('click', async () => {
                await handlePrimaryAction(content, libraryStatus);
            });
        }
        
        // Watchlist button
        const watchlistBtn = document.getElementById('cvModalWatchlistBtn');
        if (watchlistBtn) {
            watchlistBtn.addEventListener('click', async () => {
                await handleWatchlistAction(content, watchlistBtn);
            });
        }
        
        // Share button
        const shareBtn = document.getElementById('cvModalShareBtn');
        if (shareBtn) {
            shareBtn.addEventListener('click', () => {
                handleShareAction(content);
            });
        }
        
        // Recommendation cards
        const recommendationCards = document.querySelectorAll('.cv-modal-recommendation-card');
        recommendationCards.forEach(card => {
            card.addEventListener('click', () => {
                const tmdbId = parseInt(card.dataset.tmdbId);
                const type = card.dataset.type;
                showDetails(tmdbId, type);
            });
        });
    }

    /**
     * Handle primary action
     */
    async function handlePrimaryAction(content, libraryStatus) {
        try {
            if (libraryStatus === 'available' && content.jellyfinId) {
                // Play in Jellyfin
                await CinemaVaultJellyfin.playItem(content.jellyfinId);
                hide();
            } else {
                // Request content
                const success = await CinemaVaultAPI.requests.create({
                    tmdbId: content.tmdbId,
                    type: content.type
                });
                
                if (success) {
                    CinemaVaultToast.show('Request sent successfully!', 'success');
                    updatePrimaryButton('requested');
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
     * Handle watchlist action
     */
    async function handleWatchlistAction(content, button) {
        try {
            if (content.inWatchlist) {
                await CinemaVaultAPI.watchlist.remove(content.tmdbId, content.type);
                CinemaVaultToast.show('Removed from watchlist', 'info');
                updateWatchlistButton(button, false);
            } else {
                await CinemaVaultAPI.watchlist.add(content.tmdbId, content.type, content.title, content.posterPath);
                CinemaVaultToast.show('Added to watchlist', 'success');
                updateWatchlistButton(button, true);
            }
        } catch (error) {
            console.error('Error handling watchlist action:', error);
            CinemaVaultToast.show('An error occurred', 'error');
        }
    }

    /**
     * Handle share action
     */
    function handleShareAction(content) {
        const shareUrl = `${window.location.origin}/#!/item?id=${content.jellyfinId}`;
        
        if (navigator.share) {
            navigator.share({
                title: content.title,
                text: content.overview,
                url: shareUrl
            }).catch(error => {
                console.log('Share cancelled:', error);
            });
        } else {
            // Fallback: copy to clipboard
            navigator.clipboard.writeText(shareUrl).then(() => {
                CinemaVaultToast.show('Link copied to clipboard', 'success');
            }).catch(() => {
                // Last resort: show URL in prompt
                prompt('Copy this link:', shareUrl);
            });
        }
    }

    /**
     * Update primary button state
     */
    function updatePrimaryButton(status) {
        const primaryBtn = document.getElementById('cvModalPrimaryBtn');
        if (primaryBtn) {
            if (status === 'requested') {
                primaryBtn.textContent = '✓ Requested';
                primaryBtn.disabled = true;
                primaryBtn.classList.add('cv-modal-btn-disabled');
            }
        }
    }

    /**
     * Update watchlist button state
     */
    function updateWatchlistButton(button, inWatchlist) {
        if (inWatchlist) {
            button.textContent = '✓ In My List';
            button.classList.add('cv-modal-btn-active');
        } else {
            button.textContent = '＋ Add to My List';
            button.classList.remove('cv-modal-btn-active');
        }
    }

    /**
     * Show trailer modal
     */
    async function showTrailer(tmdbId, type) {
        try {
            const videos = await CinemaVaultAPI.content.getVideos(tmdbId, type);
            const trailer = videos.find(v => v.type === 'Trailer' && v.site === 'YouTube');
            
            if (!trailer) {
                CinemaVaultToast.show('No trailer available', 'info');
                return;
            }
            
            createTrailerModal(trailer);
            
        } catch (error) {
            console.error('Error loading trailer:', error);
            CinemaVaultToast.show('Failed to load trailer', 'error');
        }
    }

    /**
     * Create trailer modal
     */
    function createTrailerModal(trailer) {
        trailerModal = document.createElement('div');
        trailerModal.className = 'cv-trailer-modal';
        trailerModal.innerHTML = `
            <div class="cv-trailer-overlay">
                <div class="cv-trailer-container">
                    <button class="cv-trailer-close" id="cvTrailerClose">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <line x1="18" y1="6" x2="6" y2="18"></line>
                            <line x1="6" y1="6" x2="18" y2="18"></line>
                        </svg>
                    </button>
                    <div class="cv-trailer-video">
                        <iframe src="https://www.youtube.com/embed/${trailer.key}?autoplay=1&rel=0" 
                                frameborder="0" 
                                allowfullscreen>
                        </iframe>
                    </div>
                </div>
            </div>
        `;
        
        document.body.appendChild(trailerModal);
        
        // Bind events
        const closeBtn = document.getElementById('cvTrailerClose');
        closeBtn.addEventListener('click', hideTrailer);
        
        trailerModal.addEventListener('click', (e) => {
            if (e.target === trailerModal.querySelector('.cv-trailer-overlay')) {
                hideTrailer();
            }
        });
        
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                hideTrailer();
            }
        });
    }

    /**
     * Hide trailer modal
     */
    function hideTrailer() {
        if (trailerModal) {
            trailerModal.remove();
            trailerModal = null;
        }
    }

    /**
     * Show modal
     */
    function show() {
        if (overlay) {
            overlay.classList.add('cv-modal-active');
            document.body.style.overflow = 'hidden';
        }
    }

    /**
     * Hide modal
     */
    function hide() {
        if (overlay) {
            overlay.classList.remove('cv-modal-active');
            document.body.style.overflow = '';
            currentContent = null;
        }
    }

    /**
     * Show loading state
     */
    function showLoading() {
        isLoading = true;
        const loading = document.getElementById('cvModalLoading');
        const body = document.getElementById('cvModalBody');
        
        if (loading) loading.style.display = 'flex';
        if (body) body.style.display = 'none';
    }

    /**
     * Hide loading state
     */
    function hideLoading() {
        isLoading = false;
        const loading = document.getElementById('cvModalLoading');
        const body = document.getElementById('cvModalBody');
        
        if (loading) loading.style.display = 'none';
        if (body) body.style.display = 'block';
    }

    /**
     * Show error state
     */
    function showError(message) {
        hideLoading();
        const body = document.getElementById('cvModalBody');
        if (body) {
            body.innerHTML = `
                <div class="cv-modal-error">
                    <svg class="cv-error-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <circle cx="12" cy="12" r="10"></circle>
                        <line x1="12" y1="8" x2="12" y2="12"></line>
                        <line x1="12" y1="16" x2="12.01" y2="16"></line>
                    </svg>
                    <p>${message}</p>
                    <button class="cv-modal-btn cv-modal-btn-primary" onclick="window.CinemaVaultModal.hide()">
                        Close
                    </button>
                </div>
            `;
        }
    }

    /**
     * Check if modal is visible
     */
    function isVisible() {
        return overlay && overlay.classList.contains('cv-modal-active');
    }

    /**
     * Destroy modal
     */
    function destroy() {
        hide();
        hideTrailer();
        
        if (overlay) {
            overlay.remove();
            overlay = null;
            modal = null;
        }
    }

    // Public API
    return {
        init,
        showDetails,
        showTrailer,
        hide,
        isVisible,
        destroy
    };
})();
