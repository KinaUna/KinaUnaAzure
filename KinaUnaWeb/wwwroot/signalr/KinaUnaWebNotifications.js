let connection = null;
let notificationsCount = 0;
let notificationsMenuDiv = document.getElementById('notificationsMenu');
let notifationsCounter = document.getElementById('notificationsCounter');
let notificationsIcon = document.getElementById('notificationBellIcon');
let menuToggler = document.getElementById('navbarTogglerButton');
let togglerCounter = document.getElementById('togglerNotificationsCounter');

function notificationItemClick(event, btn) {
    let notificationId = btn.getAttribute('data-notificationid');
    window.location.href = '/Notifications?Id=' + notificationId;
    
}

function markRead(event, btn) {
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
    event.stopImmediatePropagation();
}

function removeNotification(event, btn) {
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
    
    event.stopPropagation();
}

function updateNotification(parsedMessage, newData) {
    let itemsToRemove = document.getElementsByClassName('notifId' + parsedMessage.Id);
    var countChange = false;
    if (itemsToRemove.length > 0) {
        for (var i = itemsToRemove.length - 1; i >= 0; --i) {
            let parentBtn = itemsToRemove[i].closest('button');
            if (parentBtn.classList.contains('notificationUnread')) {
                countChange = true;
            }
            parentBtn.outerHTML = newData;
        }
    }
    if (countChange) {
        notificationsCount--;
    } else {
        notificationsCount++;
    }

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

connection = new signalR.HubConnectionBuilder()
    .withUrl('/webnotificationhub')
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .configureLogging(signalR.LogLevel.Information)
    .build();


connection.onclose(function () {
    console.log('SignalR closed, reconnecting.')
    connection.start().catch(err => console.error(err.toString()));
});
connection.on('UserInfo',
    function(info) {
        console.log(info);
    });

connection.on('ReceiveMessage',
    function(message) {
        console.log('ReceiveNotification: ' + message);
        let parsedMessage = JSON.parse(message);
        let notificationsMenuDiv = document.getElementById('notificationsMenu');
        $.ajax({
                type: 'GET',
                url: '/Notifications/ShowNotification',
                data: parsedMessage,
                datatype: 'html',
                async: true,
            success: function (data) {
                let tempData = notificationsMenuDiv.innerHTML;
                    notificationsMenuDiv.innerHTML = data + tempData;
                },
                error: function (jqXhr, textStatus, errorThrown) {
                    console.log(textStatus, errorThrown);
                }
            });
        
        if (!parsedMessage.IsRead) {
            notificationsCount++;
        }
        notifationsCounter.innerHTML = notificationsCount;
        togglerCounter.innerHTML = notificationsCount;
        if (notificationsCount === 0) {
            togglerCounter.style.display = 'none';
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
);

connection.on('UpdateMessage',
    function(message) {
        console.log('UpdateNotification: ' + message);
        let parsedMessage = JSON.parse(message);
        $.ajax({
            type: 'GET',
            url: '/Notifications/ShowUpdatedNotification',
            data: parsedMessage,
            datatype: 'html',
            async: true,
            success: function (data) {
                // console.log('UpdateData: ' + data);
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
        console.log('DeleteNotification: ' + message);
        let parsedMessage = JSON.parse(message);
        let itemsToRemove = document.getElementsByClassName('notifId' + parsedMessage.Id);
        var countChange = false;
        if (itemsToRemove.length > 0) {
            for (var i = itemsToRemove.length - 1; i >= 0; --i) {
                let parentBtn = itemsToRemove[i].closest('button');
                if (parentBtn.classList.contains('notificationUnread')) {
                    countChange = true;
                }
                parentBtn.parentNode.outerHTML = "";
            }
        }
        if (countChange) {
            notificationsCount--;
        } else {
            notificationsCount++;
        }
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
);
let getNotifications = function () {
    if (connection.connection.connectionState === 1) {
        notificationsCount = 0;
        notificationsMenuDiv.innerHTML = '';
        connection.invoke('GetUpdateForUser').catch(err => console.error(err.toString()));
    }
    if (connection.connection.connectionState === 2) {
        console.log('From getNotifications: SignalR closed, reconnecting.')
        connection.start().catch(err => console.error(err.toString()));
    }
};

connection.start().catch(err => console.error(err.toString()));

$(document).ready(function () {
    $("#notificationsButton").click(function () {
        let notificationsIcon = document.getElementById('notificationBellIcon');
        notificationsIcon.classList.remove('notificationIconAnimation');
        menuToggler.classList.remove('notificationIconAnimation');
        if (notificationsCount === 0) {
            togglerCounter.style.display = 'none';
        }
    });
    let checkNotifications = setInterval(getNotifications(), 300000);
});