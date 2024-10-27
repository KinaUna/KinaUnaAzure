import { setupRemindersSection } from '../reminders/reminders.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
/**
 * Adds event listeners to all elements with the data-calendar-event-id attribute.
 * When clicked, the DisplayEventItem function is called.
 * @param {string} itemId The id of the event to add event listeners for.
 */
export function addCalendarEventListeners(itemId) {
    const eventElementsWithDataId = document.querySelectorAll('[data-calendar-event-id="' + itemId + '"]');
    if (eventElementsWithDataId) {
        eventElementsWithDataId.forEach((element) => {
            element.addEventListener('click', onCalendarItemDivClicked);
        });
    }
}
async function onCalendarItemDivClicked(event) {
    const eventElement = event.currentTarget;
    if (eventElement !== null) {
        const eventId = eventElement.dataset.calendarEventId;
        if (eventId) {
            await displayEventItem(eventId);
        }
    }
}
/**
 * Enable other scripts to call the DisplayEventItem function.
 * @param {string} eventId The id of the event to display.
 */
export async function popupEventItem(eventId) {
    await displayEventItem(eventId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Retrieves the details of a calendar event and displays them in a popup.
 * @param {string} eventId The id of the event to display.
 */
async function displayEventItem(eventId) {
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
                setEditItemButtonEventListeners();
            }
        }
        else {
            console.error('Error getting event item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting event item. Error: ' + error);
    });
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=calendar-details.js.map