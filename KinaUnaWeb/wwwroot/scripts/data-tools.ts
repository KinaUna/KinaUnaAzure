import { AutoSuggestList } from './page-models.js';

export function getCurrentProgenyId(): number {
    const progenyIdDiv = document.querySelector<HTMLDivElement>('#progenyIdDiv');
    if (progenyIdDiv !== null) {
        const progenyIdDivData = progenyIdDiv.dataset.progenyId;
        if (progenyIdDivData) {
            return parseInt(progenyIdDivData);
        }
    }
    return 0;
}

export async function getTagsList(progenyId: number): Promise<AutoSuggestList> {
    let tagsList: AutoSuggestList = new AutoSuggestList(progenyId);

    const getTagsListParameters: AutoSuggestList = new AutoSuggestList(progenyId);

    await fetch('/Progeny/GetAllProgenyTags/', {
        method: 'POST',
        body: JSON.stringify(getTagsListParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (getTagsResponse) {
        tagsList = await getTagsResponse.json();
    }).catch(function (error) {
        console.log('Error loading tags autosuggestions. Error: ' + error);
    });

    return new Promise<AutoSuggestList>(function (resolve, reject) {
        resolve(tagsList);
    });
}

export async function getContextsList(progenyId: number): Promise<AutoSuggestList> {
    let contextsList: AutoSuggestList = new AutoSuggestList(progenyId);

    const getContextsListParameters: AutoSuggestList = new AutoSuggestList(progenyId);

    await fetch('/Progeny/GetAllProgenyContexts/', {
        method: 'POST',
        body: JSON.stringify(getContextsListParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (getContextsResponse) {
        contextsList = await getContextsResponse.json();
    }).catch(function (error) {
        console.log('Error loading contexts autosuggestions. Error: ' + error);
    });

    return new Promise<AutoSuggestList>(function (resolve, reject) {
        resolve(contextsList);
    });
}

export async function getLocationsList(progenyId: number): Promise<AutoSuggestList> {
    let locationsList: AutoSuggestList = new AutoSuggestList(progenyId);

    const getLocationsListParameters: AutoSuggestList = new AutoSuggestList(progenyId);

    await fetch('/Progeny/GetAllProgenyLocations/', {
        method: 'POST',
        body: JSON.stringify(getLocationsListParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (getLocationsResponse) {
        locationsList = await getLocationsResponse.json();
    }).catch(function (error) {
        console.log('Error loading locations autosuggestions. Error: ' + error);
    });

    return new Promise<AutoSuggestList>(function (resolve, reject) {
        resolve(locationsList);
    });
}

export async function getCategoriesList(progenyId: number): Promise<AutoSuggestList> {
    let categoriesList: AutoSuggestList = new AutoSuggestList(progenyId);

    const getCategoriesListParameters: AutoSuggestList = new AutoSuggestList(progenyId);

    await fetch('/Progeny/GetAllProgenyCategories/', {
        method: 'POST',
        body: JSON.stringify(getCategoriesListParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (getCategoriesResponse) {
        categoriesList = await getCategoriesResponse.json();
    }).catch(function (error) {
        console.log('Error loading categories autosuggestions. Error: ' + error);
    });

    return new Promise<AutoSuggestList>(function (resolve, reject) {
        resolve(categoriesList);
    });
}