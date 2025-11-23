import { getCurrentLanguageId, getZebraDateTimeFormat, setMomentLocale, TimelineChangedEvent } from '../data-tools-v9.js';
import { getTranslation } from '../localization-v9.js';
import { Family, FamilyMember, TimelineItem } from '../page-models-v9.js';
import * as LocaleHelper from '../localization-v9.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v9.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v9.js';

let familiesList = new Array<Family>();
let languageId = 1; // Default to English
let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let zebraDateTimeFormat: string;

export async function GetFamiliesList(): Promise<void> {
    
    const response = await fetch('/Families/FamiliesList', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const data = await response.json() as Family[];
        for (const family of data) {
            familiesList.push(family);
        }

        return Promise.resolve();
    } else {
        console.error('Failed to fetch families list:', response.statusText);
        return Promise.reject('Failed to fetch families list: ' + response.statusText);
    }
}

async function RenderFamilyMembers(family: Family): Promise<void> {
    const familyMembersDiv = document.querySelector<HTMLDivElement>('#family-members-list-div-' + family.familyId);
    if (familyMembersDiv) {
        // Create div element with id 'family-members-{familyId}' to contain family name and its members.
        if (family.familyMembers.length > 0) {
            family.familyMembers.forEach(async (member) => {
                let familyMemberHTML = await getFamilyMemberElement(member);
                familyMembersDiv.appendChild(familyMemberHTML);
                addFamilyMemberElementEventListeners(member.familyMemberId);
            });
        } else {
            let noMembersMessage = getTranslation('No members found for this family.', 'Family', languageId)
            familyMembersDiv.innerHTML += `<p>${noMembersMessage}</p>`;
        }
        
        return Promise.resolve();
    }    
    return Promise.reject('Family members div not found in the document.');
}

async function getFamilyMemberElement(familyMember: FamilyMember): Promise<HTMLDivElement> {
    let memberDivResponse = await fetch('/FamilyMembers/FamilyMemberElement/' + familyMember.familyMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (memberDivResponse.ok) {
        const memberElement = await memberDivResponse.text();
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = memberElement;
        return tempDiv.firstElementChild as HTMLDivElement;
    } else {
        console.error('Failed to fetch family member element:', memberDivResponse.statusText);
        return Promise.reject('Failed to fetch family member element: ' + memberDivResponse.statusText);
    }
}

export function addFamilyMemberElementEventListeners(familyMemberId: number): void {
    const familyMemberElementDiv = document.querySelector<HTMLDivElement>('#family-member-element-' + familyMemberId);
    if (familyMemberElementDiv) {
        const familyMemberElementClickedAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            // Show family member details modal
            await displayFamilyMemberDetails(familyMemberId);
            return Promise.resolve();
        };

        familyMemberElementDiv.removeEventListener('click', familyMemberElementClickedAction);
        familyMemberElementDiv.addEventListener('click', familyMemberElementClickedAction);
    }
}

function addAddFamilyMemberButtonEventListeners() {
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
}

export async function displayFamilyMemberDetails(familyMemberId: number): Promise<void> {
    const response = await fetch('/FamilyMembers/FamilyMemberDetails/' + familyMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    });
    if (response.ok) {
        const familyMemberDetails = await response.text();
        const modalDiv = document.querySelector<HTMLDivElement>('#item-details-div');
        if (modalDiv) {
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.id = 'full-screen-overlay-div';
            fullScreenOverlay.innerHTML = familyMemberDetails;
            modalDiv.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            modalDiv.classList.remove('d-none');

            setFamilyMemberDetailsEventListeners(familyMemberId);
            setFamilyMemberEditItemButtonEventListeners(familyMemberId);
            setFamilyMemberDeleteItemButtonEventListeners(familyMemberId);
            setCopyContentEventListners();

            return Promise.resolve();
        } else {
            return Promise.reject('Modal div not found in the document.');
        }
    } else {
        console.error('Failed to fetch family member details:', response.statusText);
        return Promise.reject('Failed to fetch family member details: ' + response.statusText);
    }
}

function setFamilyMemberEditItemButtonEventListeners(familyMemberId: number) {
    const editButton = document.querySelector<HTMLButtonElement>('#edit-family-member-button-' + familyMemberId);
    if (editButton) {
        const editButtonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            await displayEditFamilyMemberModal(familyMemberId.toString());
        }
        // Clear existing event listeners.
        editButton.removeEventListener('click', editButtonAction);
        editButton.addEventListener('click', editButtonAction);
    }
}

function setFamilyMemberDeleteItemButtonEventListeners(familyMemberId: number) {
    const deleteButton = document.querySelector<HTMLButtonElement>('#delete-family-member-button-' + familyMemberId);
    if (deleteButton) {
        const deleteButtonAction = async function (event: MouseEvent): Promise<void> {
            event.preventDefault();
            await displayDeleteFamilyMemberModal(familyMemberId.toString());
        }
        // Clear existing event listeners.
        deleteButton.removeEventListener('click', deleteButtonAction);
        deleteButton.addEventListener('click', deleteButtonAction);
    }
}
function setFamilyMemberDetailsEventListeners(familyMemberId: number): void {
    let closeButtonsList = document.querySelectorAll('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function (): void {
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

export async function RenderAllFamilies(): Promise<void> {
    const familyDetailsDiv = document.getElementById('family-details-div');
    if (familyDetailsDiv) {
        for (const family of familiesList) {
            clearFamilyMembersDiv(family);
            await RenderFamilyMembers(family);
        }
    }
    
    return Promise.resolve();
}

function clearFamilyMembersDiv(family: Family) {
    const familyMembersDiv = document.querySelector<HTMLDivElement>('#family-members-list-div-' + family.familyId);
    if (familyMembersDiv) {
        familyMembersDiv.innerHTML = '';
    }
}

export async function displayAddFamilyMemberModal(familyId: string): Promise<void> {
    startFullPageSpinner();

    const response = await fetch('/FamilyMembers/AddFamilyMember/' + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }

    });
    if (response.ok) {
        let popup = document.getElementById('item-details-div');
        if (popup) {
            const familyDetailsHTML = await response.text();
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addAddFamilyMemberModalEventListeners();
            await initializeAddEditFamilyMember('0');
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }

    } else {
        console.error('Failed to fetch add family element:', response.statusText);
        return Promise.reject('Failed to fetch add family element: ' + response.statusText);
    }

    stopFullPageSpinner();

    return Promise.resolve();
}

function addAddFamilyMemberModalEventListeners() {
    const closeButtonsList = document.querySelectorAll('.add-family-member-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function (): void {
                let popup = document.getElementById('item-details-div');
                if (!popup) return;
                popup.innerHTML = '';
                popup.classList.add('d-none');
                hideBodyScrollbars();
            };
            // Clear existing event listeners.
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }

    const addFamilyMemberForm = document.querySelector<HTMLFormElement>('#add-family-member-form');
    if (addFamilyMemberForm) {
        const addFamilyMemberFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(addFamilyMemberForm);
            const response = await fetch('/FamilyMembers/AddFamilyMember', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                let popup = document.getElementById('item-details-div');
                if (popup) {
                    popup.innerHTML = '';
                    popup.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    const timelineItem = new TimelineItem();
                    timelineItem.itemType = 102; // FamilyMember type
                    timelineItem.itemId = '0';
                    const timelineItemChangedEvent = new TimelineChangedEvent(timelineItem);
                    window.dispatchEvent(timelineItemChangedEvent);

                    // Refresh the families list on the main page.
                    await RenderAllFamilies();
                }

            } else {
                console.error('Failed to add family member:', response.statusText);
                return Promise.reject('Failed to add family member: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        addFamilyMemberForm.removeEventListener('submit', addFamilyMemberFormSubmitAction);
        addFamilyMemberForm.addEventListener('submit', addFamilyMemberFormSubmitAction);
    }
}

export async function displayEditFamilyMemberModal(familyMemberId: string): Promise<void> {
    startFullPageSpinner();

    const response = await fetch('/FamilyMembers/UpdateFamilyMember/' + familyMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }

    });
    if (response.ok) {
        let popup = document.getElementById('item-details-div');
        if (popup) {
            const familyDetailsHTML = await response.text();
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addEditFamilyMemberModalEventListeners();
            await initializeAddEditFamilyMember(familyMemberId);
        }
        else {
            return Promise.reject('Item details div not found in the document.');
        }

    } else {
        console.error('Failed to fetch edit family element:', response.statusText);
        return Promise.reject('Failed to fetch edit family element: ' + response.statusText);
    }

    stopFullPageSpinner();

    return Promise.resolve();
}

function addEditFamilyMemberModalEventListeners() {
    const closeButtonsList = document.querySelectorAll('.add-family-member-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function (): void {
                let popup = document.getElementById('item-details-div');
                if (!popup) return;
                popup.innerHTML = '';
                popup.classList.add('d-none');
                hideBodyScrollbars();
            };
            // Clear existing event listeners.
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }

    const editFamilyMemberForm = document.querySelector<HTMLFormElement>('#edit-family-member-form');
    if (editFamilyMemberForm) {
        const editFamilyMemberFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(editFamilyMemberForm);
            const response = await fetch('/FamilyMembers/UpdateFamilyMember', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const familyDetailsDiv = document.querySelector<HTMLDivElement>('#family-details-div');
                if (familyDetailsDiv) {
                    familyDetailsDiv.innerHTML = '';
                    familyDetailsDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    const timelineItem = new TimelineItem();
                    timelineItem.itemType = 102; // FamilyMember type
                    timelineItem.itemId = '0';
                    const timelineItemChangedEvent = new TimelineChangedEvent(timelineItem);
                    window.dispatchEvent(timelineItemChangedEvent);
                   
                    // Refresh the families list on the main page.
                    await RenderAllFamilies();
                }

            } else {
                console.error('Failed to update family member:', response.statusText);
                return Promise.reject('Failed to update family member: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        editFamilyMemberForm.removeEventListener('submit', editFamilyMemberFormSubmitAction);
        editFamilyMemberForm.addEventListener('submit', editFamilyMemberFormSubmitAction);
    }
}

export async function displayDeleteFamilyMemberModal(familyMemberId: string): Promise<void> {
    startFullPageSpinner();

    const response = await fetch('/FamilyMembers/DeleteFamilyMember/' + familyMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }

    });
    if (response.ok) {
        let popup = document.getElementById('item-details-div');
        if (popup) {
            const familyDetailsHTML = await response.text();
            popup.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            popup.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            popup.classList.remove('d-none');
            addDeleteFamilyMemberModalEventListeners();
            await initializeAddEditFamilyMember(familyMemberId);
        }
        else {
            return Promise.reject('Family details div not found in the document.');
        }

    } else {
        console.error('Failed to fetch edit family element:', response.statusText);
        return Promise.reject('Failed to fetch edit family element: ' + response.statusText);
    }

    stopFullPageSpinner();

    return Promise.resolve();
}

function addDeleteFamilyMemberModalEventListeners() {
    const closeButtonsList = document.querySelectorAll('.add-family-member-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function (): void {
                let popup = document.getElementById('item-details-div');
                if (!popup) return;
                popup.innerHTML = '';
                popup.classList.add('d-none');
                hideBodyScrollbars();
            };
            // Clear existing event listeners.
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }

    const deleteFamilyMemberForm = document.querySelector<HTMLFormElement>('#delete-family-member-form');
    if (deleteFamilyMemberForm) {
        const deleteFamilyMemberFormSubmitAction = async function (event: Event): Promise<void> {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(deleteFamilyMemberForm);
            const response = await fetch('/FamilyMembers/DeleteFamilyMember', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const familyDetailsDiv = document.getElementById('item-details-div');
                if (familyDetailsDiv) {
                    familyDetailsDiv.innerHTML = '';
                    familyDetailsDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    const timelineItem = new TimelineItem();
                    timelineItem.itemType = 102; // FamilyMember type
                    timelineItem.itemId = '0';
                    const timelineItemChangedEvent = new TimelineChangedEvent(timelineItem);
                    window.dispatchEvent(timelineItemChangedEvent);
                    // Refresh the families list on the main page.
                    await RenderAllFamilies();
                }

            } else {
                stopFullPageSpinner();
                console.error('Failed to delete family member:', response.statusText);
                return Promise.reject('Failed to delete family member: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        deleteFamilyMemberForm.removeEventListener('submit', deleteFamilyMemberFormSubmitAction);
        deleteFamilyMemberForm.addEventListener('submit', deleteFamilyMemberFormSubmitAction);
    }
}

/**
 * Configures the date time picker for the progeny birthday date input field.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-progeny-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(getCurrentLanguageId());

    if (document.getElementById('progeny-date-time-picker') !== null) {
        const dateTimePicker: any = $('#progeny-date-time-picker');
        dateTimePicker.Zebra_DatePicker({
            format: zebraDateTimeFormat,
            open_icon_only: true,
            days: zebraDatePickerTranslations.daysArray,
            months: zebraDatePickerTranslations.monthsArray,
            lang_clear_date: zebraDatePickerTranslations.clearString,
            show_select_today: zebraDatePickerTranslations.todayString,
            select_other_months: true
        });
    }

    ($(".selectpicker") as any).selectpicker('refresh');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function validateInputs(): void {
    const saveButton = document.getElementById('save-family-member-submit-button') as HTMLButtonElement;
    if (!saveButton) return;

    const nameInput = document.getElementById('family-member-name-input') as HTMLInputElement;
    const nickNameInput = document.getElementById('family-member-nick-name-input') as HTMLInputElement;
    const selectProgenySelect = document.getElementById('add-to-family-progeny-select') as HTMLSelectElement;
    let progenySelectedId: string = '0';
    let validInputs: boolean = true;

    const newProgenyDataDiv = document.getElementById('new-progeny-data-div');
    if (newProgenyDataDiv) {
        newProgenyDataDiv.classList.add('d-none');
    }
    const nameRequiredDiv = document.getElementById('name-required-div');
    if (nameRequiredDiv) {
        nameRequiredDiv.classList.add('d-none');
    }
    const nickNameRequiredDiv = document.getElementById('nick-name-required-div');
    if (nickNameRequiredDiv) {
        nickNameRequiredDiv.classList.add('d-none');
    }

    const progenyDetailsInformationDiv = document.getElementById('progeny-details-information-div');
    if (progenyDetailsInformationDiv) {
        progenyDetailsInformationDiv.classList.add('d-none');
    }

    if (selectProgenySelect) {
        progenySelectedId = selectProgenySelect.value;
    }

    if (nameInput && nickNameInput) {
        const nameValid = nameInput.value.trim().length > 0;
        const nickNameValid = nickNameInput.value.trim().length > 0;
        console.log('nameValid: ' + nameValid);
        console.log('nickNameValid: ' + nickNameValid);
        if (progenySelectedId === '0') {
            if (newProgenyDataDiv) {
                newProgenyDataDiv.classList.remove('d-none');
            }

            if (progenyDetailsInformationDiv) {
                progenyDetailsInformationDiv.classList.remove('d-none');
            }

            if (!nameValid) {
                validInputs = false;

                if (nameRequiredDiv) {
                    nameRequiredDiv.classList.remove('d-none');
                }
            }
            if (!nickNameValid) {
                validInputs = false;
                if (nickNameRequiredDiv) {
                    nickNameRequiredDiv.classList.remove('d-none');
                }
            }
        }
    }

    if (validInputs) {
        saveButton.disabled = false;
    } else {
        saveButton.disabled = true;
    }
}

function setCopyContentEventListners() {
    let copyContentButtons = document.querySelectorAll<HTMLButtonElement>('.copy-content-button');
    if (copyContentButtons) {
        copyContentButtons.forEach((button) => {
            button.addEventListener('click', async function () {
                if (!button.dataset.copyContentId) {
                    return;
                }
                let copyContentElement = document.getElementById(button.dataset.copyContentId);
                if (copyContentElement !== null) {
                    let copyText = copyContentElement.textContent;
                    if (copyText === null) {
                        return;
                    }
                    navigator.clipboard.writeText(copyText);
                    // Notify that the text has been copied to the clipboard.
                    let notificationSpan = button.querySelector<HTMLSpanElement>('.toast-notification');
                    if (notificationSpan !== null) {
                        notificationSpan.classList.remove('d-none');
                        setTimeout(function () {
                            notificationSpan.classList.add('d-none');
                        }, 3000);
                    }
                }
            });
        });
    }
}

export async function initializeAddEditFamilyMember(familyMemberId: string): Promise<void> {
    languageId = getCurrentLanguageId();

    const nameInput = document.getElementById('family-member-name-input') as HTMLInputElement;
    if (nameInput) {
        nameInput.removeEventListener('input', validateInputs);
        nameInput.addEventListener('input', validateInputs);
    }

    const nickNameInput = document.getElementById('family-member-nick-name-input') as HTMLInputElement;
    if (nickNameInput) {
        nickNameInput.removeEventListener('input', validateInputs);
        nickNameInput.addEventListener('input', validateInputs);
    }

    const selectProgenySelect = document.getElementById('add-to-family-progeny-select') as HTMLSelectElement;
    if (selectProgenySelect) {
        selectProgenySelect.removeEventListener('change', validateInputs);
        selectProgenySelect.addEventListener('change', validateInputs);
    }

    setupDateTimePicker();

    validateInputs();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    languageId = getCurrentLanguageId();
    addAddFamilyMemberButtonEventListeners();

    await GetFamiliesList();
    await RenderAllFamilies();

    return Promise.resolve();
});