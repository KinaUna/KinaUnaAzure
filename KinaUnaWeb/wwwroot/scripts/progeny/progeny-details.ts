import { setEditItemButtonEventListeners } from "../addItem/add-item.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v8.js";

/**
 * Adds click event listeners to all elements with data-progeny-id on the page.
 * When clicked, the progeny details popup is displayed.
 * @param {string} progenyId The ID of the progeny to display.
 */
export async function addProgenyItemEventListeners(progenyId: string): Promise<void> {
    const progenyElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-progeny-info-id="' + progenyId + '"]');
    if (progenyElementsWithDataId) {
        progenyElementsWithDataId.forEach((element) => {
            element.addEventListener('click', async function (event) {
                event.preventDefault();
                await displayProgenyDetails(progenyId);
            });
        });
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

export function addProgenyItemEventListenersForAllProgenies(): void {
    const progenyElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-progeny-info-id]');
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
export async function popupProgenyDetails(progenyId: string): Promise<void> {
    await displayProgenyDetails(progenyId);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function displayProgenyDetails(progenyId: string): Promise<void> {
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
            const itemDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (itemDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = itemElementHtml;
                itemDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                itemDetailsPopupDiv.classList.remove('d-none');
                setEditItemButtonEventListeners();
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            itemDetailsPopupDiv.innerHTML = '';
                            itemDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
            }
        }
    }).catch(function (error) {
        console.error('Get Progeny/Details Request failed', error);
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}
