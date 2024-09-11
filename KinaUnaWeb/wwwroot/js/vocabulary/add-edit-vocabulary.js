import * as LocaleHelper from '../localization-v6.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, setVocabularyLanguagesAutoSuggestList } from '../data-tools-v7.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
/**
 * Configures the date time picker for the word date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    const dateTimePicker = $('#vocabulary-date-time-picker');
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
 * Sets up the Progeny select list and adds an event listener to update the progenyId when the selected Progeny changes.
 */
function setupProgenySelectList() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setVocabularyLanguagesAutoSuggestList(currentProgenyId);
        });
    }
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    currentProgenyId = getCurrentProgenyId();
    languageId = getCurrentLanguageId();
    await setupDateTimePicker();
    setupProgenySelectList();
    await setVocabularyLanguagesAutoSuggestList(currentProgenyId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=add-edit-vocabulary.js.map