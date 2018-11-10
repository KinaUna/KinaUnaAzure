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
    let itemsToUpdate = document.getElementsByClassName('notifId' + notifId);
    if (btn.classList.contains('notificationUnread')) {
        notificationsCount--;
        if (notificationsCount < 0) {
            notificationsCount = 0;
        }
        for (let i = itemsToUpdate.length - 1; i >= 0; --i) {
            itemsToUpdate[i].classList.remove('notificationUnread');
            itemsToUpdate[i].classList.add('notificationRead');
            itemsToUpdate[i].innerHTML = '<i class="material-icons">markunread</i> Mark as unread</a>';
            let parentBtn = itemsToUpdate[i].closest('button');
            parentBtn.classList.remove('notificationUnread');
            parentBtn.classList.add('notificationRead');
            parentBtn.classList.add('bg-dark');
        }
        connection.invoke('SetRead', notifId).catch(err => console.error(err.toString()));
    } else {
        notificationsCount++;
        if (notificationsCount < 0) {
            notificationsCount = 0;
        }
        for (let j = itemsToUpdate.length - 1; j >= 0; --j) {
            itemsToUpdate[j].classList.remove('notificationRead');
            itemsToUpdate[j].classList.add('notificationUnread');
            itemsToUpdate[j].innerHTML = '<i class="material-icons">drafts</i> Mark as read</a>';
            let parentBtn2 = itemsToUpdate[j].closest('button');
            parentBtn2.classList.remove('notificationRead');
            parentBtn2.classList.remove('bg-dark');
            parentBtn2.classList.add('notificationUnread');
        }
        connection.invoke('SetUnread', notifId).catch(err => console.error(err.toString()));
    }
    notifationsCounter.innerHTML = notificationsCount;
    if (notificationsCount === 0) {
        togglerCounter.style.display = "none";
        notifationsCounter.classList.remove('badge-danger');
        notifationsCounter.classList.add('badge-secondary');
    } else {
        notificationsIcon.classList.add('notificationIconAnimation');
        notifationsCounter.classList.remove('badge-secondary');
        notifationsCounter.classList.add('badge-danger');
    }
    event.stopImmediatePropagation();
}

function removeNotification(event, btn) {
    let notifId = btn.getAttribute('data-notificationid');
    let parentBtn = btn.closest('button');
    connection.invoke("DeleteNotification", notifId).catch(err => console.error(err.toString()));
    if (parentBtn.classList.contains('notificationUnread')) {
        notificationsCount--;
        if (notificationsCount < 0) {
            notificationsCount = 0;
        }
    }
    if (notificationsCount === 0) {
        togglerCounter.style.display = "none";
        notifationsCounter.classList.remove('badge-danger');
        notifationsCounter.classList.add('badge-secondary');
    } else {
        notificationsIcon.classList.add('notificationIconAnimation');
        notifationsCounter.classList.remove('badge-secondary');
        notifationsCounter.classList.add('badge-danger');
    }

    let itemsToRemove = document.getElementsByClassName('notifId' + notifId);
    for (var i = itemsToRemove.length - 1; i >= 0; --i) {
        itemsToRemove[i].parentNode.removeChild(itemsToRemove[i]);
    }
    notifationsCounter.innerHTML = notificationsCount;
    event.stopPropagation();
    return false;
}

connection = new signalR.HubConnectionBuilder()
    .withUrl('/webnotificationhub')
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on('UserInfo',
    function(info) {
        console.log(info);
    });

connection.on('ReceiveMessage',
    function(message) {
        console.log('Notification: ' + message);
        let parsedMessage = JSON.parse(message);
        let notificationsMenuDiv = document.getElementById('notificationsMenu');
        
        $.ajax({
            type: 'GET',
            url: '/Notifications/ShowNotification',
            data: parsedMessage,
            datatype: 'html',
            async: true,
            success: function (data) {
                notificationsMenuDiv.innerHTML += data;
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
        togglerCounter.style.display = 'block';
        if (notificationsCount === 0) {
            togglerCounter.style.display = 'none';
            notifationsCounter.classList.remove('badge-danger');
            notifationsCounter.classList.add('badge-secondary');
        } else {
            notificationsIcon.classList.add('notificationIconAnimation');
            notifationsCounter.classList.remove('badge-secondary');
            notifationsCounter.classList.add('badge-danger');
        }
    }
);
let getNotifications = function () {
    notificationsCount = 0;
    notificationsMenuDiv.innerHTML = '';
    connection.invoke('GetUpdateForUser').catch(err => console.error(err.toString()));
};

connection.start().catch(err => console.error(err.toString()));

$(document).ready(function () {
    $("#notificationsButton").click(function () {
        let notificationsIcon = document.getElementById('notificationBellIcon');
        notificationsIcon.classList.remove('notificationIconAnimation');
        menuToggler.classList.remove('notificationIconAnimation');
        togglerCounter.style.display = 'none';
    });

    let checkNotifications = setInterval(getNotifications(), 300000);
});