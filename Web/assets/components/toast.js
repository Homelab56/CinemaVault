/**
 * CinemaVault Toast Notification System
 * Handles toast notifications for user feedback
 */

window.CinemaVaultToast = (function() {
    'use strict';

    let container = null;
    let toastQueue = [];
    let activeToast = null;
    let isShowing = false;

    const TOAST_DURATION = 4000;
    const MAX_TOASTS = 5;
    const TOAST_TYPES = {
        success: 'cv-toast-success',
        error: 'cv-toast-error',
        warning: 'cv-toast-warning',
        info: 'cv-toast-info'
    };

    /**
     * Initialize toast system
     */
    function init() {
        createContainer();
        bindEvents();
    }

    /**
     * Create toast container
     */
    function createContainer() {
        container = document.createElement('div');
        container.className = 'cv-toast-container';
        container.setAttribute('aria-live', 'polite');
        container.setAttribute('aria-label', 'Notifications');
        document.body.appendChild(container);
    }

    /**
     * Bind events
     */
    function bindEvents() {
        // Handle escape key to dismiss active toast
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && activeToast) {
                dismissToast(activeToast);
            }
        });
    }

    /**
     * Show toast notification
     */
    function show(message, type = 'info', options = {}) {
        const toast = {
            id: generateId(),
            message: message,
            type: type,
            duration: options.duration || TOAST_DURATION,
            persistent: options.persistent || false,
            action: options.action || null,
            icon: options.icon || null,
            timestamp: Date.now()
        };

        // Add to queue
        toastQueue.push(toast);
        
        // Limit queue size
        if (toastQueue.length > MAX_TOASTS) {
            toastQueue.shift();
        }

        // Process queue
        processQueue();
        
        return toast.id;
    }

    /**
     * Process toast queue
     */
    function processQueue() {
        if (isShowing || toastQueue.length === 0) {
            return;
        }

        const toast = toastQueue.shift();
        showToast(toast);
    }

    /**
     * Show individual toast
     */
    function showToast(toast) {
        isShowing = true;
        activeToast = toast;

        const toastElement = createToastElement(toast);
        container.appendChild(toastElement);

        // Trigger animation
        requestAnimationFrame(() => {
            toastElement.classList.add('cv-toast-show');
        });

        // Auto dismiss if not persistent
        if (!toast.persistent) {
            setTimeout(() => {
                dismissToast(toast);
            }, toast.duration);
        }
    }

    /**
     * Create toast element
     */
    function createToastElement(toast) {
        const element = document.createElement('div');
        element.className = `cv-toast ${TOAST_TYPES[toast.type] || TOAST_TYPES.info}`;
        element.setAttribute('data-toast-id', toast.id);
        element.setAttribute('role', 'alert');
        
        const icon = getIconForType(toast.type, toast.icon);
        
        element.innerHTML = `
            <div class="cv-toast-content">
                <div class="cv-toast-icon">
                    ${icon}
                </div>
                <div class="cv-toast-message">
                    <p>${escapeHtml(toast.message)}</p>
                </div>
                ${toast.action ? `
                    <button class="cv-toast-action" data-action-id="${toast.id}">
                        ${toast.action.text}
                    </button>
                ` : ''}
                <button class="cv-toast-close" aria-label="Dismiss notification">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <line x1="18" y1="6" x2="6" y2="18"></line>
                        <line x1="6" y1="6" x2="18" y2="18"></line>
                    </svg>
                </button>
            </div>
            <div class="cv-toast-progress">
                <div class="cv-toast-progress-bar"></div>
            </div>
        `;

        // Add event listeners
        bindToastEvents(element, toast);

        return element;
    }

    /**
     * Get icon for toast type
     */
    function getIconForType(type, customIcon) {
        if (customIcon) {
            return customIcon;
        }

        const icons = {
            success: `
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <polyline points="20 6 9 17 4 12"></polyline>
                </svg>
            `,
            error: `
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <circle cx="12" cy="12" r="10"></circle>
                    <line x1="15" y1="9" x2="9" y2="15"></line>
                    <line x1="9" y1="9" x2="15" y2="15"></line>
                </svg>
            `,
            warning: `
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
                    <line x1="12" y1="9" x2="12" y2="13"></line>
                    <line x1="12" y1="17" x2="12.01" y2="17"></line>
                </svg>
            `,
            info: `
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor">
                    <circle cx="12" cy="12" r="10"></circle>
                    <line x1="12" y1="16" x2="12" y2="12"></line>
                    <line x1="12" y1="8" x2="12.01" y2="8"></line>
                </svg>
            `
        };

        return icons[type] || icons.info;
    }

    /**
     * Bind toast events
     */
    function bindToastEvents(element, toast) {
        // Close button
        const closeBtn = element.querySelector('.cv-toast-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                dismissToast(toast);
            });
        }

        // Action button
        const actionBtn = element.querySelector('.cv-toast-action');
        if (actionBtn && toast.action) {
            actionBtn.addEventListener('click', () => {
                if (toast.action.handler) {
                    toast.action.handler();
                }
                dismissToast(toast);
            });
        }

        // Progress bar animation
        if (!toast.persistent) {
            const progressBar = element.querySelector('.cv-toast-progress-bar');
            if (progressBar) {
                requestAnimationFrame(() => {
                    progressBar.style.transition = `width ${toast.duration}ms linear`;
                    progressBar.style.width = '0%';
                });
            }
        }

        // Mouse enter/leave for persistent toasts
        if (!toast.persistent) {
            element.addEventListener('mouseenter', () => {
                const progressBar = element.querySelector('.cv-toast-progress-bar');
                if (progressBar) {
                    progressBar.style.transition = 'none';
                    progressBar.style.width = progressBar.offsetWidth + 'px';
                }
            });

            element.addEventListener('mouseleave', () => {
                const progressBar = element.querySelector('.cv-toast-progress-bar');
                if (progressBar) {
                    const remainingWidth = progressBar.offsetWidth;
                    const remainingTime = (remainingWidth / progressBar.parentElement.offsetWidth) * toast.duration;
                    
                    progressBar.style.transition = `width ${remainingTime}ms linear`;
                    progressBar.style.width = '0%';
                    
                    setTimeout(() => {
                        dismissToast(toast);
                    }, remainingTime);
                }
            });
        }
    }

    /**
     * Dismiss toast
     */
    function dismissToast(toast) {
        const element = container.querySelector(`[data-toast-id="${toast.id}"]`);
        if (!element) return;

        element.classList.add('cv-toast-hide');

        setTimeout(() => {
            if (element.parentNode) {
                element.parentNode.removeChild(element);
            }
            
            if (activeToast === toast) {
                activeToast = null;
                isShowing = false;
                
                // Process next toast in queue
                setTimeout(() => {
                    processQueue();
                }, 100);
            }
        }, 300);
    }

    /**
     * Dismiss all toasts
     */
    function dismissAll() {
        const toasts = container.querySelectorAll('.cv-toast');
        toasts.forEach(toastElement => {
            const toastId = toastElement.getAttribute('data-toast-id');
            const toast = toastQueue.find(t => t.id === toastId) || activeToast;
            if (toast) {
                dismissToast(toast);
            }
        });
        
        toastQueue = [];
    }

    /**
     * Show success toast
     */
    function success(message, options = {}) {
        return show(message, 'success', options);
    }

    /**
     * Show error toast
     */
    function error(message, options = {}) {
        return show(message, 'error', options);
    }

    /**
     * Show warning toast
     */
    function warning(message, options = {}) {
        return show(message, 'warning', options);
    }

    /**
     * Show info toast
     */
    function info(message, options = {}) {
        return show(message, 'info', options);
    }

    /**
     * Show loading toast
     */
    function loading(message, options = {}) {
        return show(message, 'info', {
            ...options,
            persistent: true,
            icon: `
                <div class="cv-toast-spinner">
                    <div class="cv-spinner"></div>
                </div>
            `
        });
    }

    /**
     * Update toast message
     */
    function update(toastId, message, type = null) {
        const element = container.querySelector(`[data-toast-id="${toastId}"]`);
        if (!element) return;

        const messageElement = element.querySelector('.cv-toast-message p');
        if (messageElement) {
            messageElement.textContent = message;
        }

        if (type) {
            // Remove existing type classes
            Object.values(TOAST_TYPES).forEach(typeClass => {
                element.classList.remove(typeClass);
            });
            
            // Add new type class
            element.classList.add(TOAST_TYPES[type] || TOAST_TYPES.info);
            
            // Update icon
            const iconElement = element.querySelector('.cv-toast-icon');
            if (iconElement) {
                iconElement.innerHTML = getIconForType(type);
            }
        }
    }

    /**
     * Check if toast exists
     */
    function exists(toastId) {
        return container.querySelector(`[data-toast-id="${toastId}"]`) !== null;
    }

    /**
     * Get active toast count
     */
    function getActiveCount() {
        return container.querySelectorAll('.cv-toast').length;
    }

    /**
     * Clear queue
     */
    function clearQueue() {
        toastQueue = [];
    }

    /**
     * Generate unique ID
     */
    function generateId() {
        return 'toast_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    /**
     * Escape HTML
     */
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Destroy toast system
     */
    function destroy() {
        dismissAll();
        clearQueue();
        
        if (container) {
            container.remove();
            container = null;
        }
        
        activeToast = null;
        isShowing = false;
    }

    // Public API
    return {
        init,
        show,
        success,
        error,
        warning,
        info,
        loading,
        update,
        dismiss: dismissToast,
        dismissAll,
        exists,
        getActiveCount,
        clearQueue,
        destroy
    };
})();
