// Original Source: https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/master/Demo.AspNetCore.PushNotifications/wwwroot/scripts/push-notifications.js
var PushNotifications = (function () {
    let applicationServerPublicKey;
    let pushServiceWorkerRegistration;
    let subscribeButton, unsubscribeButton;
    let topicInput, urgencySelect, notificationInput;
    
    function urlB64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/\-/g, '+').replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }

        return outputArray;
    };
    
    function registerPushServiceWorker() {
        navigator.serviceWorker.register('/js/pushserviceworker.js', { scope: '/js/pushserviceworker/' })
            .then(function (serviceWorkerRegistration) {
                pushServiceWorkerRegistration = serviceWorkerRegistration;
                console.log('Push Service Worker has been registered successfully');
            }).catch(function (error) {
                console.log('Push Service Worker registration has failed: ' + error);
            });
    };

    
    function subscribeForPushNotifications() {
        if (applicationServerPublicKey) {
            subscribeForPushNotificationsInternal();
        } else {
            fetch('api/pushnotifications/public-key')
                .then(function (response) {
                    if (response.ok) {
                        return response.text();
                    } else {
                        console.log('PushNotifications: Failed to retrieve Public Key');
                    }
                    return response.text();
                }).then(function (applicationServerPublicKeyBase64) {
                    applicationServerPublicKey = urlB64ToUint8Array(applicationServerPublicKeyBase64);
                    console.log('PushNotifications: Successfully retrieved Public Key');

                    subscribeForPushNotificationsInternal();
                }).catch(function (error) {
                    console.log('PushNotifications: Failed to retrieve Public Key: ' + error);
                });
        }
    };

    function subscribeForPushNotificationsInternal() {
        pushServiceWorkerRegistration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: applicationServerPublicKey
        })
            .then(function (pushSubscription) {
                fetch('api/pushnotifications/subscriptions', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(pushSubscription)
                })
                    .then(function (response) {
                        if (response.ok) {
                            console.log('PushNotifications: Successfully subscribed for Push Notifications');
                        } else {
                            console.log('PushNotifications: Failed to store the Push Notifications subscrition on server');
                        }
                    }).catch(function (error) {
                        console.log('PushNotifications: Failed to store the Push Notifications subscrition on server: ' + error);
                    });
                
            }).catch(function (error) {
                if (Notification.permission === 'denied') {
                    console.log('PushNotifications: Error, access denie');
                } else {
                    console.log('Failed to subscribe for Push Notifications: ' + error);
                }
            });
    };

    function unsubscribeFromPushNotifications() {
        pushServiceWorkerRegistration.pushManager.getSubscription()
            .then(function (pushSubscription) {
                if (pushSubscription) {
                    pushSubscription.unsubscribe()
                        .then(function () {
                            fetch('api/pushnotifications/subscriptions?endpoint=' + encodeURIComponent(pushSubscription.endpoint), {
                                method: 'DELETE'
                            })
                                .then(function (response) {
                                    if (response.ok) {
                                        console.log('Successfully unsubscribed from Push Notifications');
                                    } else {
                                        console.log('Failed to discard the Push Notifications subscrition from server');
                                    }
                                }).catch(function (error) {
                                    console.log('Failed to discard the Push Notifications subscrition from server: ' + error);
                                });
                        }).catch(function (error) {
                            console.log('Failed to unsubscribe from Push Notifications: ' + error);
                        });
                }
            });
    };

    function sendPushNotification() {
        let payload = { topic: topicInput.value, notification: notificationInput.value, urgency: urgencySelect.value };

        fetch('api/pushnotifications/notifications', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
            .then(function (response) {
                if (response.ok) {
                    console.log('Successfully sent Push Notification');
                } else {
                    console.log('Failed to send Push Notification');
                }
            }).catch(function (error) {
                console.log('Failed to send Push Notification: ' + error);
            });
    };

    return {
        initialize: function () {
            subscribeButton = document.getElementById('requestNotificationPermissionButton');
            subscribeButton.addEventListener('click', subscribeForPushNotifications);
            unsubscribeButton = document.getElementById('unsubscribeNotificationPermissionButton');
            unsubscribeButton.addEventListener('click', unsubscribeFromPushNotifications);

            if (!('serviceWork' in navigator)) {
                console.log('Service Workers are not supported');
                return;
            }

            if (!('PushManager' in window)) {
                console.log('Push API not supported');
                return;
            }

            registerPushServiceWorker();
        }
    };
})();

PushNotifications.initialize();