/**
 * Detects if the user has enabled push notifications.
 * If the user has enabled push notifications, the 'Enable Push Notifications' button is hidden and the 'Disable Push Notifications' button is shown.
 */
navigator.serviceWorker.ready.then(function (reg) {
    // Do we already have a push message subscription?
    reg.pushManager.getSubscription()
        .then(function (subscription) {
            const isSubscribed = subscription;
            const enableBtn = document.getElementById('enable-push-button');
            const disableBtn = document.getElementById('disable-push-button');
            if (isSubscribed) {
                if (enableBtn !== null && disableBtn !== null) {
                    enableBtn.style.display = 'none';
                    disableBtn.style.display = 'inline-block';
                }
            } else {
                if (enableBtn !== null && disableBtn !== null) {
                    disableBtn.style.display = 'none';
                    enableBtn.style.display = 'inline-block';
                }                
            }
        })
        .catch(function (err) {
            console.log('[req.pushManager.getSubscription] Unable to get subscription details.', err);
        });
});