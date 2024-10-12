import { getCurrentProgenyId } from '../data-tools-v8.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { LocationItem, LocationsPageParameters, NearByPhotosRequest, NearByPhotosResponse, Picture, PicturesLocationsRequest, PicturesLocationsResponse, TimeLineItemViewModel, TimelineItem } from '../page-models-v8.js';
import { setUpMapClickToShowLocationListener, setupHereMaps, setupHereMapsPhotoLocations } from './location-tools.js';
import * as SettingsHelper from '../settings-tools-v8.js';
import { getSelectedProgenies } from '../settings-tools-v8.js';

const photoLocationsPageSettingsStorageKey = 'photo_locations_page_parameters';
const nearByPhotosSettingsStorageKey = 'near_by_photos_parameters';
let locationsPageParameters = new LocationsPageParameters();
let picturesLocationsRequest: PicturesLocationsRequest = new PicturesLocationsRequest();
let nearByPhotosRequest: NearByPhotosRequest = new NearByPhotosRequest();
let picturesList: Picture[] = [];
let picturesShown: number = 0;
let photoLocationsProgenyId: number;
let languageId = 1;
let selectedLocation: LocationItem;
let firstRun: boolean = true;
let map: H.Map | null;
let defaultIcon = new H.map.Icon("/images/purplemarker.svg", { size: { w: 36, h: 36 } });
let group: H.map.Group = new H.map.Group();
const sortAscendingSettingsButton = document.querySelector<HTMLButtonElement>('#setting-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector<HTMLButtonElement>('#setting-sort-descending-button');

/** Shows the loading spinner in the loading-timeline-items-div.
 */
function startLoadingSpinner(): void {
    startLoadingItemsSpinner('loading-photos-div');
}

/** Hides the loading spinner in the loading-timeline-items-div.
 */
function stopLoadingSpinner(): void {
    stopLoadingItemsSpinner('loading-photos-div');
}

async function getPicturesLocationsList(): Promise<void> {
    startLoadingSpinner();

    picturesLocationsRequest.progenyId = photoLocationsProgenyId;
    picturesLocationsRequest.progenies = locationsPageParameters.progenies;

    // Clear map markers
    if (group)
    {
        group.removeAll();
    }

    await fetch('/Pictures/GetPicturesLocations/', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(picturesLocationsRequest)
    }).then(async function (getLocationsListResult) {
        if (getLocationsListResult != null) {
            const newLocationsList = (await getLocationsListResult.json()) as PicturesLocationsResponse;
            if (newLocationsList.locationsList.length > 0) {
                await processLocationsList(newLocationsList.locationsList);
            }
        }
    }).catch(function (error) {
        console.log('Error loading pictures locations. Error: ' + error);
    });

    stopLoadingSpinner();
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });

}

async function processLocationsList(locationsList: LocationItem[]): Promise<void> {
    if (map === null) return;

    group = new H.map.Group();

    for (const locationItem of locationsList) {
        let marker = new H.map.Marker({ lat: locationItem.latitude, lng: locationItem.longitude }, { icon: defaultIcon });
        group.addObject(marker);
    }

    map.addObject(group);
    if (firstRun) {
        firstRun = false;
        map.getViewModel().setLookAtData({ bounds: group.getBoundingBox() });
    }
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function getPicturesNearLocation(locationItem: LocationItem): Promise<void> {
    nearByPhotosRequest.locationItem = locationItem;
    nearByPhotosRequest.progenyId = photoLocationsProgenyId;
    nearByPhotosRequest.progenies = locationsPageParameters.progenies;
    picturesShown = 0;
    await fetch('/Pictures/GetPicturesNearLocation/', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(nearByPhotosRequest)
    }).then(async function (getPicturesListResult) {
        if (getPicturesListResult != null) {
            const picturesListResult = (await getPicturesListResult.json()) as NearByPhotosResponse;
            picturesList = picturesListResult.picturesList;
            if (picturesListResult.picturesList.length > 0) {
                await processPicturesNearLocation();
            }
        }
    }).catch(function (error) {
        console.log('Error loading pictures list. Error: ' + error);
    });


    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function processPicturesNearLocation(): Promise<void> {
    startLoadingSpinner();
    const photoItemsParentDiv = document.querySelector<HTMLDivElement>('#photo-items-parent-div');
    if (photoItemsParentDiv !== null) {
        photoItemsParentDiv.classList.remove('d-none');
    }

    const morePicturesButton = document.getElementById('more-pictures-button');
    if (morePicturesButton !== null) {
        morePicturesButton.classList.add('d-none');
    }
    
    let count: number = 0;
    const endCount: number = picturesShown + nearByPhotosRequest.numberOfPictures;
    for await (const timelineItemToAdd of picturesList) {
        if (count >= picturesShown && count < endCount) {
            const pictureTimelineItem = new TimelineItem();
            pictureTimelineItem.itemId = timelineItemToAdd.pictureId.toString();
            pictureTimelineItem.itemType = 1;
            pictureTimelineItem.progenyId = photoLocationsProgenyId;
            await renderTimelineItem(pictureTimelineItem);
            picturesShown++;            
        }
        count++;       
    };

    stopLoadingSpinner();

    if (picturesShown < picturesList.length) {
        
        if (morePicturesButton !== null) {
            morePicturesButton.classList.remove('d-none');
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}


/** Fetches the HTML for a given timeline item and renders it at the end of timeline-items-div.
* @param timelineItem The timelineItem object to add to the page.
*/
async function renderTimelineItem(timelineItem: TimelineItem): Promise<void> {
    const timeLineItemViewModel: TimeLineItemViewModel = new TimeLineItemViewModel();
    timeLineItemViewModel.typeId = timelineItem.itemType;
    timeLineItemViewModel.itemId = parseInt(timelineItem.itemId);

    const getTimelineElementResponse = await fetch('/Timeline/GetTimelineItemElement', {
        method: 'POST',
        body: JSON.stringify(timeLineItemViewModel),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    });

    if (getTimelineElementResponse.ok && getTimelineElementResponse.text !== null) {
        const timelineElementHtml = await getTimelineElementResponse.text();
        const timelineDiv = document.querySelector<HTMLDivElement>('#photo-items-div');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
            addTimelineItemEventListener(timelineItem);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function getLocationsPageParameters(): void {
    const pageParametersDiv = document.querySelector<HTMLDivElement>('#locations-page-parameters');
    if (pageParametersDiv !== null) {
        const locationsPageParametersJson = pageParametersDiv.getAttribute('data-locations-page-parameters');
        if (locationsPageParametersJson !== null) {
            locationsPageParameters = JSON.parse(locationsPageParametersJson) as LocationsPageParameters;
            languageId = locationsPageParameters.languageId;
            nearByPhotosRequest.sortOrder = locationsPageParameters.sort;
            nearByPhotosRequest.numberOfPictures = locationsPageParameters.itemsPerPage;
            nearByPhotosRequest.sortOrder = locationsPageParameters.sort;
        }
    }
}

function setUpMap() {
    if (map === null) return;
    map.addEventListener('tap',
        async function (evt: any) {
            if (map !== null && evt.currentPointer != null) {
                let coord = map.screenToGeo(evt.currentPointer.viewportX, evt.currentPointer.viewportY);
                map.setCenter(coord, true);
            }
            // Clear pictures div
            const photosDiv = document.getElementById('photo-items-div');
            if (photosDiv !== null) {
                photosDiv.innerHTML = "";
            }

            if (evt.target instanceof H.map.Marker) {
                const locationTapped: LocationItem = new LocationItem();
                locationTapped.latitude = evt.target.hm.lat;
                locationTapped.longitude = evt.target.hm.lng;
                locationTapped.progenyId = photoLocationsProgenyId;

                selectedLocation = locationTapped;
                // scroll to photosDiv
                const photoItemsParentDiv = document.querySelector<HTMLDivElement>('#photo-items-parent-div');
                if (photoItemsParentDiv !== null) {
                    photoItemsParentDiv.scrollIntoView({ behavior: 'smooth' });
                }

                await getPicturesNearLocation(locationTapped);
            }
        });
}

function sortPicturesAscending() {
    nearByPhotosRequest.sortOrder = 0;
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
}

function sortPicturesDescending() {
    nearByPhotosRequest.sortOrder = 1;
    sortDescendingSettingsButton?.classList.add('active');
    sortAscendingSettingsButton?.classList.remove('active');
}

async function savePhotoLocationsPageSettings() {
    const numberOfItemsToGetSelect = document.querySelector<HTMLSelectElement>('#items-per-page-select');
    if (numberOfItemsToGetSelect !== null) {
        nearByPhotosRequest.numberOfPictures = parseInt(numberOfItemsToGetSelect.value);
    }
    else {
        nearByPhotosRequest.numberOfPictures = 10;
    }

    const locationsDistanceSelect = document.querySelector<HTMLSelectElement>('#locations-distance-select');
    if (locationsDistanceSelect !== null) {
        picturesLocationsRequest.distance = parseInt(locationsDistanceSelect.value) / 1000;
    }
    else {
        picturesLocationsRequest.distance = 0.25;        
    }
    nearByPhotosRequest.distance = picturesLocationsRequest.distance;

    SettingsHelper.savePageSettings<PicturesLocationsRequest>(photoLocationsPageSettingsStorageKey, picturesLocationsRequest);
    SettingsHelper.savePageSettings<NearByPhotosRequest>(nearByPhotosSettingsStorageKey, nearByPhotosRequest);
    SettingsHelper.toggleShowPageSettings();

    nearByPhotosRequest.distance = picturesLocationsRequest.distance * 1.1; // Add 10% to the distance for the near by photos, to ensure all areas are covered.
    await getPicturesLocationsList();
}

function loadPhotoLocationsPageSettings() {
    const savedSettings = SettingsHelper.getPageSettings<PicturesLocationsRequest>(photoLocationsPageSettingsStorageKey);
    if (savedSettings !== null) {
        picturesLocationsRequest.distance = savedSettings.distance?? 0.25;
    }

    const savedNearByPhotosSettings = SettingsHelper.getPageSettings<NearByPhotosRequest>(nearByPhotosSettingsStorageKey);
    if (savedNearByPhotosSettings !== null) {
        nearByPhotosRequest.distance = savedNearByPhotosSettings.distance ?? 0.25;
        nearByPhotosRequest.sortOrder = savedNearByPhotosSettings.sortOrder ?? 1;
        nearByPhotosRequest.numberOfPictures = savedNearByPhotosSettings.numberOfPictures ?? 10;
    }

    if (nearByPhotosRequest.sortOrder == 0) {
        sortPicturesAscending();
    }
    else {
        sortPicturesDescending();
    }

    // update items per page select
    const selectItemsPerPageElement = document.querySelector<HTMLSelectElement>('#items-per-page-select');
    if (selectItemsPerPageElement !== null) {
        selectItemsPerPageElement.value = nearByPhotosRequest.numberOfPictures?.toString();
        ($(".selectpicker") as any).selectpicker('refresh');
    }

    // update distance select
    const distanceSelectInput = document.querySelector<HTMLSelectElement>('#locations-distance-select');
    if (distanceSelectInput !== null) {
        distanceSelectInput.value = (nearByPhotosRequest.distance * 1000).toString();
        ($(".selectpicker") as any).selectpicker('refresh');
    }
}

function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            locationsPageParameters.progenies = getSelectedProgenies();
            await getPicturesLocationsList();
        }

    });
}



document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    photoLocationsProgenyId = getCurrentProgenyId();
    getLocationsPageParameters();
    loadPhotoLocationsPageSettings();
    addSelectedProgeniesChangedEventListener();
    locationsPageParameters.progenies = getSelectedProgenies();

    map = setupHereMapsPhotoLocations(locationsPageParameters.languageId);
    setUpMap();

    const photoLocationsSaveSettingsButton = document.querySelector<HTMLButtonElement>('#photo-locations-page-save-settings-button');
    if (photoLocationsSaveSettingsButton !== null) {
        photoLocationsSaveSettingsButton.addEventListener('click', savePhotoLocationsPageSettings);
    }

    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortPicturesAscending);
        sortDescendingSettingsButton.addEventListener('click', sortPicturesDescending);
    }

    SettingsHelper.initPageSettings();

    await getPicturesLocationsList();

    const morePicturesButton = document.getElementById('more-pictures-button');
    if (morePicturesButton !== null) {
        morePicturesButton.addEventListener('click', async function () {
            await processPicturesNearLocation();
        });
    }

    window.addEventListener('resize', () => map?.getViewPort().resize());
    map?.getViewPort().resize();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});