import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v6.js';
export function addCalendarEventListeners(itemId) {
    const eventElementsWithDataId = document.querySelectorAll('[data-calendar-event-id="' + itemId + '"]');
    if (eventElementsWithDataId) {
        eventElementsWithDataId.forEach((element) => {
            element.addEventListener('click', function () {
                DisplayEventItem(itemId);
            });
        });
    }
}
export function popupEventItem(eventId) {
    DisplayEventItem(eventId);
}
async function DisplayEventItem(eventId) {
    startLoadingItemsSpinner('body-content');
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
            }
        }
        else {
            console.error('Error getting event item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting event item. Error: ' + error);
    });
    stopLoadingItemsSpinner('body-content');
}
//# sourceMappingURL=calendar-details.js.map