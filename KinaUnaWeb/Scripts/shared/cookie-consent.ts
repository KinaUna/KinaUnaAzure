declare var cookieDomainName: string;
$(function (): void {
    const saveGdprButton = document.querySelector<HTMLButtonElement>('#saveGdprButton');
    const gdprAllowMapsSwitch = document.querySelector<HTMLInputElement>('#allowMapsSwitch');
    const gdprAllowYoutubeSwitch = document.querySelector<HTMLInputElement>('#allowYouTubeSwitch');

    if (saveGdprButton !== null) {
        saveGdprButton.addEventListener('click',
            function () {
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
            },
            false);
    }
    
});