import { showPopupAtLoad } from '../item-details/items-display-v8.js';
import * as LocaleHelper from '../localization-v8.js';
import { startFullPageSpinner, startLoadingItemsSpinner, stopFullPageSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { CalendarItemsRequest, TimeLineType } from '../page-models-v8.js';
import { getSelectedProgenies } from '../settings-tools-v8.js';
import { popupEventItem } from './calendar-details.js';
let progeniesList = [];
let selectedEventId = 0;
let currentCulture = 'en';
let calendarRequest = new CalendarItemsRequest();
async function getCalendarItems() {
    startLoadingItemsSpinner('schedule');
    calendarRequest.progenyIds = progeniesList;
    await fetch('/Calendar/GetCalendarList', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(calendarRequest)
    }).then(async function (getCalendarItemsResult) {
        if (getCalendarItemsResult.ok) {
            const calendarItems = (await getCalendarItemsResult.json());
            const scheduleObj = document.querySelector('.e-schedule').ej2_instances[0];
            scheduleObj.eventSettings.dataSource = calendarItems;
        }
    });
    stopLoadingItemsSpinner('schedule');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Retrieves the details of a calendar event and displays them in a popup.
 * @param {number} eventId The id of the event to display.
 */
async function DisplayEventItem(eventId) {
    await popupEventItem(eventId.toString());
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Event handler for the edit and delete buttons in the Syncfusion Schedule component.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#popupopen
 * @param {any} args The PopupOpenEventArgs provided by the Synfusion scheduler
 */
function onPopupOpen(args) {
    args.cancel = true;
}
/**
 * The event handler for clicking an event in the Syncfusion Schedule component.
 * Sets the selectedEventId to the id of the clicked event.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#eventclick
 * @param {any} args The EventClickArgs provided by the Synfusion scheduler
 */
function onEventClick(args) {
    let scheduleObj = document.querySelector('.e-schedule').ej2_instances[0];
    let event = scheduleObj.getEventDetails(args.element);
    selectedEventId = event.eventId;
    DisplayEventItem(selectedEventId);
}
/**
 * The event handler for clicking an empty cell in the Syncfusion Schedule component.
 * Currently cancels the default behaviour and does nothing.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#cellclick
 * @param {any} args  The CellClicEventkArgs provided by the Syncfusion Schedule component.
 */
function onCellClick(args) {
    args.cancel = true;
    // Todo: Show add event form
}
/**
 * Event handler for double-clicking a cell in the Syncfusion Schedule component.
 * Currently cancels the default behaviour and does nothing.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#celldoubleclick
 * @param {any} args The CellClicEventkArgs provided by Schedule component.
 */
function onCellDoubleClick(args) {
    args.cancel = true;
}
/**
 * Sets the locale for the Syncfusion Schedule component.
 * Syncfusion documentation: https://ej2.syncfusion.com/documentation/schedule/localization
 */
function setLocale() {
    let scheduleInstance = document.querySelector('.e-schedule').ej2_instances[0];
    scheduleInstance.locale = currentCulture;
}
/**
 * Obtains the current culture from the page and loads the corresponding CLDR files for the Syncfusion Schedule component.
 */
async function loadLocale() {
    const currentCultureDiv = document.querySelector('#calendar-current-culture-div');
    if (currentCultureDiv !== null) {
        const currentCultureData = currentCultureDiv.dataset.currentCulture;
        if (currentCultureData) {
            currentCulture = currentCultureData;
        }
    }
    await LocaleHelper.loadCldrCultureFiles(currentCulture, syncfusionReference);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Adds event listners for clicking a cell, double-clicking a cell, click an event, and clicking edit or delete.
 * cellClick is the event sent when an empty time cell is clicked. This currently cancels the default behaviour and does nothing.
 * cellDoubleClick cancels the default and currently does nothing.
 * eventClick is the event sent when a calendar item is clicked. Syncfusion Schedule shows the item details and this event handler sets the id of the currently selected item.
 * popupOpen is triggered when the edit or delete buttons are clicked.
 */
function addScheduleEventListeners() {
    let scheduleInstance = document.querySelector('.e-schedule').ej2_instances[0];
    scheduleInstance.addEventListener('cellClick', (args) => { onCellClick(args); });
    scheduleInstance.addEventListener('cellDoubleClick', (args) => { onCellDoubleClick(args); });
    scheduleInstance.addEventListener('eventClick', (args) => { onEventClick(args); });
    scheduleInstance.addEventListener('popupOpen', (args) => { onPopupOpen(args); });
}
function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            progeniesList = getSelectedProgenies();
            await getCalendarItems();
        }
    });
}
function initializeCalendarItemsRequest() {
    calendarRequest = new CalendarItemsRequest();
    calendarRequest.progenyIds = progeniesList;
    // Get current date and time
    let currentDate = new Date();
    // Default start date is 1 month before current date
    let startDate = new Date();
    startDate.setMonth(currentDate.getMonth() - 1);
    calendarRequest.startYear = startDate.getFullYear();
    calendarRequest.startMonth = startDate.getMonth() + 1;
    calendarRequest.startDay = startDate.getDate();
    // Default end date is 1 month after current date
    let endDate = new Date();
    endDate.setMonth(currentDate.getMonth() + 1);
    calendarRequest.endYear = endDate.getFullYear();
    calendarRequest.endMonth = endDate.getMonth() + 1;
    calendarRequest.endDay = endDate.getDate();
}
/**
 * Initializes page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    await showPopupAtLoad(TimeLineType.Calendar);
    addScheduleEventListeners();
    await loadLocale();
    setLocale();
    progeniesList = getSelectedProgenies();
    startFullPageSpinner();
    initializeCalendarItemsRequest();
    await getCalendarItems();
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=calendar-index.js.map