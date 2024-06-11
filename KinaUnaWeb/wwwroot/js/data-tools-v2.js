import { AutoSuggestList } from './page-models-v2.js';
let currentMomentLocale = 'en';
export function getCurrentProgenyId() {
    const progenyIdDiv = document.querySelector('#progenyIdDiv');
    if (progenyIdDiv !== null) {
        const progenyIdDivData = progenyIdDiv.dataset.progenyId;
        if (progenyIdDivData) {
            return parseInt(progenyIdDivData);
        }
    }
    return 0;
}
export function getCurrentLanguageId() {
    const languageIdDiv = document.querySelector('#languageIdDiv');
    if (languageIdDiv !== null) {
        const languageIdData = languageIdDiv.dataset.languageId;
        if (languageIdData) {
            return parseInt(languageIdData);
        }
    }
    return 1;
}
export function setMomentLocale() {
    const currentMomentLocaleDiv = document.querySelector('#currentMomentLocaleDiv');
    if (currentMomentLocaleDiv !== null) {
        const currentLocaleData = currentMomentLocaleDiv.dataset.currentLocale;
        if (currentLocaleData) {
            currentMomentLocale = currentLocaleData;
        }
    }
    moment.locale(currentMomentLocale);
}
export function getZebraDateTimeFormat() {
    const zebraDateTimeFormatDiv = document.querySelector('#zebraDateTimeFormatDiv');
    if (zebraDateTimeFormatDiv !== null) {
        const zebraDateTimeFormatData = zebraDateTimeFormatDiv.dataset.zebraDateTimeFormat;
        if (zebraDateTimeFormatData) {
            return zebraDateTimeFormatData;
        }
    }
    return 'd-F-Y';
}
export function getLongDateTimeFormatMoment() {
    const longDateTimeFormatMomentDiv = document.querySelector('#longDateTimeFormatMomentDiv');
    if (longDateTimeFormatMomentDiv !== null) {
        const longDateTimeFormatMomentData = longDateTimeFormatMomentDiv.dataset.longDateTimeFormatMoment;
        if (longDateTimeFormatMomentData) {
            return longDateTimeFormatMomentData;
        }
    }
    return 'DD-MMMM-YYYY HH:mm';
}
export async function getTagsList(progenyId) {
    let tagsList = new AutoSuggestList(progenyId);
    const getTagsListParameters = new AutoSuggestList(progenyId);
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
    return new Promise(function (resolve, reject) {
        resolve(tagsList);
    });
}
export async function setTagsAutoSuggestList(progenyId, elementId = 'tagList') {
    let tagListElement = document.getElementById(elementId);
    if (tagListElement !== null) {
        const tagsList = await getTagsList(progenyId);
        $('#' + elementId).amsifySuggestags({
            suggestions: tagsList.suggestions,
            selectOnHover: false,
            printValues: false
        });
        const suggestInputElement = tagListElement.querySelector('.amsify-suggestags-input');
        if (suggestInputElement !== null) {
            suggestInputElement.tabIndex = -1;
            suggestInputElement.addEventListener('keydown', function (event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    const originalInputElement = document.getElementById(elementId);
                    if (originalInputElement !== null) {
                        if (originalInputElement.value.length > 0) {
                            originalInputElement.value += ',';
                        }
                        originalInputElement.value += this.value;
                    }
                    return false;
                }
            });
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function getContextsList(progenyId) {
    let contextsList = new AutoSuggestList(progenyId);
    const getContextsListParameters = new AutoSuggestList(progenyId);
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
    return new Promise(function (resolve, reject) {
        resolve(contextsList);
    });
}
export async function setContextAutoSuggestList(progenyId, elementId = 'contextInput') {
    let contextInputElement = document.getElementById(elementId);
    if (contextInputElement !== null) {
        const contextsList = await getContextsList(progenyId);
        $('#' + elementId).amsifySuggestags({
            suggestions: contextsList.suggestions,
            selectOnHover: false,
            printValues: false
        });
        const suggestInputElement = contextInputElement.querySelector('.amsify-suggestags-input');
        if (suggestInputElement !== null) {
            suggestInputElement.tabIndex = -1;
            suggestInputElement.addEventListener('keydown', function (event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    const originalInputElement = document.getElementById(elementId);
                    if (originalInputElement !== null) {
                        if (originalInputElement.value.length > 0) {
                            originalInputElement.value += ',';
                        }
                        originalInputElement.value += this.value;
                    }
                    return false;
                }
            });
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function getLocationsList(progenyId) {
    let locationsList = new AutoSuggestList(progenyId);
    const getLocationsListParameters = new AutoSuggestList(progenyId);
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
    return new Promise(function (resolve, reject) {
        resolve(locationsList);
    });
}
export async function setLocationAutoSuggestList(progenyId, elementId = 'locationInput') {
    let locationInputElement = document.getElementById(elementId);
    if (locationInputElement !== null) {
        const locationsList = await getLocationsList(progenyId);
        $('#' + elementId).amsifySuggestags({
            suggestions: locationsList.suggestions,
            selectOnHover: false,
            printValues: false,
            tagLimit: 5
        });
        const suggestInputElement = locationInputElement.querySelector('.amsify-suggestags-input');
        if (suggestInputElement !== null) {
            suggestInputElement.tabIndex = -1;
            suggestInputElement.addEventListener('keydown', function (event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    const originalInputElement = document.getElementById(elementId);
                    if (originalInputElement !== null) {
                        if (originalInputElement.value.length > 0) {
                            originalInputElement.value += ',';
                        }
                        originalInputElement.value += this.value;
                    }
                    return false;
                }
            });
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function getCategoriesList(progenyId) {
    let categoriesList = new AutoSuggestList(progenyId);
    const getCategoriesListParameters = new AutoSuggestList(progenyId);
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
    return new Promise(function (resolve, reject) {
        resolve(categoriesList);
    });
}
export async function setCategoriesAutoSuggestList(progenyId, elementId = 'categoryInput') {
    let categoryInputElement = document.getElementById(elementId);
    if (categoryInputElement !== null) {
        const categoriesList = await getCategoriesList(progenyId);
        $('#' + elementId).amsifySuggestags({
            suggestions: categoriesList.suggestions,
            selectOnHover: false,
            printValues: false
        });
        const suggestInputElement = categoryInputElement.querySelector('.amsify-suggestags-input');
        if (suggestInputElement !== null) {
            suggestInputElement.tabIndex = -1;
            suggestInputElement.addEventListener('keydown', function (event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    const originalInputElement = document.getElementById(elementId);
                    if (originalInputElement !== null) {
                        if (originalInputElement.value.length > 0) {
                            originalInputElement.value += ',';
                        }
                        originalInputElement.value += this.value;
                    }
                    return false;
                }
            });
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export function hideItemsWithClass(classToHide) {
    const queryString = '.' + classToHide;
    const items = document.querySelectorAll(queryString);
    items.forEach((item) => {
        item.classList.add('d-none');
    });
}
export function showItemsWithClass(classToShow) {
    const queryString = '.' + classToShow;
    const items = document.querySelectorAll(queryString);
    items.forEach((item) => {
        item.classList.remove('d-none');
    });
}
export function updateFilterButtonDisplay(button) {
    const iconElement = button.querySelector('.checkbox-icon');
    if (!button.classList.contains('active') && iconElement !== null) {
        iconElement.classList.value = '';
        iconElement.classList.add('checkbox-icon');
        iconElement.classList.add('fas');
        iconElement.classList.add('fa-check-square');
        button.classList.add('active');
        showItemsWithClass(button.name);
    }
    else {
        if (iconElement !== null) {
            iconElement.classList.value = '';
            iconElement.classList.add('checkbox-icon');
            iconElement.classList.add('fas');
            iconElement.classList.add('fa-square');
            button.classList.remove('active');
            hideItemsWithClass(button.name);
        }
    }
}
export function checkTimes(longDateTimeFormatMoment, warningStartIsAfterEndString) {
    let sTime = moment($('#datetimepicker1').val(), longDateTimeFormatMoment);
    let eTime = moment($('#datetimepicker2').val(), longDateTimeFormatMoment);
    if (sTime < eTime && sTime.isValid() && eTime.isValid()) {
        $('#notification').text('');
        $('#submitBtn').prop('disabled', false);
    }
    else {
        $('#submitBtn').prop('disabled', true);
        $('#notification').text(warningStartIsAfterEndString);
    }
    ;
}
;
export function getTimeLineStartDate(longDateTimeFormatMoment) {
    let timelineStartTimeString = moment($('#timeline-start-date-datetimepicker').val(), longDateTimeFormatMoment);
    return timelineStartTimeString;
}
export function getFormattedDateString(date, timeFormat) {
    let timeString = moment(date).format(timeFormat);
    return timeString;
}
//# sourceMappingURL=data-tools-v2.js.map