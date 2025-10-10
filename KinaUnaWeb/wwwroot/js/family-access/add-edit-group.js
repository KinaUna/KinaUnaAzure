import { hideBodyScrollbars } from "../item-details/items-display-v9";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9";
import { loadPermissionsList } from "./family-access-index";
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
        const modalDiv = document.querySelector('#access-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = addGroupContent;
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');
            addAddGroupModalEventListeners();
            return Promise.resolve();
        }
        else {
            return Promise.reject('Modal div not found in the document.');
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
            const modalDiv = document.querySelector('#access-details-div');
            if (modalDiv) {
                modalDiv.innerHTML = '';
                modalDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
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
                const modalDiv = document.querySelector('#access-details-div');
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
        const modalDiv = document.querySelector('#access-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = editGroupContent;
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');
            adEditGroupModalEventListeners();
            return Promise.resolve();
        }
        else {
            return Promise.reject('Modal div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch edit group details:', response.statusText);
        return Promise.reject('Failed to fetch edit group details: ' + response.statusText);
    }
}
function adEditGroupModalEventListeners() {
    const closeButton = document.querySelector('#close-edit-group-modal-button');
    if (closeButton) {
        const closeButtonClickedAction = function (event) {
            event.preventDefault();
            event.stopPropagation();
            const modalDiv = document.querySelector('#access-details-div');
            if (modalDiv) {
                modalDiv.innerHTML = '';
                modalDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
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
                const modalDiv = document.querySelector('#access-details-div');
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
        const modalDiv = document.querySelector('#access-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = deleteGroupContent;
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');
            addDeleteGroupModalEventListeners();
            return Promise.resolve();
        }
        else {
            return Promise.reject('Modal div not found in the document.');
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
            const modalDiv = document.querySelector('#access-details-div');
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
                const modalDiv = document.querySelector('#access-details-div');
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
//# sourceMappingURL=add-edit-group.js.map