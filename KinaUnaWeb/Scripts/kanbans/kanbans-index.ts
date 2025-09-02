import { setAddItemButtonEventListeners } from "../addItem/add-item.js";
import { setContextAutoSuggestList, setTagsAutoSuggestList, TimelineChangedEvent } from "../data-tools-v9.js";
import { addTimelineItemEventListener, showPopupAtLoad } from "../item-details/items-display-v9.js";
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from "../navigation-tools-v9.js";
import { KanbanBoardElementParameters, KanbanBoardsPageParameters, KanbanBoardsPageResponse, TimelineItem, TimeLineType } from "../page-models-v9.js";
import { getSelectedProgenies } from "../settings-tools-v9.js";
import * as SettingsHelper from '../settings-tools-v9.js';

let kanbanBoardsPageParameters = new KanbanBoardsPageParameters();
const kanbansPageSettingsStorageKey = 'kanbans_page_parameters';
const kanbansIndexPageParametersDiv = document.querySelector<HTMLDivElement>('#kanbans-index-page-parameters-div');
const kanbansListDiv = document.querySelector<HTMLDivElement>('#kanban-boards-list-div');
const todosPageMainDiv = document.querySelector<HTMLDivElement>('#kinauna-main-div');
let moreKanbanBoardsButton: HTMLButtonElement | null;
const itemsPerPageInput = document.querySelector<HTMLInputElement>('#kanbans-index-items-per-page-input');

declare global {
    interface WindowEventMap {
        'timelineChanged': TimelineChangedEvent;
    }
}

/**
 * Sets the kanbans page parameters from the data attributes of the kanbansIndexPageParametersDiv.
 */
function setKabansPageParametersFromPageData(): void {
    if (kanbansIndexPageParametersDiv !== null) {
        const pageParameters = kanbansIndexPageParametersDiv.dataset.todosIndexPageParameters;
        if (pageParameters) {
            kanbanBoardsPageParameters = JSON.parse(pageParameters);
        }
    }
}

async function getKanbanBoards(): Promise<void> {
    startLoadingSpinner();
    moreKanbanBoardsButton?.classList.add('d-none');

    const getMoreKanbanBoardsResponse = await fetch('/Kanbans/GetKanbanBoardsList', {
        method: 'POST',
        body: JSON.stringify(kanbanBoardsPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
        }
    });

    if (getMoreKanbanBoardsResponse.ok && getMoreKanbanBoardsResponse.body !== null) {
        const kanbanBoardsPageResponse = await getMoreKanbanBoardsResponse.json() as KanbanBoardsPageResponse;
        if (kanbanBoardsPageResponse) {
            kanbanBoardsPageParameters.totalPages = kanbanBoardsPageResponse.totalPages;
            kanbanBoardsPageParameters.totalItems = kanbanBoardsPageResponse.totalItems;
            
            if (kanbanBoardsPageResponse.totalItems < 1) {
                getKanbanBoardElement(0);
            }
            else {
                for await (const kanbanBoardItem of kanbanBoardsPageResponse.kanbanBoards) {
                    await getKanbanBoardElement(kanbanBoardItem.kanbanBoardId);
                    const timelineItem = new TimelineItem();
                    timelineItem.itemId = kanbanBoardItem.kanbanBoardId.toString();
                    timelineItem.itemType = 16;
                    addTimelineItemEventListener(timelineItem);
                };
            }
            kanbanBoardsPageParameters.currentPageNumber++;
            if (kanbanBoardsPageResponse.totalPages > kanbanBoardsPageResponse.pageNumber && moreKanbanBoardsButton !== null) {
                moreKanbanBoardsButton.classList.remove('d-none');
            }
        }
    }

    stopLoadingSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function refreshKanbanBoards(changedKanbanBoardId: string) {
    let tempPageNumber = kanbanBoardsPageParameters.currentPageNumber;
    kanbanBoardsPageParameters.currentPageNumber = 1;
    clearKanbanBoardElements();
    while (kanbanBoardsPageParameters.currentPageNumber <= tempPageNumber) {
        await getKanbanBoards();
    }

    // If changedKanbanBoardId is not zero, scroll to the KanbanBoard item with data-kanban-board-id={changedKanbanBoardId}, if it exists in the page.
    if (parseInt(changedKanbanBoardId) > 0) {
        const kanbanBoardElement = document.querySelector<HTMLButtonElement>(`[data-todo-id="${changedKanbanBoardId}"]`);
        if (kanbanBoardElement !== null) {
            kanbanBoardElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }
}


async function getKanbanBoardElement(id: number): Promise<void> {
    const getKanbanBoardElementParameters = new KanbanBoardElementParameters();
    getKanbanBoardElementParameters.kanbanBoardId = id;
    getKanbanBoardElementParameters.languageId = kanbanBoardsPageParameters.languageId;

    const getKanbanBoardElementResponse = await fetch('/Kanbans/KanbanBoardElement', {
        method: 'POST',
        body: JSON.stringify(getKanbanBoardElementParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    });

    if (getKanbanBoardElementResponse.ok && getKanbanBoardElementResponse.text !== null) {
        const kanbanBoardHtml = await getKanbanBoardElementResponse.text();
        if (kanbansListDiv != null) {
            kanbansListDiv.insertAdjacentHTML('beforeend', kanbanBoardHtml);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            kanbanBoardsPageParameters.progenies = getSelectedProgenies();
            kanbanBoardsPageParameters.currentPageNumber = 1;
            await getKanbanBoards();
        }
    });
}


function addTimelineChangedEventListener() {
    // Subscribe to the timelineChanged event to refresh the KanbanBoards list when a KanbanBoard is added, updated, or deleted.
    window.addEventListener('timelineChanged', async (event: TimelineChangedEvent) => {
        let changedItem = event.TimelineItem;
        if (changedItem !== null && changedItem.itemType === 16) { // 16 is the item type for KanbanBoards.
            if (changedItem.itemId !== '') {
                await refreshKanbanBoards(changedItem.itemId);
            }
        }
    });
}

/** Shows the loading spinner in the loading-todo-items-div.
 */
function startLoadingSpinner(): void {
    startLoadingItemsSpinner('loading-kanban-boards-div');
}

/** Hides the loading spinner in the loading-todo-items-div.
 */
function stopLoadingSpinner(): void {
    stopLoadingItemsSpinner('loading-kanban-boards-div');
}

/** Clears the list of KanbanBoard elements in the kanban-boards-list-div and scrolls to above the kanban-boards-list-div.
*/
function clearKanbanBoardElements(): void {
    const pageTitleDiv = document.querySelector<HTMLDivElement>('#page-title-div');
    if (pageTitleDiv !== null) {
        pageTitleDiv.scrollIntoView();
    }

    const kanbanBoardItemsDiv = document.querySelector<HTMLDivElement>('#kanban-boards-list-div');
    if (kanbanBoardItemsDiv !== null) {
        kanbanBoardItemsDiv.innerHTML = '';
    }

    kanbanBoardsPageParameters.currentPageNumber = 1;
}

function decreaseKanbanBoardsItemsPerPage(): void {
    if (itemsPerPageInput !== null) {
        let itemsPerPageInputValue: number = itemsPerPageInput.valueAsNumber;
        itemsPerPageInputValue--;
        if (itemsPerPageInputValue > 0) {
            itemsPerPageInput.value = itemsPerPageInputValue.toString();
        }

        kanbanBoardsPageParameters.itemsPerPage = itemsPerPageInput.valueAsNumber;
    }
}

function increaseKanbanBoardsItemsPerPage(): void {
    if (itemsPerPageInput !== null) {
        let itemsPerPageInputValue: number = itemsPerPageInput.valueAsNumber;
        itemsPerPageInputValue++;
        if (itemsPerPageInputValue < 101) {
            itemsPerPageInput.value = itemsPerPageInputValue.toString();
        }

        kanbanBoardsPageParameters.itemsPerPage = itemsPerPageInput.valueAsNumber;
    }
}

function setEventListenersForItemsPerPage(): void {
    const decreaseItemsPerPageButton = document.querySelector<HTMLButtonElement>('#decrease-kanban-index-items-per-page-button');
    const increaseItemsPerPageButton = document.querySelector<HTMLButtonElement>('#increase-kanban-index-items-per-page-button');
    if (decreaseItemsPerPageButton !== null) {
        decreaseItemsPerPageButton.addEventListener('click', decreaseKanbanBoardsItemsPerPage);
    }
    if (increaseItemsPerPageButton !== null) {
        increaseItemsPerPageButton.addEventListener('click', increaseKanbanBoardsItemsPerPage);
    }
}

/**
* Toggles the state of the filter button and updates the display of filter elements.
*/
function toggleShowFilters(): void {
    const filtersElements = document.querySelectorAll<HTMLDivElement>('.kanbans-filter-options');
    const toggleShowFiltersChevron = document.getElementById('show-filters-chevron');
    filtersElements.forEach(function (element: HTMLDivElement) {
        if (element.classList.contains('d-none')) {
            element.classList.remove('d-none');
        }
        else {
            element.classList.add('d-none');
        }
    });

    if (toggleShowFiltersChevron !== null) {
        if (toggleShowFiltersChevron.classList.contains('chevron-right-rotate-down')) {
            toggleShowFiltersChevron.classList.remove('chevron-right-rotate-down');
        }
        else {
            toggleShowFiltersChevron.classList.add('chevron-right-rotate-down');
        }
    }
}

function loadKanbansPageSettings(): void {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings<KanbanBoardsPageParameters>(kanbansPageSettingsStorageKey);
    if (pageSettingsFromStorage !== null) {

        kanbanBoardsPageParameters.itemsPerPage = pageSettingsFromStorage.itemsPerPage ?? 10;
       
    }

    if (itemsPerPageInput !== null) {
        itemsPerPageInput.value = kanbanBoardsPageParameters.itemsPerPage.toString();
    }
}

/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup(): Promise<void> {
    const kanbansPageSaveSettingsButton = document.querySelector<HTMLButtonElement>('#kanbans-page-save-settings-button');
    if (kanbansPageSaveSettingsButton !== null) {
        kanbansPageSaveSettingsButton.removeEventListener('click', saveKanbansPageSettings);
        kanbansPageSaveSettingsButton.addEventListener('click', saveKanbansPageSettings);
    }
        
    setEventListenersForItemsPerPage();
    
    const toggleShowFiltersButton = document.querySelector<HTMLButtonElement>('#kanbans-toggle-filters-button');
    if (toggleShowFiltersButton !== null) {
        const toggleShowFiltersFunction = function (event: Event) {
            event.preventDefault();
            toggleShowFilters();
        }
        toggleShowFiltersButton.removeEventListener('click', toggleShowFiltersFunction)
        toggleShowFiltersButton.addEventListener('click', toggleShowFiltersFunction);
    }
    
    await setTagsAutoSuggestList(kanbanBoardsPageParameters.progenies, 'tag-filter-input', true);
    await setContextAutoSuggestList(kanbanBoardsPageParameters.progenies, 'context-filter-input', true);

    updateSettingsNotificationDiv();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates the settings notification div with the current settings.
 * This function is called when the settings are changed or saved.
 */
function updateSettingsNotificationDiv() {
    let kanbansSettingsNotificationText: string = '';
        
    const itemsPerPageSpan = document.querySelector<HTMLDivElement>('#settings-items-per-page-label');
    if (itemsPerPageSpan !== null) {
        kanbansSettingsNotificationText += '<br/>' + itemsPerPageSpan.innerHTML + ' ' + kanbanBoardsPageParameters.itemsPerPage;
    }
    
    const contextFilterInput = document.querySelector<HTMLInputElement>('#context-filter-input');
    if (contextFilterInput !== null && contextFilterInput.value.length > 0) {
        const settingsContextFilterLabel = document.querySelector<HTMLSpanElement>('#context-filter-span');
        if (settingsContextFilterLabel !== null) {
            kanbansSettingsNotificationText += '<br/>' + settingsContextFilterLabel.innerHTML;
        }
        kanbansSettingsNotificationText += ' [' + contextFilterInput.value + ']';
    }

    const tagFilterInput = document.querySelector<HTMLInputElement>('#tag-filter-input');
    if (tagFilterInput !== null && tagFilterInput.value.length > 0) {
        const settingsTagFilterLabel = document.querySelector<HTMLSpanElement>('#tag-filter-span');
        if (settingsTagFilterLabel !== null) {
            kanbansSettingsNotificationText += '<br/>' + settingsTagFilterLabel.innerHTML;
        }
        kanbansSettingsNotificationText += ' [' + tagFilterInput.value + ']';
    }

    const settingsNotificationsDiv = document.querySelector<HTMLDivElement>('#settings-notification-div');
    if (settingsNotificationsDiv !== null && kanbansSettingsNotificationText !== undefined) {
        settingsNotificationsDiv.innerHTML = kanbansSettingsNotificationText;
    }
}

/**
 * Saves the current page parameters to local storage and reloads the KanbanBoards items list.
 */
async function saveKanbansPageSettings(): Promise<void> {
    const numberOfItemsToGetInput = document.querySelector<HTMLInputElement>('#kanbans-index-items-per-page-input');
    if (numberOfItemsToGetInput !== null) {
        kanbanBoardsPageParameters.itemsPerPage = parseInt(numberOfItemsToGetInput.value);
    }
    else {
        kanbanBoardsPageParameters.itemsPerPage = 10;
    }
    
    const tagFilterInput = document.querySelector<HTMLInputElement>('#tag-filter-input');
    if (tagFilterInput !== null) {
        kanbanBoardsPageParameters.tagFilter = tagFilterInput.value;
    }

    const contextFilterInput = document.querySelector<HTMLInputElement>('#context-filter-input');
    if (contextFilterInput !== null) {
        kanbanBoardsPageParameters.contextFilter = contextFilterInput.value;
    }

    // If the 'set as default' checkbox is checked, save the page settings to local storage.
    const setAsDefaultCheckbox = document.querySelector<HTMLInputElement>('#kanbans-settings-save-default-checkbox');
    if (setAsDefaultCheckbox !== null && setAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings<KanbanBoardsPageParameters>(kanbansPageSettingsStorageKey, kanbanBoardsPageParameters);
    }

    SettingsHelper.toggleShowPageSettings();
    clearKanbanBoardElements();
    updateSettingsNotificationDiv();
    await getKanbanBoards();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });

}

/** Initializes the Todos page by setting up event listeners and fetching initial data.
 * This function is called when the DOM content is fully loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    await showPopupAtLoad(TimeLineType.KanbanBoard);

    setKabansPageParametersFromPageData();
    loadKanbansPageSettings();
    addSelectedProgeniesChangedEventListener();
    addTimelineChangedEventListener();
    kanbanBoardsPageParameters.progenies = getSelectedProgenies();

    moreKanbanBoardsButton = document.querySelector<HTMLButtonElement>('#more-kanban-boards-button');
    if (moreKanbanBoardsButton !== null) {
        moreKanbanBoardsButton.removeEventListener('click', getKanbanBoards);
        moreKanbanBoardsButton.addEventListener('click', getKanbanBoards);
    }

    SettingsHelper.initPageSettings();
    initialSettingsPanelSetup();
    setAddItemButtonEventListeners();

    getKanbanBoards();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});