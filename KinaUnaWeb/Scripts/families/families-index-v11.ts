import { Family } from "../page-models-v11.js";
import { getCurrentLanguageId, TimelineChangedEvent } from "../data-tools-v11.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v11.js";
import { hideBodyScrollbars } from "../item-details/items-display-v11.js";
import { displayAddFamilyMemberModal, displayFamilyMemberDetails } from "./family-members-v11.js";

let familiesList = new Array<Family>();
let languageId = 1; // Default to English

declare global {
    interface WindowEventMap {
        'timelineChanged': TimelineChangedEvent;
    }
}

async function getFamiliesList(): Promise<void> {
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
                await renderFamilyElement(family.familyId);
            }

            return Promise.resolve();
        } else {
            console.error('Failed to fetch families list:', response.statusText);
            return Promise.reject('Failed to fetch families list: ' + response.statusText);
        }
    }    
    return Promise.reject('Families list div not found in the document.');
}

async function renderFamilyElement(familyId: number): Promise<void> {
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
    const familyInfoButton = document.querySelector<HTMLButtonElement>('#family-info-button-' + familyId);
    if (familyInfoButton) {
        const familyInfoButtonClickedAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            await displayFamilyDetails(familyId);
        };
        familyInfoButton.removeEventListener('click', familyInfoButtonClickedAction);
        familyInfoButton.addEventListener('click', familyInfoButtonClickedAction);
    }

    const familyEditButton = document.querySelector<HTMLButtonElement>('#edit-family-button-' + familyId);
    if (familyEditButton) {
        const familyEditButtonClickedAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            await displayEditFamilyModal(familyId);
        };
        familyEditButton.removeEventListener('click', familyEditButtonClickedAction);
        familyEditButton.addEventListener('click', familyEditButtonClickedAction);
    }

    const addFamilyMemberButtons = document.querySelectorAll<HTMLButtonElement>('.add-new-family-member-button');
    addFamilyMemberButtons.forEach((button) => {
        const addFamilyMemberButtonClickedAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const familyId = button.getAttribute('data-family-id');
            if (familyId) {
                // Show add family member modal
                await displayAddFamilyMemberModal(familyId);
            }
            return Promise.resolve();
        };
        button.removeEventListener('click', addFamilyMemberButtonClickedAction);
        button.addEventListener('click', addFamilyMemberButtonClickedAction);
    });

    const familyMemberDivs = document.querySelectorAll<HTMLDivElement>('[data-family-member-family-id]');
    familyMemberDivs.forEach((div) => {
        // Check if the div's family ID matches the current familyId
        const divFamilyId = div.getAttribute('data-family-member-family-id');
        if (divFamilyId && parseInt(divFamilyId) === familyId) {
            const familyMemberDivClickedAction = async function (event: MouseEvent): Promise<void> {
                event.preventDefault();
                const familyMemberId = div.getAttribute('data-family-member-id');
                if (familyMemberId) {
                    // Show family member details modal
                    await displayFamilyMemberDetails(parseInt(familyMemberId));
                }
            }
            div.removeEventListener('click', familyMemberDivClickedAction);
            div.addEventListener('click', familyMemberDivClickedAction);
        }
    });
}

async function displayFamilyDetails(familyId: number): Promise<void> {
    startFullPageSpinner();

    const response = await fetch('/Families/FamilyDetails?familyId=' + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }

    });
    if (response.ok) {
        const familyDetailsDiv = document.querySelector<HTMLDivElement>('#item-details-div');
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

    } else {
        console.error('Failed to fetch family element:', response.statusText);
        return Promise.reject('Failed to fetch family element: ' + response.statusText);
    }

    stopFullPageSpinner();

    return Promise.resolve();
}

function addCloseButtonsEventListeners(): void {
    const closeButtons = document.querySelectorAll<HTMLButtonElement>('.family-details-close-button');
    closeButtons.forEach((closeButton) => {
        const closeButtonClickedAction = function (event: MouseEvent): void {
            event.preventDefault();
            event.stopPropagation();
            const modalDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (modalDiv) {
                modalDiv.innerHTML = '';
                modalDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
    });
}

function addFamilyDetailsEventListeners(): void {
    addCloseButtonsEventListeners();
    
    const editFamilyButton = document.querySelector<HTMLButtonElement>('#edit-family-button');
    if (editFamilyButton) {
        const editFamilyClickedAction = function (event: MouseEvent): void {
            event.preventDefault();
            event.stopPropagation();

            const familyId = parseInt(editFamilyButton.getAttribute('data-edit-item-item-id') || '0');
            displayEditFamilyModal(familyId);
        }

        editFamilyButton.removeEventListener('click', editFamilyClickedAction);
        editFamilyButton.addEventListener('click', editFamilyClickedAction);
    }

    const deleteFamilyButton = document.querySelector<HTMLButtonElement>('#delete-family-button');
    if (deleteFamilyButton) {
        const deleteFamilyClickedAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            event.stopPropagation();

            const familyId = parseInt(deleteFamilyButton.getAttribute('data-delete-item-item-id') || '0');
            displayDeleteFamilyModal(familyId);
            
        }
        deleteFamilyButton.removeEventListener('click', deleteFamilyClickedAction);
        deleteFamilyButton.addEventListener('click', deleteFamilyClickedAction);
    }
}

function addNewFamilyButtonEventListener(): void {
    const addNewFamilyButton = document.querySelector<HTMLButtonElement>('#add-new-family-button');
    if (addNewFamilyButton) {
        const addNewFamilyButtonClickedAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            await displayAddFamilyModal();
        };

        addNewFamilyButton.removeEventListener('click', addNewFamilyButtonClickedAction);
        addNewFamilyButton.addEventListener('click', addNewFamilyButtonClickedAction);
    }
}

async function displayAddFamilyModal(): Promise<void> {
    startFullPageSpinner();
    let popup = document.getElementById('item-details-div');
    const response = await fetch('/Families/AddFamily', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        if (popup) {
            let modalContent = await response.text();
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.id = 'full-screen-overlay-div';
            fullScreenOverlay.innerHTML = modalContent;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addAddFamilyModalEventListeners();
            await initializeAddEditFamily(0);
        }
        else {
            stopFullPageSpinner();
            return Promise.reject('Item details div not found in the document.');
        }

    } else {
        stopFullPageSpinner();
        console.error('Failed to fetch add family element:', response.statusText);
        return Promise.reject('Failed to fetch add family element: ' + response.statusText);
    }

    stopFullPageSpinner();

    return Promise.resolve();
}

function addAddFamilyModalEventListeners(): void {
    addCloseButtonsEventListeners();

    const addFamilyForm = document.querySelector<HTMLFormElement>('#add-family-form');
    if (addFamilyForm) {
        const addFamilyFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(addFamilyForm);
            const response = await fetch('/Families/AddFamily', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                let popup = document.getElementById('item-details-div');
                if (popup) {
                    popup.innerHTML = '';
                    popup.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the families list on the main page.
                    await getFamiliesList();
                }

            } else {
                console.error('Failed to add family:', response.statusText);
                return Promise.reject('Failed to add family: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        addFamilyForm.removeEventListener('submit', addFamilyFormSubmitAction);
        addFamilyForm.addEventListener('submit', addFamilyFormSubmitAction);
    }
}

async function displayEditFamilyModal(familyId: number): Promise<void> {
    startFullPageSpinner();
    let popup = document.getElementById('item-details-div');
    const response = await fetch('/Families/EditFamily?familyId=' + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        if (popup) {
            const familyDetailsHTML = await response.text();
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.id = 'full-screen-overlay-div';
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addEditFamilyModalEventListeners();
            await initializeAddEditFamily(familyId);
        }
        else {
            stopFullPageSpinner();
            return Promise.reject('Item details div not found in the document.');
        }

    } else {
        stopFullPageSpinner();
        console.error('Failed to fetch edit family element:', response.statusText);
        return Promise.reject('Failed to fetch edit family element: ' + response.statusText);
    }

    stopFullPageSpinner();

    return Promise.resolve();
}

function addEditFamilyModalEventListeners(): void {
    addCloseButtonsEventListeners();

    const editFamilyForm = document.querySelector<HTMLFormElement>('#edit-family-form');
    if (editFamilyForm) {
        const editFamilyFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(editFamilyForm);
            const response = await fetch('/Families/EditFamily', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const familyDetailsDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (familyDetailsDiv) {
                    familyDetailsDiv.innerHTML = '';
                    familyDetailsDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the families list on the main page.
                    await getFamiliesList();
                }

            } else {
                stopFullPageSpinner();
                console.error('Failed to update family:', response.statusText);
                return Promise.reject('Failed to update family: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        editFamilyForm.removeEventListener('submit', editFamilyFormSubmitAction);
        editFamilyForm.addEventListener('submit', editFamilyFormSubmitAction);
    }
}

async function displayDeleteFamilyModal(familyId: number): Promise<void> {
    startFullPageSpinner();
    let popup = document.getElementById('item-details-div');
    const response = await fetch('/Families/DeleteFamily?familyId=' + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        if (popup) {
            const familyDetailsHTML = await response.text();
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addDeleteFamilyModalEventListeners();
            await initializeAddEditFamily(familyId);
        }
        else {
            stopFullPageSpinner();
            return Promise.reject('Item details div not found in the document.');
        }

    } else {
        stopFullPageSpinner();
        console.error('Failed to fetch edit family element:', response.statusText);
        return Promise.reject('Failed to fetch edit family element: ' + response.statusText);
    }

    stopFullPageSpinner();

    return Promise.resolve();
}

function addDeleteFamilyModalEventListeners(): void {
    addCloseButtonsEventListeners();

    const deleteFamilyForm = document.querySelector<HTMLFormElement>('#delete-family-form');
    if (deleteFamilyForm) {
        const deleteFamilyFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(deleteFamilyForm);
            const response = await fetch('/Families/DeleteFamily', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const familyDetailsDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (familyDetailsDiv) {
                    familyDetailsDiv.innerHTML = '';
                    familyDetailsDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the families list on the main page.
                    await getFamiliesList();
                }

            } else {
                stopFullPageSpinner();
                console.error('Failed to update family:', response.statusText);
                return Promise.reject('Failed to update family: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        deleteFamilyForm.removeEventListener('submit', deleteFamilyFormSubmitAction);
        deleteFamilyForm.addEventListener('submit', deleteFamilyFormSubmitAction);
    }
}

/**
* Sets up the Rich Text Editor for the todo description field and adds event listeners for image upload success and editor creation.
*/
function setupRichTextEditor(): void {
    const fullScreenOverlay = document.getElementById('full-screen-overlay-div');
    if (fullScreenOverlay !== null) {
        if (fullScreenOverlay.querySelector('script') !== null) {
            eval((fullScreenOverlay.querySelector('script') as HTMLElement).innerHTML);
        }
        const richTextEditor: any = document.getElementById('description-rich-text-editor');
        if (richTextEditor && richTextEditor.ej2_instances) {
            richTextEditor.ej2_instances[0].addEventListener('imageUploadSuccess', onImageUploadSuccess);
            richTextEditor.ej2_instances[0].addEventListener('created', onRichTextEditorCreated);
            richTextEditor.ej2_instances[0].addEventListener('focus', onRichTextEditorFocus);
        }
    }
}

/**
 * Handles the image upload success event for the Rich Text Editor.
 * Updates the file name in the editor after a successful image upload.
 * @param {any} args - The event arguments containing the uploaded file information.
 */
function onImageUploadSuccess(args: any): void {
    if (args.e.currentTarget.getResponseHeader('name') != null) {
        args.file.name = args.e.currentTarget.getResponseHeader('name');
        let filename: any = document.querySelectorAll(".e-file-name")[0];
        filename.innerHTML = args.file.name.replace(document.querySelectorAll(".e-file-type")[0].innerHTML, '');
        filename.title = args.file.name;
    }
}

/**
 * Refreshes the Rich Text Editor UI after it has been created.
 * This is necessary to ensure that the editor is properly initialized and displayed.
 */
function onRichTextEditorCreated(): void {
    setTimeout(function () {
        let rteElement: any = document.getElementById('description-rich-text-editor');
        if (rteElement) {
            if (rteElement.ej2_instances && rteElement.ej2_instances.length > 0) {
                rteElement.ej2_instances[0].refreshUI();
            }
        }
    },
        1000);
}

/**
 * Refreshes the Rich Text Editor UI when it receives focus.
 * This ensures that the editor is properly displayed and ready for user input.
 */
function onRichTextEditorFocus(): void {
    let rteElement: any = document.getElementById('description-rich-text-editor');
    if (rteElement) {
        if (rteElement.ej2_instances && rteElement.ej2_instances.length > 0) {
            rteElement.ej2_instances[0].refreshUI();
        }
    }
}

/**
 * Validates the inputs on the Add/Edit Todo page.
 * Checks if the title is empty, and if the date inputs are valid.
 * Enables or disables the save button based on the validation results.
 */
function validateInputs(): void {
    let isValid = true;
    const saveButton = document.getElementById('save-family-button');
    if (saveButton !== null) {
        const nameInput = document.getElementById('family-name-input') as HTMLInputElement;
        const nameRequiredDiv = document.querySelector<HTMLDivElement>('#name-required-div');
        if (nameInput && nameInput.value.trim() === '') {
            isValid = false;
            if (nameRequiredDiv) {
                nameRequiredDiv.classList.remove('d-none');
            }
        }
        else {
            if (nameRequiredDiv) {
                nameRequiredDiv.classList.add('d-none');
            }
        }

        if (isValid) {
            saveButton.removeAttribute('disabled');
        }
        else {
            saveButton.setAttribute('disabled', 'disabled');
        }
    }
}

function addFamiliesChangedEventListener() {
    // Subscribe to the timelineChanged event to refresh the KanbanBoards list when a KanbanBoard is added, updated, or deleted.
    const familiesIndexFamiliesChangedAction = async (event: TimelineChangedEvent) => {
        let changedItem = event.TimelineItem;
        if (changedItem !== null) {
            if (changedItem.itemType === 101 || changedItem.itemType === 102) { // 101 is the item type for Families, 102 is the item type for FamilyMembers.
                if (changedItem.itemId !== '') {
                    await getFamiliesList();;
                }
            }
        }
    }
    window.removeEventListener('timelineChanged', familiesIndexFamiliesChangedAction);
    window.addEventListener('timelineChanged', familiesIndexFamiliesChangedAction);
}

export async function initializeAddEditFamily(familyId: number): Promise<void> {
    languageId = getCurrentLanguageId();
    setupRichTextEditor();

    const nameInput = document.getElementById('family-name-input') as HTMLInputElement;
    if (nameInput) {
        nameInput.addEventListener('input', validateInputs);
    }

    validateInputs();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

document.addEventListener('DOMContentLoaded', async function () {
    languageId = getCurrentLanguageId();

    addNewFamilyButtonEventListener();
    addFamiliesChangedEventListener();

    await getFamiliesList();
    
    return Promise.resolve();
});