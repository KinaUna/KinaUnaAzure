import * as LocaleHelper from '../localization-v9.js';
import { setTagsAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getCurrentItemFamilyId } from '../data-tools-v9.js';
import { TimelineItem, TimeLineType } from '../page-models-v9.js';
import { renderItemPermissionsEditor } from '../item-permissions.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
let currentFamilyId;
let permissionsEditorTimelineItem = new TimelineItem();
/**
 * Configures the date time picker for the location date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-location-zebra-date-time-format-div');
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
function setupHereMaps() {
    const fullScreenOverlay = document.getElementById('full-screen-overlay-div');
    if (fullScreenOverlay !== null) {
        if (fullScreenOverlay.querySelector('script') !== null) {
            eval(fullScreenOverlay.querySelector('script').innerHTML);
        }
    }
}
export async function initializeAddEditLocation(itemId) {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentProgenyId();
    currentFamilyId = getCurrentItemFamilyId();
    await setupDateTimePicker();
    await setTagsAutoSuggestList([currentProgenyId], []);
    //await setContextAutoSuggestList(currentProgenyId);
    setupProgenySelectList();
    permissionsEditorTimelineItem.itemId = itemId;
    permissionsEditorTimelineItem.itemType = TimeLineType.Location;
    permissionsEditorTimelineItem.progenyId = currentProgenyId;
    permissionsEditorTimelineItem.familyId = currentFamilyId;
    await renderItemPermissionsEditor(permissionsEditorTimelineItem);
    $(".selectpicker").selectpicker('refresh');
    setupHereMaps();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-location.js.map