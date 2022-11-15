let pixelRatio = window.devicePixelRatio || 1;
let iconWidth = Math.floor(36 * pixelRatio + (pixelRatio - 1) * 8);
let iconHeight = Math.floor(36 * pixelRatio + (pixelRatio - 1) * 8);
let defaultIcon = new H.map.Icon("/images/purplemarker.svg", { size: { w: iconWidth, h: iconHeight } });
let platform = new H.service.Platform({
    'app_id': 'bJjORf0UVOc5U4LjADgX',
    'app_code': 'GNDm65qsmujcmIK9-2X_Uw',
    'useHTTPS': true
});

let maptypes = platform.createDefaultLayers({
    tileSize: pixelRatio === 1 ? 256 : 512,
    ppi: pixelRatio === 1 ? undefined : 320
});

let kinaUnaLatitude = 0;
let kinaUnaLongitude = 0;
let map;
let behavior;
let ui;
let marker;
let reverseGeocodingParameters;

function initializeKinaUnaMap(latitude, longitude)
{
    kinaUnaLatitude = latitude;
    kinaUnaLongitude = longitude;

    map = new H.Map(document.getElementById('mapContainer'),
        maptypes.normal.map,
        {
            zoom: 14,
            center: { lng: kinaUnaLongitude, lat: kinaUnaLatitude },
            pixelRatio: pixelRatio
        });

    behavior = new H.mapevents.Behavior(new H.mapevents.MapEvents(map));
    ui = H.ui.UI.createDefault(map, maptypes);

    marker = new H.map.Marker({ lat: kinaUnaLatitude, lng: kinaUnaLongitude }, { icon: defaultIcon });
    map.addObject(marker);
}

