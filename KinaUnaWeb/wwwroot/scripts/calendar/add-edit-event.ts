import * as LocaleHelper from '../localization-v8.js';
import { setContextAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, checkStartBeforeEndTime, getZebraDateTimeFormat, getLongDateTimeFormatMoment } from '../data-tools-v8.js';
import { setupRemindersSection } from '../reminders/reminders.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment: string;
let zebraDateTimeFormat: string;
let warningStartIsAfterEndString = 'Warning: Start time is after End time.';
let currentProgenyId: number;
let startDateTimePickerId: string = '#event-start-date-time-picker';
let endDateTimePickerId: string = '#event-end-date-time-picker';

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
        
        //$('#submit-button').prop('disabled', false);
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
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-event-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    warningStartIsAfterEndString = await LocaleHelper.getTranslation('Warning: Start time is after End time.', 'Sleep', languageId);

    const startDateTimePicker: any = $(startDateTimePickerId);
    startDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    const endDateTimePicker: any = $(endDateTimePickerId);
    endDateTimePicker.Zebra_DatePicker({
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
 * Sets up the Progeny select list and adds an event listener to update the context and location auto suggest lists when the selected Progeny changes.
 */
function setupProgenySelectList() {
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', onProgenySelectListChanged);
    }
}

async function onProgenySelectListChanged() {
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = parseInt(progenyIdSelect.value);
        await setContextAutoSuggestList([currentProgenyId]);
        await setLocationAutoSuggestList([currentProgenyId]);
    }
}

function setupFrequencySelectList() {
    const frequencySelect = document.querySelector<HTMLSelectElement>('#event-repeat-frequency-select');
    if (frequencySelect !== null) {
        frequencySelect.addEventListener('change', onFrequencySelectListChanged);
    }

    const eventEndOptionsSelect = document.querySelector<HTMLSelectElement>('#event-end-option-select');
    if (eventEndOptionsSelect !== null) {
        eventEndOptionsSelect.addEventListener('change', onEventEndOptionsSelectListChanged);
    }
}

function onEventEndOptionsSelectListChanged() {
    updateEventRepeatDetailsDiv();
}

function onFrequencySelectListChanged() {
    const frequencySelect = document.querySelector<HTMLSelectElement>('#event-repeat-frequency-select');
    if (frequencySelect !== null) {
        const frequencyValue = parseInt(frequencySelect.value);
        const eventIntervalInputDiv = document.querySelector<HTMLDivElement>('#event-interval-input-div');
        const eventRepeatDetailsDiv = document.querySelector<HTMLDivElement>('#event-repeat-details-div');
        const eventRepeatUntilDiv = document.querySelector<HTMLDivElement>('#event-repeat-until-div');
        if (frequencyValue === 0) {
            eventIntervalInputDiv?.classList.add('d-none');
            eventRepeatUntilDiv?.classList.add('d-none');
            eventRepeatDetailsDiv?.classList.add('d-none');
            
        } else {
            eventIntervalInputDiv?.classList.remove('d-none');
            eventRepeatUntilDiv?.classList.remove('d-none');
            eventRepeatDetailsDiv?.classList.remove('d-none');

            updateEventRepeatDetailsDiv();
        }

        const eventRepeatDailyDiv = document.querySelector<HTMLDivElement>('#event-repeat-daily-div');
        if (frequencyValue === 1) {
            eventRepeatDailyDiv?.classList.remove('d-none');
        }
        else {
            eventRepeatDailyDiv?.classList.add('d-none');
        }
    }
}

function updateEventRepeatDetailsDiv() {
    const eventEndOptionsSelect = document.querySelector<HTMLSelectElement>('#event-end-option-select');
    if (eventEndOptionsSelect !== null) {
        const eventEndOptionsValue = parseInt(eventEndOptionsSelect.value);
        const eventRepeatUntilDateDiv = document.querySelector<HTMLDivElement>('#event-repeat-until-date-div');
        const eventRepeatUntilCountDiv = document.querySelector<HTMLDivElement>('#event-repeat-until-count-div');
        if (eventEndOptionsValue === 0) {
            eventRepeatUntilDateDiv?.classList.add('d-none');
            eventRepeatUntilCountDiv?.classList.add('d-none');
        }
        else {
            if (eventEndOptionsValue === 1) {
                eventRepeatUntilDateDiv?.classList.remove('d-none');
                eventRepeatUntilCountDiv?.classList.add('d-none');
            }
            else {
                eventRepeatUntilDateDiv?.classList.add('d-none');
                eventRepeatUntilCountDiv?.classList.remove('d-none');
            }
        }
    }
}

export async function initializeAddEditEvent(): Promise<void> {
    currentProgenyId = getCurrentProgenyId();
    languageId = getCurrentLanguageId();

    await setContextAutoSuggestList([currentProgenyId]);
    await setLocationAutoSuggestList([currentProgenyId]);

    setupProgenySelectList();
    setupDateTimePickers();
    setupRemindersSection();
    setupFrequencySelectList();

    ($(".selectpicker") as any).selectpicker('refresh');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}