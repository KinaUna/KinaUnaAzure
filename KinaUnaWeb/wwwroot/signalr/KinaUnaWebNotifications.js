let connection = null;
let notificationsCount = 0;
let notificationsMenuDiv = document.getElementById('notifications-menu');
let notificationsList = document.getElementById('notifications-list-div');
let recentNotificationsList = document.getElementById('notifications-page-recent-web-notifications-div');
let notifationsCounter = document.getElementById('menu-notifications-counter');
let notificationsIcon = document.getElementById('menu-notification-bell-icon');
let menuToggler = document.getElementById('nav-main-menu-button');
let togglerCounter = document.getElementById('toggler-notifications-counter');
let navMain = document.getElementById('nav-main');

async function notificationItemClick(btn, event) {
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

    let notificationLink = btn.getAttribute('data-notificationLink');
    window.location.href = notificationLink;

    return new Promise (function (resolve, reject) {
        resolve();
    });
}
/**
 * Marks a notification as read or unread, by sending a request to the server to update the notification.
 * The server will then send a message to all clients to update the notification via signalR
 * @param {any} btn The element clicked when setting it read/unread.
 * @param {any} event The click event.
 * @returns
 */
async function markRead(btn, event) {
    event.stopImmediatePropagation();
    
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

    return new Promise(function (resolve, reject) {
        resolve();
    });
}

async function removeNotification(btn, event) {
    event.stopPropagation();
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

function countNotifications() {
    
    let notificationsMenuChildren = notificationsList.children;
    let unreadNotesCount = notificationsMenuChildren.length;
    for (let i = 0; i < notificationsMenuChildren.length; i++) {
        if (notificationsMenuChildren[i].classList.contains('bg-dark')) {
            unreadNotesCount--;
        }
    }
    notificationsCount = unreadNotesCount;
    notifationsCounter.innerHTML = notificationsCount;
    togglerCounter.innerHTML = notificationsCount;
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


function clearNotifications() {
    let itemsToRemove = notificationsList.getElementsByClassName('notification-item');
    if (itemsToRemove.length > 0) {
        for (var i = itemsToRemove.length - 1; i >= 0; --i) {
            let parentDiv = itemsToRemove[i].closest('div');
            parentDiv.parentNode.removeChild(parentDiv);
        }
    }
    notificationsCount = 0;
    notifationsCounter.innerHTML = notificationsCount;
    togglerCounter.innerHTML = notificationsCount;
    togglerCounter.style.display = "none";
    notifationsCounter.classList.remove('badge-danger');
    notifationsCounter.classList.add('badge-secondary');
    togglerCounter.style.display = 'none';
}

function sortNotifications() {
    if (notificationsList !== null) {
        Array.prototype.slice.call(notificationsList.children)
            .map(function (x) { return notificationsList.removeChild(x); })
            .sort(function (x, y) { return y.getAttribute('data-notificationTime') - x.getAttribute('data-notificationTime'); })
            .forEach(function (x) { notificationsList.appendChild(x); });
    }

    if (recentNotificationsList !== null) {
        Array.prototype.slice.call(recentNotificationsList.children)
            .map(function (x) { return recentNotificationsList.removeChild(x); })
            .sort(function (x, y) { return y.getAttribute('data-notificationTime') - x.getAttribute('data-notificationTime'); })
            .forEach(function (x) { recentNotificationsList.appendChild(x); });
    }
}

let checkConnectionInterval;
let checkNotifications;
let signalRdisconnected = false;

connection = new signalR.HubConnectionBuilder()
    .withUrl('/webnotificationhub')
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .configureLogging(signalR.LogLevel.Information)
    .build();


connection.onclose(function () {
    signalRdisconnected = true;
    console.log('SignalR connection closed, reconnecting.');
    clearInterval(checkNotifications);
    clearNotifications();
    checkConnectionInterval = setInterval(function() {
        connection.start().catch(err => console.error(err.toString()));
    }, 30000);
});

connection.on('UserInfo',
    function (info) {
        console.log(info);
    });

connection.on('ReceiveMessage',
    function(message) {
        if (signalRdisconnected) {
            clearInterval(checkConnectionInterval);
            checkNotifications = setInterval(getNotifications(), 300000);
            signalRdisconnected = false;
        }
        let parsedMessage = JSON.parse(message);
        await fetch('/Notifications/ShowNotification' + notifId, {
            method: 'GET',
            body: parsedMessage,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        }).then(function (showNotificationResponse) {
            removeNotificationDiv(parsedMessage);
            addNotificationDiv(showNotificationResponse);
        }).catch(function (error) {
            console.log('Error removing notification: ' + error);
        });

    }
);

function addNotificationDiv(data) {
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

function removeNotificationDiv(parsedMessage) {
    if (notificationsList !== null) {
        const notificationsInMenu = notificationsList.querySelectorAll('.notification-item');
        notificationsInMenu.forEach((notificationItem) => {
            let childDiv = notificationItem.firstElementChild;
            let notificationIdAttribute = childDiv.getAttribute('data-notificationId');
            if (notificationIdAttribute) {
                let notificationToRemoveId = parseInt(notificationIdAttribute);
                if (notificationToRemoveId == parsedMessage.Id) {
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
            let notificationIdAttribute = childDiv.getAttribute('data-notificationId');
            if (notificationIdAttribute) {
                let notificationToRemoveId = parseInt(notificationIdAttribute);
                if (notificationToRemoveId == parsedMessage.Id) {
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
    function (message) {
        let parsedMessage = JSON.parse(message);
        removeNotificationDiv(parsedMessage);
        sortNotifications();
        countNotifications();
    }
);

connection.on('MarkAllReadMessage',
    function () {
        if (signalRdisconnected) {
            clearInterval(checkConnectionInterval);
            checkNotifications = setInterval(getNotifications(), 300000);
            signalRdisconnected = false;
        }
        getNotifications();
        countNotifications();
        }
        
    );
let getNotifications = function () {
    if (connection.connection.connectionState === 1) {
        signalRdisconnected = false;
        clearNotifications();
        connection.invoke('GetUpdateForUser', 10, 1).catch(err => console.error(err.toString()));
    } else {
        signalRdisconnected = true;

    }
};

connection.start().catch(err => console.error(err.toString()));

document.addEventListener('DOMContentLoaded', function () {
    let notificationsButton = document.getElementById('notificationsButton');
    notificationsButton.addEventListener('click', function () {
        let notificationsIcon = document.getElementById('menu-notification-bell-icon');
        notificationsIcon.classList.remove('notification-icon-animation');
        menuToggler.classList.remove('notification-icon-animation');
    });
    
    checkNotifications = setInterval(getNotifications(), 300000);
});