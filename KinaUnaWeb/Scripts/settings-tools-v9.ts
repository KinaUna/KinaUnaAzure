import { getCurrentProgenyId } from "./data-tools-v9.js";

declare let moment: any;
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
        currentPageSettingsButton.removeEventListener('click', toggleShowPageSettings);
        currentPageSettingsButton.addEventListener('click', toggleShowPageSettings);
    }

    if (closePageSettingsButton !== null) {
        closePageSettingsButton.removeEventListener('click', toggleShowPageSettings);
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
export function getPageSettingsStartDate(momentDateTimeFormat: string, startElement: string = '#settings-start-date-datetimepicker' ): any {
    let settingsStartTime: any = moment($(startElement).val(), momentDateTimeFormat);
    return settingsStartTime;
}

export function getPageSettingsEndDate(momentDateTimeFormat: string, endElement: string = '#settings-end-date-datetimepicker'): any {
    let settingsStartTime: any = moment($(endElement).val(), momentDateTimeFormat);
    return settingsStartTime;
}

export function getSelectedProgenies(): number[] {
    let selectedProgenyIds: string[] = [];
    let selectedProgenies = localStorage.getItem('selectedProgenies');
    if (selectedProgenies !== null) {
        let parsedSelectedProgenies = JSON.parse(selectedProgenies);
        if (parsedSelectedProgenies as string[] !== null) {
            selectedProgenyIds = parsedSelectedProgenies as string[];
        }

        // if progeny with id 0 is in the selectedProgenyIds, remove it.
        if (selectedProgenyIds.includes('0')) {
            selectedProgenyIds = selectedProgenyIds.filter(function (value) {
                return value !== '0';
            });
        }
        

        if (selectedProgenyIds.length === 0) {
            let allProgenyButtons = document.querySelectorAll<HTMLAnchorElement>('.select-progeny-button');
            allProgenyButtons.forEach(function (button) {
                let selectedProgenyData = button.getAttribute('data-select-progeny-id');
                if (selectedProgenyData) {
                    selectedProgenyIds.push(selectedProgenyData);
                    button.classList.add('selected');
                }
            });
            if (selectedProgenyIds.length === 0) {
                selectedProgenyIds = [getCurrentProgenyId().toString()];
            }
            localStorage.setItem('selectedProgenies', JSON.stringify(selectedProgenyIds));
        }
        
        let selectProgenyButtons = document.querySelectorAll('.select-progeny-button');
        selectProgenyButtons.forEach(function (button) {
            let buttonElement = button as HTMLAnchorElement;
            let progenyCheckSpan = buttonElement.querySelector('.progeny-check-span');
            let selectedProgenyData = button.getAttribute('data-select-progeny-id');
            if (selectedProgenyData) {
                if (selectedProgenyIds.includes(selectedProgenyData)) {
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
        selectedProgenyButtons.forEach(function (button) {
            let buttonElement = button as HTMLButtonElement;
            let progenyCheckSpan = buttonElement.querySelector('.progeny-check-span');
            let selectedProgenyData = button.getAttribute('data-select-progeny-id');
            if (selectedProgenyData) {
                selectedProgenyIds.push(selectedProgenyData);
                buttonElement.classList.add('selected');
                if (progenyCheckSpan !== null) {
                    progenyCheckSpan.classList.remove('d-none');
                }
            }
        });

        if (selectedProgenyIds.length === 0) {
            selectedProgenyIds = [getCurrentProgenyId().toString()];
        }
        localStorage.setItem('selectedProgenies', JSON.stringify(selectedProgenyIds));
    }

    
    let progeniesIds = selectedProgenyIds.map(function (id) {
        return parseInt(id);
    });

    if (progeniesIds.length === 1 && progeniesIds[0] === 0) {
        progeniesIds[0] = 2;
    }
    return progeniesIds;
}

export function getSelectedFamilies(): number[] {
    let selectedFamilyIds: string[] = [];
    let selectedFamilies = localStorage.getItem('selectedFamilies');
    if (selectedFamilies !== null) {
        let parsedSelectedFamilies = JSON.parse(selectedFamilies);
        if (parsedSelectedFamilies as string[] !== null) {
            selectedFamilyIds = parsedSelectedFamilies as string[];
        }

        // if family with id 0 is in the selectedProgenyIds, remove it.
        if (selectedFamilyIds.includes('0')) {
            selectedFamilyIds = selectedFamilyIds.filter(function (value) {
                return value !== '0';
            });
        }

        if (selectedFamilyIds.length === 0) {
            let allFamilyButtons = document.querySelectorAll<HTMLAnchorElement>('.select-family-button');
            allFamilyButtons.forEach(function (button) {
                let selectedFamilyData = button.getAttribute('data-select-family-id');
                if (selectedFamilyData) {
                    selectedFamilyIds.push(selectedFamilyData);
                    button.classList.add('selected');
                }
            });
            
            localStorage.setItem('selectedFamilies', JSON.stringify(selectedFamilyIds));
        }

        let selectFamilyButtons = document.querySelectorAll('.select-family-button');
        selectFamilyButtons.forEach(function (button) {
            let buttonElement = button as HTMLAnchorElement;
            let familyCheckSpan = buttonElement.querySelector('.family-check-span');
            let selectedFamilyData = button.getAttribute('data-select-family-id');
            if (selectedFamilyData) {
                if (selectedFamilyIds.includes(selectedFamilyData)) {
                    buttonElement.classList.add('selected');

                    if (familyCheckSpan !== null) {
                        familyCheckSpan.classList.remove('d-none');
                    }
                }
                else {
                    buttonElement.classList.remove('selected');
                    if (familyCheckSpan !== null) {
                        familyCheckSpan.classList.add('d-none');
                    }
                }
            }
        });
    }
    else {
        let selectedFamiliyButtons = document.querySelectorAll<HTMLButtonElement>('.select-family-button');
        selectedFamiliyButtons.forEach(function (button) {
            let buttonElement = button as HTMLButtonElement;
            let familyCheckSpan = buttonElement.querySelector('.family-check-span');
            let selectedFamilyData = button.getAttribute('data-select-family-id');
            if (selectedFamilyData) {
                selectedFamilyIds.push(selectedFamilyData);
                buttonElement.classList.add('selected');
                if (familyCheckSpan !== null) {
                    familyCheckSpan.classList.remove('d-none');
                }
            }
        });
                
        localStorage.setItem('selectedFamilies', JSON.stringify(selectedFamilyIds));
    }


    let familiesIds = selectedFamilyIds.map(function (id) {
        return parseInt(id);
    });
        
    return familiesIds;
}

