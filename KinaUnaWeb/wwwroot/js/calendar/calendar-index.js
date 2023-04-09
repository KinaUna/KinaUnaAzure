import * as LocaleHelper from '../localization.js';
let selectedEventId = 0;
let currentCulture = 'en';
function onPopupOpen(args) {
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
function onEventClick(args) {
    let scheduleObj = document.querySelector('.e-schedule').ej2_instances[0];
    let event = scheduleObj.getEventDetails(args.element);
    selectedEventId = event.EventId;
}
function onCellClick(args) {
    args.cancel = true;
    // Todo: Show add event form
}
function onCellDoubleClick(args) {
    args.cancel = true;
}
function setLocale() {
    let scheduleInstance = document.querySelector('.e-schedule').ej2_instances[0];
    scheduleInstance.locale = currentCulture;
}
async function loadLocale() {
    const currentCultureDiv = document.querySelector('#calendarCurrentCultureDiv');
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
$(async function () {
    let scheduleInstance = document.querySelector('.e-schedule').ej2_instances[0];
    scheduleInstance.addEventListener('cellClick', (args) => { onCellClick(args); });
    scheduleInstance.addEventListener('cellDoubleClick', (args) => { onCellDoubleClick(args); });
    scheduleInstance.addEventListener('eventClick', (args) => { onEventClick(args); });
    scheduleInstance.addEventListener('popupOpen', (args) => { onPopupOpen(args); });
    await loadLocale();
    setLocale();
});
//# sourceMappingURL=calendar-index.js.map