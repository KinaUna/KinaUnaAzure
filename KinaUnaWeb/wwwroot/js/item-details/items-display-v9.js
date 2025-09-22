import { addPictureItemEventListeners, popupPictureDetails } from '../pictures/picture-details.js';
import { addCalendarEventListeners, popupEventItem } from '../calendar/calendar-details.js';
import { addVideoItemEventListeners, popupVideoDetails } from '../videos/video-details.js';
import { addNoteEventListeners, popupNoteItem } from '../notes/note-details.js';
import { addSleepEventListeners, popupSleepItem } from '../sleep/sleep-details.js';
import { addFriendItemListeners, popupFriendItem } from '../friends/friend-details.js';
import { addContactItemListeners, popupContactItem } from '../contacts/contact-details.js';
import { addLocationItemListeners, popupLocationItem } from '../locations/location-details.js';
import { addMeasurementItemListeners, popupMeasurementItem } from '../measurements/measurement-details.js';
import { addSkillItemListeners, popupSkillItem } from '../skills/skill-details.js';
import { addVocabularyItemListeners, popupVocabularyItem } from '../vocabulary/vocabulary-details.js';
import { addVaccinationItemListeners, popupVaccinationItem } from '../vaccinations/vaccination-details.js';
import { addTodoItemListeners, popupTodoItem } from '../todos/todo-details.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v9.js';
import { addKanbanBoardListeners, popupKanbanBoard } from '../kanbans/kanban-board-details.js';
/**
 * Adds event listeners for a given timeline item. Used to show popups for items.
 * @param {TimelineItem} item The timeline item to add event listeners for.
 */
export async function addTimelineItemEventListener(item) {
    if (item.itemType === 1) {
        await addPictureItemEventListeners(item.itemId);
    }
    if (item.itemType === 2) {
        addVideoItemEventListeners(item.itemId);
    }
    if (item.itemType === 3) {
        addCalendarEventListeners(item.itemId);
    }
    if (item.itemType === 4) {
        addVocabularyItemListeners(item.itemId);
    }
    if (item.itemType === 5) {
        addSkillItemListeners(item.itemId);
    }
    if (item.itemType === 6) {
        addFriendItemListeners(item.itemId);
    }
    if (item.itemType === 7) {
        addMeasurementItemListeners(item.itemId);
    }
    if (item.itemType === 8) {
        addSleepEventListeners(item.itemId);
    }
    if (item.itemType === 9) {
        addNoteEventListeners(item.itemId);
    }
    if (item.itemType === 10) {
        addContactItemListeners(item.itemId);
    }
    if (item.itemType === 11) {
        addVaccinationItemListeners(item.itemId);
    }
    if (item.itemType === 12) {
        addLocationItemListeners(item.itemId);
    }
    if (item.itemType === 15) {
        addTodoItemListeners(item.itemId);
    }
    if (item.itemType === 16) {
        addKanbanBoardListeners(item.itemId);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Hides scrollbars on the body element, to prevent scrolling while a popup is displayed.
 */
export function hideBodyScrollbars() {
    let bodyElement = document.querySelector('body');
    if (bodyElement) {
        bodyElement.style.overflow = 'hidden';
    }
}
/**
 * Shows scrollbars on the body element, to allow scrolling when a popup is closed.
 */
export function showBodyScrollbars() {
    let bodyElement = document.querySelector('body');
    if (bodyElement) {
        bodyElement.style.removeProperty('overflow');
    }
}
/**
 * Retrieves the item ID from the popup div based on the item type string.
 * @param {string} itemTypeString The string representing the item type (e.g., 'picture', 'video').
 * @returns {number} The item ID, or 0 if not found.
 */
function getItemIdFromPopupDiv(itemTypeString) {
    let itemId = 0;
    const popupIdDiv = document.querySelector('#popup-' + itemTypeString + '-id-div');
    if (popupIdDiv !== null) {
        let popIdData = popupIdDiv.getAttribute('data-popup-' + itemTypeString + '-id');
        if (popIdData) {
            let parsedItemId = parseInt(popIdData.valueOf());
            if (parsedItemId) {
                itemId = parsedItemId;
            }
        }
    }
    console.log('item-id: ' + itemId);
    return itemId;
}
/**
 * Shows the video details popup when the page is loaded, if the url query string contains videoId that is not 0.
 */
export async function showPopupAtLoad(itemType) {
    startFullPageSpinner();
    console.log('showPopupAtLoad: itemType: ' + itemType);
    if (itemType === 1) {
        let itemId = getItemIdFromPopupDiv('picture');
        if (itemId !== 0) {
            await popupPictureDetails(itemId.toString());
        }
    }
    if (itemType === 2) {
        let itemId = getItemIdFromPopupDiv('video');
        if (itemId !== 0) {
            await popupVideoDetails(itemId.toString());
        }
    }
    if (itemType === 3) { // Special case for calendar events, as we need to navigate to the date of the event.
        const popupEventIdDiv = document.querySelector('#popup-event-id-div');
        if (popupEventIdDiv !== null) {
            if (popupEventIdDiv.dataset.popupEventId) {
                let eventId = parseInt(popupEventIdDiv.dataset.popupEventId);
                if (eventId > 0) {
                    if (popupEventIdDiv.dataset.popupEventDateYear && popupEventIdDiv.dataset.popupEventDateMonth && popupEventIdDiv.dataset.popupEventDateDay) {
                        const popupEventYear = parseInt(popupEventIdDiv.dataset.popupEventDateYear);
                        const popupEventMonth = parseInt(popupEventIdDiv.dataset.popupEventDateMonth) - 1;
                        const popupEventDay = parseInt(popupEventIdDiv.dataset.popupEventDateDay);
                        if (popupEventYear && popupEventMonth && popupEventDay) {
                            let scheduleInstance = document.querySelector('.e-schedule').ej2_instances[0];
                            scheduleInstance.selectedDate = new Date(popupEventYear, popupEventMonth, popupEventDay);
                            await popupEventItem(eventId.toString(), popupEventYear.toString(), popupEventMonth.toString(), popupEventDay.toString());
                        }
                    }
                    else {
                        await popupEventItem(eventId.toString(), '0', '0', '0');
                    }
                }
            }
        }
    }
    if (itemType === 4) {
        let itemId = getItemIdFromPopupDiv('vocabulary');
        if (itemId !== 0) {
            await popupVocabularyItem(itemId.toString());
        }
    }
    if (itemType === 5) {
        let itemId = getItemIdFromPopupDiv('skill');
        if (itemId !== 0) {
            await popupSkillItem(itemId.toString());
        }
    }
    if (itemType === 6) {
        let itemId = getItemIdFromPopupDiv('friend');
        if (itemId !== 0) {
            await popupFriendItem(itemId.toString());
        }
    }
    if (itemType === 7) {
        let itemId = getItemIdFromPopupDiv('measurement');
        if (itemId !== 0) {
            await popupMeasurementItem(itemId.toString());
        }
    }
    if (itemType === 8) {
        let itemId = getItemIdFromPopupDiv('sleep');
        if (itemId !== 0) {
            await popupSleepItem(itemId.toString());
        }
    }
    if (itemType === 9) {
        let itemId = getItemIdFromPopupDiv('note');
        if (itemId !== 0) {
            await popupNoteItem(itemId.toString());
        }
    }
    if (itemType === 10) {
        let itemId = getItemIdFromPopupDiv('contact');
        if (itemId !== 0) {
            await popupContactItem(itemId.toString());
        }
    }
    if (itemType === 11) {
        let itemId = getItemIdFromPopupDiv('vaccination');
        if (itemId !== 0) {
            await popupVaccinationItem(itemId.toString());
        }
    }
    if (itemType === 12) {
        let itemId = getItemIdFromPopupDiv('location');
        if (itemId !== 0) {
            await popupLocationItem(itemId.toString());
        }
    }
    if (itemType === 15) {
        let itemId = getItemIdFromPopupDiv('todo');
        if (itemId !== 0) {
            await popupTodoItem(itemId.toString());
        }
    }
    if (itemType === 16) {
        let itemId = getItemIdFromPopupDiv('kanban-board');
        if (itemId !== 0) {
            await popupKanbanBoard(itemId.toString());
        }
    }
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=items-display-v9.js.map