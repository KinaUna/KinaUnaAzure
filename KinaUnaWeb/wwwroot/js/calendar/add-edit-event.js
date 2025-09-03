import * as LocaleHelper from '../localization-v9.js';
import { setContextAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, checkStartBeforeEndTime, getZebraDateTimeFormat, getLongDateTimeFormatMoment } from '../data-tools-v9.js';
import { setupRemindersSection } from '../reminders/reminders.js';
import { setupRecurrenceSection } from './add-edit-recurrence.js';
let zebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment;
let zebraDateTimeFormat;
let repeatUntilZebraDateTimeFormat;
let warningStartIsAfterEndString = 'Warning: Start time is after End time.';
let currentProgenyId;
let startDateTimePickerId = '#event-start-date-time-picker';
let endDateTimePickerId = '#event-end-date-time-picker';
/**
 * Validates that the start date is before the end date.
 * If the start date is after the end date, the submit button is disabled and a warning is shown.
 */
function validateDatePickerStartEnd() {
    const saveItemNotificationDiv = document.querySelector('#save-item-notification');
    const submitEventButton = document.querySelector('#submit-event-button');
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
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-event-zebra-date-time-format-div');
    repeatUntilZebraDateTimeFormat = getZebraDateTimeFormat('#add-event-repeat-until-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    warningStartIsAfterEndString = await LocaleHelper.getTranslation('Warning: Start time is after End time.', 'Sleep', languageId);
    const startDateTimePicker = $(startDateTimePickerId);
    startDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const endDateTimePicker = $(endDateTimePickerId);
    endDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const repeatUntilDateTimePicker = $('#event-repeat-until-date-picker');
    repeatUntilDateTimePicker.Zebra_DatePicker({
        format: repeatUntilZebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const startZebraPicker = document.querySelector(startDateTimePickerId);
    if (startZebraPicker !== null) {
        startZebraPicker.removeEventListener('change', validateDatePickerStartEnd);
        startZebraPicker.addEventListener('change', validateDatePickerStartEnd);
        startZebraPicker.removeEventListener('blur', validateDatePickerStartEnd);
        startZebraPicker.addEventListener('blur', validateDatePickerStartEnd);
        startZebraPicker.removeEventListener('focus', validateDatePickerStartEnd);
        startZebraPicker.addEventListener('focus', validateDatePickerStartEnd);
    }
    const endZebraPicker = document.querySelector(endDateTimePickerId);
    if (endZebraPicker !== null) {
        endZebraPicker.removeEventListener('change', validateDatePickerStartEnd);
        endZebraPicker.addEventListener('change', validateDatePickerStartEnd);
        endZebraPicker.removeEventListener('blur', validateDatePickerStartEnd);
        endZebraPicker.addEventListener('blur', validateDatePickerStartEnd);
        endZebraPicker.removeEventListener('focus', validateDatePickerStartEnd);
        endZebraPicker.addEventListener('focus', validateDatePickerStartEnd);
    }
    validateDatePickerStartEnd();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets up the Progeny select list and adds an event listener to update the context and location auto suggest lists when the selected Progeny changes.
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
    if (progenyIdSelect !== null) {
        currentProgenyId = parseInt(progenyIdSelect.value);
        await setContextAutoSuggestList([currentProgenyId]);
        await setLocationAutoSuggestList([currentProgenyId]);
    }
}
export async function initializeAddEditEvent() {
    currentProgenyId = getCurrentProgenyId();
    languageId = getCurrentLanguageId();
    await setContextAutoSuggestList([currentProgenyId]);
    await setLocationAutoSuggestList([currentProgenyId]);
    setupProgenySelectList();
    setupDateTimePickers();
    setupRemindersSection();
    setupRecurrenceSection();
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-event.js.map