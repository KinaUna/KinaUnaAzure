import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
/**
 * Adds event listeners to all elements with the data-measurement-id attribute.
 * When clicked, the DisplayMeasurementItem function is called.
 * @param {string} itemId The id of the Measurement to add event listeners for.
 */
export function addMeasurementItemListeners(itemId) {
    const elementsWithDataId = document.querySelectorAll('[data-measurement-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', async function () {
                await displayMeasurementItem(itemId);
            });
        });
    }
}
/**
 * Enable other scripts to call the DisplayMeasurementItem function.
 * @param {string} measurementId The id of the measurement item to display.
 */
export async function popupMeasurementItem(measurementId) {
    await displayMeasurementItem(measurementId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Displays a measurement item in a popup.
 * @param {string} measurementId The id of the measurement item to display.
 */
async function displayMeasurementItem(measurementId) {
    startFullPageSpinner();
    let url = '/Measurements/ViewMeasurement?measurementId=' + measurementId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const measurementElementHtml = await response.text();
            const measurementDetailsPopupDiv = document.querySelector('#item-details-div');
            if (measurementDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = measurementElementHtml;
                measurementDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                measurementDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            measurementDetailsPopupDiv.innerHTML = '';
                            measurementDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
            }
        }
        else {
            console.error('Error getting measurement item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting measurement item. Error: ' + error);
    });
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=measurement-details.js.map