import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v9.js";

export async function displayFamilyMemberDetails(familyMemberId: number): Promise<void> {
    const response = await fetch('/Families/FamilyMemberDetails?familyMemberId=' + familyMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const familyMemberDetails = await response.text();
        const modalDiv = document.querySelector<HTMLDivElement>('#item-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = familyMemberDetails;
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');

            setFamilyMemberDetailsEventListeners(familyMemberId);
            setEditItemButtonEventListeners();
            setDeleteItemButtonEventListeners();

            return Promise.resolve();
        } else {
            return Promise.reject('Modal div not found in the document.');
        }
    } else {
        console.error('Failed to fetch family member details:', response.statusText);
        return Promise.reject('Failed to fetch family member details: ' + response.statusText);
    }
}

function setFamilyMemberDetailsEventListeners(familyMemberId: number): void {
    let closeButtonsList = document.querySelectorAll('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function () {
                const modalDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (!modalDiv) return;

                modalDiv.innerHTML = '';
                modalDiv.classList.add('d-none');
                showBodyScrollbars();
            };
            // Clear existing event listeners.
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }
}