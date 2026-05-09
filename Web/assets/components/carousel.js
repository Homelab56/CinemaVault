/**
 * CinemaVault Carousel Component
 * Handles horizontal scrolling carousels for content
 */

window.CinemaVaultCarousel = (function() {
    'use strict';

    let container = null;
    let items = [];
    let options = {};
    let scrollContainer = null;
    let scrollLeftBtn = null;
    let scrollRightBtn = null;
    let isLoading = false;
    let hasMore = true;
    let currentPage = 1;

    /**
     * Initialize carousel
     */
    async function init(parentContainer, carouselOptions) {
        options = {
            title: carouselOptions.title || '',
            subtitle: carouselOptions.subtitle || '',
            endpoint: carouselOptions.endpoint || '',
            type: carouselOptions.type || 'discover',
            params: carouselOptions.params || {},
            cardWidth: 180,
            cardGap: 12,
            itemsPerRow: 6,
            lazyLoad: true,
            showSeeAll: true,
            ...carouselOptions
        };

        container = document.createElement('div');
        container.className = 'cv-carousel';
        container.innerHTML = getCarouselHTML();
        
        parentContainer.appendChild(container);
        
        // Initialize components
        initializeScroll();
        
        // Load content
        await loadContent();
    }

    /**
     * Get carousel HTML
     */
    function getCarouselHTML() {
        return `
            <div class="cv-carousel-header">
                <div class="cv-carousel-title-section">
                    <h2 class="cv-carousel-title">${options.title}</h2>
                    ${options.subtitle ? `<p class="cv-carousel-subtitle">${options.subtitle}</p>` : ''}
                </div>
                ${options.showSeeAll ? `
                    <button class="cv-carousel-see-all" data-endpoint="${options.endpoint}">
                        See All
                        <svg class="cv-see-all-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <polyline points="9 18 15 12 9 6"></polyline>
                        </svg>
                    </button>
                ` : ''}
            </div>
            
            <div class="cv-carousel-container">
                <button class="cv-carousel-scroll-btn cv-carousel-scroll-left" id="cvScrollLeft_${Date.now()}">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <polyline points="15 18 9 12 15 6"></polyline>
                    </svg>
                </button>
                
                <div class="cv-carousel-scroll" id="cvScrollContainer_${Date.now()}">
                    <div class="cv-carousel-items" id="cvCarouselItems_${Date.now()}">
                        <div class="cv-carousel-loading">
                            <div class="cv-spinner"></div>
                            <p>Loading...</p>
                        </div>
                    </div>
                </div>
                
                <button class="cv-carousel-scroll-btn cv-carousel-scroll-right" id="cvScrollRight_${Date.now()}">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <polyline points="9 18 15 12 9 6"></polyline>
                    </svg>
                </button>
            </div>
        `;
    }

    /**
     * Initialize scroll functionality
     */
    function initializeScroll() {
        const timestamp = Date.now();
        scrollContainer = document.getElementById(`cvScrollContainer_${timestamp}`);
        const itemsContainer = document.getElementById(`cvCarouselItems_${timestamp}`);
        scrollLeftBtn = document.getElementById(`cvScrollLeft_${timestamp}`);
        scrollRightBtn = document.getElementById(`cvScrollRight_${timestamp}`);
        
        if (!scrollContainer || !itemsContainer || !scrollLeftBtn || !scrollRightBtn) {
            return;
        }
        
        // Scroll button handlers
        scrollLeftBtn.addEventListener('click', () => scroll('left'));
        scrollRightBtn.addEventListener('click', () => scroll('right'));
        
        // Update scroll buttons visibility
        scrollContainer.addEventListener('scroll', CinemaVaultAPI.utils.throttle(updateScrollButtons, 16));
        
        // Keyboard navigation
        scrollContainer.addEventListener('keydown', (e) => {
            if (e.key === 'ArrowLeft') {
                scroll('left');
            } else if (e.key === 'ArrowRight') {
                scroll('right');
            }
        });
        
        // See All button handler
        const seeAllBtn = container.querySelector('.cv-carousel-see-all');
        if (seeAllBtn) {
            seeAllBtn.addEventListener('click', () => {
                const endpoint = seeAllBtn.dataset.endpoint;
                CinemaVaultRouter.navigate(`/browse?endpoint=${encodeURIComponent(endpoint)}`);
            });
        }
        
        // Intersection Observer for lazy loading
        if (options.lazyLoad) {
            initializeLazyLoading(itemsContainer);
        }
        
        // Initial scroll button update
        updateScrollButtons();
    }

    /**
     * Load content
     */
    async function loadContent() {
        if (isLoading || !hasMore) return;
        
        isLoading = true;
        showLoading();
        
        try {
            let response;
            
            // Determine which API to call based on type
            switch (options.type) {
                case 'trending':
                    response = await CinemaVaultAPI.discovery.getTrending(
                        options.params.type || 'movie', 
                        currentPage
                    );
                    break;
                case 'popular':
                    response = await CinemaVaultAPI.discovery.getPopular(
                        options.params.type || 'movie', 
                        currentPage
                    );
                    break;
                case 'toprated':
                    response = await CinemaVaultAPI.discovery.getTopRated(
                        options.params.type || 'movie', 
                        currentPage
                    );
                    break;
                case 'nowplaying':
                    response = await CinemaVaultAPI.discovery.getNowPlaying(currentPage);
                    break;
                case 'genre':
                    response = await CinemaVaultAPI.discovery.getByGenre(
                        options.params.genreId,
                        options.params.type || 'movie',
                        currentPage
                    );
                    break;
                case 'resume':
                    response = await CinemaVaultAPI.library.getResume();
                    break;
                case 'recent':
                    response = await CinemaVaultAPI.library.getRecent(
                        options.params.limit || 20
                    );
                    break;
                default:
                    // Direct API call
                    response = await CinemaVaultAPI.request(options.endpoint);
            }
            
            const newItems = response.results || response || [];
            items = items.concat(newItems);
            
            // Check if there are more items
            hasMore = response.totalPages ? currentPage < response.totalPages : newItems.length > 0;
            currentPage++;
            
            // Render items
            renderItems();
            
            // Update scroll buttons
            updateScrollButtons();
            
        } catch (error) {
            console.error('Error loading carousel content:', error);
            showError();
        } finally {
            isLoading = false;
            hideLoading();
        }
    }

    /**
     * Render items
     */
    function renderItems() {
        const itemsContainer = scrollContainer.querySelector('.cv-carousel-items');
        if (!itemsContainer) return;
        
        if (items.length === 0) {
            itemsContainer.innerHTML = `
                <div class="cv-carousel-empty">
                    <p>No items found</p>
                </div>
            `;
            return;
        }
        
        const itemsHTML = items.map(item => createCardHTML(item)).join('');
        itemsContainer.innerHTML = `
            <div class="cv-carousel-grid">
                ${itemsHTML}
            </div>
        `;
        
        // Add card event listeners
        initializeCards();
    }

    /**
     * Create card HTML
     */
    function createCardHTML(item) {
        const posterUrl = CinemaVaultAPI.utils.getImageUrl(item.posterPath, 'w342');
        const statusColor = CinemaVaultAPI.utils.getStatusColor(item.status);
        const statusText = CinemaVaultAPI.utils.getStatusText(item.status);
        const progressPercent = item.lastEpisode ? item.lastEpisode.watchedPercentage : 0;
        
        return `
            <div class="cv-card" data-tmdb-id="${item.tmdbId}" data-type="${item.type}">
                <div class="cv-card-poster">
                    <img src="${posterUrl || '/assets/images/placeholder.png'}" 
                         alt="${item.title}" 
                         loading="lazy"
                         class="cv-card-image">
                    
                    ${item.status && item.status !== 'unknown' ? `
                        <div class="cv-card-status" style="background-color: ${statusColor}">
                            ${statusText}
                        </div>
                    ` : ''}
                    
                    ${progressPercent > 0 && progressPercent < 100 ? `
                        <div class="cv-card-progress">
                            <div class="cv-card-progress-bar" style="width: ${progressPercent}%"></div>
                        </div>
                    ` : ''}
                </div>
                
                <div class="cv-card-overlay">
                    <div class="cv-card-content">
                        <h3 class="cv-card-title">${item.title}</h3>
                        <div class="cv-card-meta">
                            <span class="cv-card-year">${item.year || ''}</span>
                            ${item.voteAverage ? `
                                <span class="cv-card-rating">
                                    <svg class="cv-star-icon" viewBox="0 0 24 24" fill="currentColor">
                                        <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon>
                                    </svg>
                                    ${CinemaVaultAPI.utils.formatVoteAverage(item.voteAverage)}
                                </span>
                            ` : ''}
                            ${item.runtime ? `
                                <span class="cv-card-runtime">${CinemaVaultAPI.utils.formatRuntime(item.runtime)}</span>
                            ` : ''}
                        </div>
                        <p class="cv-card-overview">${item.overview ? item.overview.substring(0, 120) + '...' : ''}</p>
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
            </div>
        `;
    }

    /**
     * Initialize cards
     */
    function initializeCards() {
        const cards = scrollContainer.querySelectorAll('.cv-card');
        
        cards.forEach(card => {
            const tmdbId = parseInt(card.dataset.tmdbId);
            const type = card.dataset.type;
            const item = items.find(i => i.tmdbId === tmdbId && i.type === type);
            
            if (!item) return;
            
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
                CinemaVaultModal.showDetails(tmdbId, type);
            });
            
            // Touch support for mobile
            let touchStartY = 0;
            let touchEndY = 0;
            
            card.addEventListener('touchstart', (e) => {
                touchStartY = e.touches[0].clientY;
            });
            
            card.addEventListener('touchend', (e) => {
                touchEndY = e.changedTouches[0].clientY;
                const diff = Math.abs(touchStartY - touchEndY);
                
                // If it's a tap (not a scroll), show overlay
                if (diff < 10) {
                    card.classList.add('cv-card-hover');
                    setTimeout(() => {
                        card.classList.remove('cv-card-hover');
                    }, 3000);
                }
            });
        });
    }

    /**
     * Handle card action
     */
    async function handleCardAction(action, item, card) {
        try {
            switch (action) {
                case 'play':
                    if (item.jellyfinId) {
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
                    break;
                    
                case 'info':
                    CinemaVaultModal.showDetails(item.tmdbId, item.type);
                    break;
                    
                case 'watchlist':
                    if (item.inWatchlist) {
                        await CinemaVaultAPI.watchlist.remove(item.tmdbId, item.type);
                        CinemaVaultToast.show('Removed from watchlist', 'info');
                        updateCardWatchlist(card, false);
                    } else {
                        await CinemaVaultAPI.watchlist.add(item.tmdbId, item.type, item.title, item.posterPath);
                        CinemaVaultToast.show('Added to watchlist', 'success');
                        updateCardWatchlist(card, true);
                    }
                    break;
            }
        } catch (error) {
            console.error('Error handling card action:', error);
            CinemaVaultToast.show('An error occurred', 'error');
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
        
        // Update item in data
        const tmdbId = parseInt(card.dataset.tmdbId);
        const type = card.dataset.type;
        const item = items.find(i => i.tmdbId === tmdbId && i.type === type);
        if (item) {
            item.inWatchlist = inWatchlist;
        }
    }

    /**
     * Initialize lazy loading
     */
    function initializeLazyLoading(container) {
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
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
        
        container.querySelectorAll('img[data-src]').forEach(img => {
            imageObserver.observe(img);
        });
    }

    /**
     * Scroll carousel
     */
    function scroll(direction) {
        if (!scrollContainer) return;
        
        const scrollAmount = (options.cardWidth + options.cardGap) * options.itemsPerRow;
        const currentScroll = scrollContainer.scrollLeft;
        
        if (direction === 'left') {
            scrollContainer.scrollTo({
                left: Math.max(0, currentScroll - scrollAmount),
                behavior: 'smooth'
            });
        } else {
            scrollContainer.scrollTo({
                left: currentScroll + scrollAmount,
                behavior: 'smooth'
            });
        }
    }

    /**
     * Update scroll buttons visibility
     */
    function updateScrollButtons() {
        if (!scrollContainer || !scrollLeftBtn || !scrollRightBtn) return;
        
        const canScrollLeft = scrollContainer.scrollLeft > 0;
        const canScrollRight = scrollContainer.scrollLeft < scrollContainer.scrollWidth - scrollContainer.clientWidth;
        
        scrollLeftBtn.style.display = canScrollLeft ? 'flex' : 'none';
        scrollRightBtn.style.display = canScrollRight ? 'flex' : 'none';
    }

    /**
     * Show loading state
     */
    function showLoading() {
        const itemsContainer = scrollContainer.querySelector('.cv-carousel-items');
        if (itemsContainer && items.length === 0) {
            itemsContainer.innerHTML = `
                <div class="cv-carousel-loading">
                    <div class="cv-spinner"></div>
                    <p>Loading...</p>
                </div>
            `;
        }
    }

    /**
     * Hide loading state
     */
    function hideLoading() {
        // Loading state is hidden when items are rendered
    }

    /**
     * Show error state
     */
    function showError() {
        const itemsContainer = scrollContainer.querySelector('.cv-carousel-items');
        if (itemsContainer) {
            itemsContainer.innerHTML = `
                <div class="cv-carousel-error">
                    <p>Failed to load content</p>
                    <button class="cv-btn cv-btn-primary" onclick="window.CinemaVaultCarousel.reload()">
                        Retry
                    </button>
                </div>
            `;
        }
    }

    /**
     * Reload carousel content
     */
    async function reload() {
        items = [];
        currentPage = 1;
        hasMore = true;
        isLoading = false;
        await loadContent();
    }

    /**
     * Destroy carousel
     */
    function destroy() {
        if (container) {
            container.remove();
            container = null;
        }
        
        items = [];
        options = {};
        isLoading = false;
        hasMore = true;
        currentPage = 1;
    }

    // Public API
    return {
        init,
        reload,
        destroy
    };
})();
