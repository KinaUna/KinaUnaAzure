import * as pageModels from 'page-models-v2.js';

let currentPageSettingsDiv = document.querySelector<HTMLDivElement>('#pageSettingsDiv');
let currentPageMainDiv = document.querySelector<HTMLDivElement>('#kinaunaMainDiv');
let currentPageSettingsButton = document.querySelector<HTMLButtonElement>('#page-settings-button');
let closePageSettingsButton = document.querySelector<HTMLButtonElement>('#closePageSettingsButton');
export function toggleShowPageSettings(): void {
    currentPageMainDiv?.classList.toggle('main-show-page-settings')
    currentPageSettingsDiv?.classList.toggle('d-none');
    currentPageSettingsButton?.classList.toggle('d-none');
}

export function initPageSettings(): void {
    currentPageSettingsDiv = document.querySelector<HTMLDivElement>('#pageSettingsDiv');
    currentPageMainDiv = document.querySelector<HTMLDivElement>('#kinaunaMainDiv');
    currentPageSettingsButton = document.querySelector<HTMLButtonElement>('#page-settings-button');
    closePageSettingsButton = document.querySelector<HTMLButtonElement>('#closePageSettingsButton');
    
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

    const selectItemsPerPageElement = document.querySelector<HTMLSelectElement>('#nextItemsCount');
    if (selectItemsPerPageElement !== null) {
        ($(".selectpicker") as any).selectpicker('refresh');
    }
}
export function savePageSettings<Type>(storageKey: string, settingsValue: Type): void {
    localStorage.setItem(storageKey, JSON.stringify(settingsValue));
}

export function getPageSettings<Type>(storageKey: string): Type | null {
    let pageSettingsString = localStorage.getItem(storageKey);
    if (pageSettingsString) {
        let pageSettingsObject: Type = JSON.parse(pageSettingsString);
        return pageSettingsObject;
    }

    return null;
}

export function removePageSettings(storageKey: string): void {
    localStorage.removeItem(storageKey);
}

