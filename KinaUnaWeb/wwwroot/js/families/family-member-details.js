import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v9.js";
import { displayDeleteFamilyMemberModal, displayEditFamilyMemberModal } from "./add-edit-family-member.js";
export async function displayFamilyMemberDetails(familyMemberId) {
    const response = await fetch('/Families/FamilyMemberDetails?familyMemberId=' + familyMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const familyMemberDetails = await response.text();
        const modalDiv = document.querySelector('#item-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = familyMemberDetails;
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');
            setFamilyMemberDetailsEventListeners(familyMemberId);
            setFamilyMemberEditItemButtonEventListeners(familyMemberId);
            setFamilyMemberDeleteItemButtonEventListeners(familyMemberId);
            return Promise.resolve();
        }
        else {
            return Promise.reject('Modal div not found in the document.');
        }
    }
    else {
        console.error('Failed to fetch family member details:', response.statusText);
        return Promise.reject('Failed to fetch family member details: ' + response.statusText);
    }
}
function setFamilyMemberEditItemButtonEventListeners(familyMemberId) {
    const editButton = document.querySelector('#edit-family-member-button-' + familyMemberId);
    if (editButton) {
        const editButtonAction = async function (event) {
            event.preventDefault();
            await displayEditFamilyMemberModal(familyMemberId.toString());
        };
        // Clear existing event listeners.
        editButton.removeEventListener('click', editButtonAction);
        editButton.addEventListener('click', editButtonAction);
    }
}
function setFamilyMemberDeleteItemButtonEventListeners(familyMemberId) {
    const deleteButton = document.querySelector('#delete-family-member-button-' + familyMemberId);
    if (deleteButton) {
        const deleteButtonAction = async function (event) {
            event.preventDefault();
            await displayDeleteFamilyMemberModal(familyMemberId.toString());
        };
        // Clear existing event listeners.
        deleteButton.removeEventListener('click', deleteButtonAction);
        deleteButton.addEventListener('click', deleteButtonAction);
    }
}
function setFamilyMemberDetailsEventListeners(familyMemberId) {
    let closeButtonsList = document.querySelectorAll('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function () {
                const modalDiv = document.querySelector('#item-details-div');
                if (!modalDiv)
                    return;
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
//# sourceMappingURL=family-member-details.js.map