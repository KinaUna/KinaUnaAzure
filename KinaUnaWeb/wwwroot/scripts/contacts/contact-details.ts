import { setCopyContentEventListners } from '../data-tools-v8.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';

/**
 * Adds event listeners to all elements with the data-contact-id attribute.
 * When clicked, the displayContactItem function is called.
 * @param {string} itemId The id of the Contact to add event listeners for.
 */
export function addContactItemListeners(itemId: string): void {
    const elementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-contact-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', async function () {
                await displayContactItem(itemId);
            });
        });
    }
}

/**
 * Enable other scripts to call the displayContactItem function.
 * @param {string} contactId The id of the contact item to display.
 */
export async function popupContactItem(contactId: string): Promise<void> {
    await displayContactItem(contactId);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Displays a contact item in a popup.
 * @param {string} contactId The id of the contact item to display.
 */
async function displayContactItem(contactId: string): Promise<void> {
    startFullPageSpinner();
    let url = '/Contacts/ViewContact?contactId=' + contactId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const contactElementHtml = await response.text();
            const contactDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (contactDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = contactElementHtml;
                contactDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                contactDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            contactDetailsPopupDiv.innerHTML = '';
                            contactDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }

                setCopyContentEventListners();
            }
        } else {
            console.error('Error getting contact item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting contact item. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}