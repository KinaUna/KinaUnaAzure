import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
/**
 * Adds event listeners to all elements with the data-location-id attribute.
 * When clicked, the DisplayLocationItem function is called.
 * @param {string} itemId The id of the Location to add event listeners for.
 */
export function addLocationItemListeners(itemId) {
    const elementsWithDataId = document.querySelectorAll('[data-location-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', onLocationItemDivClicked);
        });
    }
}
async function onLocationItemDivClicked(event) {
    const locationElement = event.currentTarget;
    if (locationElement !== null) {
        const locationId = locationElement.dataset.locationId;
        if (locationId) {
            await displayLocationItem(locationId);
            if (map) {
                // find location's marker on map and center it.
                let locationMarker;
                const mapObjects = map.getObjects();
                const len = map.getObjects().length;
                const locationIdParsed = parseInt(locationId);
                for (let i = 0; i < len; i++) {
                    if (mapObjects[i] instanceof H.map.Group) {
                        let mapGroup = mapObjects[i];
                        mapGroup.forEach((mapGroupItem) => {
                            if (mapGroupItem instanceof H.map.Marker && mapGroupItem.getData() === locationIdParsed) {
                                locationMarker = mapGroupItem;
                            }
                        });
                    }
                }
                ;
                if (locationMarker instanceof H.map.Marker) {
                    let markerPosition = locationMarker.getGeometry();
                    if (markerPosition instanceof H.geo.Point) {
                        map.setCenter(markerPosition);
                        map.setZoom(14);
                    }
                    ;
                }
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Enable other scripts to call the DisplayLocationItem function.
 * @param {string} locationId The id of the location item to display.
 */
export async function popupLocationItem(locationId) {
    displayLocationItem(locationId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Displays a location item in a popup.
 * @param {string} locationId The id of the location item to display.
 */
async function displayLocationItem(locationId) {
    startFullPageSpinner();
    let url = '/Locations/ViewLocation?locationId=' + locationId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const locationElementHtml = await response.text();
            const locationDetailsPopupDiv = document.querySelector('#item-details-div');
            if (locationDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = locationElementHtml;
                locationDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                locationDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            locationDetailsPopupDiv.innerHTML = '';
                            locationDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
                setEditItemButtonEventListeners();
                // Todo: If a map is loaded, center it on this location.
            }
        }
        else {
            console.error('Error getting location item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting location item. Error: ' + error);
    });
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=location-details.js.map