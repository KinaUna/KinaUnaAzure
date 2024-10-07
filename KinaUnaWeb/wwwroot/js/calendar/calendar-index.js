import { showPopupAtLoad } from '../item-details/items-display-v8.js';
import * as LocaleHelper from '../localization-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { TimeLineType } from '../page-models-v8.js';
import { popupEventItem } from './calendar-details.js';
let selectedEventId = 0;
let currentCulture = 'en';
/**
 * Retrieves the details of a calendar event and displays them in a popup.
 * @param {number} eventId The id of the event to display.
 */
async function DisplayEventItem(eventId) {
    startLoadingItemsSpinner('schedule');
    await popupEventItem(eventId.toString());
    stopLoadingItemsSpinner('schedule');
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
    selectedEventId = event.EventId;
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
/**
 * Initializes page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    addScheduleEventListeners();
    await loadLocale();
    setLocale();
    await showPopupAtLoad(TimeLineType.Calendar);
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=calendar-index.js.map