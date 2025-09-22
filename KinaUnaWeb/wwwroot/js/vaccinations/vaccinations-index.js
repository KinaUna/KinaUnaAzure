import { setEditItemButtonEventListeners } from "../addItem/add-item.js";
import { setMomentLocale } from "../data-tools-v9.js";
import { showPopupAtLoad } from "../item-details/items-display-v9.js";
import { TimeLineType } from "../page-models-v9.js";
function setupDataTable() {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMM-YYYY');
    $('#vaccinations-list').DataTable({ 'scrollX': false, 'order': [[0, 'desc']], drawCallback: setEditItemButtonEventListeners });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    setupDataTable();
    await showPopupAtLoad(TimeLineType.Vaccination);
    setEditItemButtonEventListeners();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=vaccinations-index.js.map