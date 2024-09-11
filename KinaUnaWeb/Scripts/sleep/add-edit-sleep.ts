import * as LocaleHelper from '../localization-v6.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, checkStartBeforeEndTime, getZebraDateTimeFormat, getLongDateTimeFormatMoment } from '../data-tools-v7.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment: string;
let zebraDateTimeFormat: string;
let warningStartIsAfterEndString = 'Warning: Start time is after End time.';
let currentProgenyId: number;
let startDateTimePickerId: string = '#sleep-start-date-time-picker';
let endDateTimePickerId: string = '#sleep-end-date-time-picker';

/**
 * Validates that the start date is before the end date.
 * If the start date is after the end date, the submit button is disabled and a warning is shown.
 */
function validateDatePickerStartEnd() {
    const saveItemNotificationDiv = document.querySelector<HTMLDivElement>('#save-item-notification');
    const submitEventButton = document.querySelector<HTMLButtonElement>('#submit-button');
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
async function setupDateTimePickers(): Promise<void> {
    setMomentLocale();
    longDateTimeFormatMoment = getLongDateTimeFormatMoment();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    warningStartIsAfterEndString = await LocaleHelper.getTranslation('Warning: Start time is after End time.', 'Sleep', languageId);

    const sleepStartDateTimePicker: any = $(startDateTimePickerId);
    sleepStartDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    const sleepEndDateTimePicker: any = $(endDateTimePickerId);
    sleepEndDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    validateDatePickerStartEnd();

    const startZebraPicker = document.querySelector<HTMLInputElement>(startDateTimePickerId);
    if (startZebraPicker !== null) {
        startZebraPicker.addEventListener('change', () => { validateDatePickerStartEnd(); });
        startZebraPicker.addEventListener('blur', () => { validateDatePickerStartEnd(); });
        startZebraPicker.addEventListener('focus', () => { validateDatePickerStartEnd(); });
    }

    const endZebraPicker = document.querySelector<HTMLInputElement>(endDateTimePickerId);
    if (endZebraPicker !== null) {
        endZebraPicker.addEventListener('change', () => { validateDatePickerStartEnd(); });
        endZebraPicker.addEventListener('blur', () => { validateDatePickerStartEnd(); });
        endZebraPicker.addEventListener('focus', () => { validateDatePickerStartEnd(); });
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets up the Progeny select list.
 */
function setupProgenySelectList() {
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
        });
    }
}

document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    currentProgenyId = getCurrentProgenyId();
    languageId = getCurrentLanguageId();

    await setupDateTimePickers();
    setupProgenySelectList();

    return new Promise<void>(function (resolve) {
        resolve();
    });
});