import { getCurrentProgenyId } from '../data-tools-v8.js';
import { showPopupAtLoad } from '../item-details/items-display-v8.js';
import * as LocaleHelper from '../localization-v8.js';
import { startFullPageSpinner, startLoadingItemsSpinner, stopFullPageSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { CalendarItem, TimeLineType, TimelineItem } from '../page-models-v8.js';
import { popupEventItem } from './calendar-details.js';

declare var syncfusionReference: any;
declare var isCurrentUserProgenyAdmin: boolean;

let progeniesList: number[] = [];
let selectedEventId: number = 0;
let currentCulture = 'en';

async function getCalendarItems(): Promise<void> {
    startLoadingItemsSpinner('schedule');

    await fetch('/Calendar/GetCalendarList', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(progeniesList)
    }).then(async function (getCalendarItemsResult) {
        if (getCalendarItemsResult.ok) {
            const calendarItems = (await getCalendarItemsResult.json()) as CalendarItem[];
            const scheduleObj = document.querySelector<any>('.e-schedule').ej2_instances[0];
            scheduleObj.eventSettings.dataSource = calendarItems;
        }
    });

    stopLoadingItemsSpinner('schedule');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });

}
/**
 * Retrieves the details of a calendar event and displays them in a popup.
 * @param {number} eventId The id of the event to display.
 */
async function DisplayEventItem(eventId: number): Promise<void> {
    await popupEventItem(eventId.toString());

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
    selectedEventId = event.eventId;

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

function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            getSelectedProgenies();
            await getCalendarItems();
        }

    });
}

function getSelectedProgenies() {
    let selectedProgenies = localStorage.getItem('selectedProgenies');
    if (selectedProgenies !== null) {
        let selectedProgenyIds: string[] = JSON.parse(selectedProgenies);
        let progeniesIds = selectedProgenyIds.map(function (id) {
            return parseInt(id);
        });
        progeniesList = progeniesIds;
        return;
    }

    progeniesList = [getCurrentProgenyId()];
}

/**
 * Initializes page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    await showPopupAtLoad(TimeLineType.Calendar);

    addScheduleEventListeners();
    await loadLocale();
    setLocale();
    
    getSelectedProgenies();
    startFullPageSpinner();
    await getCalendarItems();
    stopFullPageSpinner();
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});