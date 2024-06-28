/**
 * Adds an event listener to the copy location button to copy the selected location to the latitude and longitude fields.
 */
export function addCopyLocationButtonEventListener(): void {
    const copyLocationButton = document.querySelector<HTMLButtonElement>('#copy-location-button');
    if (copyLocationButton !== null) {
        copyLocationButton.addEventListener('click', function () {
            const latitudeInput = document.getElementById('latitude') as HTMLInputElement;
            const longitudeInput = document.getElementById('longitude') as HTMLInputElement;
            const locationSelectList = document.getElementById('copy-location') as HTMLSelectElement;

            if (latitudeInput !== null && longitudeInput !== null && locationSelectList !== null) {
                let latitude = locationSelectList.options[locationSelectList.selectedIndex].getAttribute('data-latitude');
                let longitude = locationSelectList.options[locationSelectList.selectedIndex].getAttribute('data-longitude');

                if (latitude !== null && longitude !== null) {
                    latitudeInput.setAttribute('value', latitude);
                    longitudeInput.setAttribute('value', longitude);
                }
            }
        });
    }
}