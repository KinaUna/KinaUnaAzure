import { setEditItemButtonEventListeners } from '../addItem/add-item-v12.js';
import { getCurrentProgenyId, setMomentLocale } from '../data-tools-v12.js';
import { showPopupAtLoad } from '../item-details/items-display-v12.js';
import { startTopMenuSpinner, stopTopMenuSpinner } from '../navigation-tools-v12.js';
import { TimeLineType } from '../page-models-v12.js';
import { getProgenySelector } from '../shared/progeny-selector-v12.js';
async function getSkills(progenyId) {
    const skillsListTable = document.querySelector('#skills-container-div');
    if (skillsListTable) {
        await fetch('/Skills/SkillsTable?progenyId=' + progenyId, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(async function (skillsTableResponse) {
            if (skillsTableResponse != null) {
                const skillsTableContent = await skillsTableResponse.text();
                skillsListTable.innerHTML = skillsTableContent;
                setupDataTable();
                setEditItemButtonEventListeners();
            }
        });
    }
}
function addProgenyChangedEventListener() {
    // Subscribe to the timelineChanged event to refresh the todos list when a todo is added, updated, or deleted.
    window.addEventListener('progenyChanged', async (event) => {
        let changedItem = event.Progeny;
        if (changedItem !== null && changedItem.id !== 0) {
            await getSkills(changedItem.id);
        }
    });
}
/**
 * Sets up the DataTable for the Skills list.
 */
function setupDataTable() {
    setMomentLocale();
    $.fn.dataTable.moment('DD-MMMM-YYYY');
    $('#skills-list').DataTable({ 'scrollX': false, 'order': [[3, 'desc']], drawCallback: setEditItemButtonEventListeners });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    startTopMenuSpinner();
    const currentProgenyId = await getCurrentProgenyId();
    await getProgenySelector(currentProgenyId, 0, 'progeny-selector-container');
    await showPopupAtLoad(TimeLineType.Skill);
    await getSkills(currentProgenyId);
    addProgenyChangedEventListener();
    stopTopMenuSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=skills-index-v12.js.map