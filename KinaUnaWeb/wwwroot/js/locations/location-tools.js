/**
 * Adds an event listener to the copy location button to copy the selected location to the latitude and longitude fields.
 */
export function addCopyLocationButtonEventListener() {
    const copyLocationButton = document.querySelector('#copy-location-button');
    if (copyLocationButton !== null) {
        copyLocationButton.addEventListener('click', function () {
            const latitudeInput = document.getElementById('latitude');
            const longitudeInput = document.getElementById('longitude');
            const locationSelectList = document.getElementById('copy-location');
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
//# sourceMappingURL=location-tools.js.map