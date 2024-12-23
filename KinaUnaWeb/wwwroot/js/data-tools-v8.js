import { AutoSuggestList } from './page-models-v8.js';
let currentMomentLocale = 'en';
/** Gets the Id of the current Progeny from the progeny-id-div's data-progeny-id attribute's value.
  * @returns The Progeny's Id number.
  */
export function getCurrentProgenyId() {
    const progenyIdDiv = document.querySelector('#progeny-id-div');
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
export function getCurrentLanguageId() {
    const languageIdDiv = document.querySelector('#language-id-div');
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
export function setMomentLocale() {
    const currentMomentLocaleDiv = document.querySelector('#current-moment-locale-div');
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
export function getZebraDateTimeFormat(zebraDateTimeFormatElement = '#zebra-date-time-format-div') {
    const zebraDateTimeFormatDiv = document.querySelector(zebraDateTimeFormatElement);
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
export function getLongDateTimeFormatMoment() {
    const longDateTimeFormatMomentDiv = document.querySelector('#long-date-time-format-moment-div');
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
export async function getTagsList(progenies) {
    let tagsList = new AutoSuggestList(progenies);
    const getTagsListParameters = new AutoSuggestList(progenies);
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
/** Updates the autosuggest list for tags for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set tags for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setTagsAutoSuggestList(progenyIds, elementId = 'tag-list', whiteList = false, tagLimit = 0) {
    let tagListElement = document.getElementById(elementId);
    if (tagListElement !== null) {
        const tagsList = await getTagsList(progenyIds);
        $('#' + elementId).amsifySuggestags({
            suggestions: tagsList.suggestions,
            selectOnHover: false,
            printValues: false,
            whiteList: whiteList,
            tagLimit: tagLimit
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
/** Fetches the full auto-suggest list of contexts for a given progenyId.
 * @param progenyId The Id of the Progeny to get contexts for.
 * @returns The full list of contexts for the Progeny.
 */
export async function getContextsList(progenyIds) {
    let contextsList = new AutoSuggestList(progenyIds);
    const getContextsListParameters = new AutoSuggestList(progenyIds);
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
/** Updates the autosuggest list for contexts for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set contexts for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setContextAutoSuggestList(progenyIds, elementId = 'context-input', whiteList = false, tagLimit = 0) {
    let contextInputElement = document.getElementById(elementId);
    if (contextInputElement !== null) {
        const contextsList = await getContextsList(progenyIds);
        $('#' + elementId).amsifySuggestags({
            suggestions: contextsList.suggestions,
            selectOnHover: false,
            printValues: false,
            whiteList: whiteList,
            tagLimit: tagLimit
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
/** Fetches the full auto-suggest list of locations for a given progenyId.
 * @param progenyId The Id of the Progeny to get locations for.
 * @returns The full list of locations for the Progeny.
 */
export async function getLocationsList(progenyIds) {
    let locationsList = new AutoSuggestList(progenyIds);
    const getLocationsListParameters = new AutoSuggestList(progenyIds);
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
/** Updates the autosuggest list for locations for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set locations for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setLocationAutoSuggestList(progenyIds, elementId = 'location-input', whiteList = false, tagLimit = 5) {
    let locationInputElement = document.getElementById(elementId);
    if (locationInputElement !== null) {
        const locationsList = await getLocationsList(progenyIds);
        $('#' + elementId).amsifySuggestags({
            suggestions: locationsList.suggestions,
            selectOnHover: false,
            printValues: false,
            whiteList: whiteList,
            tagLimit: tagLimit
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
/** Fetches the full auto-suggest list of categories for a given progenyId.
 * @param progenyId The Id of the Progeny to get categories for.
 * @returns The full list of categories for the Progeny.
 */
export async function getCategoriesList(progenyIds) {
    let categoriesList = new AutoSuggestList(progenyIds);
    const getCategoriesListParameters = new AutoSuggestList(progenyIds);
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
/** Updates the autosuggest list for categories for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set categories for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setCategoriesAutoSuggestList(progenyIds, elementId = 'category-input', whiteList = false, tagLimit = 0) {
    let categoryInputElement = document.getElementById(elementId);
    if (categoryInputElement !== null) {
        const categoriesList = await getCategoriesList(progenyIds);
        $('#' + elementId).amsifySuggestags({
            suggestions: categoriesList.suggestions,
            selectOnHover: false,
            printValues: false,
            whiteList: whiteList,
            tagLimit: tagLimit
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
/** Fetches the full auto-suggest list of Vocabulary languages for a given progenyId.
 * @param progenyId The Id of the Progeny to get languages for.
 * @returns The full list of languages for the Progeny.
 */
export async function getVocabularyLanguagesList(progenyIds) {
    let languagesList = new AutoSuggestList(progenyIds);
    const getLanguagesListParameters = new AutoSuggestList(progenyIds);
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
    return new Promise(function (resolve, reject) {
        resolve(languagesList);
    });
}
/** Updates the autosuggest list for Vocabulary languages for a given progenyId, and cofigures the Amsify-Suggestags properties.
 * @param progenyId The Id of the Progeny to set languages for.
 * @param elementId The Id of the element to set the autosuggest list for.
 */
export async function setVocabularyLanguagesAutoSuggestList(progenyIds, elementId = 'vocabulary-languages-input', whiteList = false, tagLimit = 0) {
    let languageInputElement = document.getElementById(elementId);
    if (languageInputElement !== null) {
        const languagesList = await getVocabularyLanguagesList(progenyIds);
        $('#' + elementId).amsifySuggestags({
            suggestions: languagesList.suggestions,
            selectOnHover: false,
            printValues: false,
            whiteList: whiteList,
            tagLimit: tagLimit
        });
        const suggestInputElement = languageInputElement.querySelector('.amsify-suggestags-input');
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
/** Hides all items with the specified class by adding the d-none class.
 *  @param classToHide The class of elements to hide.
 */
export function hideItemsWithClass(classToHide) {
    const queryString = '.' + classToHide;
    const items = document.querySelectorAll(queryString);
    items.forEach((item) => {
        item.classList.add('d-none');
    });
}
/** Shows all items with the specified class by removing the d-none class.
 *  @param classToShow The class of elements to show.
 */
export function showItemsWithClass(classToShow) {
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
/** Compares the date and time of two date-time-pickers, shows a warning if the second time comes before the first.
 * @param startElementId The id of the start picker element.
 * @param endElementId The id of the end picker element.
 * @param pickerDateTimeFormatMoment The format string of the value used by the date-time-pickers.
 * @param warningStartIsAfterEndString The string to display if the end value is before the start value.
 * @returns True if the start time is before the end time, false if not.
 */
export function checkStartBeforeEndTime(startElementId, endElementId, pickerDateTimeFormatMoment, warningStartIsAfterEndString) {
    const startElement = document.querySelector(startElementId);
    const endElement = document.querySelector(endElementId);
    if (startElement === null || endElement === null) {
        return false;
    }
    let sTime = moment(startElement.value, pickerDateTimeFormatMoment);
    let eTime = moment(endElement?.value, pickerDateTimeFormatMoment);
    const notificationDiv = document.querySelector('#notification');
    const submitButton = document.querySelector('#submit-button');
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
}
;
/** Formats a date object as a string using the specified Moment formatting string.
 * @param date The date object to format.
 * @returns The formatted date string.
 */
export function getFormattedDateString(date, timeFormat = '') {
    if (timeFormat === '') {
        timeFormat = getLongDateTimeFormatMoment();
    }
    let timeString = moment(date).format(timeFormat);
    return timeString;
}
export function getDateFromFormattedString(dateString, timeFormat = '') {
    if (timeFormat === '') {
        timeFormat = getLongDateTimeFormatMoment();
    }
    let date = moment(dateString, timeFormat).toDate();
    return date;
}
export function dateStringFormatConverter(originalDateString, inputFormat, outputFormat) {
    let pickertime = moment.utc(originalDateString, inputFormat);
    let timeString = pickertime.format(outputFormat);
    return timeString;
}
export function setCopyContentEventListners() {
    let copyContentButtons = document.querySelectorAll('.copy-content-button');
    if (copyContentButtons) {
        copyContentButtons.forEach((button) => {
            button.addEventListener('click', async function () {
                if (!button.dataset.copyContentId) {
                    return;
                }
                let copyContentElement = document.getElementById(button.dataset.copyContentId);
                if (copyContentElement !== null) {
                    let copyText = copyContentElement.textContent;
                    if (copyText === null) {
                        return;
                    }
                    navigator.clipboard.writeText(copyText);
                    // Notify that the text has been copied to the clipboard.
                    let notificationSpan = button.querySelector('.toast-notification');
                    if (notificationSpan !== null) {
                        notificationSpan.classList.remove('d-none');
                        setTimeout(function () {
                            notificationSpan.classList.add('d-none');
                        }, 3000);
                    }
                }
            });
        });
    }
}
//# sourceMappingURL=data-tools-v8.js.map