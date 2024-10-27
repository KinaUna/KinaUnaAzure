import * as LocaleHelper from '../localization-v8.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v8.js';
import { setAddItemButtonEventListeners } from '../addItem/add-item.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
let toggleEditButton;
let copyLocationButton;
/**
 * Configures the date time picker for the video date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    if (document.getElementById('video-date-time-picker') !== null) {
        const dateTimePicker = $('#video-date-time-picker');
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
 * Sets up the Progeny select list and adds an event listener to update the tags and locations auto suggest lists when the selected Progeny changes.
 */
function setupProgenySelectList() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', onProgenySelectListChanged);
    }
}
async function onProgenySelectListChanged() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = parseInt(progenyIdSelect.value);
        await setTagsAutoSuggestList([currentProgenyId]);
        await setLocationAutoSuggestList([currentProgenyId]);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets up the Copy Location button and adds an event listener to copy the selected location to the latitude and longitude fields.
 */
function setupCopyLocationButton() {
    copyLocationButton = document.querySelector('#copy-location-button');
    if (copyLocationButton !== null) {
        copyLocationButton.addEventListener('click', onCopyLocationButtonClicked);
    }
}
function onCopyLocationButtonClicked() {
    const latitudeInput = document.getElementById('latitude');
    const longitudeInput = document.getElementById('longitude');
    const locationSelect = document.getElementById('copy-location');
    if (latitudeInput !== null && longitudeInput !== null && locationSelect !== null) {
        let locId = parseInt(locationSelect.value);
        let selectedLocation = copyLocationList.find((obj) => { return obj.id === locId; });
        latitudeInput.setAttribute('value', selectedLocation.lat);
        longitudeInput.setAttribute('value', selectedLocation.lng);
    }
}
/**
 * Sets up the Edit button and adds an event listener to toggle show/hide edit section.
 */
function setupEditButton() {
    toggleEditButton = document.querySelector('#toggle-edit-button');
    if (toggleEditButton !== null) {
        $("#toggle-edit-button").on('click', function () {
            $("#edit-section").toggle(500);
        });
    }
}
export async function initializeAddEditVideo() {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentProgenyId();
    await setupDateTimePicker();
    setupProgenySelectList();
    await setTagsAutoSuggestList([currentProgenyId]);
    await setLocationAutoSuggestList([currentProgenyId]);
    setupCopyLocationButton();
    setupEditButton();
    setAddItemButtonEventListeners();
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-video.js.map