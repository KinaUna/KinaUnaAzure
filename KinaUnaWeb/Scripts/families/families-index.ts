import { Family } from "../page-models-v9.js";
import { displayFamilyDetails } from "./family-details.js";
import { displayAddFamilyModal, displayEditFamilyModal } from "./add-edit-family.js";

let familiesList = new Array<Family>();

export async function GetFamiliesList(): Promise<void> {
    const familiesListDiv = document.querySelector<HTMLDivElement>('#families-list-div');
    if (familiesListDiv) {
        familiesListDiv.innerHTML = '';
        const response = await fetch('/Families/FamiliesList', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (response.ok) {
            const data = await response.json() as Family[];
            for (const family of data) {
                await RenderFamilyElement(family.familyId);
            }

            return Promise.resolve();
        } else {
            console.error('Failed to fetch families list:', response.statusText);
            return Promise.reject('Failed to fetch families list: ' + response.statusText);
        }
    }    
    return Promise.reject('Families list div not found in the document.');
}

async function RenderFamilyElement(familyId: number): Promise<void> {
        const response = await fetch('/Families/FamilyElement?familyId=' + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const familyElement = await response.text();
        const familiesListDiv = document.querySelector<HTMLDivElement>('#families-list-div');
        if (familiesListDiv) {
            familiesListDiv.insertAdjacentHTML('beforeend', familyElement);
            addFamilyElementEventListeners(familyId);
            return Promise.resolve();
        }
        return Promise.reject('Families list div not found in the document.');
    } else {
        console.error('Failed to fetch family element:', response.statusText);
        return Promise.reject('Failed to fetch family element: ' + response.statusText);
    }
}

function addFamilyElementEventListeners(familyId: number): void {
    const familyElementDiv = document.querySelector<HTMLDivElement>('#family-element-' + familyId);
    if (familyElementDiv) {
        const familyElementClickedAction = async function (event: MouseEvent) {
            event.preventDefault();
            event.stopPropagation();
            await displayFamilyDetails(familyId);
        };

        familyElementDiv.removeEventListener('click', familyElementClickedAction);
        familyElementDiv.addEventListener('click', familyElementClickedAction);
    }

    const familyEditButton = document.querySelector<HTMLButtonElement>('#edit-family-button-' + familyId);
    if (familyEditButton) {
        const familyEditButtonClickedAction = async function (event: MouseEvent) {
            event.preventDefault();
            event.stopPropagation();
            await displayEditFamilyModal(familyId);
        };
        familyEditButton.removeEventListener('click', familyEditButtonClickedAction);
        familyEditButton.addEventListener('click', familyEditButtonClickedAction);
    }
}

function addNewFamilyButtonEventListener(): void {
    const addNewFamilyButton = document.querySelector<HTMLButtonElement>('#add-new-family-button');
    if (addNewFamilyButton) {
        const addNewFamilyButtonClickedAction = async function (event: MouseEvent) {
            event.preventDefault();
            event.stopPropagation();
            await displayAddFamilyModal();
        };

        addNewFamilyButton.removeEventListener('click', addNewFamilyButtonClickedAction);
        addNewFamilyButton.addEventListener('click', addNewFamilyButtonClickedAction);
    }
}