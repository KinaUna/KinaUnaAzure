import { addPictureItemEventListeners } from '../item-details/picture-details.js';
import { addCalendarEventListeners } from '../calendar/calendar-details.js';
import { TimelineItem } from '../page-models-v6.js'

export function addTimelineItemEventListener(item: TimelineItem): void {
    if (item.itemType === 1) {
        addPictureItemEventListeners(item.itemId);
    }

    if (item.itemType === 2) {
        // Add video event listeners.
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