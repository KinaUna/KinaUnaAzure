import { getCurrentProgenyId } from '../data-tools-v8.js';
import { showPopupAtLoad } from '../item-details/items-display-v8.js';
import * as LocaleHelper from '../localization-v8.js';
import { startFullPageSpinner, startLoadingItemsSpinner, stopFullPageSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { CalendarItem, CalendarItemsRequest, TimeLineType, TimelineItem } from '../page-models-v8.js';
import { getSelectedProgenies } from '../settings-tools-v8.js';
import { popupEventItem } from './calendar-details.js';

declare var syncfusionReference: any;
declare var isCurrentUserProgenyAdmin: boolean;

let progeniesList: number[] = [];
let selectedEventId: number = 0;
let currentCulture = 'en';
let calendarRequest: CalendarItemsRequest = new CalendarItemsRequest();
let currentDate = new Date();
let minDate = new Date();
let maxDate = new Date();

async function getCalendarItems(): Promise<void> {
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
async function DisplayEventItem(eventId: number, eventYear: string, eventMonth: string, eventDay: string): Promise<void> {
    await popupEventItem(eventId.toString(), eventYear, eventMonth, eventDay);

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
    console.log(event.startTime);
    let startYear = event.startTime.getFullYear();
    let startMonth = event.startTime.getMonth() + 1;
    let startDay = event.startTime.getDate();

    DisplayEventItem(selectedEventId, startYear, startMonth, startDay);
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

async function onNavigation(args: any): Promise<void> {
    currentDate = args.currentDate;

    // if currentDate is before minDate plus 1 month, or after maxDate minus 1 month, get new calendar items
    let minDatePlusOneMonth = new Date(minDate);
    minDatePlusOneMonth.setMonth(minDatePlusOneMonth.getMonth() + 1);
    let maxDateMinusOneMonth = new Date(maxDate);
    maxDateMinusOneMonth.setMonth(maxDateMinusOneMonth.getMonth() - 1);

    if (currentDate < minDatePlusOneMonth || currentDate > maxDateMinusOneMonth) {
        // Default start date is 2 month before current date
        let startDate = new Date(currentDate);
        startDate.setMonth(currentDate.getMonth() - 2);
        calendarRequest.startYear = startDate.getFullYear();
        calendarRequest.startMonth = startDate.getMonth() + 1;
        calendarRequest.startDay = startDate.getDate();

        // Default end date is 6 months after current date
        let endDate = new Date(currentDate);
        endDate.setMonth(currentDate.getMonth() + 6);
        calendarRequest.endYear = endDate.getFullYear();
        calendarRequest.endMonth = endDate.getMonth() + 1;
        calendarRequest.endDay = endDate.getDate();

        minDate = new Date(startDate);
        maxDate = new Date(endDate);

        await getCalendarItems();
 
    }
    
    

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
    scheduleInstance.addEventListener('navigating', (args: any) => { onNavigation(args); });

    window.addEventListener('calendarDataChanged', async () => {
        await getCalendarItems();
    });
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

function initializeCalendarItemsRequest(): void {
    calendarRequest = new CalendarItemsRequest();
    calendarRequest.progenyIds = progeniesList;

    // Get current date and time
    
    // Default start date is 1 month before current date
    let startDate = new Date();
    startDate.setMonth(currentDate.getMonth() - 2);
    calendarRequest.startYear = startDate.getFullYear();
    calendarRequest.startMonth = startDate.getMonth() + 1;
    calendarRequest.startDay = startDate.getDate();

    // Default end date is 1 month after current date
    let endDate = new Date();
    endDate.setMonth(currentDate.getMonth() + 6);
    calendarRequest.endYear = endDate.getFullYear();
    calendarRequest.endMonth = endDate.getMonth() + 1;
    calendarRequest.endDay = endDate.getDate();

    minDate = new Date(startDate);
    maxDate = new Date(endDate);

}
/**
 * Initializes page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    await showPopupAtLoad(TimeLineType.Calendar);

    addScheduleEventListeners();
    await loadLocale();
    setLocale();
    
    progeniesList = getSelectedProgenies();
    startFullPageSpinner();
    initializeCalendarItemsRequest();
    await getCalendarItems();
    stopFullPageSpinner();
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});