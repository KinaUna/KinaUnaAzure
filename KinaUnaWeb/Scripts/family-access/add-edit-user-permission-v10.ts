import { hideBodyScrollbars } from "../item-details/items-display-v10.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v10.js";
import { loadPermissionsList } from "./family-access-index-v10.js";

export async function displayAddUserPermissionModal(progenyId: string, familyId: string) {
    const response = await fetch('/FamilyAccess/AddPermission/' + progenyId + "/" + familyId, {
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
            addAddUserPermissionModalEventListeners();
            validateInputs();

            return Promise.resolve();
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }
    } else {
        console.error('Failed to fetch add permission details:', response.statusText);
        return Promise.reject('Failed to fetch add permission details: ' + response.statusText);
    }
}

function addAddUserPermissionModalEventListeners(): void {
    const closeButton = document.querySelector<HTMLButtonElement>('#close-add-permission-modal-button');
    if (closeButton) {
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
    }

    const emailInput = document.getElementById('user-permission-email-input') as HTMLInputElement;
    if (emailInput) {
        emailInput.addEventListener('input', validateInputs);
    }

    const addPermissionForm = document.querySelector<HTMLFormElement>('#add-permission-form');
    if (addPermissionForm) {
        const addPermissionFormSubmitAction = async function (event: SubmitEvent): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(addPermissionForm);
            const response = await fetch('/FamilyAccess/AddPermission', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const modalDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (modalDiv) {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the permissions on the main page.
                    await loadPermissionsList();
                }

            } else {
                console.error('Failed to add permission:', response.statusText);
                return Promise.reject('Failed to add permission: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        addPermissionForm.removeEventListener('submit', addPermissionFormSubmitAction);
        addPermissionForm.addEventListener('submit', addPermissionFormSubmitAction);
    }
}

export async function displayEditFamilyPermissionModal(permissionId: string) {
    const response = await fetch('/FamilyAccess/EditFamilyPermission/' + permissionId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const editPermissionContent = await response.text();
        let popup = document.getElementById('item-details-div');
        if (popup) {
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = editPermissionContent;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addEditFamilyPermissionModalEventListeners();
            validateInputs();

            return Promise.resolve();
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }
    } else {
        console.error('Failed to fetch edit family permission details:', response.statusText);
        return Promise.reject('Failed to fetch edit family permission details: ' + response.statusText);
    }
}

function addEditFamilyPermissionModalEventListeners(): void {
    const closeButton = document.querySelector<HTMLButtonElement>('#close-edit-permission-modal-button');
    if (closeButton) {
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
    }

    const emailInput = document.getElementById('user-permission-email-input') as HTMLInputElement;
    if (emailInput) {
        emailInput.addEventListener('input', validateInputs);
    }

    const editPermissionForm = document.querySelector<HTMLFormElement>('#edit-permission-form');
    if (editPermissionForm) {
        const editPermissionFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(editPermissionForm);
            const response = await fetch('/FamilyAccess/EditFamilyPermission', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const modalDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (modalDiv) {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the permissions on the main page.
                    await loadPermissionsList();
                }
            } else {
                console.error('Failed to update family permission:', response.statusText);
                return Promise.reject('Failed to update family permission: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        editPermissionForm.removeEventListener('submit', editPermissionFormSubmitAction);
        editPermissionForm.addEventListener('submit', editPermissionFormSubmitAction);
    }
}

export async function displayDeleteFamilyPermissionModal(permissionId: string) {
    const response = await fetch('/FamilyAccess/DeleteFamilyPermission/' + permissionId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const deletePermissionContent = await response.text();
        let popup = document.getElementById('item-details-div');
        if (popup) {
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = deletePermissionContent;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addDeleteFamilyPermissionModalEventListeners();

            return Promise.resolve();
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }
    } else {
        console.error('Failed to fetch delete family permission details:', response.statusText);
        return Promise.reject('Failed to fetch delete family permission details: ' + response.statusText);
    }
}

function addDeleteFamilyPermissionModalEventListeners(): void {
    const closeButton = document.querySelector<HTMLButtonElement>('#close-delete-permission-modal-button');
    if (closeButton) {
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
    }
    const deletePermissionForm = document.querySelector<HTMLFormElement>('#delete-permission-form');
    if (deletePermissionForm) {
        const deletePermissionFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(deletePermissionForm);
            const response = await fetch('/FamilyAccess/DeleteFamilyPermission', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const modalDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (modalDiv) {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the permissions on the main page.
                    await loadPermissionsList();
                }
            } else {
                console.error('Failed to delete family permission:', response.statusText);
                return Promise.reject('Failed to delete family permission: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        deletePermissionForm.removeEventListener('submit', deletePermissionFormSubmitAction);
        deletePermissionForm.addEventListener('submit', deletePermissionFormSubmitAction);
    }
}

export async function displayEditProgenyPermissionModal(permissionId: string) {
    const response = await fetch('/FamilyAccess/EditProgenyPermission/' + permissionId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const editPermissionContent = await response.text();
        let popup = document.getElementById('item-details-div');
        if (popup) {
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = editPermissionContent;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addEditProgenyPermissionModalEventListeners();
            validateInputs();

            return Promise.resolve();
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }
    } else {
        console.error('Failed to fetch edit progeny permission details:', response.statusText);
        return Promise.reject('Failed to fetch edit progeny permission details: ' + response.statusText);
    }
}

function addEditProgenyPermissionModalEventListeners(): void {
    const closeButton = document.querySelector<HTMLButtonElement>('#close-edit-permission-modal-button');
    if (closeButton) {
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
    }

    const emailInput = document.getElementById('user-permission-email-input') as HTMLInputElement;
    if (emailInput) {
        emailInput.addEventListener('input', validateInputs);
    }

    const editPermissionForm = document.querySelector<HTMLFormElement>('#edit-permission-form');
    if (editPermissionForm) {
        const editPermissionFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(editPermissionForm);
            const response = await fetch('/FamilyAccess/EditProgenyPermission', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const modalDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (modalDiv) {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the permissions on the main page.
                    await loadPermissionsList();
                }
            } else {
                console.error('Failed to update progeny permission:', response.statusText);
                return Promise.reject('Failed to update progeny permission: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        editPermissionForm.removeEventListener('submit', editPermissionFormSubmitAction);
        editPermissionForm.addEventListener('submit', editPermissionFormSubmitAction);
    }
}

export async function displayDeleteProgenyPermissionModal(permissionId: string) {
    const response = await fetch('/FamilyAccess/DeleteProgenyPermission/' + permissionId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const deletePermissionContent = await response.text();
        let popup = document.getElementById('item-details-div');
        if (popup) {
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = deletePermissionContent;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addDeleteProgenyPermissionModalEventListeners();
            
            return Promise.resolve();
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }
    } else {
        console.error('Failed to fetch delete progeny permission details:', response.statusText);
        return Promise.reject('Failed to fetch delete progeny permission details: ' + response.statusText);
    }
}

function addDeleteProgenyPermissionModalEventListeners(): void {
    const closeButton = document.querySelector<HTMLButtonElement>('#close-delete-permission-modal-button');
    if (closeButton) {
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
    }
    const deletePermissionForm = document.querySelector<HTMLFormElement>('#delete-permission-form');
    if (deletePermissionForm) {
        const deletePermissionFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(deletePermissionForm);
            const response = await fetch('/FamilyAccess/DeleteProgenyPermission', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const modalDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (modalDiv) {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the permissions on the main page.
                    await loadPermissionsList();
                }
            } else {
                console.error('Failed to delete progeny permission:', response.statusText);
                return Promise.reject('Failed to delete progeny permission: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        deletePermissionForm.removeEventListener('submit', deletePermissionFormSubmitAction);
        deletePermissionForm.addEventListener('submit', deletePermissionFormSubmitAction);
    }
}

function validateInputs() {
    let isValid = true;
    const saveButton = document.getElementById('save-permission-button');
    if (saveButton !== null) {
        const emailInput = document.getElementById('user-permission-email-input') as HTMLInputElement;
        const emailRequiredDiv = document.querySelector<HTMLDivElement>('#email-required-div');
        if (emailInput && emailInput.value.trim() === '') {
            isValid = false;
            if (emailRequiredDiv) {
                emailRequiredDiv.classList.remove('d-none');
            }
        }
        else {
            if (emailRequiredDiv) {
                emailRequiredDiv.classList.add('d-none');
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