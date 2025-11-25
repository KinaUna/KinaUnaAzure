import { hideBodyScrollbars } from "../item-details/items-display-v10.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v10.js";
import { loadPermissionsList } from "./family-access-index-v10.js";
let languageId = 1;
export async function displayAddGroupModal(progenyId, familyId) {
    const response = await fetch('/FamilyAccess/AddGroup/' + progenyId + "/" + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const addGroupContent = await response.text();
        let popup = document.getElementById('item-details-div');
        if (popup) {
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = addGroupContent;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addAddGroupModalEventListeners();
            validateInputs();
            $(".selectpicker").selectpicker('refresh');
            return Promise.resolve();
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch add group details:', response.statusText);
        return Promise.reject('Failed to fetch add group details: ' + response.statusText);
    }
}
function addAddGroupModalEventListeners() {
    const closeButton = document.querySelector('#close-add-group-modal-button');
    if (closeButton) {
        const closeButtonClickedAction = function (event) {
            event.preventDefault();
            event.stopPropagation();
            const modalDiv = document.querySelector('#item-details-div');
            if (modalDiv) {
                modalDiv.innerHTML = '';
                modalDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
    }
    const nameInput = document.getElementById('group-name-input');
    if (nameInput) {
        nameInput.addEventListener('input', validateInputs);
    }
    const addGroupForm = document.querySelector('#add-group-form');
    if (addGroupForm) {
        const addGroupFormSubmitAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(addGroupForm);
            const response = await fetch('/FamilyAccess/AddGroup', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const modalDiv = document.querySelector('#item-details-div');
                if (modalDiv) {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the permissions on the main page.
                    await loadPermissionsList();
                }
            }
            else {
                console.error('Failed to add group:', response.statusText);
                return Promise.reject('Failed to add group: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        addGroupForm.removeEventListener('submit', addGroupFormSubmitAction);
        addGroupForm.addEventListener('submit', addGroupFormSubmitAction);
    }
}
export async function displayEditGroupModal(groupId) {
    const response = await fetch('/FamilyAccess/EditGroup/' + groupId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const editGroupContent = await response.text();
        let popup = document.getElementById('item-details-div');
        if (popup) {
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = editGroupContent;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addEditGroupModalEventListeners();
            validateInputs();
            $(".selectpicker").selectpicker('refresh');
            return Promise.resolve();
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch edit group details:', response.statusText);
        return Promise.reject('Failed to fetch edit group details: ' + response.statusText);
    }
}
function addEditGroupModalEventListeners() {
    const closeButton = document.querySelector('#close-edit-group-modal-button');
    if (closeButton) {
        const closeButtonClickedAction = function (event) {
            event.preventDefault();
            event.stopPropagation();
            const modalDiv = document.querySelector('#item-details-div');
            if (modalDiv) {
                modalDiv.innerHTML = '';
                modalDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
    }
    const nameInput = document.getElementById('group-name-input');
    if (nameInput) {
        nameInput.addEventListener('input', validateInputs);
    }
    const editGroupForm = document.querySelector('#edit-group-form');
    if (editGroupForm) {
        const editGroupFormSubmitAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(editGroupForm);
            const response = await fetch('/FamilyAccess/EditGroup', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const modalDiv = document.querySelector('#item-details-div');
                if (modalDiv) {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the permissions on the main page.
                    await loadPermissionsList();
                }
            }
            else {
                console.error('Failed to update group:', response.statusText);
                return Promise.reject('Failed to update group: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        editGroupForm.removeEventListener('submit', editGroupFormSubmitAction);
        editGroupForm.addEventListener('submit', editGroupFormSubmitAction);
    }
}
export async function displayDeleteGroupModal(groupId) {
    const response = await fetch('/FamilyAccess/DeleteGroup/' + groupId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const deleteGroupContent = await response.text();
        let popup = document.getElementById('item-details-div');
        if (popup) {
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = deleteGroupContent;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addDeleteGroupModalEventListeners();
            return Promise.resolve();
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch delete group details:', response.statusText);
        return Promise.reject('Failed to fetch delete group details: ' + response.statusText);
    }
}
function addDeleteGroupModalEventListeners() {
    const closeButton = document.querySelector('#close-delete-group-modal-button');
    if (closeButton) {
        const closeButtonClickedAction = function (event) {
            event.preventDefault();
            event.stopPropagation();
            const modalDiv = document.querySelector('#item-details-div');
            if (modalDiv) {
                modalDiv.innerHTML = '';
                modalDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
    }
    const deleteGroupForm = document.querySelector('#delete-group-form');
    if (deleteGroupForm) {
        const deleteGroupFormSubmitAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(deleteGroupForm);
            const response = await fetch('/FamilyAccess/DeleteGroup', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const modalDiv = document.querySelector('#item-details-div');
                if (modalDiv) {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the permissions on the main page.
                    await loadPermissionsList();
                }
            }
            else {
                console.error('Failed to delete group:', response.statusText);
                return Promise.reject('Failed to delete group: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        deleteGroupForm.removeEventListener('submit', deleteGroupFormSubmitAction);
        deleteGroupForm.addEventListener('submit', deleteGroupFormSubmitAction);
    }
}
function validateInputs() {
    let isValid = true;
    const saveButton = document.getElementById('save-group-button');
    if (saveButton !== null) {
        const nameInput = document.getElementById('group-name-input');
        const nameRequiredDiv = document.querySelector('#name-required-div');
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
//# sourceMappingURL=add-edit-group-v10.js.map