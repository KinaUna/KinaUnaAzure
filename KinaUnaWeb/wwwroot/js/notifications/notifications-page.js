import { WebNotification, WebNotificationViewModel, WebNotficationsParameters } from '../page-models-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { updateNoficationElementEvents } from './notification-actions.js';
let webNotificationsList = [];
const webNotificationsParameters = new WebNotficationsParameters();
let moreNotificationsButton;
let numberOfWebNotificationsDiv;
let markAllAsReadButton;
let webNotificationsDiv;
/**
 * Starts the spinner for loading the timeline items.
 */
function startLoadingNotificationsSpinner() {
    startLoadingItemsSpinner('loading-web-notifications-div');
}
/**
 * Stops the spinner for loading the timeline items.
 */
function stopLoadingNotificationsSpinner() {
    stopLoadingItemsSpinner('loading-web-notifications-div');
}
/**
 * Fetches the list of web notifications, based on the parameters provided and the number of items already retrieved, then updates the page.
 * @param parameters The parameters to use for retrieving the web notifications.

 */
async function getWebNotificationsList(parameters) {
    startLoadingNotificationsSpinner();
    if (moreNotificationsButton !== null) {
        moreNotificationsButton.classList.add('d-none');
    }
    parameters.skip = webNotificationsList.length;
    await fetch('/Notifications/GetWebNotificationsPage', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(parameters)
    }).then(async function (getNotificationsListResult) {
        if (getNotificationsListResult != null) {
            const newNotificationsList = (await getNotificationsListResult.json());
            if (newNotificationsList.notificationsList.length > 0) {
                const webNotificationsParentDiv = document.querySelector('#notifications-page-web-notifications-parent-div');
                if (webNotificationsParentDiv !== null) {
                    webNotificationsParentDiv.classList.remove('d-none');
                }
                for await (const webNotificationToAdd of newNotificationsList.notificationsList) {
                    webNotificationsList.push(webNotificationToAdd);
                    await renderWebNotification(webNotificationToAdd);
                    window.history.replaceState("state", "title", "Notifications?Id=0&&count=" + webNotificationsList.length);
                }
                ;
                if (newNotificationsList.remainingItemsCount > 0 && moreNotificationsButton !== null) {
                    moreNotificationsButton.classList.remove('d-none');
                }
                window.history.replaceState("state", "title", "Notifications?Id=0&&count=" + webNotificationsList.length);
                webNotificationsParameters.count = 10;
            }
        }
    }).catch(function (error) {
        console.log('Error loading NotificationsList. Error: ' + error);
    });
    stopLoadingNotificationsSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sorts the web notification in webNotificationsDiv by the data-notificationTime attribute.
 */
function sortAllWebNotifications() {
    if (webNotificationsDiv !== null) {
        Array.prototype.slice.call(webNotificationsDiv.children)
            .map(function (x) { if (webNotificationsDiv !== null)
            return webNotificationsDiv.removeChild(x); })
            .sort(function (x, y) { return y.getAttribute('data-notificationTime') - x.getAttribute('data-notificationTime'); })
            .forEach(function (x) { if (webNotificationsDiv !== null)
            webNotificationsDiv.appendChild(x); });
        const notificationButtonsList = webNotificationsDiv.getElementsByClassName('notification-button');
        updateNoficationElementEvents(notificationButtonsList);
    }
}
/**
 * Updates all unread web notifications to read for this user in the database.
 * This will also trigger a signalR message to update all notification on the page, including in the menu.
 */
async function markAllNotificationsAsRead() {
    const markAllNotificationsAsReadResponse = await fetch('/Notifications/SetAllRead');
    if (markAllNotificationsAsReadResponse.ok) {
        if (webNotificationsDiv !== null) {
            const unreadNotificationsOnPage = webNotificationsDiv.querySelectorAll('.notification-unread');
            unreadNotificationsOnPage.forEach((unreadNotification) => {
                let webNotificationToUpdate = new WebNotification();
                let notificationIdAttribute = unreadNotification.getAttribute('data-notificationId');
                if (notificationIdAttribute) {
                    webNotificationToUpdate.id = parseInt(notificationIdAttribute);
                    if (webNotificationToUpdate.id > 0) {
                        let parentDiv = unreadNotification.closest('.notification-item');
                        if (parentDiv !== null) {
                            parentDiv.remove();
                        }
                        renderWebNotification(webNotificationToUpdate);
                    }
                }
                ;
            });
        }
    }
}
/**
 * Fetches the HTML for the given WebNotification and adds to the webNotificationsDiv.
 * @param notificationItem The WebNotification item to add.
 */
async function renderWebNotification(notificationItem) {
    const webNotificationViewModel = new WebNotificationViewModel();
    webNotificationViewModel.id = notificationItem.id;
    const getWebNotificationElementResponse = await fetch('/Notifications/GetWebNotificationElement', {
        method: 'POST',
        body: JSON.stringify(webNotificationViewModel),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    });
    if (getWebNotificationElementResponse.ok && getWebNotificationElementResponse.text !== null) {
        removeNotificationDiv(webNotificationViewModel.id);
        const webNotificationElementHtml = await getWebNotificationElementResponse.text();
        if (webNotificationsDiv != null) {
            webNotificationsDiv.insertAdjacentHTML('beforeend', webNotificationElementHtml);
            sortAllWebNotifications();
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Removes the HTML element, in the webNotificationsDiv, containing the WebNotification with the given id.
 * @param id The id of the WebNotification to remove.
 */
function removeNotificationDiv(id) {
    if (webNotificationsDiv !== null) {
        const notificationsInRecents = webNotificationsDiv.querySelectorAll('.notification-item');
        notificationsInRecents.forEach((notificationItem) => {
            let childDiv = notificationItem.firstElementChild;
            if (childDiv === null)
                return;
            let notificationIdAttribute = childDiv.getAttribute('data-notificationId');
            if (notificationIdAttribute) {
                let notificationToRemoveId = parseInt(notificationIdAttribute);
                if (notificationToRemoveId === id) {
                    let parentDiv = notificationItem.closest('.notification-item');
                    if (parentDiv !== null) {
                        parentDiv.remove();
                    }
                }
            }
            ;
        });
    }
}
/**
 * Add event listener for the Load More button.
 */
function addLoadMoreButtonEventListener() {
    moreNotificationsButton = document.querySelector('#load-more-web-notifications-button');
    if (moreNotificationsButton !== null) {
        moreNotificationsButton.addEventListener('click', async () => {
            getWebNotificationsList(webNotificationsParameters);
        });
    }
}
function addMarkAllAsReadButtonEventListener() {
    markAllAsReadButton = document.querySelector('#mark-all-notifications-as-read-button');
    if (markAllAsReadButton !== null) {
        markAllAsReadButton.addEventListener('click', async () => {
            markAllNotificationsAsRead();
        });
    }
}
/**
 * Initializes the elements on the page when it is first loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    webNotificationsParameters.count = 10;
    webNotificationsParameters.skip = 0;
    webNotificationsDiv = document.querySelector('#notifications-page-recent-web-notifications-div');
    numberOfWebNotificationsDiv = document.querySelector('#number-of-web-notifications-div');
    if (numberOfWebNotificationsDiv !== null) {
        const itemsCountDivData = numberOfWebNotificationsDiv.dataset.itemsCount;
        if (itemsCountDivData) {
            webNotificationsParameters.count = parseInt(itemsCountDivData);
        }
    }
    addLoadMoreButtonEventListener();
    addMarkAllAsReadButtonEventListener();
    await getWebNotificationsList(webNotificationsParameters);
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=notifications-page.js.map