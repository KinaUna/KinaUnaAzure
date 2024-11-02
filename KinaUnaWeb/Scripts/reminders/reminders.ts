import { dateStringFormatConverter, getCurrentLanguageId, getLongDateTimeFormatMoment, getZebraDateTimeFormat, setMomentLocale } from "../data-tools-v8.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v8.js";
import { CalendarReminderRequest } from "../page-models-v8.js";
import * as LocaleHelper from '../localization-v8.js';

const reminderCustomOffsetDateTimePickerId = '#custom-offset-date-time-picker';

/**
 * Sets up date time picker, select list and event listeners for reminders.
 */
export function setupRemindersSection(): void {
    refreshReminderSelectPicker();
    setupOffSetDateTimePicker();

    // if any elements with data-calendar-reminder-id exists, add event listeners to them.
    const deleteReminderElements = document.querySelectorAll<HTMLButtonElement>('[data-delete-reminder-id]');
    const remindersDiv = document.querySelector<HTMLDivElement>('#reminders-list-div');
    const remindersSectionDiv = document.querySelector<HTMLDivElement>('#reminders-section-div');
    if (deleteReminderElements) {

        if (remindersDiv && remindersSectionDiv) {
            remindersSectionDiv.classList.remove('d-none');
            remindersDiv.classList.remove('d-none');
        }

        deleteReminderElements.forEach((element) => {
            if (element.dataset.deleteReminderId) {
                element.addEventListener('click', function () {
                    deleteReminder(element.dataset.deleteReminderId);
                });
            }
        });
    }
    else {
        remindersDiv?.classList.add('d-none');
        remindersSectionDiv?.classList.add('d-none');
    }

    const addReminderButton = document.querySelector<HTMLButtonElement>('#add-calendar-reminder-button');
    if (addReminderButton !== null) {
        addReminderButton.addEventListener('click', addReminder);

    }
}

/**
 * Refreshes the reminder select picker.
 */
function refreshReminderSelectPicker(): void {
    const reminderOffsetSelectElement = document.querySelector<HTMLSelectElement>('#reminder-offset-select');
    if (reminderOffsetSelectElement !== null) {
        ($(".selectpicker") as any).selectpicker('refresh');
        reminderOffsetSelectElement.addEventListener('change', reminderOffSetChanged);
    }
}

/**
 * Event listener for the reminder offset select list.
 * Shows or hides the custom reminder offset input field based on the selected value.
 */
function reminderOffSetChanged() {
    const reminderOffsetSelectElement = document.querySelector<HTMLSelectElement>('#reminder-offset-select');
    const customReminderOffsetDiv = document.querySelector<HTMLDivElement>('#custom-reminder-offset-div');

    if (reminderOffsetSelectElement?.value === "0") {

        if (customReminderOffsetDiv !== null) {
            customReminderOffsetDiv.classList.remove('d-none');
        }
    }
    else {
        if (customReminderOffsetDiv !== null) {
            customReminderOffsetDiv.classList.add('d-none');
        }
    }
}

/**
 * Sets up the date time picker for the custom reminder input field.
 * Also sets up event listener for the input field to validate that the date/time is before the event starts.
 * @returns
 */
async function setupOffSetDateTimePicker(): Promise<void> {
    setMomentLocale();
    const zebraDateTimeFormat = getZebraDateTimeFormat('#add-reminder-zebra-date-time-format-div');
    const zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(getCurrentLanguageId());

    const startDateTimePicker: any = $(reminderCustomOffsetDateTimePickerId);
    startDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { validateCustomOffsetDatePicker(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    const startZebraPicker = document.querySelector<HTMLInputElement>(reminderCustomOffsetDateTimePickerId);
    if (startZebraPicker !== null) {
        startZebraPicker.addEventListener('change', () => { validateCustomOffsetDatePicker(); });
        startZebraPicker.addEventListener('blur', () => { validateCustomOffsetDatePicker(); });
        startZebraPicker.addEventListener('focus', () => { validateCustomOffsetDatePicker(); });
    }

    ($(".selectpicker") as any).selectpicker('refresh');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Validates the custom reminder offset date picker's value.
 */
function validateCustomOffsetDatePicker() {
    // Todo: check if picker value is before CalenderItem after now.
    const longDateTimeFormatMoment = getLongDateTimeFormatMoment();
    const customDateInput = document.querySelector<HTMLInputElement>('#custom-date-input');
    if (customDateInput === null) {
        return;
    }

    const customTimeZebraPicker = document.querySelector<HTMLInputElement>(reminderCustomOffsetDateTimePickerId);

    let currentDateString = customTimeZebraPicker?.value;
    if (currentDateString) {
        customDateInput.value = dateStringFormatConverter(currentDateString, longDateTimeFormatMoment, 'DD/MM/YYYY HH:mm');
    }
}

/**
 * Adds a reminder to the calendar event for the current user.
 * Gets the values from the reminder offset select list and the custom reminder offset input field.
 */
async function addReminder(): Promise<void> {

    let reminderItem = new CalendarReminderRequest();
    const reminderOffsetSelectElement = document.querySelector<HTMLSelectElement>('#reminder-offset-select');
    if (reminderOffsetSelectElement === null) {
        return;
    }
    console.log('add calendar reminder..1');
    const customReminderOffsetDiv = document.querySelector<HTMLDivElement>('#custom-reminder-offset-div');
    if (customReminderOffsetDiv === null) {
        return;
    }
    console.log('add calendar reminder..2');
    const customReminderOffsetPicker = document.querySelector<HTMLInputElement>(reminderCustomOffsetDateTimePickerId);
    if (customReminderOffsetPicker === null) {
        return;
    }
    console.log('add calendar reminder..3');
    const calendarItemStartReferenceInput = document.querySelector<HTMLInputElement>('#calendar-item-start-reference');
    if (calendarItemStartReferenceInput === null) {
        return
    }
    console.log('add calendar reminder..4');
    const eventIdDiv = document.querySelector<HTMLDivElement>('#event-id-div');
    if (eventIdDiv === null || !eventIdDiv.dataset.eventId) {
        return;
    }
    console.log('add calendar reminder..5');
    const eventId = parseInt(eventIdDiv.dataset.eventId);
    const reminderOffset = reminderOffsetSelectElement.value;

    if (reminderOffset && calendarItemStartReferenceInput !== null) {
        reminderItem.notifyTimeOffsetType = parseInt(reminderOffset);
    }
    if (reminderItem.notifyTimeOffsetType === 0) {
        const customDateInput = document.querySelector<HTMLInputElement>('#custom-date-input');
        if (customDateInput === null) {
            return;
        }
        reminderItem.notifyTimeString = customDateInput.value;
    }

    reminderItem.eventId = eventId;

    await saveReminder(reminderItem);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Saves a reminder for the calendar event for the current user.
 * @param reminder The reminder to save.
 */
async function saveReminder(reminder: CalendarReminderRequest): Promise<void> {
    startFullPageSpinner();

    await fetch('/Calendar/AddReminder', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(reminder)
    }).then(async function (response) {
        if (response.ok) {
            const reminderElementHtml = await response.text();
            const remindersDiv = document.querySelector<HTMLDivElement>('#reminders-list-div');
            const remindersSectionDiv = document.querySelector<HTMLDivElement>('#reminders-section-div');
            if (remindersDiv && remindersSectionDiv) {
                let addedReminderElement = document.createElement('div');
                addedReminderElement.innerHTML = reminderElementHtml;
                remindersDiv.appendChild(addedReminderElement);
                remindersSectionDiv.classList.remove('d-none');
                remindersDiv.classList.remove('d-none');
            }
        }
        else {
            console.error('Error adding reminder. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error adding reminder. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Deletes a reminder for the calendar event for the current user.
 * @param reminderId The id of the reminder to delete.
 */
async function deleteReminder(reminderId: string | undefined) {
    if (!reminderId) {
        return;
    }

    startFullPageSpinner();

    let reminderToDelete = new CalendarReminderRequest();
    reminderToDelete.calendarReminderId = parseInt(reminderId);

    let url = '/Calendar/DeleteReminder';
    await fetch(url, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(reminderToDelete)
    }).then(async function (response) {
        if (response.ok) {
            const reminderElement = document.querySelector<HTMLDivElement>('[data-reminder-id="' + reminderId + '"]');
            if (reminderElement) {
                reminderElement.remove();
            }
        }
        else {
            console.error('Error deleting reminder. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error deleting reminder. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}