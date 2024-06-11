import { AutoSuggestList } from './page-models-v2.js';

declare let moment: any;
let currentMomentLocale: string = 'en';

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

export function getCurrentLanguageId(): number {
    const languageIdDiv = document.querySelector<HTMLDivElement>('#languageIdDiv');

    if (languageIdDiv !== null) {
        const languageIdData = languageIdDiv.dataset.languageId;
        if (languageIdData) {
            return parseInt(languageIdData);
        }
    }

    return 1;
}

export function setMomentLocale() {
    const currentMomentLocaleDiv = document.querySelector<HTMLDivElement>('#currentMomentLocaleDiv');

    if (currentMomentLocaleDiv !== null) {
        const currentLocaleData = currentMomentLocaleDiv.dataset.currentLocale;
        if (currentLocaleData) {
            currentMomentLocale = currentLocaleData;
        }
    }
    moment.locale(currentMomentLocale);
}

export function getZebraDateTimeFormat() {
    const zebraDateTimeFormatDiv = document.querySelector<HTMLDivElement>('#zebraDateTimeFormatDiv');
    if (zebraDateTimeFormatDiv !== null) {
        const zebraDateTimeFormatData = zebraDateTimeFormatDiv.dataset.zebraDateTimeFormat;
        if (zebraDateTimeFormatData) {
            return zebraDateTimeFormatData;
        }
    }

    return 'd-F-Y';
}

export function getLongDateTimeFormatMoment() {
    const longDateTimeFormatMomentDiv = document.querySelector<HTMLDivElement>('#longDateTimeFormatMomentDiv');
    if (longDateTimeFormatMomentDiv !== null) {
        const longDateTimeFormatMomentData = longDateTimeFormatMomentDiv.dataset.longDateTimeFormatMoment;
        if (longDateTimeFormatMomentData) {
            return longDateTimeFormatMomentData;
        }
    }

    return 'DD-MMMM-YYYY HH:mm';
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

export async function setTagsAutoSuggestList(progenyId: number, elementId: string = 'tagList') {
    let tagListElement = document.getElementById(elementId);
    if (tagListElement !== null) {
        const tagsList = await getTagsList(progenyId);
                
        ($('#' + elementId) as any).amsifySuggestags({
            suggestions: tagsList.suggestions,
            selectOnHover: false,
            printValues: false
        });

        const suggestInputElement = tagListElement.querySelector<HTMLInputElement>('.amsify-suggestags-input');
        if (suggestInputElement !== null) {
            suggestInputElement.tabIndex =-1;
            suggestInputElement.addEventListener('keydown', function(this, event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    const originalInputElement = document.getElementById(elementId) as HTMLInputElement;
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
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

export async function setContextAutoSuggestList(progenyId: number, elementId: string = 'contextInput') {
    let contextInputElement = document.getElementById(elementId);
    if (contextInputElement !== null) {
        const contextsList = await getContextsList(progenyId);

        ($('#' + elementId) as any).amsifySuggestags({
            suggestions: contextsList.suggestions,
            selectOnHover: false,
            printValues: false
        });

        const suggestInputElement = contextInputElement.querySelector<HTMLInputElement>('.amsify-suggestags-input');
        if (suggestInputElement !== null) {
            suggestInputElement.tabIndex = -1;
            suggestInputElement.addEventListener('keydown', function (this, event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    const originalInputElement = document.getElementById(elementId) as HTMLInputElement;
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
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

export async function setLocationAutoSuggestList(progenyId: number, elementId: string = 'locationInput') {
    let locationInputElement = document.getElementById(elementId);
    if (locationInputElement !== null) {
        const locationsList = await getLocationsList(progenyId);

        ($('#' + elementId) as any).amsifySuggestags({
            suggestions: locationsList.suggestions,
            selectOnHover: false,
            printValues: false,
            tagLimit: 5
        });

        const suggestInputElement = locationInputElement.querySelector<HTMLInputElement>('.amsify-suggestags-input');
        if (suggestInputElement !== null) {
            suggestInputElement.tabIndex = -1;
            suggestInputElement.addEventListener('keydown', function (this, event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    const originalInputElement = document.getElementById(elementId) as HTMLInputElement;
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
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

export async function setCategoriesAutoSuggestList(progenyId: number, elementId: string = 'categoryInput') {
    let categoryInputElement = document.getElementById(elementId);
    if (categoryInputElement !== null) {
        const categoriesList = await getCategoriesList(progenyId);

        ($('#' + elementId) as any).amsifySuggestags({
            suggestions: categoriesList.suggestions,
            selectOnHover: false,
            printValues: false
        });

        const suggestInputElement = categoryInputElement.querySelector<HTMLInputElement>('.amsify-suggestags-input');
        if (suggestInputElement !== null) {
            suggestInputElement.tabIndex = -1;
            suggestInputElement.addEventListener('keydown', function (this, event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    const originalInputElement = document.getElementById(elementId) as HTMLInputElement;
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

export function hideItemsWithClass(classToHide: string) {
    const queryString = '.' + classToHide;
    const items = document.querySelectorAll(queryString);
    items.forEach((item) => {
        item.classList.add('d-none');
    });
}

export function showItemsWithClass(classToShow: string) {
    const queryString = '.' + classToShow;
    const items = document.querySelectorAll(queryString);
    items.forEach((item) => {
        item.classList.remove('d-none');
    });
}

export function updateFilterButtonDisplay(button: HTMLButtonElement) {
    const iconElement = button.querySelector<HTMLSpanElement>('.checkbox-icon');
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

export function checkTimes(longDateTimeFormatMoment: string, warningStartIsAfterEndString: string) {
    let sTime: any = moment($('#datetimepicker1').val(), longDateTimeFormatMoment);
    let eTime: any = moment($('#datetimepicker2').val(), longDateTimeFormatMoment);
    if (sTime < eTime && sTime.isValid() && eTime.isValid()) {
        $('#notification').text('');
        $('#submitBtn').prop('disabled', false);

    } else {
        $('#submitBtn').prop('disabled', true);
        $('#notification').text(warningStartIsAfterEndString);
    };
};

export function getTimeLineStartDate(longDateTimeFormatMoment: string): string {
    let timelineStartTimeString: any = moment($('#timeline-start-date-datetimepicker').val(), longDateTimeFormatMoment);
    return timelineStartTimeString;
}

export function getFormattedDateString(date: Date, timeFormat: string): string {
    let timeString: string = moment(date).format(timeFormat);
    return timeString;
}

