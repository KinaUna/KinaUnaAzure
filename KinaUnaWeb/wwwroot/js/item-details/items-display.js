import { addPictureItemEventListeners } from '../item-details/picture-details.js';
import { addCalendarEventListeners } from '../calendar/calendar-details.js';
import { addVideoItemEventListeners } from './video-details.js';
import { addNoteEventListeners } from '../notes/note-details.js';
import { addSleepEventListeners } from '../sleep/sleep-details.js';
import { addFriendItemListeners } from '../friends/friend-details.js';
import { addContactItemListeners } from '../contacts/contact-details.js';
/**
 * Adds event listeners for a given timeline item.
 * @param {TimelineItem} item The timeline item to add event listeners for.
 */
export function addTimelineItemEventListener(item) {
    if (item.itemType === 1) {
        addPictureItemEventListeners(item.itemId);
    }
    if (item.itemType === 2) {
        addVideoItemEventListeners(item.itemId);
    }
    if (item.itemType === 3) {
        addCalendarEventListeners(item.itemId);
    }
    if (item.itemType === 4) {
        // Add vocabulary event listeners.
    }
    if (item.itemType === 5) {
        // Add skill listeners.
    }
    if (item.itemType === 6) {
        addFriendItemListeners(item.itemId);
    }
    if (item.itemType === 7) {
        // Add measurement listeners.
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
        // Add vaccination listeners.
    }
    if (item.itemType === 12) {
        // Add location listeners.
    }
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
//# sourceMappingURL=items-display.js.map