import { popupEventItem } from "../calendar/calendar-details.js";
import { popupPictureDetails } from "../item-details/picture-details.js";
/**
 * Used to handle the click event on a notification.
 * Updates the notification as read if it is unread.
 * Redirects to the notification link if it is set.
 * @param btn The notification element clicked.
 */
async function notificationItemClick(btn) {
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
async function markRead(btn) {
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
    }
    else {
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sends a request to delete the notification from the database.
 * @param btn The button element clicked.
 * @param event The click event on the button.
 */
async function removeNotification(btn) {
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Adds event listeners to the given notifications collection, for viewing, mark as read, and deleting.
 * @param notificationButtonsList The list of notificaiton elements.
 */
export function updateNoficationElementEvents(notificationButtonsList) {
    if (notificationButtonsList !== null) {
        Array.from(notificationButtonsList).forEach((notificationElement) => {
            let button = notificationElement;
            if (button === null) {
                return;
            }
            button.addEventListener('click', async function (event) {
                event.stopImmediatePropagation();
                notificationItemClick(button);
            });
            const markReadButton = button.getElementsByClassName('mark-notification-read-button');
            if (markReadButton !== null) {
                Array.from(markReadButton).forEach((markReadElement) => {
                    let markReadSpan = markReadElement;
                    markReadSpan.addEventListener('click', async function (event) {
                        event.stopImmediatePropagation();
                        markRead(markReadSpan);
                    });
                });
            }
            const deleteButton = button.getElementsByClassName('delete-notification-button');
            if (deleteButton !== null) {
                Array.from(deleteButton).forEach((deleteElement) => {
                    let deleteSpan = deleteElement;
                    deleteSpan.addEventListener('click', async function (event) {
                        event.stopImmediatePropagation();
                        removeNotification(deleteSpan);
                    });
                });
            }
        });
    }
}
//# sourceMappingURL=notification-actions.js.map