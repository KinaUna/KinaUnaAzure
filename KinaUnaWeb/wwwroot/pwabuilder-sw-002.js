// This is the service worker with the combined offline experience (Offline page + Offline copy of pages)

const CACHE = "kinauna-cache-v2";

importScripts('https://storage.googleapis.com/workbox-cdn/releases/5.1.2/workbox-sw.js');

// When this file changes, check if a change line 81 of _Layout.cshtml and version number in app.ts is needed.

const offlineFallbackPage = "/offline.html";

self.addEventListener("message", (event) => {
    if (event.data && event.data.type === "SKIP_WAITING") {
        self.skipWaiting();
    }
});

self.addEventListener('install', async (event) => {
    event.waitUntil(
        caches.open(CACHE)
            .then((cache) => cache.add(offlineFallbackPage))
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

if (workbox.navigationPreload.isSupported()) {
    workbox.navigationPreload.enable();
}

workbox.routing.registerRoute(
    new RegExp('/*'),
    new workbox.strategies.NetworkFirst({
        cacheName: CACHE
    })
);

self.addEventListener('fetch', (event) => {
    // Check if this is a request for an image
    if (event.request.destination === 'image') {
        event.respondWith(caches.open(cacheName).then((cache) => {
            // Go to the cache first
            return cache.match(event.request.url).then((cachedResponse) => {
                // Return a cached response if we have one
                if (cachedResponse) {
                    return cachedResponse;
                }

                // Otherwise, hit the network
                return fetch(event.request).then((fetchedResponse) => {
                    // Add the network response to the cache for later visits
                    cache.put(event.request, fetchedResponse.clone());

                    // Return the network response
                    return fetchedResponse;
                });
            });
        }));
    }

    if (event.request.mode === 'navigate') {
        event.respondWith((async () => {
            try {
                const preloadResp = await event.preloadResponse;

                if (preloadResp) {
                    return preloadResp;
                }

                const networkResp = await fetch(event.request);
                return networkResp;
            } catch (error) {

                const cache = await caches.open(CACHE);
                const cachedResp = await cache.match(offlineFallbackPage);
                return cachedResp;
            }
        })());
    }
});

self.addEventListener('push', function (event) {
    if (!(self.Notification && self.Notification.permission === 'granted')) {
        return;
    }

    let data = {};
    if (event.data) {
        data = event.data.text();
    }

    const notification = JSON.parse(data);
    const title = notification.Title;
    const message = notification.Message;
    const link = notification.Link;
    const icon = "https://web.kinauna.com/images/kinaunalogo192x192_round.png";
    const badge = "https://web.kinauna.com/images/kinaunalogo_badge4.png";
    const tag = notification.Tag;
    event.waitUntil(self.registration.showNotification(title, {
        body: message,
        icon: icon,
        badge: badge,
        data: link,
        tag: tag
    }));
});


self.addEventListener('notificationclick',
    function (event) {
        const notification = event.notification;
        const action = event.action;
        const link = notification.data;
        if (action === 'close') {
            notification.close();
        } else {
            if (action === 'open') {
                self.clients.openWindow('/notifications');
                notification.close();
            } else {
                self.clients.openWindow(link);
                notification.close();
            }
        }
    });

function displayNotification() {
    if (Notification.permission === 'granted') {
        navigator.serviceWorker.getRegistration().then(function (reg) {
            const options = {
                body: 'KinaUna Message',
                icon: 'https://web.kinauna.com/images/kinaunalogo192x192_round.png',
                badge: "https://web.kinauna.com/images/kinaunalogo_badge4.png",
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