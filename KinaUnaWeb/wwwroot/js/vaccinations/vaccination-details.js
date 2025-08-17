import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v9.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v9.js';
/**
 * Adds event listeners to all elements with the data-vaccination-id attribute.
 * When clicked, the displayVaccinationItem function is called.
 * @param {string} itemId The id of the Vaccination to add event listeners for.
 */
export function addVaccinationItemListeners(itemId) {
    const elementsWithDataId = document.querySelectorAll('[data-vaccination-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', onVaccinationItemDivClicked);
        });
    }
}
async function onVaccinationItemDivClicked(event) {
    const vaccinationElement = event.currentTarget;
    if (vaccinationElement !== null) {
        const vaccinationId = vaccinationElement.dataset.vaccinationId;
        if (vaccinationId) {
            await displayVaccinationItem(vaccinationId);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Enable other scripts to call the displayVaccinationItem function.
 * @param {string} vaccinationId The id of the vaccination item to display.
 */
export async function popupVaccinationItem(vaccinationId) {
    await displayVaccinationItem(vaccinationId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Displays a vaccination item in a popup.
 * @param {string} vaccinationId The id of the vaccination item to display.
 */
async function displayVaccinationItem(vaccinationId) {
    startFullPageSpinner();
    let url = '/Vaccinations/ViewVaccination?vaccinationId=' + vaccinationId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const vaccinationElementHtml = await response.text();
            const vaccinationDetailsPopupDiv = document.querySelector('#item-details-div');
            if (vaccinationDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = vaccinationElementHtml;
                vaccinationDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                vaccinationDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            vaccinationDetailsPopupDiv.innerHTML = '';
                            vaccinationDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
                setEditItemButtonEventListeners();
            }
        }
        else {
            console.error('Error getting vaccination item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting vaccination item. Error: ' + error);
    });
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=vaccination-details.js.map