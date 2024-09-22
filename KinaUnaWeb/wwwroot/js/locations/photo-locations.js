import { getCurrentProgenyId } from '../data-tools-v8.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { LocationItem, LocationsPageParameters, NearByPhotosRequest, PicturesLocationsRequest, TimeLineItemViewModel, TimelineItem } from '../page-models-v8.js';
import { setupHereMapsPhotoLocations } from './location-tools.js';
import * as SettingsHelper from '../settings-tools-v8.js';
let locationsPageParameters = new LocationsPageParameters();
let picturesLocationsRequest = new PicturesLocationsRequest();
let nearByPhotosRequest = new NearByPhotosRequest();
let distanceForLocationSearch = 0.1;
let distanceForPhotoSearch = 0.125;
let picturesList = [];
let picturesShown = 0;
let photoLocationsProgenyId;
let languageId = 1;
let selectedLocation;
let firstRun = true;
let map;
let defaultIcon = new H.map.Icon("/images/purplemarker.svg", { size: { w: 36, h: 36 } });
let group = new H.map.Group();
const sortAscendingSettingsButton = document.querySelector('#setting-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#setting-sort-descending-button');
/** Shows the loading spinner in the loading-timeline-items-div.
 */
function startLoadingSpinner() {
    startLoadingItemsSpinner('loading-photos-div');
}
/** Hides the loading spinner in the loading-timeline-items-div.
 */
function stopLoadingSpinner() {
    stopLoadingItemsSpinner('loading-photos-div');
}
async function getPicturesLocationsList() {
    startLoadingSpinner();
    picturesLocationsRequest.progenyId = photoLocationsProgenyId;
    picturesLocationsRequest.distance = distanceForLocationSearch;
    if (group) {
        group.removeAll();
    }
    await fetch('/Pictures/GetPicturesLocations/', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(picturesLocationsRequest)
    }).then(async function (getLocationsListResult) {
        if (getLocationsListResult != null) {
            const newLocationsList = (await getLocationsListResult.json());
            if (newLocationsList.locationsList.length > 0) {
                await processLocationsList(newLocationsList.locationsList);
            }
        }
    }).catch(function (error) {
        console.log('Error loading pictures locations. Error: ' + error);
    });
    stopLoadingSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function processLocationsList(locationsList) {
    if (map === null)
        return;
    group = new H.map.Group();
    for (const locationItem of locationsList) {
        let marker = new H.map.Marker({ lat: locationItem.latitude, lng: locationItem.longitude }, { icon: defaultIcon });
        group.addObject(marker);
    }
    map.addObject(group);
    if (firstRun) {
        firstRun = false;
        map.getViewModel().setLookAtData({ bounds: group.getBoundingBox() });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function getPicturesNearLocation(locationItem) {
    nearByPhotosRequest.locationItem = locationItem;
    nearByPhotosRequest.progenyId = photoLocationsProgenyId;
    nearByPhotosRequest.distance = distanceForPhotoSearch;
    nearByPhotosRequest.sortOrder = locationsPageParameters.sort;
    picturesShown = 0;
    await fetch('/Pictures/GetPicturesNearLocation/', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(nearByPhotosRequest)
    }).then(async function (getPicturesListResult) {
        if (getPicturesListResult != null) {
            const picturesListResult = (await getPicturesListResult.json());
            picturesList = picturesListResult.picturesList;
            if (picturesListResult.picturesList.length > 0) {
                await processPicturesNearLocation();
            }
        }
    }).catch(function (error) {
        console.log('Error loading pictures list. Error: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function processPicturesNearLocation() {
    startLoadingSpinner();
    const photoItemsParentDiv = document.querySelector('#photo-items-parent-div');
    if (photoItemsParentDiv !== null) {
        photoItemsParentDiv.classList.remove('d-none');
    }
    const morePicturesButton = document.getElementById('more-pictures-button');
    if (morePicturesButton !== null) {
        morePicturesButton.classList.add('d-none');
    }
    let count = 0;
    const endCount = picturesShown + locationsPageParameters.itemsPerPage;
    for await (const timelineItemToAdd of picturesList) {
        if (count >= picturesShown && count < endCount) {
            const pictureTimelineItem = new TimelineItem();
            pictureTimelineItem.itemId = timelineItemToAdd.pictureId.toString();
            pictureTimelineItem.itemType = 1;
            pictureTimelineItem.progenyId = photoLocationsProgenyId;
            await renderTimelineItem(pictureTimelineItem);
            picturesShown++;
        }
        count++;
    }
    ;
    stopLoadingSpinner();
    if (picturesShown < picturesList.length) {
        if (morePicturesButton !== null) {
            morePicturesButton.classList.remove('d-none');
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Fetches the HTML for a given timeline item and renders it at the end of timeline-items-div.
* @param timelineItem The timelineItem object to add to the page.
*/
async function renderTimelineItem(timelineItem) {
    const timeLineItemViewModel = new TimeLineItemViewModel();
    timeLineItemViewModel.typeId = timelineItem.itemType;
    timeLineItemViewModel.itemId = parseInt(timelineItem.itemId);
    const getTimelineElementResponse = await fetch('/Timeline/GetTimelineItemElement', {
        method: 'POST',
        body: JSON.stringify(timeLineItemViewModel),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    });
    if (getTimelineElementResponse.ok && getTimelineElementResponse.text !== null) {
        const timelineElementHtml = await getTimelineElementResponse.text();
        const timelineDiv = document.querySelector('#photo-items-div');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
            addTimelineItemEventListener(timelineItem);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function getLocationsPageParameters() {
    const pageParametersDiv = document.querySelector('#locations-page-parameters');
    if (pageParametersDiv !== null) {
        const locationsPageParametersJson = pageParametersDiv.getAttribute('data-locations-page-parameters');
        if (locationsPageParametersJson !== null) {
            locationsPageParameters = JSON.parse(locationsPageParametersJson);
        }
    }
}
function setUpMap() {
    if (map === null)
        return;
    map.addEventListener('tap', async function (evt) {
        if (map !== null && evt.currentPointer != null) {
            let coord = map.screenToGeo(evt.currentPointer.viewportX, evt.currentPointer.viewportY);
            map.setCenter(coord, true);
        }
        // Clear pictures div
        const photosDiv = document.getElementById('photo-items-div');
        if (photosDiv !== null) {
            photosDiv.innerHTML = "";
        }
        if (evt.target instanceof H.map.Marker) {
            const locationTapped = new LocationItem();
            locationTapped.latitude = evt.target.hm.lat;
            locationTapped.longitude = evt.target.hm.lng;
            locationTapped.progenyId = photoLocationsProgenyId;
            selectedLocation = locationTapped;
            await getPicturesNearLocation(locationTapped);
        }
    });
}
function sortPicturesAscending() {
    locationsPageParameters.sort = 0;
}
function sortPicturesDescending() {
    locationsPageParameters.sort = 1;
}
async function savePhotoLocationsPageSettings() {
    const numberOfItemsToGetSelect = document.querySelector('#items-per-page-select');
    if (numberOfItemsToGetSelect !== null) {
        locationsPageParameters.itemsPerPage = parseInt(numberOfItemsToGetSelect.value);
    }
    else {
        locationsPageParameters.itemsPerPage = 10;
    }
    const locationsDistanceSelect = document.querySelector('#locations-distance-select');
    if (locationsDistanceSelect !== null) {
        distanceForLocationSearch = parseFloat(locationsDistanceSelect.value);
    }
    else {
        distanceForLocationSearch = 0.25;
    }
    distanceForPhotoSearch = distanceForLocationSearch * 1.1;
    await getPicturesLocationsList();
}
document.addEventListener('DOMContentLoaded', async function () {
    photoLocationsProgenyId = getCurrentProgenyId();
    getLocationsPageParameters();
    map = setupHereMapsPhotoLocations(locationsPageParameters.languageId);
    setUpMap();
    const photoLocationsSaveSettingsButton = document.querySelector('#photo-locations-page-save-settings-button');
    if (photoLocationsSaveSettingsButton !== null) {
        photoLocationsSaveSettingsButton.addEventListener('click', savePhotoLocationsPageSettings);
    }
    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortPicturesAscending);
        sortDescendingSettingsButton.addEventListener('click', sortPicturesDescending);
    }
    SettingsHelper.initPageSettings();
    await getPicturesLocationsList();
    const morePicturesButton = document.getElementById('more-pictures-button');
    if (morePicturesButton !== null) {
        morePicturesButton.addEventListener('click', async function () {
            await processPicturesNearLocation();
        });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=photo-locations.js.map