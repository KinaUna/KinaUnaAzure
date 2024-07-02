import { popupEventItem } from "../calendar/calendar-details.js";
import { popupPictureDetails } from "../item-details/picture-details.js";
import { popupVideoDetails } from "../item-details/video-details.js";
import { popupNoteItem } from "../notes/note-details.js";
import { popupSleepItem } from "../sleep/sleep-details.js";
/**
 * Used to handle the click event on a notification.
 * Updates the notification as read if it is unread.
 * Redirects to the notification link if it is set.
 * @param btn The notification element clicked.
 */
async function notificationItemClick(btn: HTMLElement): Promise<void> {
    let notifId = btn.getAttribute('data-notificationid');
    if (btn.classList.contains('notification-unread')) {
        await fetch('/Notifications/SetRead?Id=' + notifId, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        }).catch(function (error) {
            console.log('Error setting notification as read in notificationItemClick: ' + error);
        });
    }

    let notificationLink = btn.getAttribute('data-notificationLink');
    if (notificationLink !== null) {
        if (notificationLink.startsWith('/Pictures/Picture/')) {
            let notificationLinkWithoutPath = notificationLink.replace('/Pictures/Picture/', '');
            let notificationLinkSplit = notificationLinkWithoutPath.split('?');
            let pictureId = notificationLinkSplit[0];
            if (pictureId !== null) {
                popupPictureDetails(pictureId);
                return new Promise(function (resolve, reject) {
                    resolve();
                });
            }
        }

        if (notificationLink.startsWith('/Videos/Video/')) {
            let notificationLinkWithoutPath = notificationLink.replace('/Videos/Video/', '');
            let notificationLinkSplit = notificationLinkWithoutPath.split('?');
            let videoId = notificationLinkSplit[0];
            if (videoId !== null) {
                popupVideoDetails(videoId);
                return new Promise(function (resolve, reject) {
                    resolve();
                });
            }
        }

        if (notificationLink.startsWith('/Calendar/ViewEvent')) {
            let notificationLinkWithoutPath = notificationLink.replace('/Calendar/ViewEvent?eventId=', '');
            let notificationLinkSplit = notificationLinkWithoutPath.split('&');
            let eventId = notificationLinkSplit[0];
            if (eventId !== null) {
                popupEventItem(eventId);
                return new Promise(function (resolve, reject) {
                    resolve();
                });
            }
        }

        if (notificationLink.startsWith('/Notes/ViewNote')) {
            let notificationLinkWithoutPath = notificationLink.replace('/Notes/ViewNote?noteId=', '');
            let notificationLinkSplit = notificationLinkWithoutPath.split('&');
            let noteId = notificationLinkSplit[0];
            if (noteId !== null) {
                popupNoteItem(noteId);
                return new Promise(function (resolve, reject) {
                    resolve();
                });
            }
        }

        if (notificationLink.startsWith('/Sleep/ViewSleep')) {
            let notificationLinkWithoutPath = notificationLink.replace('/Sleep/ViewSleep?itemId=', '');
            let notificationLinkSplit = notificationLinkWithoutPath.split('&');
            let sleepId = notificationLinkSplit[0];
            if (sleepId !== null) {
                popupSleepItem(sleepId);
                return new Promise(function (resolve, reject) {
                    resolve();
                });
            }
        }


        window.location.href = notificationLink;
    }

    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Marks a notification as read or unread, by sending a request to the server to update the notification.
 * The server will then send a message to all clients to update the notification via signalR
 * @param 
 */
async function markRead(btn: HTMLElement): Promise<void> {
    
    let notifId = btn.getAttribute('data-notificationid');
    if (btn.classList.contains('notification-unread')) {
        await fetch('/Notifications/SetRead?Id=' + notifId, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        }).catch(function (error) {
            console.log('Error setting notification as read: ' + error);
        });

    } else {
        await fetch('/Notifications/SetUnread?Id=' + notifId, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        }).catch(function (error) {
            console.log('Error setting notification as unread: ' + error);
        });
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sends a request to delete the notification from the database.
 * @param btn The button element clicked.
 * @param event The click event on the button.
 */
async function removeNotification(btn: HTMLElement): Promise<void> {
    let notifId = btn.getAttribute('data-notificationid');

    await fetch('/Notifications/Remove?Id=' + notifId, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).catch(function (error) {
        console.log('Error removing notification: ' + error);
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Adds event listeners to the given notifications collection, for viewing, mark as read, and deleting.
 * @param notificationButtonsList The list of notificaiton elements.
 */
export function updateNoficationElementEvents(notificationButtonsList: HTMLCollectionOf<Element>): void {
    if (notificationButtonsList !== null) {
        Array.from(notificationButtonsList).forEach((notificationElement) => {
            let button = notificationElement as HTMLElement;
            if (button === null) {
                return;
            }

            button.addEventListener('click', async function (event: MouseEvent) {
                event.stopImmediatePropagation();
                notificationItemClick(button);
            });

            const markReadButton = button.getElementsByClassName('mark-notification-read-button');
            if (markReadButton !== null) {
                Array.from(markReadButton).forEach((markReadElement) => {
                    let markReadSpan = markReadElement as HTMLElement;
                    markReadSpan.addEventListener('click', async function (event: MouseEvent) {
                        event.stopImmediatePropagation();
                        markRead(markReadSpan);
                    });
                });
            }

            const deleteButton = button.getElementsByClassName('delete-notification-button');
            if (deleteButton !== null) {
                Array.from(deleteButton).forEach((deleteElement) => {
                    let deleteSpan = deleteElement as HTMLElement;
                    deleteSpan.addEventListener('click', async function (event: MouseEvent) {
                        event.stopImmediatePropagation();
                        removeNotification(deleteSpan);
                    });
                });
            }
        });
    }
}