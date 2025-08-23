import { WebNotification } from "../page-models-v9";
import { updateNoficationElementEvents } from "./notification-actions.js";

declare let signalR: any;

let connection: any = null;
let notificationsCount = 0;
let notificationsList = document.getElementById('notifications-list-div');
let recentNotificationsList = document.getElementById('notifications-page-recent-web-notifications-div');
let notifationsCounter = document.getElementById('menu-notifications-counter');
let notificationsIcon = document.getElementById('menu-notification-bell-icon');
let menuToggler = document.getElementById('nav-main-menu-button');
let togglerCounter = document.getElementById('toggler-notifications-counter');

function countNotifications(): void {
    if (notificationsList === null || notifationsCounter === null || togglerCounter === null || notificationsIcon === null) {
        return;
    }

    let notificationsMenuChildren = notificationsList.children;
    let unreadNotesCount = notificationsMenuChildren.length;
    for (let i = 0; i < notificationsMenuChildren.length; i++) {
        if (notificationsMenuChildren[i].classList.contains('bg-dark')) {
            unreadNotesCount--;
        }
    }

    notificationsCount = unreadNotesCount;
    notifationsCounter.innerHTML = notificationsCount.toString();
    togglerCounter.innerHTML = notificationsCount.toString();
    if (notificationsCount === 0) {
        togglerCounter.style.display = "none";
        notifationsCounter.classList.remove('badge-danger');
        notifationsCounter.classList.add('badge-secondary');
        togglerCounter.style.display = 'none';
    } else {
        notificationsIcon.classList.add('notification-icon-animation');
        notifationsCounter.classList.remove('badge-secondary');
        notifationsCounter.classList.add('badge-danger');
        togglerCounter.style.display = 'block';
    }
}


function clearNotifications(): void {
    if (notificationsList === null || notifationsCounter === null || togglerCounter === null || notificationsIcon === null) {
        return;
    }
    let itemsToRemove = notificationsList.getElementsByClassName('notification-item');
    if (itemsToRemove.length > 0) {
        for (var i = itemsToRemove.length - 1; i >= 0; --i) {
            let parentDiv = itemsToRemove[i].closest('div');
            let parentDivParentNode = parentDiv?.parentNode;
            if (parentDiv !== null && parentDivParentNode) {
                
                parentDivParentNode.removeChild(parentDiv);
            }            
        }
    }

    notificationsCount = 0;
    notifationsCounter.innerHTML = notificationsCount.toString();
    togglerCounter.innerHTML = notificationsCount.toString();
    togglerCounter.style.display = "none";
    notifationsCounter.classList.remove('badge-danger');
    notifationsCounter.classList.add('badge-secondary');
    togglerCounter.style.display = 'none';
}

function sortNotifications(): void {
    if (notificationsList !== null) {
        Array.prototype.slice.call(notificationsList.children)
            .map(function (x) {
                if (notificationsList !== null) {
                    return notificationsList.removeChild(x);
                }
            })
            .sort(function (x, y) {
                return y.getAttribute('data-notificationTime') - x.getAttribute('data-notificationTime');
            })
            .forEach(function (x) {
                if (notificationsList !== null) {
                    return notificationsList.appendChild(x);
                }
            });

        const notificationButtonsList = notificationsList.getElementsByClassName('notification-button');
        updateNoficationElementEvents(notificationButtonsList);
    }

    if (recentNotificationsList !== null) {
        Array.prototype.slice.call(recentNotificationsList.children)
            .map(function (x) {
                if (recentNotificationsList !== null) {
                    return recentNotificationsList.removeChild(x);
                }
                
            })
            .sort(function (x, y) {
                return y.getAttribute('data-notificationTime') - x.getAttribute('data-notificationTime');
            })
            .forEach(function (x) {
                if (recentNotificationsList !== null) {
                    recentNotificationsList.appendChild(x);
                }
            });

        const recentNotificationButtonsList = recentNotificationsList.getElementsByClassName('notification-button');
        updateNoficationElementEvents(recentNotificationButtonsList);
    }
}

let checkNotifications: NodeJS.Timeout;

connection = new signalR.HubConnectionBuilder()
    .withUrl('/webnotificationhub')
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();


connection.onclose(async function () {
    await start();
});

connection.on('UserInfo',
    function (info: any) {
        console.log(info);
    });

connection.on('ReceiveMessage',
    async function (message: string): Promise<void> {
        let parsedMessage: WebNotification = JSON.parse(message);
        await fetch('/Notifications/GetWebNotificationElement', {
            method: 'POST',
            body: JSON.stringify(parsedMessage),
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        }).then(async function (showNotificationResponse) {
            removeNotificationDiv(parsedMessage);
            const responseText = await showNotificationResponse.text();
            addNotificationDiv(parsedMessage.id, responseText);
        }).catch(function (error) {
            console.log('Error in RecieveMessage: ');
            console.log(error);
        });

    }
);


function addNotificationDiv(notificationId: number, data: string): void {
    if (notificationsList !== null) {
        let tempData = notificationsList.innerHTML;
        notificationsList.innerHTML = data + tempData;
    }

    if (recentNotificationsList !== null) {
        let tempData = recentNotificationsList.innerHTML;
        recentNotificationsList.innerHTML = data + tempData;
    }

    sortNotifications();
    countNotifications();
}

function removeNotificationDiv(parsedMessage: WebNotification) {
    if (notificationsList !== null) {
        const notificationsInMenu = notificationsList.querySelectorAll('.notification-item');
        notificationsInMenu.forEach((notificationItem) => {
            let childDiv = notificationItem.firstElementChild;
            if (childDiv === null) return;

            let notificationIdAttribute = childDiv.getAttribute('data-notificationId');
            if (notificationIdAttribute) {
                let notificationToRemoveId = parseInt(notificationIdAttribute);
                if (notificationToRemoveId == parsedMessage.id) {
                    let parentDiv = notificationItem.closest('.notification-item');
                    if (parentDiv !== null) {
                        parentDiv.remove();
                    }
                }
            };
        });
    }

    if (recentNotificationsList !== null) {
        const notificationsInRecents = recentNotificationsList.querySelectorAll('.notification-item');
        notificationsInRecents.forEach((notificationItem) => {
            let childDiv = notificationItem.firstElementChild;
            if (childDiv === null) return;

            let notificationIdAttribute = childDiv.getAttribute('data-notificationId');
            if (notificationIdAttribute) {
                let notificationToRemoveId = parseInt(notificationIdAttribute);
                if (notificationToRemoveId == parsedMessage.id) {
                    let parentDiv = notificationItem.closest('.notification-item');
                    if (parentDiv !== null) {
                        parentDiv.remove();
                    }
                }
            };
        });
    }
}

connection.on('DeleteMessage',
    function (message: string) {
        let parsedMessage: WebNotification = JSON.parse(message);
        removeNotificationDiv(parsedMessage);
        sortNotifications();
        countNotifications();
    }
);

connection.on('MarkAllReadMessage',
    function () {
        getNotifications();
        countNotifications();
    }
);

function getNotifications() {
    clearNotifications();
    connection.invoke('GetUpdateForUser', 10, 1).catch((err: any) => console.error(err.toString()));
};

async function start() {
    try {
        await connection.start().catch((err: any) => console.error(err.toString()));
        console.log("SignalR Connected.");
    } catch (err) {
        console.log(err);
        setTimeout(start, 60000);
    }
};

document.addEventListener('DOMContentLoaded', async function () {
    let notificationsButton = document.getElementById('notificationsButton');
    if (notificationsButton !== null) {
        notificationsButton.addEventListener('click', function () {
            let notificationsIcon = document.getElementById('menu-notification-bell-icon');
            if (notificationsIcon !== null) {
                notificationsIcon.classList.remove('notification-icon-animation');
            }
            if (menuToggler !== null) {
                menuToggler.classList.remove('notification-icon-animation');
            }
        });
    }
    await start();
    checkNotifications = setInterval(getNotifications, 600000);
});