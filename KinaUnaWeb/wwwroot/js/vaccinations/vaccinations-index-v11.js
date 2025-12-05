import { setEditItemButtonEventListeners } from "../addItem/add-item-v11.js";
import { getCurrentProgenyId, setMomentLocale } from "../data-tools-v11.js";
import { showPopupAtLoad } from "../item-details/items-display-v11.js";
import { startTopMenuSpinner, stopTopMenuSpinner } from "../navigation-tools-v11.js";
import { TimeLineType } from "../page-models-v11.js";
import { getProgenySelector } from "../shared/progeny-selector-v11.js";
async function getVaccinations(progenyId) {
    const vaccinationsListTable = document.querySelector('#vaccinations-container-div');
    if (vaccinationsListTable) {
        await fetch('/Vaccinations/VaccinationsTable?progenyId=' + progenyId).then(async function (vaccinationTableResponse) {
            if (vaccinationTableResponse != null) {
                const vaccinationTableContent = await vaccinationTableResponse.text();
                vaccinationsListTable.innerHTML = vaccinationTableContent;
                setupVaccinationsDataTable();
                setEditItemButtonEventListeners();
            }
        });
    }
}
function setupVaccinationsDataTable() {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMM-YYYY');
    $('#vaccinations-table').DataTable({ 'scrollX': false, 'order': [[0, 'desc']] });
}
function addProgenyChangedEventListener() {
    // Subscribe to the timelineChanged event to refresh the todos list when a todo is added, updated, or deleted.
    window.addEventListener('progenyChanged', async (event) => {
        let changedItem = event.Progeny;
        if (changedItem !== null && changedItem.id !== 0) {
            await getVaccinations(changedItem.id);
        }
    });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    startTopMenuSpinner();
    const currentProgenyId = await getCurrentProgenyId();
    await getProgenySelector(currentProgenyId, 0, 'progeny-selector-container');
    await showPopupAtLoad(TimeLineType.Vaccination);
    await getVaccinations(currentProgenyId);
    setEditItemButtonEventListeners();
    addProgenyChangedEventListener();
    stopTopMenuSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=vaccinations-index-v11.js.map