import { getCurrentLanguageId } from "../data-tools-v12.js";
import { hideBodyScrollbars } from "../item-details/items-display-v12.js";
import { startFullPageSpinner, startTopMenuSpinner, stopFullPageSpinner, stopTopMenuSpinner } from "../navigation-tools-v12.js";
import { Progeny } from "../page-models-v12.js";
import { popupProgenyDetails } from "./progeny-details-v12.js";

let progeniesList = new Array<Progeny>();
let languageId = 1; // Default to English

export async function getOtherPeopleList(): Promise<void> {
    const otherPeopleListDiv = document.querySelector<HTMLDivElement>('#people-and-pets-list-div');
    if (otherPeopleListDiv) {
        otherPeopleListDiv.innerHTML = '';
        const response = await fetch('/Progeny/OtherPeopleAndPetsList', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (response.ok) {
            const data = await response.json() as Progeny[];
            for (const progeny of data) {
                await renderOtherPeopleElement(progeny.id);
            }

            return Promise.resolve();
        } else {
            console.error('Failed to fetch other people and pets list:', response.statusText);
            return Promise.reject('Failed to fetch other people and pets list: ' + response.statusText);
        }
    }
    return Promise.reject('Families list div not found in the document.');
}

async function renderOtherPeopleElement(progenyId: number): Promise<void> {
    const response = await fetch('/Progeny/OtherPeopleElement?progenyId=' + progenyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const otherPeopleElement = await response.text();
        const otherPeopleListDiv = document.querySelector<HTMLDivElement>('#people-and-pets-list-div');
        if (otherPeopleListDiv) {
            otherPeopleListDiv.insertAdjacentHTML('beforeend', otherPeopleElement);
            addOtherPeopleElementEventListeners(progenyId);
            return Promise.resolve();
        }
        return Promise.reject('Other people and pets list div not found in the document.');
    } else {
        console.error('Failed to fetch other people or pets element:', response.statusText);
        return Promise.reject('Failed to fetch family element: ' + response.statusText);
    }
}

function addOtherPeopleElementEventListeners(progenyId: number): void {
    const progenyDetailsButton = document.querySelector<HTMLButtonElement>('#progeny-details-button-' + progenyId);
    if (progenyDetailsButton) {
        const progenyDetailsButtonClickedAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            await popupProgenyDetails(progenyId.toString());
        };
        // Clear existing event listeners.
        progenyDetailsButton.removeEventListener('click', progenyDetailsButtonClickedAction);
        progenyDetailsButton.addEventListener('click', progenyDetailsButtonClickedAction);
    }

    const addToFamilyButton = document.querySelector<HTMLButtonElement>('#add-to-family-button-' + progenyId);
    if (addToFamilyButton) {
        const addToFamilyButtonClickedAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const response = await fetch('/Progeny/AddOtherPersonToFamily?progenyId=' + progenyId, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            if (response.ok) {
                const addToFamilyDiv = document.querySelector<HTMLDivElement>('#add-to-family-div');
                if (addToFamilyDiv) {
                    const addToFamilyContent = await response.text();
                    addToFamilyDiv.innerHTML = '';
                    const fullScreenOverlay = document.createElement('div');
                    fullScreenOverlay.classList.add('full-screen-bg');
                    fullScreenOverlay.innerHTML = addToFamilyContent;
                    addToFamilyDiv.appendChild(fullScreenOverlay);
                    hideBodyScrollbars();
                    addToFamilyDiv.classList.remove('d-none');
                    ($(".selectpicker") as any).selectpicker('refresh');
                    addAddToFamilyDivEventListeners();
                }

                return Promise.resolve();
            } else {
                console.error('Failed to add other person to family:', response.statusText);
                return Promise.reject('Failed to add other person to family: ' + response.statusText);
            }
        };
        // Clear existing event listeners.
        addToFamilyButton.removeEventListener('click', addToFamilyButtonClickedAction);
        addToFamilyButton.addEventListener('click', addToFamilyButtonClickedAction);
    }
}

function addAddToFamilyDivEventListeners() {
    const closeButtonsList = document.querySelectorAll('.add-to-family-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function (): void {
                const addToFamilyDiv = document.querySelector<HTMLDivElement>('#add-to-family-div');
                if (!addToFamilyDiv) return;
                addToFamilyDiv.innerHTML = '';
                addToFamilyDiv.classList.add('d-none');
            };
            // Clear existing event listeners.
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }
    // Add event listener to the form submit button.
    const addToFamilyForm = document.querySelector<HTMLFormElement>('#add-to-family-form');
    if (addToFamilyForm) {
        const addToFamilyFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            startFullPageSpinner();
            const formData = new FormData(addToFamilyForm);
            const response = await fetch('/Progeny/AddOtherPersonToFamily', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                // Successfully added to family, refresh the other people and pets list.
                await getOtherPeopleList();
                const addToFamilyDiv = document.querySelector<HTMLDivElement>('#add-to-family-div');
                if (addToFamilyDiv) {
                    addToFamilyDiv.innerHTML = '';
                    addToFamilyDiv.classList.add('d-none');
                }
                stopFullPageSpinner();
                return Promise.resolve();
            } else {
                stopFullPageSpinner();
                console.error('Failed to submit add to family form:', response.statusText);
                return Promise.reject('Failed to submit add to family form: ' + response.statusText);
            }
        };
        // Clear existing event listeners.
        addToFamilyForm.removeEventListener('submit', addToFamilyFormSubmitAction);
        addToFamilyForm.addEventListener('submit', addToFamilyFormSubmitAction);
    }
}

document.addEventListener('DOMContentLoaded', async function () {
    startTopMenuSpinner();

    languageId = getCurrentLanguageId();

    await getOtherPeopleList();
    stopTopMenuSpinner();
    return Promise.resolve();
});