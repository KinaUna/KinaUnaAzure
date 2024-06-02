﻿import { updateFilterButtonDisplay } from '../data-tools-v1.js';
$(function () {
    const filterButtons = document.querySelectorAll('.button-checkbox');
    filterButtons.forEach((filterButtonParentSpan) => {
        let filterButton = filterButtonParentSpan.querySelector('button');
        if (filterButton !== null) {
            filterButton.addEventListener('click', function (this: HTMLButtonElement) {
                updateFilterButtonDisplay(this);
            });
        }
        
        if (filterButton !== null) {
            updateFilterButtonDisplay(filterButton);
        }
    });
});