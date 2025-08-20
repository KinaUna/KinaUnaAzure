import { setMomentLocale } from '../data-tools-v9.js';
import { showPopupAtLoad } from '../item-details/items-display-v9.js';
import { TimeLineType } from '../page-models-v9.js';

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

    await showPopupAtLoad(TimeLineType.Skill);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});