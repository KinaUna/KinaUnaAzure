import { popupLocationItem } from "./location-details.js";
/**
 * Adds an event listener to the copy location button to copy the selected location to the latitude and longitude fields.
 */
export function addCopyLocationButtonEventListener() {
    const copyLocationButton = document.querySelector('#copy-location-button');
    if (copyLocationButton !== null) {
        copyLocationButton.addEventListener('click', onCopyLocationButtonClicked);
    }
}
function onCopyLocationButtonClicked() {
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
}
/**
 * Setup the Here Maps API for the location page.
 * @param {number} languageId The id of the current language.
 */
export function setupHereMaps(languageId) {
    const mapContainerDiv = document.getElementById('here-map-container-div');
    const latitudeDiv = document.getElementById('here-maps-latitude-div');
    const longitudeDiv = document.getElementById('here-maps-longitude-div');
    const hereMapsApiKeyDiv = document.getElementById('here-maps-api-key-div');
    if (mapContainerDiv === null || latitudeDiv === null || longitudeDiv === null || hereMapsApiKeyDiv === null) {
        return;
    }
    const latitudeData = latitudeDiv.getAttribute('data-here-maps-latitude');
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
    let map = new H.Map(mapContainerDiv, maptypes.vector.normal.map, {
        zoom: 14,
        center: { lat: latitudeValue, lng: longitudeValue },
        pixelRatio: pixelRatio
    });
    let uiLang = 'en-US';
    if (languageId === 2) {
        uiLang = 'de-DE'; // No other languages used by KinaUna are supported by Here Maps.
    }
    let ui = H.ui.UI.createDefault(map, maptypes);
    let behavior = new H.mapevents.Behavior(new H.mapevents.MapEvents(map));
    let marker = new H.map.Marker({ lat: latitudeValue, lng: longitudeValue }, { icon: defaultIcon });
    map.addObject(marker);
}
export function setUpMapClickToShowLocationListener(map) {
    map.addEventListener('tap', function (evt) {
        if (evt.target instanceof H.map.Marker) {
            popupLocationItem(evt.target.getData());
        }
        if (evt.currentPointer != null) {
            let coord = map.screenToGeo(evt.currentPointer.viewportX, evt.currentPointer.viewportY);
            map.setCenter(coord, true);
        }
    });
}
/**
 * Setup the Here Maps API for the location page.
 * @param {number} languageId The id of the current language.
 */
export function setupHereMapsPhotoLocations(languageId) {
    const mapContainerDiv = document.getElementById('photo-locations-map-container-div');
    const hereMapsApiKeyDiv = document.getElementById('here-maps-api-key-div');
    if (mapContainerDiv === null || hereMapsApiKeyDiv === null) {
        return null;
    }
    const hereMapsApiKey = hereMapsApiKeyDiv.getAttribute('data-here-maps-api-key');
    if (hereMapsApiKey === null) {
        return null;
    }
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
    let map = new H.Map(mapContainerDiv, maptypes.vector.normal.map, {
        pixelRatio: pixelRatio
    });
    let uiLang = 'en-US';
    if (languageId === 2) {
        uiLang = 'de-DE'; // No other languages used by KinaUna are supported by Here Maps.
    }
    let ui = H.ui.UI.createDefault(map, maptypes);
    let behavior = new H.mapevents.Behavior(new H.mapevents.MapEvents(map));
    return map;
}
//# sourceMappingURL=location-tools.js.map