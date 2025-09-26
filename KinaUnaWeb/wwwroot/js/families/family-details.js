import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9.js";
import { hideBodyScrollbars } from "../item-details/items-display-v9.js";
export async function displayFamilyDetails(familyId) {
    startFullPageSpinner();
    const response = await fetch('/Families/FamilyDetails?familyId=' + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const familyDetailsDiv = document.querySelector('#item-details-div');
        if (familyDetailsDiv) {
            const familyDetailsHTML = await response.text();
            familyDetailsDiv.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            familyDetailsDiv.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            familyDetailsDiv.classList.remove('d-none');
            addFamilyDetailsEventListeners();
        }
    }
    else {
        console.error('Failed to fetch family element:', response.statusText);
        return Promise.reject('Failed to fetch family element: ' + response.statusText);
    }
    stopFullPageSpinner();
    return Promise.resolve();
}
function addFamilyDetailsEventListeners() {
}
//# sourceMappingURL=family-details.js.map