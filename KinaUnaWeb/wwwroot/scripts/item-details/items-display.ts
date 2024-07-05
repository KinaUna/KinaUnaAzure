import { addPictureItemEventListeners } from '../pictures/picture-details.js';
import { addCalendarEventListeners } from '../calendar/calendar-details.js';
import { TimelineItem } from '../page-models-v6.js'
import { addVideoItemEventListeners } from '../videos/video-details.js';
import { addNoteEventListeners } from '../notes/note-details.js';
import { addSleepEventListeners } from '../sleep/sleep-details.js';
import { addFriendItemListeners } from '../friends/friend-details.js';
import { addContactItemListeners } from '../contacts/contact-details.js';
import { addLocationItemListeners } from '../locations/location-details.js';
import { addMeasurementItemListeners } from '../measurements/measurement-details.js';
import { addSkillItemListeners } from '../skills/skill-details.js';
import { addVocabularyItemListeners } from '../vocabulary/vocabulary-details.js';
/**
 * Adds event listeners for a given timeline item.
 * @param {TimelineItem} item The timeline item to add event listeners for.
 */
export function addTimelineItemEventListener(item: TimelineItem): void {
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
        // Add vaccination listeners.
    }

    if (item.itemType === 12) {
        addLocationItemListeners(item.itemId);
    }
}

/**
 * Hides scrollbars on the body element, to prevent scrolling while a popup is displayed.
 */
export function hideBodyScrollbars(): void {
    let bodyElement = document.querySelector<HTMLBodyElement>('body');
    if (bodyElement) {
        bodyElement.style.overflow = 'hidden';
    }
}

/**
 * Shows scrollbars on the body element, to allow scrolling when a popup is closed.
 */
export function showBodyScrollbars(): void {
    let bodyElement = document.querySelector<HTMLBodyElement>('body');
    if (bodyElement) {
        bodyElement.style.removeProperty('overflow');
    }
}