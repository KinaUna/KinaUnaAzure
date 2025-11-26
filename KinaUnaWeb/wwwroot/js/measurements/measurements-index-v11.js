import { setEditItemButtonEventListeners } from "../addItem/add-item-v11.js";
import { setMomentLocale } from "../data-tools-v11.js";
import { showPopupAtLoad } from "../item-details/items-display-v11.js";
import { startTopMenuSpinner, stopTopMenuSpinner } from "../navigation-tools-v11.js";
import { TimeLineType } from "../page-models-v11.js";
function setupDataTable() {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMMM-YYYY HH:mm');
    $('#measurements-list').DataTable({ 'scrollX': false, 'order': [[0, 'desc']], drawCallback: setEditItemButtonEventListeners });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    startTopMenuSpinner();
    setupDataTable();
    await showPopupAtLoad(TimeLineType.Measurement);
    setEditItemButtonEventListeners();
    stopTopMenuSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=measurements-index-v11.js.map