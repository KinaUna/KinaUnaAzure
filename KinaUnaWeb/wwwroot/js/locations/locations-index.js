import { addTimelineItemEventListener, showPopupAtLoad } from '../item-details/items-display-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import * as pageModels from '../page-models-v8.js';
import * as SettingsHelper from '../settings-tools-v8.js';
import { getSelectedProgenies } from '../settings-tools-v8.js';
import { setUpMapClickToShowLocationListener } from './location-tools.js';
const locationsPageSettingsStorageKey = 'locations_page_parameters';
let locationsPageParameters = new pageModels.LocationsPageParameters();
const sortAscendingSettingsButton = document.querySelector('#settings-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#settings-sort-descending-button');
const sortByDateSettingsButton = document.querySelector('#settings-sort-by-date-button');
const sortByNameSettingsButton = document.querySelector('#settings-sort-by-name-button');
let group = new H.map.Group();
/** Gets the page parameters from the data-locations-page attribute.
 */
function getLocationsPageParameters() {
    const pageParametersDiv = document.querySelector('#locations-page-parameters');
    if (pageParametersDiv !== null) {
        const locationsPageParametersJson = pageParametersDiv.getAttribute('data-locations-page-parameters');
        if (locationsPageParametersJson !== null) {
            locationsPageParameters = JSON.parse(locationsPageParametersJson);
        }
    }
}
/** Shows the loading spinner in the loading-items-div.
 */
function runLoadingSpinner() {
    const loadingItemsParent = document.querySelector('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.remove('d-none');
        startLoadingItemsSpinner('loading-items-div');
    }
}
/** Hides the loading spinner in the loading-items-div.
 */
function stopLoadingSpinner() {
    const loadingItemsParent = document.querySelector('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.add('d-none');
        stopLoadingItemsSpinner('loading-items-div');
    }
}
/** Retrieves the list of locations, then updates the page.
 */
async function getLocationsList() {
    runLoadingSpinner();
    await fetch('/Locations/LocationsList', {
        method: 'POST',
        body: JSON.stringify(locationsPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json'
        }
    }).then(async function (getLocationsResult) {
        const locationsPageResponse = await getLocationsResult.json();
        if (locationsPageResponse && locationsPageResponse.locationsList.length > 0) {
            const locationListDiv = document.querySelector('#location-list-div');
            if (locationListDiv !== null) {
                locationListDiv.innerHTML = '';
            }
            let defaultMarkerIcon = new H.map.Icon("/images/purplemarker.svg", { size: { w: 36, h: 36 } });
            group.removeAll();
            let lineString = new H.geo.LineString();
            for await (const locationItem of locationsPageResponse.locationsList) {
                await getLocationElement(locationItem.locationId);
                // add marker to map
                if (locationItem.latitude !== 0 && locationItem.longitude !== 0) {
                    let location = { lat: locationItem.latitude, lng: locationItem.longitude };
                    let marker = new H.map.Marker(location, { icon: defaultMarkerIcon });
                    marker.setData(locationItem.locationId);
                    map.addObject(marker);
                    group.addObject(marker);
                    lineString.pushPoint(location);
                }
            }
            map.addObject(group);
            map.addObject(new H.map.Polyline(lineString, { style: { lineWidth: 4 } }));
            map.getViewModel().setLookAtData({ bounds: group.getBoundingBox() });
            updateTagsListDiv(locationsPageResponse.tagsList, locationsPageParameters.sortTags);
            updateActiveTagFilterDiv();
        }
    }).catch(function (error) {
        console.log('Error loading locations list. Error: ' + error);
    });
    stopLoadingSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function getLocationElement(id) {
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
        const locationListDiv = document.querySelector('#location-list-div');
        if (locationListDiv != null) {
            locationListDiv.insertAdjacentHTML('beforeend', locationHtml);
            const timelineItem = new pageModels.TimelineItem();
            timelineItem.itemId = id.toString();
            timelineItem.itemType = 12;
            addTimelineItemEventListener(timelineItem);
            // when clicking on item, center and zoom in on the marker
        }
    }).catch(function (error) {
        console.log('Error loading location element. Error: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup() {
    const locationsPageSaveSettingsButton = document.querySelector('#locations-page-save-settings-button');
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Saves the current page parameters to local storage and reloads the location items list.
 */
async function saveLocationsPageSettings() {
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null) {
        locationsPageParameters.sortTags = sortTagsSelect.selectedIndex;
    }
    const saveAsDefaultCheckbox = document.querySelector('#settings-save-default-checkbox');
    if (saveAsDefaultCheckbox !== null && saveAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings(locationsPageSettingsStorageKey, locationsPageParameters);
    }
    SettingsHelper.toggleShowPageSettings();
    await getLocationsList();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Retrieves LocationsPageParameters saved in local storage.
 */
async function loadLocationsPageSettings() {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings(locationsPageSettingsStorageKey);
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
        const sortTagsElement = document.querySelector('#sort-tags-select');
        if (sortTagsElement !== null) {
            sortTagsElement.value = locationsPageParameters.sortTags.toString();
            $(".selectpicker").selectpicker('refresh');
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortLocationsAscending() {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    locationsPageParameters.sort = 0;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortLocationsDescending() {
    sortAscendingSettingsButton?.classList.remove('active');
    sortDescendingSettingsButton?.classList.add('active');
    locationsPageParameters.sort = 1;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the Date button as active, and the Name button as inactive.
 */
async function sortByDate() {
    sortByDateSettingsButton?.classList.add('active');
    sortByNameSettingsButton?.classList.remove('active');
    locationsPageParameters.sortBy = 0;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the Name button as active, and the Date button as inactive.
 */
async function sortByName() {
    sortByDateSettingsButton?.classList.remove('active');
    sortByNameSettingsButton?.classList.add('active');
    locationsPageParameters.sortBy = 1;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Renders a list of tag buttons in the tags-list-div, each with a link to filter the page.
* @param tagsList The list of strings for each tag.
*/
function updateTagsListDiv(tagsList, sortOrder) {
    const tagsListDiv = document.querySelector('#tags-list-div');
    if (tagsListDiv !== null) {
        tagsListDiv.innerHTML = '';
        if (sortOrder === 1) {
            tagsList.sort((a, b) => a.localeCompare(b));
        }
        tagsList.forEach(function (tag) {
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
function updateActiveTagFilterDiv() {
    const activeTagFilterDiv = document.querySelector('#active-tag-filter-div');
    const activeTagFilterSpan = document.querySelector('#current-tag-filter-span');
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
async function resetActiveTagFilter() {
    if (locationsPageParameters !== null) {
        locationsPageParameters.tagFilter = '';
        await getLocationsList();
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Adds an event listener to the reset tag filter button.
 */
function addResetActiveTagFilterEventListener() {
    const resetTagFilterButton = document.querySelector('#reset-tag-filter-button');
    if (resetTagFilterButton !== null) {
        resetTagFilterButton.addEventListener('click', resetActiveTagFilter);
    }
}
/** Event handler for tag buttons, sets the tag filter and reloads the list of locations.
*/
async function tagButtonClick(event) {
    const target = event.target;
    if (target !== null && locationsPageParameters !== null) {
        const tagLink = target.dataset.tagLink;
        if (tagLink !== undefined) {
            locationsPageParameters.tagFilter = tagLink;
            await getLocationsList();
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Select pickers don't always update when their values change, this ensures they show the correct items. */
function refreshSelectPickers() {
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null && locationsPageParameters !== null) {
        sortTagsSelect.value = locationsPageParameters.sortTags.toString();
        $(".selectpicker").selectpicker('refresh');
    }
}
function setUpMap() {
    setUpMapClickToShowLocationListener(map);
}
function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            locationsPageParameters.progenies = getSelectedProgenies();
            locationsPageParameters.currentPageNumber = 1;
            await getLocationsList();
        }
    });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    initialSettingsPanelSetup();
    SettingsHelper.initPageSettings();
    getLocationsPageParameters();
    refreshSelectPickers();
    addResetActiveTagFilterEventListener();
    setUpMap();
    await loadLocationsPageSettings();
    await showPopupAtLoad(pageModels.TimeLineType.Location);
    addSelectedProgeniesChangedEventListener();
    locationsPageParameters.progenies = getSelectedProgenies();
    await getLocationsList();
    window.addEventListener('resize', () => map.getViewPort().resize());
    map.getViewPort().resize();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=locations-index.js.map