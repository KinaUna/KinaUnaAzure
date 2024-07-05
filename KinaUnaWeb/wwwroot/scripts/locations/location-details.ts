import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v6.js';
declare let map: H.Map;

/**
 * Adds event listeners to all elements with the data-location-id attribute.
 * When clicked, the DisplayLocationItem function is called.
 * @param {string} itemId The id of the Location to add event listeners for.
 */
export function addLocationItemListeners(itemId: string): void {
    const elementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-location-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', function () {
                displayLocationItem(itemId);
            });
        });
    }
}

/**
 * Enable other scripts to call the DisplayLocationItem function.
 * @param {string} locationId The id of the location item to display.
 */
export function popupLocationItem(locationId: string): void {
    displayLocationItem(locationId);
}

/**
 * Displays a location item in a popup.
 * @param {string} locationId The id of the location item to display.
 */
async function displayLocationItem(locationId: string): Promise<void> {
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
            const locationDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (locationDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = locationElementHtml;
                locationDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                locationDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            locationDetailsPopupDiv.innerHTML = '';
                            locationDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }

                // Todo: If a map is loaded, center it on this location.
            }
        } else {
            console.error('Error getting location item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting location item. Error: ' + error);
    });

    stopFullPageSpinner();
}