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

let map = new H.Map(document.getElementById('mapContainer'),
    maptypes.normal.map,
    {
        zoom: 14,
        center: { lng: @Model.Longtitude, lat: @Model.Latitude },
pixelRatio: pixelRatio
                    });
let behavior = new H.mapevents.Behavior(new H.mapevents.MapEvents(map));
let ui = H.ui.UI.createDefault(map, maptypes);

let marker = new H.map.Marker({ lat: @Model.Latitude.ToString(new CultureInfo("en-US")).Replace(',', '.'), lng: @Model.Longtitude.ToString(new CultureInfo("en-US")).Replace(',', '.') }, { icon: defaultIcon });
map.addObject(marker);

let reverseGeocodingParameters = {
    prox: '@Model.Latitude,  @Model.Longtitude, 32',
    mode: 'retrieveAddresses',
    maxresults: 1
};

function onGeoSuccess(result) {
    let location = result.Response.View[0].Result[0];
    let contextText = "";
    let streetName = "";
    let districtName = "";
    let cityName = "";
    let countyName = "";
    let stateName = "";
    if (location.Location.Address.Street !== undefined) {
        streetName = location.Location.Address.Street;
        contextText = contextText + location.Location.Address.Street;
    }
    if (location.Location.Address.District !== undefined) {
        districtName = location.Location.Address.District;
        if (districtName !== streetName) {
            contextText = contextText + " " + location.Location.Address.District;
        }
    }
    if (location.Location.Address.City !== undefined) {
        cityName = location.Location.Address.City;
        if (cityName !== districtName && districtName.indexOf(cityName) < 0) {
            contextText = contextText + ", " + location.Location.Address.City;
        }
    }
    if (location.Location.Address.County !== undefined) {
        countyName = location.Location.Address.County;
        if (countyName !== cityName && cityName.indexOf(countyName) < 0 && countyName.indexOf(cityName) < 0) {
            contextText = contextText + ", " + location.Location.Address.County;
        }
    }
    if (location.Location.Address.State !== undefined) {
        stateName = location.Location.Address.State;
        if (stateName !== cityName && stateName !== countyName) {
            contextText = contextText + ", " + location.Location.Address.State;
        }
    }
    if (location.Location.Address.AdditionalData[0].value !== undefined) {
        contextText = contextText + ", " + location.Location.Address.AdditionalData[0].value;
    }
    contextText = contextText.replace(/(^,)|(,$)/g, "").trim();
    ui.addBubble(new H.ui.InfoBubble({
        lat: location.Location.DisplayPosition.Latitude,
        lng: location.Location.DisplayPosition.Longitude
    }, { content: contextText }));
};

let geocoder = platform.getGeocodingService();
geocoder.reverseGeocode(
    reverseGeocodingParameters,
    onGeoSuccess,
    function (e) { console.log('Error in Reverse Geocode: ' + e); });