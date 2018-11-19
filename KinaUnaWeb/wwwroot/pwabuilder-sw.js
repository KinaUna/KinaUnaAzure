//This is the "Offline page" service worker

//Install stage sets up the offline page in the cache and opens a new cache
self.addEventListener('install', function (event) {
    var offlinePage = new Request('offline.html');
    event.waitUntil(
        fetch(offlinePage).then(function (response) {
            return caches.open('pwabuilder-offline').then(function (cache) {
                console.log('[PWA Builder] Cached offline page during Install' + response.url);
                return cache.put(offlinePage, response);
            });
        }));
});

//If any fetch fails, it will show the offline page.
//Maybe this should be limited to HTML documents?
self.addEventListener('fetch', function (event) {
    event.respondWith(
        fetch(event.request).catch(function (error) {
                console.error('[PWA Builder] Network request Failed. Serving offline page ' + error);
                return caches.open('pwabuilder-offline').then(function (cache) {
                    return cache.match('offline.html');
                });
            }
        ));
});

//This is a event that can be fired from your page to tell the SW to update the offline page
self.addEventListener('refreshOffline', function (response) {
    return caches.open('pwabuilder-offline').then(function (cache) {
        console.log('[PWA Builder] Offline page updated from refreshOffline event: ' + response.url);
        return cache.put(offlinePage, response);
    });
});

self.addEventListener('push', function (event) {
    if (!(self.Notification && self.Notification.permission === 'granted')) {
        return;
    }
    
    var data = {};
    if (event.data) {
        data = event.data.text();
    }

    console.log('Notification Recieved:');
    console.log('Data: ' + data);
    //console.log('Data.title: ' + data.title);
    //console.log('Data.message: ' + data.message);
    //console.log('Data.link: ' + data.link);

    var title = data; // data.title
    // var message = data.message;
    var link = data.link;
    var icon = "https://web.kinauna.com/images/kinaunalogo192x192.png";
    var badge = "https://web.kinauna.com/images/kinaunalogo_badge3.png";
    event.waitUntil(self.registration.showNotification(title, {
        body: data, // message,
        icon: icon,
        badge: badge
    }));
});


self.addEventListener('notificationclick',
    function (event) {
        var notification = event.notification;
        var action = event.action;
        var link = '/notifications';
        // var notificationId = notification.data.notificationId;
        if (action === 'close') {
            notification.close();
        } else {
            if (action === 'open') {
                clients.openWindow('/notifications'); // /notifications/push/' + notificationId);
                notification.close();
            } else {
                clients.openWindow(link);
                notification.close();
            }
        }
    });

function displayNotification() {
    if (Notification.permission === 'granted') {
        navigator.serviceWorker.getRegistration().then(function (reg) {
            var options = {
                body: 'KinaUna Message',
                icon: 'https://web.kinauna.com/images/kinaunalogo192x192.png',
                badge: "https://web.kinauna.com/images/kinaunalogo_badge3.png",
                vibrate: [100, 50, 100],
                data: { notificationId: 1 },
                actions: [
                    { action: 'open', title: 'Open', icon: 'https://web.kinauna.com/images/launch.png' },
                    { action: 'close', title: 'Close', icon: 'https://web.kinauna.com/images/clear.png' }
                ]
            };
            reg.showNotification('KinaUna Web App', options);
        });
    }
}