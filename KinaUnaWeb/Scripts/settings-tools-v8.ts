﻿declare let moment: any;
let currentPageSettingsDiv = document.querySelector<HTMLDivElement>('#page-settings-div');
let currentPageMainDiv = document.querySelector<HTMLDivElement>('#kinauna-main-div');
let currentPageSettingsButton = document.querySelector<HTMLButtonElement>('#page-settings-button');
let closePageSettingsButton = document.querySelector<HTMLButtonElement>('#close-page-settings-button');

/** Initializes the page settings panel, getting references to elements used and setting up event listeners.
 */
export function initPageSettings(): void {
    currentPageSettingsDiv = document.querySelector<HTMLDivElement>('#page-settings-div');
    currentPageMainDiv = document.querySelector<HTMLDivElement>('#kinauna-main-div');
    currentPageSettingsButton = document.querySelector<HTMLButtonElement>('#page-settings-button');
    closePageSettingsButton = document.querySelector<HTMLButtonElement>('#close-page-settings-button');
    
    const pageSettingsContentDiv = document.querySelector<HTMLDivElement>('#page-settings-content-div');
    if (pageSettingsContentDiv !== null) {
        currentPageSettingsDiv?.appendChild(pageSettingsContentDiv);
    }

    const showPageSettingsButtonDiv = document.querySelector<HTMLDivElement>('#show-page-settings-button-div');
    if (showPageSettingsButtonDiv !== null) {
        currentPageSettingsDiv?.parentElement?.appendChild(showPageSettingsButtonDiv);
    }
    
    if (currentPageSettingsButton !== null) {
        currentPageSettingsButton.addEventListener('click', toggleShowPageSettings);
    }

    if (closePageSettingsButton !== null) {
        closePageSettingsButton.addEventListener('click', toggleShowPageSettings);
    }
        
}

/** Toggles the visibility of the page settings panel and adjusts main page accordingly.
  */
export function toggleShowPageSettings(): void {
    currentPageMainDiv?.classList.toggle('main-show-page-settings');
    currentPageSettingsDiv?.classList.toggle('d-none');
    currentPageSettingsButton?.classList.toggle('d-none');
}

/** Saves the settings object to local storage.
 * @param Type The type of the settings object.
 * @param storageKey The key used to store the settings in local storage.
 * @param settingsValue The settings object to save.
 */
export function savePageSettings<Type>(storageKey: string, settingsValue: Type): void {
    localStorage.setItem(storageKey, JSON.stringify(settingsValue));
}

/** Reads the settings from local storage and returns them as an object of type Type.
 * @param Type The type of the settings object.
 * @param storageKey The key used to store the settings in local storage.
 * @returns The settings object.
 */
export function getPageSettings<Type>(storageKey: string): Type | null {
    let pageSettingsString = localStorage.getItem(storageKey);
    if (pageSettingsString) {
        let pageSettingsObject: Type = JSON.parse(pageSettingsString);
        return pageSettingsObject;
    }

    return null;
}

/** Removes the settings object from local storage.
 * @param storageKey The key used to store the settings in local storage.
 */
export function removePageSettings(storageKey: string): void {
    localStorage.removeItem(storageKey);
}

/** Reads the value of the start date-picker and returns it as a Moment object.
 * @param momentDateTimeFormat The Moment format string used for the date-picker.
 * @returns The start date as a Moment object.
 */
export function getPageSettingsStartDate(momentDateTimeFormat: string): any {
    let settingsStartTime: any = moment($('#settings-start-date-datetimepicker').val(), momentDateTimeFormat);
    return settingsStartTime;
}

export function getSelectedProgenies() {
    let selectedProgenies = localStorage.getItem('selectedProgenies');
    if (selectedProgenies !== null) {
        let selectedProgenyIds: string[] = JSON.parse(selectedProgenies);
        let selectProgenyButtons = document.querySelectorAll('.select-progeny-button');
        selectProgenyButtons.forEach(function (button) {
            let buttonElement = button as HTMLButtonElement;
            let progenyCheckSpan = buttonElement.querySelector('.progeny-check-span');
            let selectedProgenyData = button.getAttribute('data-select-progeny-id');
            if (selectedProgenyData) {
                if (selectedProgenyIds.includes(selectedProgenyData.valueOf())) {
                    buttonElement.classList.add('selected');

                    if (progenyCheckSpan !== null) {
                        progenyCheckSpan.classList.remove('d-none');
                    }
                }
                else {
                    buttonElement.classList.remove('selected');
                    if (progenyCheckSpan !== null) {
                        progenyCheckSpan.classList.add('d-none');
                    }
                }
            }

        });
    }

    else {
        let selectedProgenyButtons = document.querySelectorAll<HTMLButtonElement>('.select-progeny-button');
        let selectedProgenyIds: string[] = [];
        selectedProgenyButtons.forEach(function (button) {
            let buttonElement = button as HTMLButtonElement;
            let progenyCheckSpan = buttonElement.querySelector('.progeny-check-span');
            let selectedProgenyData = button.getAttribute('data-select-progeny-id');
            if (selectedProgenyData) {
                selectedProgenyIds.push(selectedProgenyData.valueOf());
                buttonElement.classList.add('selected');
                if (progenyCheckSpan !== null) {
                    progenyCheckSpan.classList.remove('d-none');
                }
            }
        });

        localStorage.setItem('selectedProgenies', JSON.stringify(selectedProgenyIds));

        const selectedProgeniesChangedEvent = new Event('progeniesChanged');
        window.dispatchEvent(selectedProgeniesChangedEvent);
    }
}

