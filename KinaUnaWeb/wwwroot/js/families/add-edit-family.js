import { hideBodyScrollbars } from "../item-details/items-display-v9.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9.js";
import { GetFamiliesList } from "./families-index.js";
export async function displayAddFamilyModal() {
    startFullPageSpinner();
    const response = await fetch('/Families/AddFamily', {
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
            addAddFamilyModalEventListeners();
        }
        else {
            return Promise.reject('Family details div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch add family element:', response.statusText);
        return Promise.reject('Failed to fetch add family element: ' + response.statusText);
    }
    stopFullPageSpinner();
    return Promise.resolve();
}
function addAddFamilyModalEventListeners() {
    const closeButton = document.querySelector('#close-family-modal-button');
    if (closeButton) {
        const closeButtonClickedAction = function (event) {
            event.preventDefault();
            event.stopPropagation();
            const familyDetailsDiv = document.querySelector('#item-details-div');
            if (familyDetailsDiv) {
                familyDetailsDiv.innerHTML = '';
                familyDetailsDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
    }
    const addFamilyForm = document.querySelector('#add-family-form');
    if (addFamilyForm) {
        const addFamilyFormSubmitAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(addFamilyForm);
            const response = await fetch('/Families/AddFamily', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const familyDetailsDiv = document.querySelector('#item-details-div');
                if (familyDetailsDiv) {
                    familyDetailsDiv.innerHTML = '';
                    familyDetailsDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the families list on the main page.
                    await GetFamiliesList();
                }
            }
            else {
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
export async function displayEditFamilyModal(familyId) {
    startFullPageSpinner();
    const response = await fetch('/Families/EditFamily?familyId=' + familyId, {
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
            addEditFamilyModalEventListeners();
        }
        else {
            return Promise.reject('Family details div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch edit family element:', response.statusText);
        return Promise.reject('Failed to fetch edit family element: ' + response.statusText);
    }
    stopFullPageSpinner();
    return Promise.resolve();
}
function addEditFamilyModalEventListeners() {
    const closeButton = document.querySelector('#close-family-modal-button');
    if (closeButton) {
        const closeButtonClickedAction = function (event) {
            event.preventDefault();
            event.stopPropagation();
            const familyDetailsDiv = document.querySelector('#item-details-div');
            if (familyDetailsDiv) {
                familyDetailsDiv.innerHTML = '';
                familyDetailsDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
    }
    const editFamilyForm = document.querySelector('#edit-family-form');
    if (editFamilyForm) {
        const editFamilyFormSubmitAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(editFamilyForm);
            const response = await fetch('/Families/EditFamily', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const familyDetailsDiv = document.querySelector('#item-details-div');
                if (familyDetailsDiv) {
                    familyDetailsDiv.innerHTML = '';
                    familyDetailsDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the families list on the main page.
                    await GetFamiliesList();
                }
            }
            else {
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
//# sourceMappingURL=add-edit-family.js.map