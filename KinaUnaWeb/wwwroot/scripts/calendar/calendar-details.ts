import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v6.js';
export function addCalendarEventListeners(itemId: string): void {
    const eventElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-calendar-event-id="' + itemId + '"]');
    if (eventElementsWithDataId) {
        eventElementsWithDataId.forEach((element) => {
            element.addEventListener('click', function () {
                DisplayEventItem(itemId);
            });
        });
    }
}

async function DisplayEventItem(eventId: string): Promise<void> {
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
            const eventDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (eventDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = eventElementHtml;
                eventDetailsPopupDiv.appendChild(fullScreenOverlay);
                let bodyElement = document.querySelector<HTMLBodyElement>('body');
                if (bodyElement) {
                    bodyElement.style.overflow = 'hidden';
                }
                eventDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            eventDetailsPopupDiv.innerHTML = '';
                            eventDetailsPopupDiv.classList.add('d-none');
                            let bodyElement = document.querySelector<HTMLBodyElement>('body');
                            if (bodyElement) {
                                bodyElement.style.removeProperty('overflow');
                            }
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
    stopLoadingItemsSpinner('body-content');
}