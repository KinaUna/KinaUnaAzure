import * as LocaleHelper from '../localization-v6.js';

declare var syncfusionReference: any;
declare var isCurrentUserProgenyAdmin: boolean;

let selectedEventId: number = 0;
let currentCulture = 'en';

async function DisplayEventItem(eventId: number, event: any): Promise<void> {
    let url = '/Calendar/GetEventItem?eventId=' + eventId;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const eventElementHtml = await response.text();
            const eventDetailsPopupDiv = document.querySelector<HTMLDivElement>('#event-details-div');
            if (eventDetailsPopupDiv) {
                eventDetailsPopupDiv.innerHTML = eventElementHtml;
                eventDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            eventDetailsPopupDiv.classList.add('d-none');
                        });
                    });
                }
            }
        } else {
            console.error('Error getting event item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting event item. Error: ' + error);
    });
}

/**
 * Event handler for the edit and delete buttons in the Syncfusion Schedule component.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#popupopen
 * @param args The PopupOpenEventArgs provided by the Synfusion scheduler
 */
function onPopupOpen(args: any) {
    if (args.type === 'Editor' && isCurrentUserProgenyAdmin) {
        args.cancel = true;
        if (selectedEventId > 0) {
            window.location.href = '/Calendar/EditEvent?itemId=' + selectedEventId;
            return;
        }
    }
    if (args.type === 'DeleteAlert' && isCurrentUserProgenyAdmin) {
        args.cancel = true;
        if (selectedEventId > 0) {
            window.location.href = '/Calendar/DeleteEvent?itemId=' + selectedEventId;
            return;
        }
    }

    args.cancel = true;
}

/**
 * The event handler for clicking an event in the Syncfusion Schedule component.
 * Sets the selectedEventId to the id of the clicked event.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#eventclick
 * @param args The EventClickArgs provided by the Synfusion scheduler
 */
function onEventClick(args: any) {
    let scheduleObj = document.querySelector<any>('.e-schedule').ej2_instances[0];
    let event = scheduleObj.getEventDetails(args.element);
    selectedEventId = event.EventId;

    DisplayEventItem(selectedEventId, args.event);
}

/**
 * The event handler for clicking an empty cell in the Syncfusion Schedule component.
 * Currently cancels the default behaviour and does nothing.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#cellclick
 * @param args  The CellClicEventkArgs provided by the Syncfusion Schedule component.
 */
function onCellClick(args:any): void {
    args.cancel = true;
    // Todo: Show add event form
}

/**
 * Event handler for double-clicking a cell in the Syncfusion Schedule component.
 * Currently cancels the default behaviour and does nothing.
 * Syncfusion documentation https://ej2.syncfusion.com/documentation/api/schedule/#celldoubleclick
 * @param args The CellClicEventkArgs provided by Schedule component.
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
 * Initializes page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    
    addScheduleEventListeners();
    await loadLocale();
    setLocale();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});