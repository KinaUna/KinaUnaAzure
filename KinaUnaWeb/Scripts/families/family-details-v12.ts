import { setEditItemButtonEventListeners, setDeleteItemButtonEventListeners } from "../addItem/add-item-v12.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v12.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v12.js";

/**
 * Adds click event listeners to all elements with data-family-id on the page.
 * When clicked, the family details popup is displayed.
 */
export function addFamilyItemEventListenersForAllFamilies(): void {
    const familyElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-family-info-id]');
    if (familyElementsWithDataId) {
        familyElementsWithDataId.forEach((element) => {
            let familyId = element.getAttribute('data-family-info-id');
            if (familyId) {
                element.addEventListener('click', async function (event) {
                    event.preventDefault();
                    await displayFamilyDetails(familyId);
                });
            }
        });
    }
}

async function displayFamilyDetails(familyId: string): Promise<void> {
    startFullPageSpinner();
    let url = '/Families/FamilyDetails?familyId=' + familyId;
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
                history.pushState(null, document.title, window.location.href);
                itemDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            itemDetailsPopupDiv.innerHTML = '';
                            itemDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                            history.back();
                        });
                    });
                }
                setEditItemButtonEventListeners();
                setDeleteItemButtonEventListeners();
            }
        }
    }).catch(function (error) {
        console.error('Get Progeny/Details Request failed', error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}
