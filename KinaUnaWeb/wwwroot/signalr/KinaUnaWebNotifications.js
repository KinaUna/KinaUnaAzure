let connection = null;

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
        notificationsMenuDiv.innerHTML += '<a href="#" class="dropdown-item"><div class="card" style="margin: 0; min-width: 100px; padding-bottom: 4px; background: rgba(38, 3, 51, 0.3);"><h6 class="card-header" style="color: #f7d082; background: #1a1031; margin: 0px; padding: 8px;">' + parsedMessage.Title + '</h5>' +
            '<div class="card-body" style="margin: 0px; padding: 4px;">' +
            '<div class="card-text text-white" style="margin: 4px; padding: 0px; word-wrap: break-word;">' + parsedMessage.Message + '</div></div><div class="card-footer text-right ml-auto" style="margin: 0; margin-top: 8px; padding: 2px;"><p class="text-success"><small>From: </small>' + parsedMessage.From + '<br/><small class="text-info">' + parsedMessage.DateTimeString + '</small></p></div></div></a>';
        let notificationsIcon = document.getElementById("notificationBellIcon");
        notificationsIcon.classList.add("notificationIconAnimation");
    }
);
let getNotifications = function () {
    connection.invoke("GetUpdateForUser").catch(err => console.error(err.toString()));
}

connection.start().catch(err => console.error(err.toString()));

