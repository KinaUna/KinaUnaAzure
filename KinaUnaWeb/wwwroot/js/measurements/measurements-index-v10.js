import { setEditItemButtonEventListeners } from "../addItem/add-item-v10.js";
import { setMomentLocale } from "../data-tools-v10.js";
import { showPopupAtLoad } from "../item-details/items-display-v10.js";
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
    setupDataTable();
    await showPopupAtLoad(TimeLineType.Measurement);
    setEditItemButtonEventListeners();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=measurements-index-v10.js.map