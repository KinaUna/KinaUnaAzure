import { updateFilterButtonDisplay } from '../data-tools-v2.js';
$(function () {
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
});
//# sourceMappingURL=contacts-index.js.map