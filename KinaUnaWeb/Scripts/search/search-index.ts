/**
 * Search Index Page TypeScript
 * Handles search form interactions and filter toggles
 */

/**
 * Initializes the search page functionality
 */
function initializeSearchPage(): void {
    initializeTypeFilterButtons();
    initializeCollapsePanels();
    initializeSearchInput();
}

/**
 * Sets up Select All / Clear All functionality for entity type filters
 */
function initializeTypeFilterButtons(): void {
    const selectAllButton = document.getElementById('select-all-types-button') as HTMLButtonElement | null;
    const clearAllButton = document.getElementById('clear-all-types-button') as HTMLButtonElement | null;
    const typeCheckboxes = document.querySelectorAll<HTMLInputElement>('.search-type-checkbox');

    if (selectAllButton) {
        selectAllButton.addEventListener('click', (): void => {
            typeCheckboxes.forEach((checkbox: HTMLInputElement): void => {
                checkbox.checked = true;
            });
        });
    }

    if (clearAllButton) {
        clearAllButton.addEventListener('click', (): void => {
            typeCheckboxes.forEach((checkbox: HTMLInputElement): void => {
                checkbox.checked = false;
            });
        });
    }
}

/**
 * Sets up chevron icon toggle on collapse panels
 */
function initializeCollapsePanels(): void {
    const toggleButtons = document.querySelectorAll<HTMLElement>('[data-toggle="collapse"]');

    toggleButtons.forEach((button: HTMLElement): void => {
        const targetId = button.getAttribute('data-target');
        if (!targetId) return;

        const target = document.querySelector<HTMLElement>(targetId);
        if (!target) return;

        target.addEventListener('show.bs.collapse', (): void => {
            const chevron = button.querySelector<HTMLElement>('.chevron-right');
            if (chevron) {
                chevron.textContent = 'expand_more';
            }
        });

        target.addEventListener('hide.bs.collapse', (): void => {
            const chevron = button.querySelector<HTMLElement>('.chevron-right');
            if (chevron) {
                chevron.textContent = 'chevron_right';
            }
        });
    });
}

/**
 * Sets up search input behavior (Enter key submit, auto-focus)
 */
function initializeSearchInput(): void {
    const searchInput = document.querySelector<HTMLInputElement>('input[name="Query"]');
    const searchForm = document.getElementById('search-form') as HTMLFormElement | null;

    if (searchInput) {
        searchInput.addEventListener('keypress', (e: KeyboardEvent): void => {
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