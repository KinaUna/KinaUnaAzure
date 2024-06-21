import { setMomentLocale } from '../data-tools-v6.js';

/**
 * Sets up the DataTable for the Skills list.
 */
function setupDataTable(): void {
    setMomentLocale();
    (<any>$.fn.dataTable).moment('DD-MMMM-YYYY');
    $('#skillz-list').DataTable({ 'scrollX': false, 'order': [[3, 'desc']] });
}

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    setupDataTable();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});