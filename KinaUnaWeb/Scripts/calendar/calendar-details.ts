import { setupRemindersSection } from '../reminders/reminders.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
import { setEditItemButtonEventListeners } from '../addItem/add-item.js';


/**
 * Adds event listeners to all elements with the data-calendar-event-id attribute.
 * When clicked, the DisplayEventItem function is called.
 * @param {string} itemId The id of the event to add event listeners for.
 */
export function addCalendarEventListeners(itemId: string): void {
    const eventElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-calendar-event-id="' + itemId + '"]');
    if (eventElementsWithDataId) {
        eventElementsWithDataId.forEach((element) => {
            element.addEventListener('click', onCalendarItemDivClicked);
        });
    }
}

async function onCalendarItemDivClicked(event: MouseEvent): Promise<void> {
    const eventElement: HTMLDivElement = event.currentTarget as HTMLDivElement;
    if (eventElement !== null) {
        const eventId = eventElement.dataset.calendarEventId;
        const eventYear = eventElement.dataset.eventYear;
        const eventMonth = eventElement.dataset.eventMonth;
        const eventDay = eventElement.dataset.eventDay;
        
        if (eventId) {
            if (eventYear && eventMonth && eventDay) {
                await displayEventItem(eventId, eventYear, eventMonth, eventDay);
            }
            else {
                await displayEventItem(eventId, '0', '0', '0');
            }
            
        }
    }
}
/**
 * Enable other scripts to call the DisplayEventItem function.
 * @param {string} eventId The id of the event to display.
 */
export async function popupEventItem(eventId: string, eventYear: string, eventMonth: string, eventDay: string): Promise<void> {
    await displayEventItem(eventId, eventYear, eventMonth, eventDay);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Retrieves the details of a calendar event and displays them in a popup.
 * @param {string} eventId The id of the event to display.
 */
async function displayEventItem(eventId: string, eventYear: string, eventMonth: string, eventDay: string): Promise<void> {
    startFullPageSpinner();
    let url = '/Calendar/ViewEvent?eventId=' + eventId + "&partialView=true" + "&year=" + eventYear + "&month=" + eventMonth + "&day=" + eventDay;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const eventElementHtml = await response.text();
            const eventDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (eventDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = eventElementHtml;
                eventDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                eventDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
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
        } else {
            console.error('Error getting event item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting event item. Error: ' + error);
    });
    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}
