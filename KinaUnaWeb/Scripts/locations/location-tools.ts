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

export function setupHereMaps(languageId: number) {
    const mapContainerDiv = document.getElementById('here-map-container-div');
    const latitudeDiv = document.getElementById('here-maps-latitude-div');
    const longitudeDiv = document.getElementById('here-maps-longitude-div');
    const hereMapsApiKeyDiv = document.getElementById('here-maps-api-key-div');

    if (mapContainerDiv === null || latitudeDiv === null || longitudeDiv === null || hereMapsApiKeyDiv === null) {
        return;
    }

    const latitudeData =  latitudeDiv.getAttribute('data-here-maps-latitude');
    const longitudeData = longitudeDiv.getAttribute('data-here-maps-longitude');
    const hereMapsApiKey = hereMapsApiKeyDiv.getAttribute('data-here-maps-api-key');

    if (latitudeData === null || longitudeData === null || hereMapsApiKey === null) {
        return;
    }

    const latitudeValue = parseFloat(latitudeData);
    const longitudeValue = parseFloat(longitudeData);
    
    let pixelRatio = window.devicePixelRatio || 1;
    let defaultIcon = new H.map.Icon("/images/purplemarker.svg", { size: { w: 36, h: 36 } });
    let platform = new H.service.Platform({
        'apikey': hereMapsApiKey,
        'useHTTPS': true
    });

    let maptypes = platform.createDefaultLayers({
        tileSize: pixelRatio === 1 ? 256 : 512,
        ppi: pixelRatio === 1 ? undefined : 320
    });

    let map = new H.Map(mapContainerDiv,
        maptypes.vector.normal.map,
        {
            zoom: 14,
            center: { lat: latitudeValue, lng: longitudeValue },
            pixelRatio: pixelRatio
        });
    let uiLang = 'en-US';
    if (languageId === 2) {
        uiLang = 'de-DE';
    }
    let ui = H.ui.UI.createDefault(map, maptypes);
    let behavior = new H.mapevents.Behavior(new H.mapevents.MapEvents(map));


    let marker = new H.map.Marker({ lat: latitudeValue, lng: longitudeValue }, {icon: defaultIcon });
    map.addObject(marker);
    

    
}