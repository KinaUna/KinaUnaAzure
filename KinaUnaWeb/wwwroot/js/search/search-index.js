"use strict";
/**
 * Search Index Page TypeScript
 * Handles search form interactions and filter toggles
 */
/**
 * Initializes the search page functionality
 */
function initializeSearchPage() {
    initializeTypeFilterButtons();
    initializeCollapsePanels();
    initializeSearchInput();
}
/**
 * Sets up Select All / Clear All functionality for entity type filters
 */
function initializeTypeFilterButtons() {
    const selectAllButton = document.getElementById('select-all-types-button');
    const clearAllButton = document.getElementById('clear-all-types-button');
    const typeCheckboxes = document.querySelectorAll('.search-type-checkbox');
    if (selectAllButton) {
        selectAllButton.addEventListener('click', () => {
            typeCheckboxes.forEach((checkbox) => {
                checkbox.checked = true;
            });
        });
    }
    if (clearAllButton) {
        clearAllButton.addEventListener('click', () => {
            typeCheckboxes.forEach((checkbox) => {
                checkbox.checked = false;
            });
        });
    }
}
/**
 * Sets up chevron icon toggle on collapse panels
 */
function initializeCollapsePanels() {
    const toggleButtons = document.querySelectorAll('[data-toggle="collapse"]');
    toggleButtons.forEach((button) => {
        const targetId = button.getAttribute('data-target');
        if (!targetId)
            return;
        const target = document.querySelector(targetId);
        if (!target)
            return;
        target.addEventListener('show.bs.collapse', () => {
            const chevron = button.querySelector('.chevron-right');
            if (chevron) {
                chevron.textContent = 'expand_more';
            }
        });
        target.addEventListener('hide.bs.collapse', () => {
            const chevron = button.querySelector('.chevron-right');
            if (chevron) {
                chevron.textContent = 'chevron_right';
            }
        });
    });
}
/**
 * Sets up search input behavior (Enter key submit, auto-focus)
 */
function initializeSearchInput() {
    const searchInput = document.querySelector('input[name="Query"]');
    const searchForm = document.getElementById('search-form');
    if (searchInput) {
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                if (searchForm) {
                    searchForm.submit();
                }
            }
        });
        // Focus search input on page load if empty
        if (!searchInput.value) {
            searchInput.focus();
        }
    }
}
// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initializeSearchPage);
//# sourceMappingURL=search-index.js.map