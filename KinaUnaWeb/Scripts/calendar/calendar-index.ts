import * as LocaleHelper from '../localization.js';

declare var syncfusionReference: any;
declare var isCurrentUserProgenyAdmin: boolean;

let selectedEventId: number = 0;
let currentCulture = 'en';

function onPopupOpen(args: any) {
    if (args.type === 'Editor' && isCurrentUserProgenyAdmin) {
        args.cancel = true;
        if (selectedEventId > 0) {
            window.location.href = '/Calendar/EditEvent?itemId=' + selectedEventId;
        }
    }
    if (args.type === 'DeleteAlert' && isCurrentUserProgenyAdmin) {
        args.cancel = true;
        if (selectedEventId > 0) {
            window.location.href = '/Calendar/DeleteEvent?itemId=' + selectedEventId;
        }
    }
}

function onEventClick(args: any) {
    let scheduleObj = document.querySelector<any>('.e-schedule').ej2_instances[0];
    let event = scheduleObj.getEventDetails(args.element);
    selectedEventId = event.EventId;
}

function onCellClick(args:any) {
    args.cancel = true;
    // Todo: Show add event form
}

function onCellDoubleClick(args: any) {
    args.cancel = true;
}

function setLocale() {
    let scheduleInstance = document.querySelector<any>('.e-schedule').ej2_instances[0];
    scheduleInstance.locale = currentCulture;
}

async function loadLocale() {
    const currentCultureDiv = document.querySelector<HTMLDivElement>('#calendarCurrentCultureDiv');
    
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

$(async function (): Promise<void> {
    
    let scheduleInstance = document.querySelector<any>('.e-schedule').ej2_instances[0];
    scheduleInstance.addEventListener('cellClick', (args: any) => { onCellClick(args); });
    scheduleInstance.addEventListener('cellDoubleClick', (args: any) => { onCellDoubleClick(args); });
    scheduleInstance.addEventListener('eventClick', (args: any) => { onEventClick(args); });
    scheduleInstance.addEventListener('popupOpen', (args: any) => { onPopupOpen(args); });
    await loadLocale();
    setLocale();

});