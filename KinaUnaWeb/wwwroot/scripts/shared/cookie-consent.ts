declare var cookieDomainName: string;

/**
 * Saves the GDPR cookie settings.
 * For each service that is allowed, a string is added to the cookie value.
 */
function saveGdprCookie() {
    const gdprAllowMapsSwitch = document.querySelector<HTMLInputElement>('#allow-maps-switch');
    const gdprAllowYoutubeSwitch = document.querySelector<HTMLInputElement>('#allow-youtube-switch');

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
}

/**
 * Initializes the event listener for the save button when the page is loaded.
 */
document.addEventListener('DOMContentLoaded', function (): void {
    const saveGdprButton = document.querySelector<HTMLButtonElement>('#save-gdpr-button');
    
    if (saveGdprButton !== null) {
        saveGdprButton.addEventListener('click', saveGdprCookie, false);
    }    
});