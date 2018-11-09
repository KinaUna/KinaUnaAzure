let connection = null;
let notificationsCount = 0;
let notifationsCounter = document.getElementById("notificationsCounter");
let notificationsIcon = document.getElementById("notificationBellIcon");
let menuToggler = document.getElementById('navbarTogglerButton');
let togglerCounter = document.getElementById('togglerNotificationsCounter');

function notificationItemClick(event, btn) {
    event.preventDefault();
    let isNotificationRead = btn.getAttribute('data-isread');
    if (isNotificationRead === 'false') {
        notificationsCount--;
        if (notificationsCount < 0) {
            notificationsCount = 0;
        }
    }
    notifationsCounter.innerHTML = notificationsCount;
}

function markRead(event, btn) {
    event.preventDefault();
    var parentBtn = btn.closest('button');
    var notifId = btn.getAttribute('data-notificationid');
    if (btn.classList.contains('notificationUnread')) {
        notificationsCount--;
        if (notificationsCount < 0) {
            notificationsCount = 0;
        }
        btn.classList.remove('notificationUnread');
        btn.classList.add('notificationRead');
        btn.innerHTML = '<i class="material-icons">drafts</i> Mark as unread</a>';
        parentBtn.classList.remove('notificationUnread');
        parentBtn.classList.add('notificationRead');
        parentBtn.classList.add('bg-dark');
        connection.invoke("SetRead", notifId).catch(err => console.error(err.toString()));
    } else {
        notificationsCount++;
        if (notificationsCount < 0) {
            notificationsCount = 0;
        }
        btn.classList.remove('notificationRead');
        btn.classList.add('notificationUnread');
        btn.innerHTML = '<i class="material-icons">markunread</i> Mark as read</a>';
        parentBtn.classList.remove('notificationRead');
        parentBtn.classList.remove('bg-dark');
        parentBtn.classList.add('notificationUnread');
        connection.invoke("SetUnread", notifId).catch(err => console.error(err.toString()));
    }
    notifationsCounter.innerHTML = notificationsCount;
    
}

function removeNotification(event, btn) {
    event.preventDefault();
    var parentBtn = btn.closest('button');
    if (parentBtn.classList.contains('notificationUnread')) {
        notificationsCount--;
        if (notificationsCount < 0) {
            notificationsCount = 0;
        }
    }
    parentBtn.parentNode.removeChild(parentBtn);
    notifationsCounter.innerHTML = notificationsCount;

}

connection = new signalR.HubConnectionBuilder()
    .withUrl("/webnotificationhub")
    .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("ReceiveMessage",
    function(message) {
        console.log("Notification: " + message);
        let parsedMessage = JSON.parse(message);
        let notificationsMenuDiv = document.getElementById("notificationsMenu");
        
        $.ajax({
            type: "GET",
            url: "/Notifications/ShowNotification",
            data: parsedMessage,
            datatype: "html",
            async: true,
            success: function (data) {
                notificationsMenuDiv.innerHTML += data;
            },
            error: function (jqXhr, textStatus, errorThrown) {
                console.log(textStatus, errorThrown);
            }
        });

        notificationsIcon.classList.add('notificationIconAnimation');
        if (!parsedMessage.IsRead) {
            notificationsCount++;
        }
        notifationsCounter.innerHTML = notificationsCount;
        togglerCounter.innerHTML = notificationsCount;
        togglerCounter.style.display = "block";
    }
);
let getNotifications = function () {
    connection.invoke("GetUpdateForUser").catch(err => console.error(err.toString()));
}

connection.start().catch(err => console.error(err.toString()));

$(document).ready(function () {
    $("#notificationsButton").click(function () {
        let notificationsIcon = document.getElementById("notificationBellIcon");
        notificationsIcon.classList.remove("notificationIconAnimation");
        menuToggler.classList.remove("notificationIconAnimation");
        togglerCounter.style.display = "none";
    });
});