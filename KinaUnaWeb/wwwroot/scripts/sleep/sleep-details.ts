import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v6.js';

/**
 * Adds event listeners to all elements with the data-sleep-id attribute.
 * When clicked, the DisplaySleepItem function is called.
 * @param {string} itemId The id of the sleep to add event listeners for.
 */
export function addSleepEventListeners(itemId: string): void {
    const elementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-sleep-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', function () {
                displaySleepItem(itemId);
            });
        });
    }
}

/**
 * Enable other scripts to call the DisplaySleepItem function.
 * @param {string} sleepId The id of the sleep item to display.
 */
export function popupSleepItem(sleepId: string): void {
    displaySleepItem(sleepId);
}

/**
 * Displays a sleep item in a popup.
 * @param {string} sleepId The id of the sleep item to display.
 */
async function displaySleepItem(sleepId: string): Promise<void> {
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
            const sleepDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (sleepDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = sleepElementHtml;
                sleepDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                sleepDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            sleepDetailsPopupDiv.innerHTML = '';
                            sleepDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }

            }
        } else {
            console.error('Error getting sleep item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting sleep item. Error: ' + error);
    });

    stopFullPageSpinner();
}