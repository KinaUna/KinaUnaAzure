import * as LocaleHelper from '../localization-v6.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v6.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
let toggleEditButton;
let copyLocationButton;
/**
 * Configures the date time picker for the picture date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    if (document.getElementById('picture-date-time-picker') !== null) {
        const dateTimePicker = $('#picture-date-time-picker');
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets up the Progeny select list and adds an event listener to update the tags and location auto suggest lists when the selected Progeny changes.
 */
function setupProgenySelectList() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setTagsAutoSuggestList(currentProgenyId);
            await setLocationAutoSuggestList(currentProgenyId);
        });
    }
}
/**
 * Adds an event listener to the edit button to toggle the edit section.
 * Shows/hides the edit section when the edit button is clicked.
 */
function addEditButtonEventListener() {
    toggleEditButton = document.querySelector('#toggle-edit-button');
    if (toggleEditButton !== null) {
        $("#toggle-edit-button").on('click', function () {
            $("#edit-section").toggle(500);
        });
    }
}
/**
 * Adds an event listener to the copy location button to copy the selected location to the latitude and longitude fields.
 */
function addCopyLocationButtonEventListener() {
    copyLocationButton = document.querySelector('#copy-location-button');
    if (copyLocationButton !== null) {
        copyLocationButton.addEventListener('click', function () {
            const latitudeInput = document.getElementById('latitude');
            const longitudeInput = document.getElementById('longitude');
            const locationSelect = document.getElementById('copy-location');
            if (latitudeInput !== null && longitudeInput !== null && locationSelect !== null) {
                let locId = parseInt(locationSelect.value);
                let selectedLocation = copyLocationList.find((obj) => { return obj.id === locId; });
                latitudeInput.setAttribute('value', selectedLocation.lat);
                longitudeInput.setAttribute('value', selectedLocation.lng);
            }
        });
    }
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentProgenyId();
    await setupDateTimePicker();
    setupProgenySelectList();
    await setTagsAutoSuggestList(currentProgenyId);
    await setLocationAutoSuggestList(currentProgenyId);
    addEditButtonEventListener();
    addCopyLocationButtonEventListener();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=add-edit-picture.js.map