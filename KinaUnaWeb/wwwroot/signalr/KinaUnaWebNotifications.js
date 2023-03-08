let connection = null;
let notificationsCount = 0;
let notificationsMenuDiv = document.getElementById('notificationsMenu');
let notificationsList = document.getElementById('notificationsList');
let notifationsCounter = document.getElementById('notificationsCounter');
let notificationsIcon = document.getElementById('notificationBellIcon');
let menuToggler = document.getElementById('navMainMenuButton');
let togglerCounter = document.getElementById('togglerNotificationsCounter');
let navMain = document.getElementById('navMain');

function notificationItemClick(btn, event) {
    //console.log("NotificationItemClick.Event.Target: " + event.target);
    //console.log("NotificationItemClick.Event.Type: " + event.type);
    //console.log("NotificationItemClick.Event.CurrentTarget: " + event.currentTarget);
    let notifId = btn.getAttribute('data-notificationid');
    if (btn.classList.contains('notificationUnread')) {
        $.ajax({
            type: 'GET',
            url: '/Notifications/SetRead?Id=' + notifId,
            async: true,
            success: function() {
                if ($('.navbar-toggler').css('display') !== 'none' && document.getElementById('bodyClick')) {
                    $('.navbar-toggler').trigger("click");
                }
                navMain.style.opacity = 0.8;
                runWaitMeLeave();
            },
            error: function(jqXhr, textStatus, errorThrown) {
                console.log(textStatus, errorThrown);
            }
        });
    } else {
        if ($('.navbar-toggler').css('display') !== 'none' && document.getElementById('bodyClick')) {
            $('.navbar-toggler').trigger("click");
        }
        navMain.style.opacity = 0.8;
        runWaitMeLeave();
    }
    let notificationLink = btn.getAttribute('data-notificationLink');
    window.location.href = notificationLink;
}

function markRead(btn, event) {
    event.stopImmediatePropagation();
    console.log("markRead.Event.Target: " + event.target);
    console.log("markRead.Event.Type: " + event.type);
    console.log("markRead.Event.CurrentTarget: " + event.currentTarget);
    let notifId = btn.getAttribute('data-notificationid');
    if (btn.classList.contains('notificationUnread')) {
        $.ajax({
            type: 'GET',
            url: '/Notifications/SetRead?Id=' + notifId,
            async: true,
            success: function () {
                console.log("SetRead: Done.");
            },
            error: function (jqXhr, textStatus, errorThrown) {
                console.log(textStatus, errorThrown);
            }
        });
    } else {
        $.ajax({
            type: 'GET',
            url: '/Notifications/SetUnread?Id=' + notifId,
            async: true,
            success: function () {
                console.log("SetUnread: Done.");
            },
            error: function (jqXhr, textStatus, errorThrown) {
                console.log(textStatus, errorThrown);
            }
        });
    }
    // event.stopImmediatePropagation();
}

function removeNotification(btn, event) {
    event.stopPropagation();
    let notifId = btn.getAttribute('data-notificationid');
    $.ajax({
        type: 'GET',
        url: '/Notifications/Remove?Id=' + notifId,
        async: true,
        success: function () {
            console.log("Remove: Done.");
        },
        error: function (jqXhr, textStatus, errorThrown) {
            console.log(textStatus, errorThrown);
        }
    });
    
    // event.stopPropagation();
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
        notificationsIcon.classList.add('notificationIconAnimation');
        notifationsCounter.classList.remove('badge-secondary');
        notifationsCounter.classList.add('badge-danger');
        togglerCounter.style.display = 'block';
    }
}
function updateNotification(parsedMessage, newData) {
    let itemsToRemove = document.getElementsByClassName('notifId' + parsedMessage.Id);
    if (itemsToRemove.length > 0) {
        for (var i = itemsToRemove.length - 1; i >= 0; --i) {
            let parentBtn = itemsToRemove[i].closest('.notification-button'); // closest('button');
            let parentDiv = parentBtn.parentNode;
            parentBtn.outerHTML = newData;
            if (parsedMessage.IsRead) {
                if (!parentDiv.classList.contains('bg-dark')) {
                    parentDiv.classList.add('bg-dark');
                }
            } else {
                if (parentDiv.classList.contains('bg-dark')) {
                    parentDiv.classList.remove('bg-dark');
                }
            }
        }
    }
    countNotifications();
}

function clearNotifications() {
    let itemsToRemove = document.getElementsByClassName('notification-item');
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
    var s = document.getElementById('notificationsList');
    Array.prototype.slice.call(s.children)
        .map(function (x) { return s.removeChild(x); })
        .sort(function (x, y) { return y.getAttribute('data-notificationTime') - x.getAttribute('data-notificationTime'); })
        .forEach(function (x) { s.appendChild(x); });
}

let checkConnectionInterval;
let checkNotifications;
let signalRdisconnected = false;
let checkNotificationsCount = 0;

connection = new signalR.HubConnectionBuilder()
    .withUrl('/webnotificationhub')
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .configureLogging(signalR.LogLevel.Information)
    .build();


connection.onclose(function () {
    signalRdisconnected = true;
    console.log('SignalR connection closed, reconnecting.');
    checkNotificationsCount = 0;
    clearInterval(checkNotifications);
    clearNotifications();
    checkConnectionInterval = setInterval(function() {
        connection.start().catch(err => console.error(err.toString()));
    }, 30000);
});

connection.on('UserInfo',
    function (info) {
        checkNotificationsCount = 0;
        console.log(info);
    });

connection.on('ReceiveMessage',
    function(message) {
        // console.log('ReceiveNotification: ' + message);
        checkNotificationsCount = 0;
        if (signalRdisconnected) {
            clearInterval(checkConnectionInterval);
            checkNotifications = setInterval(getNotifications(), 300000);
            signalRdisconnected = false;
        }
        let parsedMessage = JSON.parse(message);
        $.ajax({
            type: 'GET',
            url: '/Notifications/ShowNotification',
            data: parsedMessage,
            datatype: 'html',
            async: true,
            success: function (data) {
                let tempData = notificationsList.innerHTML;
                notificationsList.innerHTML = data + tempData;
                sortNotifications();
                countNotifications();
            },
                error: function (jqXhr, textStatus, errorThrown) {
                    console.log(textStatus, errorThrown);
                }
            });
    }
);

connection.on('UpdateMessage',
    function(message) {
        // console.log('UpdateNotification: ' + message);
        checkNotificationsCount = 0;
        let parsedMessage = JSON.parse(message);
        $.ajax({
            type: 'GET',
            url: '/Notifications/ShowUpdatedNotification',
            data: parsedMessage,
            datatype: 'html',
            async: true,
            success: function (data) {
                signalRdisconnected = false;
                updateNotification(parsedMessage, data);
            },
            error: function (jqXhr, textStatus, errorThrown) {
                console.log(textStatus, errorThrown);
            }
        });
    }
);

connection.on('DeleteMessage',
    function (message) {
        // console.log('DeleteNotification: ' + message);
        checkNotificationsCount = 0;
        let parsedMessage = JSON.parse(message);
        let itemsToRemove = document.getElementsByClassName('notifId' + parsedMessage.Id);
        if (itemsToRemove.length > 0) {
            for (var i = itemsToRemove.length - 1; i >= 0; --i) {
                let parentBtn = itemsToRemove[i].closest('.notification-button');
                parentBtn.parentNode.outerHTML = "";
            }
        }
        countNotifications();
    }
);
let getNotifications = function () {
    if (checkNotificationsCount > 1) {
        if (connection.connection.connectionState === 1) {
            signalRdisconnected = false;
            clearNotifications();
            connection.invoke('GetUpdateForUser', 10, 1).catch(err => console.error(err.toString()));
        } else {
            signalRdisconnected = true;

        }
    }
};

connection.start().catch(err => console.error(err.toString()));

$(document).ready(function () {
    $('#notificationsButton').click(function () {
        let notificationsIcon = document.getElementById('notificationBellIcon');
        notificationsIcon.classList.remove('notificationIconAnimation');
        menuToggler.classList.remove('notificationIconAnimation');
    });
    checkNotifications = setInterval(getNotifications(), 300000);
});