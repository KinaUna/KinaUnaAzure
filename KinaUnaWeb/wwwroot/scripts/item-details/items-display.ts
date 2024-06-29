import { addPictureItemEventListeners } from '../item-details/picture-details.js';
import { addCalendarEventListeners } from '../calendar/calendar-details.js';
import { TimelineItem } from '../page-models-v6.js'
import { addVideoItemEventListeners } from './video-details.js';

/**
 * Adds event listeners for a given timeline item.
 * @param {TimelineItem} item The timeline item to add event listeners for.
 */
export function addTimelineItemEventListener(item: TimelineItem): void {
    if (item.itemType === 1) {
        addPictureItemEventListeners(item.itemId);
    }

    if (item.itemType === 2) {
        // Add video event listeners.
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
        // Add friend listeners.
    }

    if (item.itemType === 7) {
        // Add measurement listeners.
    }

    if (item.itemType === 8) {
        // Add sleep listeners.
    }

    if (item.itemType === 9) {
        // Add note listeners.
    }

    if (item.itemType === 10) {
        // Add contact listeners.
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