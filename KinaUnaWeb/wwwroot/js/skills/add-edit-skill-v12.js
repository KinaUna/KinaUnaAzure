import * as LocaleHelper from '../localization-v12.js';
import { getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getCurrentItemProgenyId } from '../data-tools-v12.js';
import { TimeLineType } from '../page-models-v12.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection-v12.js';
let zebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment;
let zebraDateTimeFormat;
let currentProgenyId;
/**
 * Configures the date time picker for the skill date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    longDateTimeFormatMoment = getLongDateTimeFormatMoment();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-skill-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    const dateTimePicker = $('#skill-date-time-picker');
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
export async function initializeAddEditSkill(itemId) {
    currentProgenyId = getCurrentItemProgenyId();
    languageId = getCurrentLanguageId();
    await setupDateTimePicker();
    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Skill, currentProgenyId, 0);
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-skill-v12.js.map