import * as LocaleHelper from '../localization-v8.js';
import { setTagsAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v8.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
/**
 * Configures the date time picker for the location date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    const dateTimePicker1 = $('#location-date-time-picker');
    dateTimePicker1.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets up the Progeny select list and adds an event listener to update the context and tags auto suggest lists when the selected Progeny changes.
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
        //await setContextAutoSuggestList(currentProgenyId);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function setupHereMaps() {
    const fullScreenOverlay = document.getElementById('full-screen-overlay-div');
    if (fullScreenOverlay !== null) {
        if (fullScreenOverlay.querySelector('script') !== null) {
            eval(fullScreenOverlay.querySelector('script').innerHTML);
        }
    }
}
export async function initializeAddEditLocation() {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentProgenyId();
    await setupDateTimePicker();
    await setTagsAutoSuggestList([currentProgenyId]);
    //await setContextAutoSuggestList(currentProgenyId);
    setupProgenySelectList();
    $(".selectpicker").selectpicker('refresh');
    setupHereMaps();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-location.js.map