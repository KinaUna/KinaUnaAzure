"use strict";
$(function () {
    const saveGdprButton = document.querySelector('#saveGdprButton');
    const gdprAllowMapsSwitch = document.querySelector('#allowMapsSwitch');
    const gdprAllowYoutubeSwitch = document.querySelector('#allowYouTubeSwitch');
    if (saveGdprButton !== null) {
        saveGdprButton.addEventListener('click', function () {
            let gdprSettings = 'Essential';
            if (gdprAllowMapsSwitch !== null && gdprAllowMapsSwitch.checked) {
                gdprSettings = gdprSettings + 'HereMaps';
            }
            if (gdprAllowYoutubeSwitch !== null && gdprAllowYoutubeSwitch.checked) {
                gdprSettings = gdprSettings + 'YouTube';
            }
            let date = new Date();
            date.setTime(date.getTime() + (180 * 24 * 60 * 60 * 1000));
            let expires = '; expires=' + date.toUTCString();
            document.cookie = 'KinaUnaConsent=' + gdprSettings + expires + ';' + cookieDomainName;
            location.reload();
        }, false);
    }
});
//# sourceMappingURL=cookie-consent.js.map