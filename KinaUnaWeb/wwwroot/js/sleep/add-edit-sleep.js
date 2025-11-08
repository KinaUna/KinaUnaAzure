import * as LocaleHelper from '../localization-v9.js';
import { getCurrentLanguageId, setMomentLocale, checkStartBeforeEndTime, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getCurrentItemProgenyId } from '../data-tools-v9.js';
import { TimeLineType } from '../page-models-v9.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection.js';
let zebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment;
let zebraDateTimeFormat;
let warningStartIsAfterEndString = 'Warning: Start time is after End time.';
let currentProgenyId;
let startDateTimePickerId = '#sleep-start-date-time-picker';
let endDateTimePickerId = '#sleep-end-date-time-picker';
/**
 * Validates that the start date is before the end date.
 * If the start date is after the end date, the submit button is disabled and a warning is shown.
 */
function validateDatePickerStartEnd() {
    const saveItemNotificationDiv = document.querySelector('#save-item-notification');
    const submitEventButton = document.querySelector('#submit-button');
    if (checkStartBeforeEndTime(startDateTimePickerId, endDateTimePickerId, longDateTimeFormatMoment, warningStartIsAfterEndString)) {
        if (saveItemNotificationDiv !== null) {
            saveItemNotificationDiv.textContent = '';
        }
        if (submitEventButton !== null) {
            submitEventButton.disabled = false;
        }
    }
    else {
        if (saveItemNotificationDiv !== null) {
            saveItemNotificationDiv.textContent = warningStartIsAfterEndString;
        }
        if (submitEventButton !== null) {
            submitEventButton.disabled = true;
        }
    }
}
/**
 * Sets up the date time picker for the Start and End input fields.
 * Also sets up event listeners for the input fields to validate that the start date is before the end date.
 * @returns
 */
async function setupDateTimePickers() {
    setMomentLocale();
    longDateTimeFormatMoment = getLongDateTimeFormatMoment();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-sleep-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    warningStartIsAfterEndString = await LocaleHelper.getTranslation('Warning: Start time is after End time.', 'Sleep', languageId);
    const sleepStartDateTimePicker = $(startDateTimePickerId);
    sleepStartDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const sleepEndDateTimePicker = $(endDateTimePickerId);
    sleepEndDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    validateDatePickerStartEnd();
    const startZebraPicker = document.querySelector(startDateTimePickerId);
    if (startZebraPicker !== null) {
        startZebraPicker.addEventListener('change', validateDatePickerStartEnd);
        startZebraPicker.addEventListener('blur', validateDatePickerStartEnd);
        startZebraPicker.addEventListener('focus', validateDatePickerStartEnd);
    }
    const endZebraPicker = document.querySelector(endDateTimePickerId);
    if (endZebraPicker !== null) {
        endZebraPicker.addEventListener('change', validateDatePickerStartEnd);
        endZebraPicker.addEventListener('blur', validateDatePickerStartEnd);
        endZebraPicker.addEventListener('focus', validateDatePickerStartEnd);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function initializeAddEditSleep(itemId) {
    currentProgenyId = getCurrentItemProgenyId();
    languageId = getCurrentLanguageId();
    await setupDateTimePickers();
    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Sleep, currentProgenyId, 0);
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-sleep.js.map