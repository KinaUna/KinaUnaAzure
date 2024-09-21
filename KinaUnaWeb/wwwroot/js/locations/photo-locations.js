import { getCurrentProgenyId } from '../data-tools-v8.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { LocationItem, LocationsPageParameters, TimeLineItemViewModel, TimelineItem } from '../page-models-v8.js';
let locationsPageParameters = new LocationsPageParameters();
let locationsList = [];
let picturesList = [];
let picturesShown = 0;
let photoLocationsProgenyId;
let languageId = 1;
let selectedLocation;
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
    await fetch('/Pictures/GetPicturesLocations/', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(locationsPageParameters)
    }).then(async function (getLocationsListResult) {
        if (getLocationsListResult != null) {
            const newLocationsList = (await getLocationsListResult.json());
            if (newLocationsList.length > 0) {
                await processLocationsList(newLocationsList);
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
    let group = new H.map.Group();
    for (const locationItem of locationsList) {
        let marker = new H.map.Marker({ lat: locationItem.latitude, lng: locationItem.longitude }, { icon: defaultIcon });
        map.addObject(marker);
        group.addObject(marker);
    }
    map.addObject(group);
    map.getViewModel().setLookAtData({ bounds: group.getBoundingBox() });
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function getPicturesNearLocation(locationItem) {
    picturesShown = 0;
    await fetch('/Pictures/GetPicturesNearLocation/', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(locationItem)
    }).then(async function (getPicturesListResult) {
        if (getPicturesListResult != null) {
            const picturesListResult = (await getPicturesListResult.json());
            picturesList = picturesListResult;
            if (picturesListResult.length > 0) {
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
    const onThisDayPostsParentDiv = document.querySelector('#photo-items-parent-div');
    if (onThisDayPostsParentDiv !== null) {
        onThisDayPostsParentDiv.classList.remove('d-none');
    }
    const morePicturesButton = document.getElementById('more-pictures-button');
    if (morePicturesButton !== null) {
        morePicturesButton.classList.add('d-none');
    }
    let count = 0;
    const endCount = picturesShown + 10;
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
    map.addEventListener('tap', async function (evt) {
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
        if (evt.currentPointer != null) {
            let coord = map.screenToGeo(evt.currentPointer.viewportX, evt.currentPointer.viewportY);
            map.setCenter(coord, true);
        }
    });
}
document.addEventListener('DOMContentLoaded', async function () {
    setUpMap();
    photoLocationsProgenyId = getCurrentProgenyId();
    getLocationsPageParameters();
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