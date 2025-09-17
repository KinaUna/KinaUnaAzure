import { setEditItemButtonEventListeners } from "../addItem/add-item.js";
import { setMomentLocale } from "../data-tools-v9.js";
import { showPopupAtLoad } from "../item-details/items-display-v9.js";
import { TimeLineType } from "../page-models-v9.js";

function setupDataTable(): void {
    setMomentLocale();
    (<any>$.fn.dataTable).moment('DD-MMM-YYYY');
    $('#vaccinations-list').DataTable({ 'scrollX': false, 'order': [[0, 'desc']], drawCallback: setEditItemButtonEventListeners });
}

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    setupDataTable();

    await showPopupAtLoad(TimeLineType.Vaccination);

    setEditItemButtonEventListeners();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});