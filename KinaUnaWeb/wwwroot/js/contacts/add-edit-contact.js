import * as LocaleHelper from '../localization-v9.js';
import { setTagsAutoSuggestList, setContextAutoSuggestList, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getCurrentItemFamilyId, getCurrentItemProgenyId } from '../data-tools-v9.js';
import { TimelineItem, TimeLineType } from '../page-models-v9.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
let currentFamilyId;
let permissionsEditorTimelineItem = new TimelineItem();
/**
 * Configures the date time picker for the contact date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-contact-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    const dateTimePicker = $('#contact-date-time-picker');
    dateTimePicker.Zebra_DatePicker({
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
        progenyIdSelect.removeEventListener('change', onProgenySelectListChanged);
        progenyIdSelect.addEventListener('change', onProgenySelectListChanged);
    }
}
async function onProgenySelectListChanged() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect === null) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    currentProgenyId = parseInt(progenyIdSelect.value);
    await setTagsAutoSuggestList([currentProgenyId], []);
    await setContextAutoSuggestList([currentProgenyId], []);
    const familyIdSelect = document.querySelector('#item-family-id-select');
    if (familyIdSelect !== null) {
        currentFamilyId = 0;
        familyIdSelect.value = '0';
        // Deselect all items in the selectpicker.
        familyIdSelect.selectedIndex = -1;
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function setupFamilySelectList() {
    const familyIdSelect = document.querySelector('#item-family-id-select');
    if (familyIdSelect !== null) {
        familyIdSelect.removeEventListener('change', onFamilySelectListChanged);
        familyIdSelect.addEventListener('change', onFamilySelectListChanged);
    }
}
async function onFamilySelectListChanged() {
    const familyIdSelect = document.querySelector('#item-family-id-select');
    if (familyIdSelect === null) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    currentFamilyId = parseInt(familyIdSelect.value);
    await setTagsAutoSuggestList([], [currentFamilyId]);
    await setContextAutoSuggestList([], [currentFamilyId]);
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = 0;
        progenyIdSelect.value = '0';
        // Deselect all items in the selectpicker.
        progenyIdSelect.selectedIndex = -1;
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function initializeAddEditContact(itemId) {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentItemProgenyId();
    currentFamilyId = getCurrentItemFamilyId();
    await setupDateTimePicker();
    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Contact, currentProgenyId, currentFamilyId);
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-contact.js.map