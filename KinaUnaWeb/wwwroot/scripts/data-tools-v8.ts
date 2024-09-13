import { AutoSuggestList } from './page-models-v8.js';

declare let moment: any;
let currentMomentLocale: string = 'en';

/** Gets the Id of the current Progeny from the progeny-id-div's data-progeny-id attribute's value.
  * @returns The Progeny's Id number.
  */
export function getCurrentProgenyId(): number {
    const progenyIdDiv = document.querySelector<HTMLDivElement>('#progeny-id-div');
    if (progenyIdDiv !== null) {
        const progenyIdDivData = progenyIdDiv.dataset.progenyId;
        if (progenyIdDivData) {
            return parseInt(progenyIdDivData);
        }
    }
    return 0;
}

/** Gets the Id for the current language used from the language-id-div's data-current-locale attribute's value.
  * @returns The Progeny's Id number.
  */
export function getCurrentLanguageId(): number {
    const languageIdDiv = document.querySelector<HTMLDivElement>('#language-id-div');

    if (languageIdDiv !== null) {
        const languageIdData = languageIdDiv.dataset.languageId;
        if (languageIdData) {
            return parseInt(languageIdData);
        }
    }

    return 1;
}

/** Gets the locale to use for Moment functions from the current-moment-locale-div's data-current-locale attribute's value, then applies it to Moment.
  */
export function setMomentLocale(): void {
    const currentMomentLocaleDiv = document.querySelector<HTMLDivElement>('#current-moment-locale-div');

    if (currentMomentLocaleDiv !== null) {
        const currentLocaleData = currentMomentLocaleDiv.dataset.currentLocale;
        if (currentLocaleData) {
            currentMomentLocale = currentLocaleData;
        }
    }
    moment.locale(currentMomentLocale);
}

/** Gets the format to use with date-time-pickers from the zebra-date-time-format-div's data-zebra-date-time-format attribute's value.
  * @returns The date-time format string.
  */
export function getZebraDateTimeFormat(): string {
    const zebraDateTimeFormatDiv = document.querySelector<HTMLDivElement>('#zebra-date-time-format-div');
    if (zebraDateTimeFormatDiv !== null) {
        const zebraDateTimeFormatData = zebraDateTimeFormatDiv.dataset.zebraDateTimeFormat;
        if (zebraDateTimeFormatData) {
            return zebraDateTimeFormatData;
        }
    }

    return 'd-F-Y';
}

/** Gets the format to used to display long date-time string from the long-date-time-format-moment-div's data-long-date-time-format-moment attribute's value.
  * @returns The date-time format string.
  */
export function getLongDateTimeFormatMoment(): string {
    const longDateTimeFormatMomentDiv = document.querySelector<HTMLDivElement>('#long-date-time-format-moment-div');
    if (longDateTimeFormatMomentDiv !== null) {
        const longDateTimeFormatMomentData = longDateTimeFormatMomentDiv.dataset.longDateTimeFormatMoment;
        if (longDateTimeFormatMomentData) {
            return longDateTimeFormatMomentData;
        }
    }

    return 'DD-MMMM-YYYY HH:mm';
}

/** Fetches the full auto-suggest list of tags for a given progenyId.
 * @param progenyId The Id of the Progeny to get tags for.
 * @returns The full list of tags for the Progeny.
 */
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

/** Updates the autosuggest list for tags for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set tags for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setTagsAutoSuggestList(progenyId: number, elementId: string = 'tag-list'): Promise<void> {
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
/** Fetches the full auto-suggest list of contexts for a given progenyId.
 * @param progenyId The Id of the Progeny to get contexts for.
 * @returns The full list of contexts for the Progeny.
 */
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

/** Updates the autosuggest list for contexts for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set contexts for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setContextAutoSuggestList(progenyId: number, elementId: string = 'context-input'): Promise<void> {
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

/** Fetches the full auto-suggest list of locations for a given progenyId.
 * @param progenyId The Id of the Progeny to get locations for.
 * @returns The full list of locations for the Progeny.
 */
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

/** Updates the autosuggest list for locations for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set locations for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setLocationAutoSuggestList(progenyId: number, elementId: string = 'location-input'): Promise<void> {
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

/** Fetches the full auto-suggest list of categories for a given progenyId.
 * @param progenyId The Id of the Progeny to get categories for.
 * @returns The full list of categories for the Progeny.
 */
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

/** Updates the autosuggest list for categories for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set categories for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setCategoriesAutoSuggestList(progenyId: number, elementId: string = 'category-input'): Promise<void> {
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

/** Fetches the full auto-suggest list of Vocabulary languages for a given progenyId.
 * @param progenyId The Id of the Progeny to get languages for.
 * @returns The full list of languages for the Progeny.
 */
export async function getVocabularyLanguagesList(progenyId: number): Promise<AutoSuggestList> {
    let languagesList: AutoSuggestList = new AutoSuggestList(progenyId);

    const getLanguagesListParameters: AutoSuggestList = new AutoSuggestList(progenyId);

    await fetch('/Progeny/GetAllProgenyVocabularyLanguages/', {
        method: 'POST',
        body: JSON.stringify(getLanguagesListParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (getLanguagesResponse) {
        languagesList = await getLanguagesResponse.json();
    }).catch(function (error) {
        console.log('Error loading language autosuggestions. Error: ' + error);
    });

    return new Promise<AutoSuggestList>(function (resolve, reject) {
        resolve(languagesList);
    });
}

/** Updates the autosuggest list for Vocabulary languages for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set languages for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setVocabularyLanguagesAutoSuggestList(progenyId: number, elementId: string = 'vocabulary-languages-input'): Promise<void> {
    let languageInputElement = document.getElementById(elementId);
    if (languageInputElement !== null) {
        const languagesList = await getVocabularyLanguagesList(progenyId);

        ($('#' + elementId) as any).amsifySuggestags({
            suggestions: languagesList.suggestions,
            selectOnHover: false,
            printValues: false
        });

        const suggestInputElement = languageInputElement.querySelector<HTMLInputElement>('.amsify-suggestags-input');
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

/** Hides all items with the specified class by adding the d-none class.
 *  @param classToHide The class of elements to hide.
 */
export function hideItemsWithClass(classToHide: string): void {
    const queryString = '.' + classToHide;
    const items = document.querySelectorAll(queryString);
    items.forEach((item) => {
        item.classList.add('d-none');
    });
}
/** Shows all items with the specified class by removing the d-none class.
 *  @param classToShow The class of elements to show.
 */
export function showItemsWithClass(classToShow: string): void {
    const queryString = '.' + classToShow;
    const items = document.querySelectorAll(queryString);
    items.forEach((item) => {
        item.classList.remove('d-none');
    });
}

/** Toggles show/hide all elements with the class of the button element name.
 * If the button has the active class after toggling it, all elements are shown, else they are hidden.
 * @param button The toggle button
 */
export function updateFilterButtonDisplay(button: HTMLButtonElement): void {
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

/** Compares the date and time of two date-time-pickers, shows a warning if the second time comes before the first.
 * @param startElementId The id of the start picker element.
 * @param endElementId The id of the end picker element.
 * @param pickerDateTimeFormatMoment The format string of the value used by the date-time-pickers.
 * @param warningStartIsAfterEndString The string to display if the end value is before the start value.
 * @returns True if the start time is before the end time, false if not.
 */
export function checkStartBeforeEndTime(startElementId: string, endElementId: string, pickerDateTimeFormatMoment: string, warningStartIsAfterEndString: string): boolean {
    const startElement = document.querySelector<HTMLInputElement>(startElementId);
    const endElement = document.querySelector<HTMLInputElement>(endElementId);
    
    if (startElement === null || endElement === null) {
        return false;
    }

    let sTime: any = moment(startElement.value, pickerDateTimeFormatMoment);
    let eTime: any = moment(endElement?.value, pickerDateTimeFormatMoment);
    const notificationDiv = document.querySelector<HTMLDivElement>('#notification');
    const submitButton = document.querySelector<HTMLButtonElement>('#submit-button');
    if (sTime < eTime && sTime.isValid() && eTime.isValid()) {

        if (notificationDiv !== null) {
            notificationDiv.textContent = '';
        }

        if (submitButton !== null) {
            submitButton.disabled = false;
        }

        return true;

    }

    if (notificationDiv !== null) {
        notificationDiv.textContent = warningStartIsAfterEndString;
    }

    if (submitButton !== null) {
        submitButton.disabled = true;
    }

    return false;
};
/** Formats a date object as a string using the specified Moment formatting string.
 * @param date The date object to format.
 * @returns The formatted date string.
 */
export function getFormattedDateString(date: Date, timeFormat: string = ''): string {
    if (timeFormat === '') {
        timeFormat = getLongDateTimeFormatMoment();
    }
    let timeString: string = moment(date).format(timeFormat);
    return timeString;
}

