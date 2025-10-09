import { getCurrentLanguageId } from "../data-tools-v9.js";

let languageId = 1; // Default to English

function addAddGroupButtonsEventListeners() {
    const addButtonsList = document.querySelectorAll<HTMLButtonElement>('.add-group-button');
    addButtonsList.forEach((button) => {
        const buttonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const progenyId = (event.currentTarget as HTMLButtonElement).getAttribute('data-progeny-id');
            const familyId = (event.currentTarget as HTMLButtonElement).getAttribute('data-family-id');
            if (progenyId && familyId) {
                // await displayAddGroupMemberModal(progenyId, familyId);
            }
        }
        // Clear existing event listeners.
        button.removeEventListener('click', buttonAction);
        button.addEventListener('click', buttonAction);
    });
}


function addAddGroupMemberButtonsEventListeners() {
    const addButtonsList = document.querySelectorAll<HTMLButtonElement>('.add-group-member-button');
    addButtonsList.forEach((button) => {
        const buttonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const groupId = (event.currentTarget as HTMLButtonElement).getAttribute('data-group-id');
            if (groupId) {
                // await displayAddGroupMemberModal(groupId);
            }
        }
        // Clear existing event listeners.
        button.removeEventListener('click', buttonAction);
        button.addEventListener('click', buttonAction);
    });
}

function addEditUserGroupMemberButtonsEventListeners() {
    const editButtonsList = document.querySelectorAll<HTMLButtonElement>('.edit-user-group-member-button');
    editButtonsList.forEach((button) => {
        const buttonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const groupMemberId = (event.currentTarget as HTMLButtonElement).getAttribute('data-edit-user-group-member-id');
            if (groupMemberId) {
                // await displayEditGroupMemberModal(groupMemberId);
            }
        }
        // Clear existing event listeners.
        button.removeEventListener('click', buttonAction);
        button.addEventListener('click', buttonAction);
    });

}

function addDeleteUserGroupMemberButtonsEventListeners() {
    const deleteButtonsList = document.querySelectorAll<HTMLButtonElement>('.delete-user-group-member-button');
    deleteButtonsList.forEach((button) => {
        const buttonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const groupMemberId = (event.currentTarget as HTMLButtonElement).getAttribute('data-delete-user-group-member-id');
            if (groupMemberId) {
                // await displayDeleteGroupMemberModal(groupMemberId);
            }
        }
        // Clear existing event listeners.
        button.removeEventListener('click', buttonAction);
        button.addEventListener('click', buttonAction);
    });
}

function addEditUserFamilyPermissionButtonsEventListeners() {
    const editButtonsList = document.querySelectorAll<HTMLButtonElement>('.edit-user-family-permission-button');
    editButtonsList.forEach((button) => {
        const buttonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const permissionId = (event.currentTarget as HTMLButtonElement).getAttribute('data-edit-user-family-permission-id');
            if (permissionId) {
                // await displayEditFamilyPermissionsModal(permissionId);
            }
        }
        // Clear existing event listeners.
        button.removeEventListener('click', buttonAction);
        button.addEventListener('click', buttonAction);
    });
}

function addDeleteUserFamilyPermissionButtonsEventListeners() {
    const deleteButtonsList = document.querySelectorAll<HTMLButtonElement>('.delete-user-family-permission-button');
    deleteButtonsList.forEach((button) => {
        const buttonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const permissionId = (event.currentTarget as HTMLButtonElement).getAttribute('data-delete-user-family-permission-id');
            if (permissionId) {
                // await displayDeleteFamilyPermissionsModal(permissionId);
            }
        }
        // Clear existing event listeners.
        button.removeEventListener('click', buttonAction);
        button.addEventListener('click', buttonAction);
    });
}

function addEditUserProgenyPermissionButtonsEventListeners() {
    const editButtonsList = document.querySelectorAll<HTMLButtonElement>('.edit-user-progeny-permission-button');
    editButtonsList.forEach((button) => {
        const buttonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const permissionId = (event.currentTarget as HTMLButtonElement).getAttribute('data-edit-user-progeny-permission-id');
            if (permissionId) {
                // await displayEditProgenyPermissionsModal(permissionId);
            }
        }
        // Clear existing event listeners.
        button.removeEventListener('click', buttonAction);
        button.addEventListener('click', buttonAction);
    });
}

function addDeleteUserProgenyPermissionButtonsEventListeners() {
    const deleteButtonsList = document.querySelectorAll<HTMLButtonElement>('.delete-user-progeny-permission-button');
    deleteButtonsList.forEach((button) => {
        const buttonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            const permissionId = (event.currentTarget as HTMLButtonElement).getAttribute('data-delete-user-progeny-permission-id');
            if (permissionId) {
                // await displayDeleteProgenyPermissionsModal(permissionId);
            }
        }
        // Clear existing event listeners.
        button.removeEventListener('click', buttonAction);
        button.addEventListener('click', buttonAction);
    });
}

document.addEventListener('DOMContentLoaded', async function () {
    languageId = getCurrentLanguageId();

    addAddGroupButtonsEventListeners();
    addAddGroupMemberButtonsEventListeners();
    addEditUserGroupMemberButtonsEventListeners();
    addDeleteUserGroupMemberButtonsEventListeners();
    addEditUserFamilyPermissionButtonsEventListeners();
    addDeleteUserFamilyPermissionButtonsEventListeners();
    addEditUserProgenyPermissionButtonsEventListeners();
    addDeleteUserProgenyPermissionButtonsEventListeners();

    return Promise.resolve();
});