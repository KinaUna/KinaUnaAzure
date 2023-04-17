"use strict";
navigator.serviceWorker.ready.then(function (reg) {
    // Do we already have a push message subscription?
    reg.pushManager.getSubscription()
        .then(function (subscription) {
        const isSubscribed = subscription;
        const enableBtn = document.getElementById('enablePushButton');
        const disableBtn = document.getElementById('disablePushButton');
        if (isSubscribed) {
            console.log('User is already subscribed to push notifications');
            if (enableBtn !== null && disableBtn !== null) {
                enableBtn.style.display = 'none';
                disableBtn.style.display = 'inline-block';
            }
        }
        else {
            console.log('User is not yet subscribed to push notifications');
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
//# sourceMappingURL=my-account.js.map