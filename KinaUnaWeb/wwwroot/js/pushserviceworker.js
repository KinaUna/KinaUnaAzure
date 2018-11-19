//self.addEventListener('push', function (event) {
//    if (!(self.Notification && self.Notification.permission === 'granted')) {
//        return;
//    }

//    var data = {};
//    if (event.data) {
//        data = event.data.json();
//    }

//    console.log('Notification Recieved:');
//    console.log(data);

//    var title = data.title;
//    var message = data.message;
//    var icon = "images/Square44x44Logo.scale-100.png";

//    event.waitUntil(self.registration.showNotification(title, {
//        body: message,
//        icon: icon,
//        badge: icon
//    }));
//});


//self.addEventListener('notificationclick',
//    function (event) {
//        var notification = event.notification;
//        var action = event.action;
//        var notificationId = notification.data.notificationId;
//        if (action === 'close') {
//            notification.close();
//        } else {
//            if (action === 'open') {
//                clients.openWindow('http://web.kinauna.com/notifications/push/' + notificationId);
//            } else {
//                clients.openWindow('http://web.kinauna.com');
//            }
//        }
//    });

//function displayNotification() {
//    if (Notification.permission === 'granted') {
//        navigator.serviceWorker.getRegistration().then(function (reg) {
//            var options = {
//                body: 'KinaUna Message',
//                icon: '/images/Square44x44Logo.scale-100.png',
//                vibrate: [100, 50, 100],
//                data: { notificationId: 1 },
//                actions: [
//                    { action: 'open', title: 'Open', icon: '/images/launch.png' },
//                    { action: 'close', title: 'Close', icon: '/images/clear.png' }
//                ]
//            };
//            reg.showNotification('KinaUna Web App', options);
//        });
//    }
//}

//navigator.serviceWorker.ready.then(function (reg) {
//    reg.pushManager.getSubscription().then(function (sub) {
//        if (sub == undefined) {
//            console.log('PushManger.getSubscription retured null');
//            // ask user to register for Push
//        } else {
//            console.log('PushManger.getSubscription retured a subscription');
//            // update database on server
//        }
//    });
//});

//navigator.serviceWorker.getRegistration().then(function (reg) {
//    reg.pushManager.subscribe({
//        userVisibleOnly: true
//    }).then(function (sub) {
//        console.log('getRegistration subscribing');
//        // send sub.toJSON() to server
//    });
//});