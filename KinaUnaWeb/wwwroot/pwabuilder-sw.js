//This is the service worker with the Advanced caching

const CACHE = 'kinauna-cache-v1';
const precacheFiles = [
/* Add an array of files to precache for your app */

];

const offlineFallbackPage = '/offline.html';

const networkFirstPaths = [
    /* Add an array of regex of paths that should go network first */
    // Example: /\/api\/.*/
    
];

const avoidCachingPaths = [
    /* Add an array of regex of paths that shouldn't be cached */
    // Example: /\/api\/.*/
];

function pathComparer(requestUrl, pathRegEx) {
    return requestUrl.match(new RegExp(pathRegEx));
}

function comparePaths(requestUrl, pathsArray) {
    if (requestUrl) {
        for (let index = 0; index < pathsArray.length; index++) {
            const pathRegEx = pathsArray[index];
            if (pathComparer(requestUrl, pathRegEx)) {
                return true;
            }
        }
    }

    return false;
}

function isPreCached(requestUrl, preCacheArray) {
	if (requestUrl) {
        for (let index = 0; index < preCacheArray.length; index++) {
            const path = preCacheArray[index];
            if (requestUrl.toLowerCase().indexOf(path.toLowerCase()) !== -1) {
				return true;
			}
		}
	}

	return false;
}

self.addEventListener('install', function (event) {
    //console.log("[PWA Builder] Install Event processing");

    //console.log("[PWA Builder] Skip waiting on install");
    self.skipWaiting();

    event.waitUntil(
        caches.open(CACHE).then(function (cache) {
            console.log("[PWA Builder] Caching pages during install");

            return cache.addAll(precacheFiles).then(function () {
                return cache.add(offlineFallbackPage);
            });
        })
    );
});

// Allow sw to control of current page
self.addEventListener('activate', function (event) {
    //console.log("[PWA Builder] Claiming clients for current page");
    event.waitUntil(self.clients.claim());
});

// If any fetch fails, it will look for the request in the cache and serve it from there first
self.addEventListener('fetch', function (event) {
    if (event.request.method !== 'GET') return;

    if (!isPreCached(event.request.url, precacheFiles)) {
	    networkFirstFetch(event);
    } else {
	    cacheFirstFetch(event);
    }
    //if (comparePaths(event.request.url, networkFirstPaths)) {
    //    networkFirstFetch(event);
    //} else {
    //    cacheFirstFetch(event);
    //}
});

function cacheFirstFetch(event) {
    event.respondWith(
        fromCache(event.request).then(
            function (response) {
                // The response was found in the cache so we respond with it and update the entry

                // This is where we call the server to get the newest version of the
                // file to use the next time we show view
                event.waitUntil(
                    fetch(event.request).then(function (response) {
                        return updateCache(event.request, response);
                    })
                );

                return response;
            },
            function () {
                // The response was not found in the cache so we look for it on the server
                return fetch(event.request)
                    .then(function (response) {
                        // If request was success, add or update it in the cache
                        event.waitUntil(updateCache(event.request, response.clone()));

                        return response;
                    })
                    .catch(function (error) {
                        // The following validates that the request was for a navigation to a new document
                        if (event.request.destination !== "document" || event.request.mode !== "navigate") {
                            return null;
                        }

                        console.log("[PWA Builder - cacheFirstFetch] Network request failed and no cache." + error);
                        // Use the precached offline page as fallback
                        return caches.open(CACHE).then(function (cache) {
                            cache.match(offlineFallbackPage);
                        });
                    });
            }
        )
    );
}

function networkFirstFetch(event) {
    event.respondWith(
        fetch(event.request)
            .then(function (response) {
                event.waitUntil(updateCache(event.request, response.clone()));
                return response;
            })
            .catch(function (error) {
                console.log("[PWA Builder - networkFirstFetch] Network request Failed. Serving content from cache: " + error);
                return fromCache(event.request);
            })
    );
}

function fromCache(request) {
	return caches.open(CACHE).then(function (cache) {
		return cache.match(request).then(function (matching) {
			if (!matching || matching.status === 404) {
				if (request.destination !== "document" || request.mode !== "navigate") {
					return Promise.reject("no-match");
				}
				console.log("[PWA Builder = fromCache] Serving offline page.");
				return cache.match(offlineFallbackPage);
			}

			return matching;
		});
	});
}

function updateCache(request, response) {
    if (!(request.url.indexOf('http') === 0)) return null;
    if (!comparePaths(request.url, avoidCachingPaths)) {
        return caches.open(CACHE).then(function (cache) {
            return cache.put(request, response);
        });
    }

    return Promise.resolve();
}


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
        const link = notification.data; // '/notifications';
        // var notificationId = notification.data.notificationId;
        if (action === 'close') {
            notification.close();
        } else {
            if (action === 'open') {
                self.clients.openWindow('/notifications'); // /notifications/push/' + notificationId);
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