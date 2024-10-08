let currentPageSettingsDiv = document.querySelector('#page-settings-div');
let currentPageMainDiv = document.querySelector('#kinauna-main-div');
let currentPageSettingsButton = document.querySelector('#page-settings-button');
let closePageSettingsButton = document.querySelector('#close-page-settings-button');
/** Initializes the page settings panel, getting references to elements used and setting up event listeners.
 */
export function initPageSettings() {
    currentPageSettingsDiv = document.querySelector('#page-settings-div');
    currentPageMainDiv = document.querySelector('#kinauna-main-div');
    currentPageSettingsButton = document.querySelector('#page-settings-button');
    closePageSettingsButton = document.querySelector('#close-page-settings-button');
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
}
/** Toggles the visibility of the page settings panel and adjusts main page accordingly.
  */
export function toggleShowPageSettings() {
    currentPageMainDiv?.classList.toggle('main-show-page-settings');
    currentPageSettingsDiv?.classList.toggle('d-none');
    currentPageSettingsButton?.classList.toggle('d-none');
}
/** Saves the settings object to local storage.
 * @param Type The type of the settings object.
 * @param storageKey The key used to store the settings in local storage.
 * @param settingsValue The settings object to save.
 */
export function savePageSettings(storageKey, settingsValue) {
    localStorage.setItem(storageKey, JSON.stringify(settingsValue));
}
/** Reads the settings from local storage and returns them as an object of type Type.
 * @param Type The type of the settings object.
 * @param storageKey The key used to store the settings in local storage.
 * @returns The settings object.
 */
export function getPageSettings(storageKey) {
    let pageSettingsString = localStorage.getItem(storageKey);
    if (pageSettingsString) {
        let pageSettingsObject = JSON.parse(pageSettingsString);
        return pageSettingsObject;
    }
    return null;
}
/** Removes the settings object from local storage.
 * @param storageKey The key used to store the settings in local storage.
 */
export function removePageSettings(storageKey) {
    localStorage.removeItem(storageKey);
}
/** Reads the value of the start date-picker and returns it as a Moment object.
 * @param momentDateTimeFormat The Moment format string used for the date-picker.
 * @returns The start date as a Moment object.
 */
export function getPageSettingsStartDate(momentDateTimeFormat) {
    let settingsStartTime = moment($('#settings-start-date-datetimepicker').val(), momentDateTimeFormat);
    return settingsStartTime;
}
//# sourceMappingURL=settings-tools-v8.js.map