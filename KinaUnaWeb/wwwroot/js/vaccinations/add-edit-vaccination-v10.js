import * as LocaleHelper from '../localization-v10.js';
import { getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getCurrentItemProgenyId } from '../data-tools-v10.js';
import { TimeLineType } from '../page-models-v10.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection-v10.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
/**
 * Configures the date time picker for the vaccination date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-vaccination-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    const dateTimePicker = $('#vaccination-date-time-picker');
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
export async function initializeAddEditVaccination(itemId) {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentItemProgenyId();
    await setupDateTimePicker();
    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Vaccination, currentProgenyId, 0);
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-vaccination-v10.js.map