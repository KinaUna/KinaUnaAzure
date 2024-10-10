import { getCurrentProgenyId } from '../data-tools-v8.js';
import { addTimelineItemEventListener, showPopupAtLoad } from '../item-details/items-display-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import * as pageModels from '../page-models-v8.js';
import * as SettingsHelper from '../settings-tools-v8.js';
import { setUpMapClickToShowLocationListener } from './location-tools.js';

const locationsPageSettingsStorageKey = 'locations_page_parameters';
let locationsPageParameters = new pageModels.LocationsPageParameters();
const sortAscendingSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-descending-button');
const sortByDateSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-by-date-button');
const sortByNameSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-by-name-button');
declare let map: H.Map;

/** Gets the page parameters from the data-locations-page attribute.
 */
function getLocationsPageParameters(): void {
    const pageParametersDiv = document.querySelector<HTMLDivElement>('#locations-page-parameters');
    if (pageParametersDiv !== null) {
        const locationsPageParametersJson = pageParametersDiv.getAttribute('data-locations-page-parameters');
        if (locationsPageParametersJson !== null) {
            locationsPageParameters = JSON.parse(locationsPageParametersJson) as pageModels.LocationsPageParameters;
        }
    }
}

/** Shows the loading spinner in the loading-items-div.
 */
function runLoadingSpinner(): void {
    const loadingItemsParent: HTMLDivElement | null = document.querySelector<HTMLDivElement>('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.remove('d-none');
        startLoadingItemsSpinner('loading-items-div');
    }
}

/** Hides the loading spinner in the loading-items-div.
 */
function stopLoadingSpinner(): void {
    const loadingItemsParent: HTMLDivElement | null = document.querySelector<HTMLDivElement>('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.add('d-none');
        stopLoadingItemsSpinner('loading-items-div');
    }
}

/** Retrieves the list of locations, then updates the page.
 */
async function getLocationsList(): Promise<void> {
    runLoadingSpinner();

    await fetch('/Locations/LocationsList', {
        method: 'POST',
        body: JSON.stringify(locationsPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json'
        }
    }).then(async function (getLocationsResult) {
        const locationsPageResponse = await getLocationsResult.json() as pageModels.LocationsPageResponse;
        if (locationsPageResponse && locationsPageResponse.locationsList.length > 0) {
            const locationListDiv = document.querySelector<HTMLDivElement>('#location-list-div');
            if (locationListDiv !== null) {
                locationListDiv.innerHTML = '';
            }

            for await (const locationId of locationsPageResponse.locationsList) {
                await getLocationElement(locationId);
            }

            updateTagsListDiv(locationsPageResponse.tagsList, locationsPageParameters.sortTags);
            updateActiveTagFilterDiv();
        }

    }).catch(function (error) {
        console.log('Error loading locations list. Error: ' + error);
    });

    stopLoadingSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function getLocationElement(id: number): Promise<void> {
    const getLocationElementParameters = new pageModels.LocationItemParameters();
    getLocationElementParameters.locationId = id;
    getLocationElementParameters.languageId = locationsPageParameters.languageId;

    await fetch('/Locations/LocationElement', {
        method: 'POST',
        body: JSON.stringify(getLocationElementParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (getLocationElementResult) {
        const locationHtml = await getLocationElementResult.text();
        const locationListDiv = document.querySelector<HTMLDivElement>('#location-list-div');
        if (locationListDiv != null) {
            locationListDiv.insertAdjacentHTML('beforeend', locationHtml);
            const timelineItem = new pageModels.TimelineItem();
            timelineItem.itemId = id.toString();
            timelineItem.itemType = 12;
            addTimelineItemEventListener(timelineItem);
        }
    }).catch(function (error) {
        console.log('Error loading location element. Error: ' + error);
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}


/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup(): Promise<void> {
    const locationsPageSaveSettingsButton = document.querySelector<HTMLButtonElement>('#locations-page-save-settings-button');
    if (locationsPageSaveSettingsButton !== null) {
        locationsPageSaveSettingsButton.addEventListener('click', saveLocationsPageSettings);
    }

    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortLocationsAscending);
        sortDescendingSettingsButton.addEventListener('click', sortLocationsDescending);
    }

    if (sortByDateSettingsButton !== null && sortByNameSettingsButton !== null) {
        sortByDateSettingsButton.addEventListener('click', sortByDate);
        sortByNameSettingsButton.addEventListener('click', sortByName);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Saves the current page parameters to local storage and reloads the location items list.
 */
async function saveLocationsPageSettings(): Promise<void> {
    const sortTagsSelect = document.querySelector<HTMLSelectElement>('#sort-tags-select');
    if (sortTagsSelect !== null) {
        locationsPageParameters.sortTags = sortTagsSelect.selectedIndex;
    }
    const saveAsDefaultCheckbox = document.querySelector<HTMLInputElement>('#settings-save-default-checkbox');
    if (saveAsDefaultCheckbox !== null && saveAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings<pageModels.LocationsPageParameters>(locationsPageSettingsStorageKey, locationsPageParameters);
    }

    SettingsHelper.toggleShowPageSettings();
    await getLocationsList();


    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Retrieves LocationsPageParameters saved in local storage.
 */
async function loadLocationsPageSettings(): Promise<void> {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings<pageModels.LocationsPageParameters>(locationsPageSettingsStorageKey);
    if (pageSettingsFromStorage) {

        locationsPageParameters.sortBy = pageSettingsFromStorage.sortBy;
        if (locationsPageParameters.sortBy === 0) {
            sortLocationsAscending();
        }
        else {
            sortByName();
        }

        locationsPageParameters.sort = pageSettingsFromStorage.sort;
        if (locationsPageParameters.sort === 0) {
            sortLocationsAscending();
        }
        else {
            sortLocationsDescending();
        }

        locationsPageParameters.sortTags = pageSettingsFromStorage.sortTags;
        const sortTagsElement = document.querySelector<HTMLSelectElement>('#sort-tags-select');
        if (sortTagsElement !== null) {
            sortTagsElement.value = locationsPageParameters.sortTags.toString();
            ($(".selectpicker") as any).selectpicker('refresh');
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortLocationsAscending(): Promise<void> {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    locationsPageParameters.sort = 0;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortLocationsDescending(): Promise<void> {
    sortAscendingSettingsButton?.classList.remove('active');
    sortDescendingSettingsButton?.classList.add('active');
    locationsPageParameters.sort = 1;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the Date button as active, and the Name button as inactive.
 */
async function sortByDate(): Promise<void> {
    sortByDateSettingsButton?.classList.add('active');
    sortByNameSettingsButton?.classList.remove('active');
    locationsPageParameters.sortBy = 0;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the Name button as active, and the Date button as inactive.
 */
async function sortByName(): Promise<void> {
    sortByDateSettingsButton?.classList.remove('active');
    sortByNameSettingsButton?.classList.add('active');
    locationsPageParameters.sortBy = 1;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Renders a list of tag buttons in the tags-list-div, each with a link to filter the page.
* @param tagsList The list of strings for each tag.
*/
function updateTagsListDiv(tagsList: string[], sortOrder: number): void {
    const tagsListDiv = document.querySelector<HTMLDivElement>('#tags-list-div');
    if (tagsListDiv !== null) {
        tagsListDiv.innerHTML = '';

        if (sortOrder === 1) {
            tagsList.sort((a, b) => a.localeCompare(b));
        }

        tagsList.forEach(function (tag: string) {
            tagsListDiv.innerHTML += '<a class="btn tag-item" data-tag-link="' + tag + '">' + tag + '</a>';
        });

        const tagButtons = document.querySelectorAll('[data-tag-link]');
        tagButtons.forEach((tagButton) => {
            tagButton.addEventListener('click', tagButtonClick);
        });
    }
}

/** If a tag filter is active, show the tag in the active tag filter div and provide a button to clear it.
*/
function updateActiveTagFilterDiv(): void {
    const activeTagFilterDiv = document.querySelector<HTMLDivElement>('#active-tag-filter-div');
    const activeTagFilterSpan = document.querySelector<HTMLSpanElement>('#current-tag-filter-span');
    if (activeTagFilterDiv !== null && activeTagFilterSpan !== null && locationsPageParameters !== null) {
        if (locationsPageParameters.tagFilter !== '') {
            activeTagFilterDiv.classList.remove('d-none');
            activeTagFilterSpan.innerHTML = locationsPageParameters.tagFilter;
        }
        else {
            activeTagFilterDiv.classList.add('d-none');
            activeTagFilterSpan.innerHTML = '';
        }
    }
}

/** Clears the active tag filter and reloads the default full list of locations.
*/
async function resetActiveTagFilter(): Promise<void> {
    if (locationsPageParameters !== null) {
        locationsPageParameters.tagFilter = '';
        await getLocationsList();
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Adds an event listener to the reset tag filter button.
 */
function addResetActiveTagFilterEventListener(): void {
    const resetTagFilterButton = document.querySelector<HTMLButtonElement>('#reset-tag-filter-button');
    if (resetTagFilterButton !== null) {
        resetTagFilterButton.addEventListener('click', resetActiveTagFilter);
    }
}

/** Event handler for tag buttons, sets the tag filter and reloads the list of locations.
*/
async function tagButtonClick(event: Event): Promise<void> {
    const target = event.target as HTMLElement;
    if (target !== null && locationsPageParameters !== null) {
        const tagLink = target.dataset.tagLink;
        if (tagLink !== undefined) {
            locationsPageParameters.tagFilter = tagLink;
            await getLocationsList();
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Select pickers don't always update when their values change, this ensures they show the correct items. */
function refreshSelectPickers(): void {
    const sortTagsSelect = document.querySelector<HTMLSelectElement>('#sort-tags-select');
    if (sortTagsSelect !== null && locationsPageParameters !== null) {
        sortTagsSelect.value = locationsPageParameters.sortTags.toString();
        ($(".selectpicker") as any).selectpicker('refresh');
    }
}

function setUpMap() {
    setUpMapClickToShowLocationListener(map);
}

function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            getSelectedProgenies();
            locationsPageParameters.currentPageNumber = 1;
            await getLocationsList();
        }

    });
}

function getSelectedProgenies() {
    let selectedProgenies = localStorage.getItem('selectedProgenies');
    if (selectedProgenies !== null) {
        let selectedProgenyIds: string[] = JSON.parse(selectedProgenies);
        let progeniesIds = selectedProgenyIds.map(function (id) {
            return parseInt(id);
        });
        locationsPageParameters.progenies = progeniesIds;

        return;
    }
    locationsPageParameters.progenies = [getCurrentProgenyId()];
}

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    initialSettingsPanelSetup();

    SettingsHelper.initPageSettings();
    getLocationsPageParameters();
    refreshSelectPickers();
    addResetActiveTagFilterEventListener();
    setUpMap();
    await loadLocationsPageSettings();
    await showPopupAtLoad(pageModels.TimeLineType.Location);

    addSelectedProgeniesChangedEventListener();
    getSelectedProgenies();

    await getLocationsList();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});