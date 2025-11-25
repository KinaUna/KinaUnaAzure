import * as LocaleHelper from '../localization-v10.js';
import { getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getCurrentItemFamilyId, getCurrentItemProgenyId } from '../data-tools-v10.js';
import { TimelineItem, TimeLineType } from '../page-models-v11.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection-v10.js';
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
//# sourceMappingURL=add-edit-contact-v10.js.map