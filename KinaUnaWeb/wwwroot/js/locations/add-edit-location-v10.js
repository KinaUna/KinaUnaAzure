import * as LocaleHelper from '../localization-v10.js';
import { getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getCurrentItemFamilyId, getCurrentItemProgenyId } from '../data-tools-v10.js';
import { TimeLineType } from '../page-models-v10.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection-v10.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
let currentFamilyId;
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
    currentProgenyId = getCurrentItemProgenyId();
    currentFamilyId = getCurrentItemFamilyId();
    await setupDateTimePicker();
    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Location, currentProgenyId, currentFamilyId);
    $(".selectpicker").selectpicker('refresh');
    setupHereMaps();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-location-v10.js.map