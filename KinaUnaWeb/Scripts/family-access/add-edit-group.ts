import { hideBodyScrollbars } from "../item-details/items-display-v9";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9";
import { loadPermissionsList } from "./family-access-index";

let languageId = 1;

export async function displayAddGroupModal(progenyId: string, familyId: string) {
    const response = await fetch('/FamilyAccess/AddGroup/' + progenyId + "/" + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const addGroupContent = await response.text();
        const modalDiv = document.querySelector<HTMLDivElement>('#access-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = addGroupContent;
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');
            
            return Promise.resolve();
        } else {
            return Promise.reject('Modal div not found in the document.');
        }
    } else {
        console.error('Failed to fetch add group details:', response.statusText);
        return Promise.reject('Failed to fetch add group details: ' + response.statusText);
    }
}

function addAddGroupModalEventListeners(): void {
    const closeButton = document.querySelector<HTMLButtonElement>('#close-add-group-modal-button');
    if (closeButton) {
        const closeButtonClickedAction = function (event: MouseEvent): void {
            event.preventDefault();
            event.stopPropagation();
            const modalDiv = document.querySelector<HTMLDivElement>('#access-details-div');
            if (modalDiv) {
                modalDiv.innerHTML = '';
                modalDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
    }

    const addGroupForm = document.querySelector<HTMLFormElement>('#add-group-form');
    if (addGroupForm) {
        const addGroupFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(addGroupForm);
            const response = await fetch('/FamilyAccess/AddGroup', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const modalDiv = document.querySelector<HTMLDivElement>('#access-details-div');
                if (modalDiv) {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the permissions on the main page.
                    await loadPermissionsList();
                }

            } else {
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

export async function displayEditGroupModal(groupId: string) {

}

export async function displayDeleteGroupModal(groupId: string) {

}