import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v9.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v9.js';

/**
 * Adds event listeners to all elements with the data-measurement-id attribute.
 * When clicked, the DisplayMeasurementItem function is called.
 * @param {string} itemId The id of the Measurement to add event listeners for.
 */
export function addMeasurementItemListeners(itemId: string): void {
    const elementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-measurement-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', onMeasurmentItemDivClicked);
        });
    }
}

async function onMeasurmentItemDivClicked(event: MouseEvent): Promise<void> {
    const measurementElement: HTMLDivElement = event.currentTarget as HTMLDivElement;
    if (measurementElement !== null) {
        const measurementId = measurementElement.dataset.measurementId;
        if (measurementId) {
            await displayMeasurementItem(measurementId);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Enable other scripts to call the DisplayMeasurementItem function.
 * @param {string} measurementId The id of the measurement item to display.
 */
export async function popupMeasurementItem(measurementId: string): Promise<void> {
    await displayMeasurementItem(measurementId);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Displays a measurement item in a popup.
 * @param {string} measurementId The id of the measurement item to display.
 */
async function displayMeasurementItem(measurementId: string): Promise<void> {
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
            const measurementDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (measurementDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = measurementElementHtml;
                measurementDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                measurementDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            measurementDetailsPopupDiv.innerHTML = '';
                            measurementDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
                setEditItemButtonEventListeners();
            }
        } else {
            console.error('Error getting measurement item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting measurement item. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}