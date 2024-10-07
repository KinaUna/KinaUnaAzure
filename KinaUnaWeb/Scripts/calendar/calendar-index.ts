import * as LocaleHelper from '../localization-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { popupEventItem } from './calendar-details.js';

declare var syncfusionReference: any;
declare var isCurrentUserProgenyAdmin: boolean;

let selectedEventId: number = 0;
let currentCulture = 'en';

/**
 * Retrieves the details of a calendar event and displays them in a popup.
 * @param {number} eventId The id of the event to display.
 */
async function DisplayEventItem(eventId: number): Promise<void> {
    startLoadingItemsSpinner('schedule');

    await popupEventItem(eventId.toString());

    stopLoadingItemsSpinner('schedule');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Event handler for the edit and delete buttons in the Syncfusion Schedule component.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#popupopen
 * @param {any} args The PopupOpenEventArgs provided by the Synfusion scheduler
 */
function onPopupOpen(args: any) {
    args.cancel = true;
}

/**
 * The event handler for clicking an event in the Syncfusion Schedule component.
 * Sets the selectedEventId to the id of the clicked event.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#eventclick
 * @param {any} args The EventClickArgs provided by the Synfusion scheduler
 */
function onEventClick(args: any) {
    let scheduleObj = document.querySelector<any>('.e-schedule').ej2_instances[0];
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
function onCellClick(args:any): void {
    args.cancel = true;
    // Todo: Show add event form
}

/**
 * Event handler for double-clicking a cell in the Syncfusion Schedule component.
 * Currently cancels the default behaviour and does nothing.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#celldoubleclick
 * @param {any} args The CellClicEventkArgs provided by Schedule component.
 */
function onCellDoubleClick(args: any): void {
    args.cancel = true;
}

/**
 * Sets the locale for the Syncfusion Schedule component.
 * Syncfusion documentation: https://ej2.syncfusion.com/documentation/schedule/localization
 */
function setLocale(): void {
    let scheduleInstance = document.querySelector<any>('.e-schedule').ej2_instances[0];
    scheduleInstance.locale = currentCulture;
}

/**
 * Obtains the current culture from the page and loads the corresponding CLDR files for the Syncfusion Schedule component.
 */
async function loadLocale(): Promise<void> {
    const currentCultureDiv = document.querySelector<HTMLDivElement>('#calendar-current-culture-div');
    
    if (currentCultureDiv !== null) {
        const currentCultureData = currentCultureDiv.dataset.currentCulture;
        if (currentCultureData) {
            currentCulture = currentCultureData;
        }
    }

    await LocaleHelper.loadCldrCultureFiles(currentCulture, syncfusionReference);
    return new Promise<void>(function (resolve, reject) {
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
function addScheduleEventListeners(): void {
    let scheduleInstance = document.querySelector<any>('.e-schedule').ej2_instances[0];
    scheduleInstance.addEventListener('cellClick', (args: any) => { onCellClick(args); });
    scheduleInstance.addEventListener('cellDoubleClick', (args: any) => { onCellDoubleClick(args); });
    scheduleInstance.addEventListener('eventClick', (args: any) => { onEventClick(args); });
    scheduleInstance.addEventListener('popupOpen', (args: any) => { onPopupOpen(args); });
}

/**
 * Shows the event details popup when the page is loaded, if the url query string contains an eventId.
 */
function showPopupAtLoad() {
    const popupEventIdDiv = document.querySelector<HTMLDivElement>('#popup-event-id-div');
    if (popupEventIdDiv !== null) {
        if (popupEventIdDiv.dataset.popupEventId) {
            let eventId = parseInt(popupEventIdDiv.dataset.popupEventId);
            if (eventId > 0) {
                if (popupEventIdDiv.dataset.popupEventDateYear && popupEventIdDiv.dataset.popupEventDateMonth && popupEventIdDiv.dataset.popupEventDateDay) {
                    const popupEventYear = parseInt(popupEventIdDiv.dataset.popupEventDateYear);
                    const popupEventMonth = parseInt(popupEventIdDiv.dataset.popupEventDateMonth) -1;
                    const popupEventDay = parseInt(popupEventIdDiv.dataset.popupEventDateDay);
                    let scheduleInstance = document.querySelector<any>('.e-schedule').ej2_instances[0];
                    scheduleInstance.selectedDate = new Date(popupEventYear, popupEventMonth, popupEventDay);
                }
                
                DisplayEventItem(eventId);

            }
        }
    }
}

/**
 * Initializes page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    
    addScheduleEventListeners();
    await loadLocale();
    setLocale();

    showPopupAtLoad();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});