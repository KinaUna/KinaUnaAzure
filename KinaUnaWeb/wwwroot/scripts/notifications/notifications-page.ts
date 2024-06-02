import { WebNotification, WebNotificationViewModel, WebNotficationsParameters, WebNotificationsList } from '../page-models-v1.js';

let webNotificationsList: WebNotification[] = [];
const webNotificationsParameters: WebNotficationsParameters = new WebNotficationsParameters();
let moreNotificationsButton: HTMLButtonElement | null;
let numberOfWebNotificationsDiv : HTMLDivElement | null;
let markAllAsReadButton: HTMLButtonElement | null;
let webNotificationsDiv: HTMLDivElement | null;

function runWaitMeMoreNotificationsButton(): void {
    const moreItemsButton: any = $('#loadingWebNotificationsDiv');
    moreItemsButton.waitMe({
        effect: 'bounce',
        text: '',
        bg: 'rgba(177, 77, 227, 0.0)',
        color: '#9011a1',
        maxSize: '',
        waitTime: -1,
        source: '',
        textPos: 'vertical',
        fontSize: '',
        onClose: function () { }
    });
}

function stopWaitMeMoreNotifcationsButton(): void {
    const moreItemsButton: any = $('#loadingWebNotificationsDiv');
    moreItemsButton.waitMe("hide");
}

async function getWebNotificationsList(parameters: WebNotficationsParameters) {
    runWaitMeMoreNotificationsButton();
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
            const newNotificationsList = (await getNotificationsListResult.json()) as WebNotificationsList;
            if (newNotificationsList.notificationsList.length > 0) {
                const webNotificationsParentDiv = document.querySelector<HTMLDivElement>('#webNotificationsParentDiv');
                if (webNotificationsParentDiv !== null) {
                    webNotificationsParentDiv.classList.remove('d-none');
                }
                for await (const webNotificationToAdd of newNotificationsList.notificationsList) {
                    webNotificationsList.push(webNotificationToAdd);
                    await renderWebNotification(webNotificationToAdd);
                    window.history.replaceState("state", "title", "Notifications?Id=0&&count=" + webNotificationsList.length);
                };
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

    stopWaitMeMoreNotifcationsButton();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function sortAllWebNotifications() {
    
    if (webNotificationsDiv !== null) {
        Array.prototype.slice.call(webNotificationsDiv.children)
            .map(function (x) { if (webNotificationsDiv !== null) return webNotificationsDiv.removeChild(x); })
            .sort(function (x, y) { return y.getAttribute('data-notificationTime') - x.getAttribute('data-notificationTime'); })
            .forEach(function (x) { if (webNotificationsDiv !== null) webNotificationsDiv.appendChild(x); });
    }
    
}

async function markAllNotificationsAsRead() {
    const markAllNotificationsAsReadResponse = await fetch('/Notifications/SetAllRead');
    if (markAllNotificationsAsReadResponse.ok) {
        
        if (webNotificationsDiv !== null) {
            const unreadNotificationsOnPage = webNotificationsDiv.querySelectorAll<HTMLDivElement>('.notificationUnread');
            unreadNotificationsOnPage.forEach((unreadNotification: HTMLDivElement) => {
                let webNotificationToUpdate: WebNotification = new WebNotification();
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
                };

            });
        }        
    }
}

async function renderWebNotification(notificationItem: WebNotification) {
    const webNotificationViewModel: WebNotificationViewModel = new WebNotificationViewModel();
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function removeNotificationDiv(id: number) {
    
    if (webNotificationsDiv !== null) {
        const notificationsInRecents = webNotificationsDiv.querySelectorAll('.notification-item');
        notificationsInRecents.forEach((notificationItem) => {
            let childDiv = notificationItem.firstElementChild;
            if (childDiv === null) return;

            let notificationIdAttribute = childDiv.getAttribute('data-notificationId');
            if (notificationIdAttribute) {
                let notificationToRemoveId = parseInt(notificationIdAttribute);
                
                if (notificationToRemoveId === id) {
                    let parentDiv = notificationItem.closest('.notification-item');
                    if (parentDiv !== null) {
                        parentDiv.remove();
                    }
                }
            };

        });
    }
}

$(async function () {
    webNotificationsParameters.count = 10;
    webNotificationsParameters.skip = 0;

    webNotificationsDiv = document.querySelector<HTMLDivElement>('#webNotificationsDiv');

    numberOfWebNotificationsDiv = document.querySelector<HTMLDivElement>('#numberOfWebNotificationsDiv');
    if (numberOfWebNotificationsDiv !== null) {
        const itemsCountDivData: string | undefined = numberOfWebNotificationsDiv.dataset.itemsCount;
        if (itemsCountDivData) {
            webNotificationsParameters.count = parseInt(itemsCountDivData);
        }
    }
    moreNotificationsButton = document.querySelector<HTMLButtonElement>('#moreWebNotificationsButton');
    if (moreNotificationsButton !== null) {
        moreNotificationsButton.addEventListener('click', async () => {
            getWebNotificationsList(webNotificationsParameters);
        });
    }

    markAllAsReadButton = document.querySelector<HTMLButtonElement>('#markAllAsReadButton');
    if (markAllAsReadButton !== null) {
        markAllAsReadButton.addEventListener('click', async () => {
            markAllNotificationsAsRead();
        });
    }

    await getWebNotificationsList(webNotificationsParameters);
});