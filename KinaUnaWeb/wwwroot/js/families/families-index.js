import { displayFamilyDetails } from "./family-details.js";
import { displayAddFamilyModal, displayEditFamilyModal } from "./add-edit-family.js";
let familiesList = new Array();
export async function GetFamiliesList() {
    const familiesListDiv = document.querySelector('#families-list-div');
    if (familiesListDiv) {
        familiesListDiv.innerHTML = '';
        const response = await fetch('/Families/FamiliesList', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (response.ok) {
            const data = await response.json();
            for (const family of data) {
                await RenderFamilyElement(family.familyId);
            }
            return Promise.resolve();
        }
        else {
            console.error('Failed to fetch families list:', response.statusText);
            return Promise.reject('Failed to fetch families list: ' + response.statusText);
        }
    }
    return Promise.reject('Families list div not found in the document.');
}
async function RenderFamilyElement(familyId) {
    const response = await fetch('/Families/FamilyElement?familyId=' + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const familyElement = await response.text();
        const familiesListDiv = document.querySelector('#families-list-div');
        if (familiesListDiv) {
            familiesListDiv.insertAdjacentHTML('beforeend', familyElement);
            addFamilyElementEventListeners(familyId);
            return Promise.resolve();
        }
        return Promise.reject('Families list div not found in the document.');
    }
    else {
        console.error('Failed to fetch family element:', response.statusText);
        return Promise.reject('Failed to fetch family element: ' + response.statusText);
    }
}
function addFamilyElementEventListeners(familyId) {
    const familyElementDiv = document.querySelector('#family-element-' + familyId);
    if (familyElementDiv) {
        const familyElementClickedAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            await displayFamilyDetails(familyId);
        };
        familyElementDiv.removeEventListener('click', familyElementClickedAction);
        familyElementDiv.addEventListener('click', familyElementClickedAction);
    }
    const familyEditButton = document.querySelector('#edit-family-button-' + familyId);
    if (familyEditButton) {
        const familyEditButtonClickedAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            await displayEditFamilyModal(familyId);
        };
        familyEditButton.removeEventListener('click', familyEditButtonClickedAction);
        familyEditButton.addEventListener('click', familyEditButtonClickedAction);
    }
}
function addNewFamilyButtonEventListener() {
    const addNewFamilyButton = document.querySelector('#add-new-family-button');
    if (addNewFamilyButton) {
        const addNewFamilyButtonClickedAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            await displayAddFamilyModal();
        };
        addNewFamilyButton.removeEventListener('click', addNewFamilyButtonClickedAction);
        addNewFamilyButton.addEventListener('click', addNewFamilyButtonClickedAction);
    }
}
//# sourceMappingURL=families-index.js.map