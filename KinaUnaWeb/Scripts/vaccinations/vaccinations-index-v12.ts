import { setEditItemButtonEventListeners } from "../addItem/add-item-v12.js";
import { getCurrentProgenyId, ProgenyChangedEvent, setMomentLocale } from "../data-tools-v12.js";
import { showPopupAtLoad } from "../item-details/items-display-v12.js";
import { startTopMenuSpinner, stopTopMenuSpinner } from "../navigation-tools-v12.js";
import { TimeLineType } from "../page-models-v12.js";
import { getProgenySelector } from "../shared/progeny-selector-v12.js";
import { popupVaccinationItem } from "./vaccination-details-v12.js";

declare global {
    interface WindowEventMap {
        'progenyChanged': ProgenyChangedEvent;
    }
}

async function getVaccinations(progenyId: number): Promise<void> {
    const vaccinationsListTable = document.querySelector<HTMLTableElement>('#vaccinations-container-div');
    if (vaccinationsListTable) {
        await fetch('/Vaccinations/VaccinationsTable?progenyId=' + progenyId).then(async function (vaccinationTableResponse) {
            if (vaccinationTableResponse != null) {
                const vaccinationTableContent = await vaccinationTableResponse.text();
                vaccinationsListTable.innerHTML = vaccinationTableContent;
                setupVaccinationsDataTable();
                setEditItemButtonEventListeners();
                setDetailsButtonEventListeners();
            }
        });
    }
}

function setDetailsButtonEventListeners(): void {
    const detailsButtons = document.querySelectorAll<HTMLAnchorElement>('.vaccination-details-button');
    detailsButtons.forEach((button) => {
        function detailsButtonClicked(): void {
            const itemId = button.getAttribute('data-vaccination-item-id');
            if (itemId) {
                popupVaccinationItem(itemId);
            }
        }
        button.removeEventListener('click', detailsButtonClicked)
        button.addEventListener('click', detailsButtonClicked);
    });
}

function setupVaccinationsDataTable(): void {
    setMomentLocale();
    (<any>$.fn.dataTable).moment('DD-MMM-YYYY');
    $('#vaccinations-table').DataTable({ 'scrollX': false, 'order': [[0, 'desc']] });
}

function addProgenyChangedEventListener() {
    // Subscribe to the timelineChanged event to refresh the todos list when a todo is added, updated, or deleted.
    window.addEventListener('progenyChanged', async (event: ProgenyChangedEvent) => {
        let changedItem = event.Progeny;
        if (changedItem !== null && changedItem.id !== 0) {
            await getVaccinations(changedItem.id);
        }
    });
}

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    startTopMenuSpinner();

    const currentProgenyId = await getCurrentProgenyId();
    await getProgenySelector(currentProgenyId, 0, 'progeny-selector-container');

    await showPopupAtLoad(TimeLineType.Vaccination);

    await getVaccinations(currentProgenyId);
    
    addProgenyChangedEventListener();

    stopTopMenuSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});