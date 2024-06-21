import { updateFilterButtonDisplay } from '../data-tools-v6.js';
/**
 * Sets up the event listners for filtering buttons on the page, to select/deselect options and show/hide the associated contact items.
 */
function setupFilterButtons() {
    const filterButtons = document.querySelectorAll('.button-checkbox');
    filterButtons.forEach((filterButtonParentSpan) => {
        let filterButton = filterButtonParentSpan.querySelector('button');
        if (filterButton !== null) {
            filterButton.addEventListener('click', function () {
                updateFilterButtonDisplay(this);
            });
        }
        if (filterButton !== null) {
            updateFilterButtonDisplay(filterButton);
        }
    });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', function () {
    setupFilterButtons();
});
//# sourceMappingURL=friends-index.js.map