import { setMomentLocale } from '../data-tools-v8.js';
/**
 * Sets up the DataTable for the Skills list.
 */
function setupDataTable() {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMMM-YYYY');
    $('#skillz-list').DataTable({ 'scrollX': false, 'order': [[3, 'desc']] });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    setupDataTable();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=skills-index.js.map