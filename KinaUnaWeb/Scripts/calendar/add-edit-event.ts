import * as LocaleHelper from '../localization-v9.js';
import { setContextAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, checkStartBeforeEndTime, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getCurrentItemFamilyId, getCurrentItemProgenyId } from '../data-tools-v9.js';
import { setupRemindersSection } from '../reminders/reminders.js';
import { setupRecurrenceSection } from './add-edit-recurrence.js';
import { TimelineItem, TimeLineType } from '../page-models-v9.js';
import { renderItemPermissionsEditor } from '../item-permissions.js';
import { setupForIndividualOrFamilyButtons } from '../addItem/setup-for-selection.js';

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let longDateTimeFormatMoment: string;
let zebraDateTimeFormat: string;
let repeatUntilZebraDateTimeFormat: string;
let warningStartIsAfterEndString = 'Warning: Start time is after End time.';
let currentProgenyId: number;
let currentFamilyId: number;
let startDateTimePickerId: string = '#event-start-date-time-picker';
let endDateTimePickerId: string = '#event-end-date-time-picker';

/**
 * Validates that the start date is before the end date.
 * If the start date is after the end date, the submit button is disabled and a warning is shown.
 */
function validateDatePickerStartEnd(): void {
    const saveItemNotificationDiv = document.querySelector<HTMLDivElement>('#save-item-notification');
    const submitEventButton = document.querySelector<HTMLButtonElement>('#submit-event-button');
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
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-event-zebra-date-time-format-div');
    repeatUntilZebraDateTimeFormat = getZebraDateTimeFormat('#add-event-repeat-until-zebra-date-time-format-div');
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

    const repeatUntilDateTimePicker: any = $('#event-repeat-until-date-picker')
    repeatUntilDateTimePicker.Zebra_DatePicker({
        format: repeatUntilZebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { validateDatePickerStartEnd(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    
    const startZebraPicker = document.querySelector<HTMLInputElement>(startDateTimePickerId);
    if (startZebraPicker !== null) {
        startZebraPicker.removeEventListener('change', validateDatePickerStartEnd);
        startZebraPicker.addEventListener('change', validateDatePickerStartEnd);
        startZebraPicker.removeEventListener('blur', validateDatePickerStartEnd);
        startZebraPicker.addEventListener('blur', validateDatePickerStartEnd);
        startZebraPicker.removeEventListener('focus', validateDatePickerStartEnd);
        startZebraPicker.addEventListener('focus', validateDatePickerStartEnd);
    }

    const endZebraPicker = document.querySelector<HTMLInputElement>(endDateTimePickerId);
    if (endZebraPicker !== null) {
        endZebraPicker.removeEventListener('change', validateDatePickerStartEnd);
        endZebraPicker.addEventListener('change', validateDatePickerStartEnd);
        endZebraPicker.removeEventListener('blur', validateDatePickerStartEnd);
        endZebraPicker.addEventListener('blur', validateDatePickerStartEnd);
        endZebraPicker.removeEventListener('focus', validateDatePickerStartEnd);
        endZebraPicker.addEventListener('focus', validateDatePickerStartEnd);
    }

    validateDatePickerStartEnd();
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets up the Progeny select list and adds an event listener to update the context and location auto suggest lists when the selected Progeny changes.
 */
function setupProgenySelectList(): void {
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.removeEventListener('change', onProgenySelectListChanged);
        progenyIdSelect.addEventListener('change', onProgenySelectListChanged);
    }
}

async function onProgenySelectListChanged(): Promise<void> {
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = parseInt(progenyIdSelect.value);
        await setContextAutoSuggestList([currentProgenyId], []);
        await setLocationAutoSuggestList([currentProgenyId], []);
        const familyIdSelect = document.querySelector<HTMLSelectElement>('#item-family-id-select');
        if (familyIdSelect !== null) {
            currentFamilyId = 0;
            familyIdSelect.value = '0';
            // Deselect all items in the selectpicker.
            familyIdSelect.selectedIndex = -1;
        }
    }
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function setupFamilySelectList(): void {
    const familyIdSelect = document.querySelector<HTMLSelectElement>('#item-family-id-select');
    if (familyIdSelect !== null) {
        familyIdSelect.removeEventListener('change', onFamilySelectListChanged);
        familyIdSelect.addEventListener('change', onFamilySelectListChanged);
    }
}

async function onFamilySelectListChanged(): Promise<void> {
    const familyIdSelect = document.querySelector<HTMLSelectElement>('#item-family-id-select');
    if (familyIdSelect === null) {
        return new Promise<void>(function (resolve, reject) {
            resolve();
        });
    }
    currentFamilyId = parseInt(familyIdSelect.value);
    await setContextAutoSuggestList([], [currentFamilyId]);
    await setLocationAutoSuggestList([], [currentFamilyId]);
    const progenyIdSelect = document.querySelector<HTMLSelectElement>('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = 0;
        progenyIdSelect.value = '0';
        // Deselect all items in the selectpicker.
        progenyIdSelect.selectedIndex = -1;
    }
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

export async function initializeAddEditEvent(itemId: string): Promise<void> {
    currentProgenyId = getCurrentItemProgenyId();
    currentFamilyId = getCurrentItemFamilyId();
    languageId = getCurrentLanguageId();

    setupDateTimePickers();
    setupRemindersSection();
    setupRecurrenceSection();
    
    await setupForIndividualOrFamilyButtons(itemId, TimeLineType.Calendar, currentProgenyId, currentFamilyId);

    ($(".selectpicker") as any).selectpicker('refresh');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}