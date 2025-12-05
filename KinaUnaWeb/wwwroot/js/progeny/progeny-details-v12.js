import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item-v12.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v12.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v12.js";
/**
 * Adds click event listeners to all elements with data-progeny-id on the page.
 * When clicked, the progeny details popup is displayed.
 * @param {string} progenyId The ID of the progeny to display.
 */
export async function addProgenyItemEventListeners(progenyId) {
    const progenyElementsWithDataId = document.querySelectorAll('[data-progeny-info-id="' + progenyId + '"]');
    if (progenyElementsWithDataId) {
        progenyElementsWithDataId.forEach((element) => {
            element.addEventListener('click', async function (event) {
                event.preventDefault();
                await displayProgenyDetails(progenyId);
            });
        });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Adds click event listeners to all elements with data-progeny-id on the page.
 * When clicked, the progeny details popup is displayed.
 */
export function addProgenyItemEventListenersForAllProgenies() {
    const progenyElementsWithDataId = document.querySelectorAll('[data-progeny-info-id]');
    if (progenyElementsWithDataId) {
        progenyElementsWithDataId.forEach((element) => {
            let progenyId = element.getAttribute('data-progeny-info-id');
            if (progenyId) {
                element.addEventListener('click', async function (event) {
                    event.preventDefault();
                    await displayProgenyDetails(progenyId);
                });
            }
        });
    }
}
/**
 * Enable other scripts to call the displayProgenyDetails function.
 * @param {string} progenyId The id of the progeny to display.
 */
export async function popupProgenyDetails(progenyId) {
    await displayProgenyDetails(progenyId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function displayProgenyDetails(progenyId) {
    startFullPageSpinner();
    let url = '/Progeny/Details?progenyId=' + progenyId;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    }).then(async function (response) {
        if (response.ok) {
            const itemElementHtml = await response.text();
            const itemDetailsPopupDiv = document.querySelector('#item-details-div');
            if (itemDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = itemElementHtml;
                itemDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                itemDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            itemDetailsPopupDiv.innerHTML = '';
                            itemDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
                setEditItemButtonEventListeners();
                setDeleteItemButtonEventListeners();
                setCopyContentEventListners();
            }
        }
    }).catch(function (error) {
        console.error('Get Progeny/Details Request failed', error);
    });
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function setCopyContentEventListners() {
    let copyContentButtons = document.querySelectorAll('.copy-content-button');
    if (copyContentButtons) {
        copyContentButtons.forEach((button) => {
            button.addEventListener('click', async function () {
                if (!button.dataset.copyContentId) {
                    return;
                }
                let copyContentElement = document.getElementById(button.dataset.copyContentId);
                if (copyContentElement !== null) {
                    let copyText = copyContentElement.textContent;
                    if (copyText === null) {
                        return;
                    }
                    navigator.clipboard.writeText(copyText);
                    // Notify that the text has been copied to the clipboard.
                    let notificationSpan = button.querySelector('.toast-notification');
                    if (notificationSpan !== null) {
                        notificationSpan.classList.remove('d-none');
                        setTimeout(function () {
                            notificationSpan.classList.add('d-none');
                        }, 3000);
                    }
                }
            });
        });
    }
}
//# sourceMappingURL=progeny-details-v12.js.map