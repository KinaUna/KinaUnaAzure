import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
/**
 * Adds event listeners to all elements with the data-sleep-id attribute.
 * When clicked, the DisplaySleepItem function is called.
 * @param {string} itemId The id of the sleep to add event listeners for.
 */
export function addSleepEventListeners(itemId) {
    const elementsWithDataId = document.querySelectorAll('[data-sleep-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', onSleepItemDivClicked);
        });
    }
}
async function onSleepItemDivClicked(event) {
    const sleepElement = event.currentTarget;
    if (sleepElement !== null) {
        const sleepId = sleepElement.dataset.sleepId;
        if (sleepId) {
            await displaySleepItem(sleepId);
        }
    }
}
/**
 * Enable other scripts to call the DisplaySleepItem function.
 * @param {string} sleepId The id of the sleep item to display.
 */
export async function popupSleepItem(sleepId) {
    await displaySleepItem(sleepId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Displays a sleep item in a popup.
 * @param {string} sleepId The id of the sleep item to display.
 */
async function displaySleepItem(sleepId) {
    startFullPageSpinner();
    let url = '/Sleep/ViewSleep?sleepId=' + sleepId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const sleepElementHtml = await response.text();
            const sleepDetailsPopupDiv = document.querySelector('#item-details-div');
            if (sleepDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = sleepElementHtml;
                sleepDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                sleepDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            sleepDetailsPopupDiv.innerHTML = '';
                            sleepDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
                setEditItemButtonEventListeners();
            }
        }
        else {
            console.error('Error getting sleep item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting sleep item. Error: ' + error);
    });
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=sleep-details.js.map