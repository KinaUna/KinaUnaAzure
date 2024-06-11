let currentPageSettingsDiv = document.querySelector('#pageSettingsDiv');
let currentPageMainDiv = document.querySelector('#kinaunaMainDiv');
let currentPageSettingsButton = document.querySelector('#page-settings-button');
let closePageSettingsButton = document.querySelector('#closePageSettingsButton');
export function toggleShowPageSettings() {
    currentPageMainDiv?.classList.toggle('main-show-page-settings');
    currentPageSettingsDiv?.classList.toggle('d-none');
    currentPageSettingsButton?.classList.toggle('d-none');
}
export function initPageSettings() {
    currentPageSettingsDiv = document.querySelector('#pageSettingsDiv');
    currentPageMainDiv = document.querySelector('#kinaunaMainDiv');
    currentPageSettingsButton = document.querySelector('#page-settings-button');
    closePageSettingsButton = document.querySelector('#closePageSettingsButton');
    const pageSettingsContentDiv = document.querySelector('#page-settings-content-div');
    if (pageSettingsContentDiv !== null) {
        currentPageSettingsDiv?.appendChild(pageSettingsContentDiv);
    }
    const showPageSettingsButtonDiv = document.querySelector('#show-page-settings-button-div');
    if (showPageSettingsButtonDiv !== null) {
        currentPageSettingsDiv?.parentElement?.appendChild(showPageSettingsButtonDiv);
    }
    if (currentPageSettingsButton !== null) {
        currentPageSettingsButton.addEventListener('click', toggleShowPageSettings);
    }
    if (closePageSettingsButton !== null) {
        closePageSettingsButton.addEventListener('click', toggleShowPageSettings);
    }
    const selectItemsPerPageElement = document.querySelector('#nextItemsCount');
    if (selectItemsPerPageElement !== null) {
        $(".selectpicker").selectpicker('refresh');
    }
}
export function savePageSettings(storageKey, settingsValue) {
    localStorage.setItem(storageKey, JSON.stringify(settingsValue));
}
export function getPageSettings(storageKey) {
    let pageSettingsString = localStorage.getItem(storageKey);
    if (pageSettingsString) {
        let pageSettingsObject = JSON.parse(pageSettingsString);
        return pageSettingsObject;
    }
    return null;
}
export function removePageSettings(storageKey) {
    localStorage.removeItem(storageKey);
}
//# sourceMappingURL=settings-tools.js.map