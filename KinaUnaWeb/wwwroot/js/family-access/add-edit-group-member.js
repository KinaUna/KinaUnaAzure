import { hideBodyScrollbars } from "../item-details/items-display-v9.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9.js";
import { loadPermissionsList } from "./family-access-index.js";
export async function displayAddGroupMemberModal(groupId) {
    const response = await fetch('/FamilyAccess/AddGroupMember/' + groupId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const addGroupMemberContent = await response.text();
        const modalDiv = document.querySelector('#access-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = addGroupMemberContent;
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');
            addAddGroupMemberModalEventListeners();
            return Promise.resolve();
        }
        else {
            return Promise.reject('Modal div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch add group member details:', response.statusText);
        return Promise.reject('Failed to fetch add group member details: ' + response.statusText);
    }
}
function addAddGroupMemberModalEventListeners() {
    const closeButton = document.querySelector('#close-add-group-member-modal-button');
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
    const addGroupMemberForm = document.querySelector('#add-group-member-form');
    if (addGroupMemberForm) {
        const addGroupMemberFormSubmitAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(addGroupMemberForm);
            const response = await fetch('/FamilyAccess/AddGroupMember', {
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
                console.error('Failed to add group member:', response.statusText);
                return Promise.reject('Failed to add group member: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        addGroupMemberForm.removeEventListener('submit', addGroupMemberFormSubmitAction);
        addGroupMemberForm.addEventListener('submit', addGroupMemberFormSubmitAction);
    }
}
export async function displayEditGroupMemberModal(groupMemberId) {
    const response = await fetch('/FamilyAccess/EditGroupMember/' + groupMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const editGroupMemberContent = await response.text();
        const modalDiv = document.querySelector('#access-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = editGroupMemberContent;
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');
            addEditGroupMemberModalEventListeners();
            return Promise.resolve();
        }
        else {
            return Promise.reject('Modal div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch edit group member details:', response.statusText);
        return Promise.reject('Failed to fetch edit group member details: ' + response.statusText);
    }
}
function addEditGroupMemberModalEventListeners() {
    const closeButton = document.querySelector('#close-edit-group-member-modal-button');
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
    const editGroupMemberForm = document.querySelector('#edit-group-member-form');
    if (editGroupMemberForm) {
        const editGroupMemberFormSubmitAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(editGroupMemberForm);
            const response = await fetch('/FamilyAccess/EditGroupMember', {
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
                console.error('Failed to update group member:', response.statusText);
                return Promise.reject('Failed to update group member: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        editGroupMemberForm.removeEventListener('submit', editGroupMemberFormSubmitAction);
        editGroupMemberForm.addEventListener('submit', editGroupMemberFormSubmitAction);
    }
}
export async function displayDeleteGroupMemberModal(groupMemberId) {
    const response = await fetch('/FamilyAccess/DeleteGroupMember/' + groupMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const deleteGroupMemberContent = await response.text();
        const modalDiv = document.querySelector('#access-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = deleteGroupMemberContent;
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');
            addDeleteGroupMemberModalEventListeners();
            return Promise.resolve();
        }
        else {
            return Promise.reject('Modal div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch delete group member details:', response.statusText);
        return Promise.reject('Failed to fetch delete group member details: ' + response.statusText);
    }
}
function addDeleteGroupMemberModalEventListeners() {
    const closeButton = document.querySelector('#close-delete-group-member-modal-button');
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
    const deleteGroupMemberForm = document.querySelector('#delete-group-member-form');
    if (deleteGroupMemberForm) {
        const deleteGroupMemberFormSubmitAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(deleteGroupMemberForm);
            const response = await fetch('/FamilyAccess/DeleteGroupMember', {
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
                console.error('Failed to delete group member:', response.statusText);
                return Promise.reject('Failed to delete group member: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        deleteGroupMemberForm.removeEventListener('submit', deleteGroupMemberFormSubmitAction);
        deleteGroupMemberForm.addEventListener('submit', deleteGroupMemberFormSubmitAction);
    }
}
//# sourceMappingURL=add-edit-group-member.js.map