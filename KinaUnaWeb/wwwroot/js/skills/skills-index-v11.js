import { setEditItemButtonEventListeners } from '../addItem/add-item-v11.js';
import { setMomentLocale } from '../data-tools-v11.js';
import { showPopupAtLoad } from '../item-details/items-display-v11.js';
import { TimeLineType } from '../page-models-v11.js';
/**
 * Sets up the DataTable for the Skills list.
 */
function setupDataTable() {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMMM-YYYY');
    $('#skillz-list').DataTable({ 'scrollX': false, 'order': [[3, 'desc']], drawCallback: setEditItemButtonEventListeners });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    setupDataTable();
    await showPopupAtLoad(TimeLineType.Skill);
    setEditItemButtonEventListeners();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=skills-index-v11.js.map