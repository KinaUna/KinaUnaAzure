let languageId: number;
import { getCurrentLanguageId, getZebraDateTimeFormat, setMomentLocale } from "../data-tools-v9.js";
import { hideBodyScrollbars } from "../item-details/items-display-v9.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9.js";
import { RenderAllFamilies } from "./family-members.js";
import * as LocaleHelper from '../localization-v9.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let zebraDateTimeFormat: string;

export async function displayAddFamilyMemberModal(): Promise<void> {
    startFullPageSpinner();

    const response = await fetch('/FamilyMembers/AddFamilyMember', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }

    });
    if (response.ok) {
        const familyDetailsDiv = document.querySelector<HTMLDivElement>('#family-details-div');
        if (familyDetailsDiv) {
            const familyDetailsHTML = await response.text();
            familyDetailsDiv.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            familyDetailsDiv.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            familyDetailsDiv.classList.remove('d-none');
            addAddFamilyMemberModalEventListeners();
            await initializeAddEditFamilyMember('0');
        }
        else {
            return Promise.reject('Family details div not found in the document.');
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
                const familyDetailsDiv = document.querySelector<HTMLDivElement>('#family-details-div');
                if (!familyDetailsDiv) return;
                familyDetailsDiv.innerHTML = '';
                familyDetailsDiv.classList.add('d-none');
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
                const familyDetailsDiv = document.querySelector<HTMLDivElement>('#family-details-div');
                if (familyDetailsDiv) {
                    familyDetailsDiv.innerHTML = '';
                    familyDetailsDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
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

    const response = await fetch('/FamilyMembers/EditFamilyMember/' + familyMemberId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }

    });
    if (response.ok) {
        const familyDetailsDiv = document.querySelector<HTMLDivElement>('#family-details-div');
        if (familyDetailsDiv) {
            const familyDetailsHTML = await response.text();
            familyDetailsDiv.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            familyDetailsDiv.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            familyDetailsDiv.classList.remove('d-none');
            addAddFamilyMemberModalEventListeners();
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

function addEditFamilyMemberModalEventListeners() {
    const closeButtonsList = document.querySelectorAll('.add-family-member-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function (): void {
                const familyDetailsDiv = document.querySelector<HTMLDivElement>('#family-details-div');
                if (!familyDetailsDiv) return;
                familyDetailsDiv.innerHTML = '';
                familyDetailsDiv.classList.add('d-none');
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

export async function initializeAddEditFamilyMember(familyMemberId: string): Promise<void> {
    languageId = getCurrentLanguageId();
        
    const nameInput = document.getElementById('family-member-name-input') as HTMLInputElement;
    if (nameInput) {
        nameInput.removeEventListener('input', validateInputs);
        nameInput.addEventListener('input', validateInputs);
    }

    const nickNameInput = document.getElementById('family-member-nick-name-input') as HTMLInputElement;
    if (nameInput) {
        nameInput.removeEventListener('input', validateInputs);
        nameInput.addEventListener('input', validateInputs);
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