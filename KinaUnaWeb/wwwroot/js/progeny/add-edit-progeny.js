import * as LocaleHelper from '../localization-v8.js';
import { getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v8.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
/**
 * Configures the date time picker for the progeny birthday date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    if (document.getElementById('progeny-date-time-picker') !== null) {
        const dateTimePicker = $('#progeny-date-time-picker');
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
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    languageId = getCurrentLanguageId();
    await setupDateTimePicker();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=add-edit-progeny.js.map