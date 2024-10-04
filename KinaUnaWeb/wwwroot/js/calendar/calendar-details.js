import { getCurrentLanguageId, getLongDateTimeFormatMoment, dateStringFormatConverter, getZebraDateTimeFormat, setMomentLocale } from '../data-tools-v8.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
import * as LocaleHelper from '../localization-v8.js';
import { CalendarReminderRequest } from '../page-models-v8.js';
const reminderCustomOffsetDateTimePickerId = '#custom-offset-date-time-picker';
/**
 * Adds event listeners to all elements with the data-calendar-event-id attribute.
 * When clicked, the DisplayEventItem function is called.
 * @param {string} itemId The id of the event to add event listeners for.
 */
export function addCalendarEventListeners(itemId) {
    const eventElementsWithDataId = document.querySelectorAll('[data-calendar-event-id="' + itemId + '"]');
    if (eventElementsWithDataId) {
        eventElementsWithDataId.forEach((element) => {
            element.addEventListener('click', function () {
                DisplayEventItem(itemId);
            });
        });
    }
}
/**
 * Enable other scripts to call the DisplayEventItem function.
 * @param {string} eventId The id of the event to display.
 */
export function popupEventItem(eventId) {
    DisplayEventItem(eventId);
}
async function DisplayEventItem(eventId) {
    startFullPageSpinner();
    let url = '/Calendar/ViewEvent?eventId=' + eventId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const eventElementHtml = await response.text();
            const eventDetailsPopupDiv = document.querySelector('#item-details-div');
            if (eventDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = eventElementHtml;
                eventDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                eventDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            eventDetailsPopupDiv.innerHTML = '';
                            eventDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
                setupRemindersSection();
            }
        }
        else {
            console.error('Error getting event item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting event item. Error: ' + error);
    });
    stopFullPageSpinner();
}
function setupRemindersSection() {
    refreshReminderSelectPicker();
    setupOffSetDateTimePicker();
    // if any elements with data-calendar-reminder-id exists, add event listeners to them.
    const reminderElements = document.querySelectorAll('[data-calendar-reminder-id]');
    const remindersDiv = document.querySelector('#reminders-list-div');
    const remindersSectionDiv = document.querySelector('#reminders-section-div');
    if (reminderElements) {
        if (remindersDiv && remindersSectionDiv) {
            remindersSectionDiv.classList.remove('d-none');
            remindersDiv.classList.remove('d-none');
        }
        reminderElements.forEach((element) => {
            if (element.dataset.calendarReminderId) {
                element.addEventListener('click', function () {
                    deleteReminder(element.dataset.calendarReminderId);
                });
            }
        });
    }
    else {
        remindersDiv?.classList.add('d-none');
        remindersSectionDiv?.classList.add('d-none');
    }
    const addReminderButton = document.querySelector('#add-calendar-reminder-button');
    if (addReminderButton !== null) {
        addReminderButton.addEventListener('click', addReminder);
    }
}
function refreshReminderSelectPicker() {
    const reminderOffsetSelectElement = document.querySelector('#reminder-offset-select');
    if (reminderOffsetSelectElement !== null) {
        $(".selectpicker").selectpicker('refresh');
        reminderOffsetSelectElement.addEventListener('change', reminderOffSetChanged);
    }
}
function reminderOffSetChanged() {
    const reminderOffsetSelectElement = document.querySelector('#reminder-offset-select');
    const customReminderOffsetDiv = document.querySelector('#custom-reminder-offset-div');
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
async function setupOffSetDateTimePicker() {
    setMomentLocale();
    const zebraDateTimeFormat = getZebraDateTimeFormat();
    const zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(getCurrentLanguageId());
    const startDateTimePicker = $(reminderCustomOffsetDateTimePickerId);
    startDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { validateCustomOffsetDatePicker(); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const startZebraPicker = document.querySelector(reminderCustomOffsetDateTimePickerId);
    if (startZebraPicker !== null) {
        startZebraPicker.addEventListener('change', () => { validateCustomOffsetDatePicker(); });
        startZebraPicker.addEventListener('blur', () => { validateCustomOffsetDatePicker(); });
        startZebraPicker.addEventListener('focus', () => { validateCustomOffsetDatePicker(); });
    }
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function validateCustomOffsetDatePicker() {
    // Todo: check if picker value is before CalenderItem after now.
    const longDateTimeFormatMoment = getLongDateTimeFormatMoment();
    const customDateInput = document.querySelector('#custom-date-input');
    if (customDateInput === null) {
        return;
    }
    const customTimeZebraPicker = document.querySelector(reminderCustomOffsetDateTimePickerId);
    let currentDateString = customTimeZebraPicker?.value;
    if (currentDateString) {
        console.log(currentDateString);
        customDateInput.value = dateStringFormatConverter(currentDateString, longDateTimeFormatMoment, 'DD/MM/YYYY HH:mm');
        console.log(customDateInput.value);
    }
}
async function addReminder() {
    let reminderItem = new CalendarReminderRequest();
    const reminderOffsetSelectElement = document.querySelector('#reminder-offset-select');
    if (reminderOffsetSelectElement === null) {
        return;
    }
    console.log('add calendar reminder..1');
    const customReminderOffsetDiv = document.querySelector('#custom-reminder-offset-div');
    if (customReminderOffsetDiv === null) {
        return;
    }
    console.log('add calendar reminder..2');
    const customReminderOffsetPicker = document.querySelector(reminderCustomOffsetDateTimePickerId);
    if (customReminderOffsetPicker === null) {
        return;
    }
    console.log('add calendar reminder..3');
    const calendarItemStartReferenceInput = document.querySelector('#calendar-item-start-reference');
    if (calendarItemStartReferenceInput === null) {
        return;
    }
    console.log('add calendar reminder..4');
    const eventIdDiv = document.querySelector('#event-id-div');
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
        const customDateInput = document.querySelector('#custom-date-input');
        if (customDateInput === null) {
            return;
        }
        reminderItem.notifyTimeString = customDateInput.value;
    }
    reminderItem.eventId = eventId;
    await saveReminder(reminderItem);
}
async function saveReminder(reminder) {
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
            const remindersDiv = document.querySelector('#reminders-list-div');
            const remindersSectionDiv = document.querySelector('#reminders-section-div');
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
}
async function deleteReminder(reminderId) {
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
            const reminderElementHtml = await response.text();
            const reminderElement = document.querySelector('[data-calendar-reminder-id="' + reminderId + '"]');
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
}
//# sourceMappingURL=calendar-details.js.map