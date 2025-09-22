import * as LocaleHelper from '../localization-v9.js';
import { PictureViewModel, PicturesPageParameters, TimeLineType, TimelineItem } from '../page-models-v9.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getFormattedDateString } from '../data-tools-v9.js';
import * as SettingsHelper from '../settings-tools-v9.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v9.js';
import { addTimelineItemEventListener, showPopupAtLoad } from '../item-details/items-display-v9.js';
import { getSelectedProgenies } from '../settings-tools-v9.js';
let picturesPageParameters = new PicturesPageParameters();
let popupPictureId = 0;
const picturesPageSettingsStorageKey = 'pictures_page_parameters';
let languageId = 1;
let picturesPageProgenyId;
let zebraDateTimeFormat;
let startDateTimeFormatMoment;
let firstRun = true;
let currentSort = 1;
let currentStartYear = 0;
let currentStartMonth = 0;
let currentStartDay = 0;
const photoListDiv = document.querySelector('#photo-list-div');
let sortAscendingSettingsButton = document.querySelector('#settings-sort-ascending-button');
let sortDescendingSettingsButton = document.querySelector('#setting-sort-descending-button');
let settingsStartDateTimePicker = $('#settings-start-date-datetimepicker');
let startLabelDiv = document.querySelector('#start-label-div');
/** Reads the initial page parameters from json serialized data in the pictures-page-parameters div elements data-pictures-page-parameters attribute.
 * If the page is navigated to without specific parameters, itemsPerPage, sort, and sortTags parameters are loaded from local storage.
 * @returns The PicturesPageParameters object with the initial page parameters.
 */
function getPageParametersFromPageData() {
    const picturesPageParametersDiv = document.querySelector('#pictures-page-parameters');
    let picturesPageParametersResult = new PicturesPageParameters();
    if (picturesPageParametersDiv !== null) {
        const pageParametersString = picturesPageParametersDiv.dataset.picturesPageParameters;
        if (pageParametersString) {
            let picturesPageParametersFromPageData = JSON.parse(pageParametersString);
            if (picturesPageParametersFromPageData !== null) {
                picturesPageParametersResult = picturesPageParametersFromPageData;
                // If year, month and day are all 0, it means the page was navigated to without specifying paramters to use, so load them from storage.
                if (picturesPageParametersResult.year === 0 && picturesPageParametersResult.month === 0 && picturesPageParametersResult.day === 0) {
                    const pageSettingsFromStorage = loadPicturesPageParametersFromStorage();
                    if (pageSettingsFromStorage !== null) {
                        pageSettingsFromStorage.currentPageNumber = 1;
                        pageSettingsFromStorage.progenyId = picturesPageParametersFromPageData.progenyId;
                        pageSettingsFromStorage.languageId = picturesPageParametersFromPageData.languageId;
                        pageSettingsFromStorage.tagFilter = picturesPageParametersFromPageData.tagFilter;
                        pageSettingsFromStorage.year = picturesPageParametersFromPageData.year;
                        pageSettingsFromStorage.month = picturesPageParametersFromPageData.month;
                        pageSettingsFromStorage.day = picturesPageParametersFromPageData.day;
                        if (pageSettingsFromStorage.itemsPerPage === null) {
                            pageSettingsFromStorage.itemsPerPage = 10;
                        }
                        if (pageSettingsFromStorage.sort === null) {
                            pageSettingsFromStorage.sort = 1;
                        }
                        if (pageSettingsFromStorage.sortTags === null) {
                            pageSettingsFromStorage.sortTags = 0;
                        }
                        picturesPageParametersResult = pageSettingsFromStorage;
                    }
                }
                // Override progenyId, it should never be retrieved from storage on initial page loads.
                picturesPageParametersResult.progenyId = picturesPageParametersFromPageData.progenyId;
            }
            if (picturesPageParametersResult === null) {
                picturesPageParametersResult = new PicturesPageParameters();
            }
        }
    }
    if (picturesPageParametersResult.sort > 1) {
        picturesPageParametersResult.sort = 1;
    }
    if (picturesPageParametersResult.sort === 0) {
        sortPicturesAscending();
    }
    else {
        sortPicturesDescending();
    }
    return picturesPageParametersResult;
}
/** Shows the loading spinner in the loading-items-div.
 */
function runLoadingSpinner() {
    const loadingItemsParent = document.querySelector('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.remove('d-none');
        startLoadingItemsSpinner('loading-items-div');
    }
}
/** Hides the loading spinner in the loading-items-div.
 */
function stopLoadingSpinner() {
    const loadingItemsParent = document.querySelector('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.add('d-none');
        stopLoadingItemsSpinner('loading-items-div');
    }
}
/** Retrieves the list of pictures, based on the parameters provided, then updates the page.
 *
 * @param parameters The PicturesPageParameters object with the parameters to use for the query.
 * @param updateHistory If updateHistory is true the browser history is updated to reflect the current page. If false it is assumed the page was loaded from history or reload, and is already in the history stack.
 * @returns The paramters object, with updated data for firstItemYear, totalPages, totalItems,
 */
async function getPicturesList(parameters, updateHistory = true) {
    runLoadingSpinner();
    if (parameters.sort > 1) {
        parameters.sort = 1;
    }
    // These variables are used to check if setting have been changed and update the page number accordingly.
    currentSort = parameters.sort;
    currentStartYear = parameters.year;
    currentStartMonth = parameters.month;
    currentStartDay = parameters.day;
    // Update the url in the browser to reflect the current page, in case of a page reload.
    if (firstRun) {
        setBrowserUrl(parameters, true);
    }
    await fetch('/Pictures/GetPictureList', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(parameters)
    }).then(async function (getItemsListResult) {
        if (getItemsListResult != null) {
            const newItemsList = (await getItemsListResult.json());
            updateActiveTagFilterDiv(parameters);
            if (newItemsList.pictureItems.length > 0) {
                parameters = await processPicturesList(newItemsList, parameters);
                if (updateHistory) {
                    setBrowserUrl(parameters, false);
                }
            }
        }
    }).catch(function (error) {
        console.log('Error loading Pictures list. Error: ' + error);
    });
    stopLoadingSpinner();
    return new Promise(function (resolve, reject) {
        resolve(parameters);
    });
}
/**
 * Updates the parameters with the data from the newItemsList, then replaces the pictures on the page with the items in the newItemsList.
 * Also makes calls to update the navigation elements and tags list.
 * @param newItemsList The PicturesList object with an array of pictures to render, and updated data for firstItemYear.
 * @param parameters The current PicturePageParameters for the page.
 * @returns The updated PicturePageParameters object.
 */
async function processPicturesList(newItemsList, parameters) {
    parameters.totalPages = newItemsList.totalPages;
    parameters.totalItems = newItemsList.allItemsCount;
    parameters.currentPageNumber = newItemsList.currentPageNumber;
    // If this is the initial page load and pictures are sorted in ascending order, check if year for the earliest picture needs to be updated.
    if (firstRun && parameters.sort === 0 && parameters.firstItemYear !== newItemsList.firstItemYear) {
        parameters.firstItemYear = newItemsList.firstItemYear;
        parameters.year = newItemsList.firstItemYear;
        parameters.month = 1;
        parameters.day = 1;
        setStartDatePicker(new Date(parameters.year, parameters.month - 1, parameters.day));
    }
    else {
        firstRun = false;
    }
    // If the year is 0, the start date hasn't been initialized, so set to current date.
    if (parameters.year === 0) {
        let currentDate = new Date();
        parameters.year = currentDate.getFullYear();
        parameters.month = currentDate.getMonth() + 1;
        parameters.day = currentDate.getDate();
    }
    parameters.firstItemYear = newItemsList.firstItemYear;
    updateSettingsNotificationDiv(parameters.sort);
    updateNavigationDiv(parameters);
    const photoListParentDiv = document.querySelector('#photo-list-parent-div');
    if (photoListParentDiv !== null) {
        photoListParentDiv.classList.remove('d-none');
    }
    for await (const itemToAdd of newItemsList.pictureItems) {
        await renderPictureItem(itemToAdd);
    }
    ;
    updateTagsListDiv(newItemsList.tagsList, parameters.sortTags);
    parameters.progenies = getSelectedProgenies();
    return new Promise(function (resolve, reject) {
        resolve(parameters);
    });
}
/** Sets the url in the browser address bar to reflect the current page.
 * @param parameters The PicturesPageParameters currently in use.
 * @param replaceState If true, the current url will replace the url in the active one in history, if false the url will be added to the history.
 */
function setBrowserUrl(parameters, replaceState) {
    const url = new URL(window.location.href);
    url.pathname = '/Pictures/Index/' + parameters.currentPageNumber.toString();
    url.searchParams.set('childId', parameters.progenyId.toString());
    url.searchParams.set('sortBy', parameters.sort.toString());
    url.searchParams.set('pageSize', parameters.itemsPerPage.toString());
    url.searchParams.set('tagFilter', parameters.tagFilter.toString());
    url.searchParams.set('year', parameters.year.toString());
    url.searchParams.set('month', parameters.month.toString());
    url.searchParams.set('day', parameters.day.toString());
    url.searchParams.set('sortTags', parameters.sortTags.toString());
    url.searchParams.set('pictureId', popupPictureId.toString());
    if (replaceState) {
        window.history.replaceState({}, '', url);
    }
    else {
        window.history.pushState({}, '', url);
    }
    const currentPageTitleDiv = document.querySelector('#current-page-title-div');
    if (currentPageTitleDiv !== null) {
        const title = currentPageTitleDiv.dataset.currentPageTitle;
        document.title = title + '(' + parameters.currentPageNumber.toString() + ') : KinaUna';
    }
}
/** Retrieves the parameters from the url in browser address bar.
 * Then loads the picturesList with the parameters retrived.
 */
async function loadPageFromHistory() {
    const url = new URL(window.location.href);
    if (picturesPageParameters !== null) {
        picturesPageParameters.currentPageNumber = parseInt(url.pathname.replace('/Pictures/Index/', ''));
        picturesPageParameters.progenyId = url.searchParams.get('childId') ? parseInt(url.searchParams.get('childId')) : 0;
        picturesPageParameters.sort = url.searchParams.get('sortBy') ? parseInt(url.searchParams.get('sortBy')) : 1;
        picturesPageParameters.itemsPerPage = url.searchParams.get('pageSize') ? parseInt(url.searchParams.get('pageSize')) : 10;
        picturesPageParameters.tagFilter = url.searchParams.get('tagFilter') ? url.searchParams.get('tagFilter') : '';
        picturesPageParameters.year = url.searchParams.get('year') ? parseInt(url.searchParams.get('year')) : 0;
        picturesPageParameters.month = url.searchParams.get('month') ? parseInt(url.searchParams.get('month')) : 0;
        picturesPageParameters.day = url.searchParams.get('day') ? parseInt(url.searchParams.get('day')) : 0;
        picturesPageParameters.sortTags = url.searchParams.get('sortTags') ? parseInt(url.searchParams.get('sortTags')) : 0;
        popupPictureId = url.searchParams.get('pictureId') ? parseInt(url.searchParams.get('pictureId')) : 0;
        firstRun = false;
        clearPictureElements();
        picturesPageParameters = await getPicturesList(picturesPageParameters, false);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Updates the div that shows the current sort order and start date.
* @param sort The sort order, 0=ascending, 1=descending.
*/
function updateSettingsNotificationDiv(sort) {
    let pageSettingsNotificationText;
    const pageSettingsNotificationDiv = document.querySelector('#settings-notification-div');
    if (sort === 0) {
        pageSettingsNotificationText = sortAscendingSettingsButton?.innerHTML;
    }
    else {
        pageSettingsNotificationText = sortDescendingSettingsButton?.innerHTML;
    }
    pageSettingsNotificationText += '<br/>' + startLabelDiv?.innerHTML;
    pageSettingsNotificationText += getStartDateValueFromDatePicker();
    if (pageSettingsNotificationDiv !== null && pageSettingsNotificationText !== undefined) {
        pageSettingsNotificationDiv.innerHTML = pageSettingsNotificationText;
    }
}
/** Renders a list of tag buttons in the tags-list-div, each with a link to filter the page.
* @param tagsList The list of strings for each tag.
*/
function updateTagsListDiv(tagsList, sortOrder) {
    const tagsListDiv = document.querySelector('#tags-list-div');
    if (tagsListDiv !== null) {
        tagsListDiv.innerHTML = '';
        if (sortOrder === 1) {
            tagsList.sort((a, b) => a.localeCompare(b));
        }
        tagsList.forEach(function (tag) {
            tagsListDiv.innerHTML += '<a class="btn tag-item" data-tag-link="' + tag + '">' + tag + '</a>';
        });
        const tagButtons = document.querySelectorAll('[data-tag-link]');
        tagButtons.forEach((tagButton) => {
            tagButton.addEventListener('click', tagButtonClick);
        });
    }
}
/** If a tag filter is active, show the tag in the active tag filter div and provide a button to clear it.
* @param parameters The picturesPageParameters with the tag used for filtering.
*/
function updateActiveTagFilterDiv(parameters) {
    const activeTagFilterDiv = document.querySelector('#active-tag-filter-div');
    const activeTagFilterSpan = document.querySelector('#current-tag-filter-span');
    if (activeTagFilterDiv !== null && activeTagFilterSpan !== null && parameters !== null) {
        if (parameters.tagFilter !== '') {
            activeTagFilterDiv.classList.remove('d-none');
            activeTagFilterSpan.innerHTML = parameters.tagFilter;
        }
        else {
            activeTagFilterDiv.classList.add('d-none');
            activeTagFilterSpan.innerHTML = '';
        }
    }
}
/** Clears the active tag filter and reloads the default full list of pictures.
*/
async function resetActiveTagFilter() {
    if (picturesPageParameters !== null) {
        picturesPageParameters.tagFilter = '';
        clearPictureElements();
        picturesPageParameters = await getPicturesList(picturesPageParameters);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Event handler for tag buttons, sets the tag filter and reloads the list of pictures.
*/
async function tagButtonClick(event) {
    const target = event.target;
    if (target !== null && picturesPageParameters !== null) {
        const tagLink = target.dataset.tagLink;
        if (tagLink !== undefined) {
            picturesPageParameters.tagFilter = tagLink;
            picturesPageParameters.currentPageNumber = 1;
            clearPictureElements();
            await getPicturesList(picturesPageParameters);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Fetches the HTML for a given picture and renders it at the end of photo-list-div.
* @param pictureItem The picture object to add to the page.
*/
async function renderPictureItem(pictureItem) {
    if (picturesPageParameters !== null) {
        const pictureViewModel = new PictureViewModel();
        pictureViewModel.pictureId = pictureItem.pictureId;
        pictureViewModel.progenyId = pictureItem.progenyId;
        pictureViewModel.sortBy = picturesPageParameters.sort;
        pictureViewModel.tagFilter = picturesPageParameters.tagFilter;
        pictureViewModel.pictureNumber = pictureItem.pictureNumber;
        const getPictureElementResponse = await fetch('/Pictures/GetPictureElement', {
            method: 'POST',
            body: JSON.stringify(pictureViewModel),
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        });
        if (getPictureElementResponse.ok && getPictureElementResponse.text !== null) {
            const pictureElementHtml = await getPictureElementResponse.text();
            if (photoListDiv != null) {
                photoListDiv.insertAdjacentHTML('beforeend', pictureElementHtml);
                const timelineItem = new TimelineItem();
                timelineItem.itemId = pictureItem.pictureId.toString();
                timelineItem.itemType = 1;
                addTimelineItemEventListener(timelineItem);
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Clears the list of picture elements in the photoListDiv and scrolls to above the photo-list-div.
*/
function clearPictureElements() {
    const pageTitleDiv = document.querySelector('#page-title-div');
    if (pageTitleDiv !== null) {
        pageTitleDiv.scrollIntoView();
    }
    const photoListDiv = document.querySelector('#photo-list-div');
    if (photoListDiv !== null) {
        photoListDiv.innerHTML = '';
    }
}
/** Gets the formatted date value from the start date picker and sets the start date in the parameters.
* @param dateTimeFormatMoment The Moment format of the date, which is used by the date picker.
*/
function setStartDate(dateTimeFormatMoment) {
    let settingsStartValue = SettingsHelper.getPageSettingsStartDate(dateTimeFormatMoment);
    if (settingsStartValue === null) {
        settingsStartValue = new Date();
    }
    if (picturesPageParameters !== null) {
        picturesPageParameters.year = settingsStartValue.year();
        picturesPageParameters.month = settingsStartValue.month() + 1;
        picturesPageParameters.day = settingsStartValue.date();
    }
}
/**
 * Sets the value of the startDateTimePicker to the given date in the format defined by startDateTimeFormatMoment..
 * @param date The date to assign to the DateTimePicker.
 */
function setStartDatePicker(date) {
    if (settingsStartDateTimePicker !== null) {
        const dateString = getFormattedDateString(date, startDateTimeFormatMoment);
        settingsStartDateTimePicker.val(dateString);
    }
}
/**
 * Gets the value from the startDateTimePicker in the format defined by startTimeFormatMoment.
 * @returns The formatted string value.
 */
function getStartDateValueFromDatePicker() {
    if (settingsStartDateTimePicker === null) {
        return settingsStartDateTimePicker.val();
    }
    const currentDate = new Date();
    const currentDateString = getFormattedDateString(currentDate, startDateTimeFormatMoment);
    return currentDateString;
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 * Updates the Start date if it hasn't been set so the list starts at the earliest date.
 */
function sortPicturesAscending() {
    if (picturesPageParameters === null) {
        return;
    }
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    picturesPageParameters.sort = 0;
    let currentDate = new Date();
    if (picturesPageParameters.year === 0) {
        picturesPageParameters.year = currentDate.getFullYear();
    }
    if (picturesPageParameters.month === 0) {
        picturesPageParameters.month = currentDate.getMonth() + 1;
    }
    if (picturesPageParameters.day === 0) {
        picturesPageParameters.day = currentDate.getDay();
    }
    if (picturesPageParameters.year === currentDate.getFullYear() && picturesPageParameters.month - 1 === currentDate.getMonth() && picturesPageParameters.day === currentDate.getDate()) {
        picturesPageParameters.year = picturesPageParameters.firstItemYear;
        picturesPageParameters.month = 1;
        picturesPageParameters.day = 1;
        currentStartYear = picturesPageParameters.year;
        currentStartMonth = picturesPageParameters.month;
        currentStartDay = picturesPageParameters.day;
        const earliestDate = new Date(picturesPageParameters.firstItemYear, 0, 1);
        setStartDatePicker(earliestDate);
    }
}
/**
 * Updates parameters sort value, sets the sort buttons to show the descending button as active, and the ascending button as inactive.
 * Updates the Start date if it hasn't been set, so the list starts at today's date.
 */
function sortPicturesDescending() {
    if (picturesPageParameters === null) {
        return;
    }
    sortDescendingSettingsButton?.classList.add('active');
    sortAscendingSettingsButton?.classList.remove('active');
    picturesPageParameters.sort = 1;
    const currentDate = new Date();
    if (picturesPageParameters.year === picturesPageParameters.firstItemYear && picturesPageParameters.month === 1 && picturesPageParameters.day === 1) {
        picturesPageParameters.year = currentDate.getFullYear();
        picturesPageParameters.month = currentDate.getMonth() + 1;
        picturesPageParameters.day = currentDate.getDate();
        currentStartYear = picturesPageParameters.year;
        currentStartMonth = picturesPageParameters.month;
        currentStartDay = picturesPageParameters.day;
        setStartDatePicker(currentDate);
    }
}
/**
 * Saves the current page parameters to local storage and reloads the pictures list.
 */
async function savePicturesPageSettings() {
    if (picturesPageParameters === null) {
        return new Promise(function (resolve, reject) {
            reject();
        });
    }
    const firstPictureNumber = (picturesPageParameters.currentPageNumber - 1) * picturesPageParameters.itemsPerPage + 1;
    const itemsPerPageInput = document.querySelector('#items-per-page-select');
    if (itemsPerPageInput !== null) {
        picturesPageParameters.itemsPerPage = parseInt(itemsPerPageInput.value);
    }
    else {
        picturesPageParameters.itemsPerPage = 10;
    }
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null) {
        picturesPageParameters.sortTags = sortTagsSelect.selectedIndex;
    }
    const saveAsDefaultCheckbox = document.querySelector('#settings-save-default-checkbox');
    if (saveAsDefaultCheckbox !== null && saveAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings(picturesPageSettingsStorageKey, picturesPageParameters);
    }
    if (picturesPageParameters.year === currentStartYear && picturesPageParameters.month === currentStartMonth && picturesPageParameters.day === currentStartDay) {
        const pageOfFirstPicture = Math.ceil(firstPictureNumber / picturesPageParameters.itemsPerPage);
        if (picturesPageParameters.sort === currentSort) {
            picturesPageParameters.currentPageNumber = pageOfFirstPicture;
        }
        else {
            const newTotalPages = Math.ceil(picturesPageParameters.totalItems / picturesPageParameters.itemsPerPage);
            picturesPageParameters.currentPageNumber = newTotalPages - pageOfFirstPicture + 1;
        }
    }
    else {
        picturesPageParameters.currentPageNumber = 1;
    }
    SettingsHelper.toggleShowPageSettings();
    clearPictureElements();
    picturesPageParameters = await getPicturesList(picturesPageParameters);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Retrieves the picturesPageParameters saved in local storage.
 * @returns The PicturesPageParameters object retrieved from local storage.
 */
function loadPicturesPageParametersFromStorage() {
    let pageParametersFromStorage = new PicturesPageParameters();
    let pageSettingsFromStorage = SettingsHelper.getPageSettings(picturesPageSettingsStorageKey);
    if (pageSettingsFromStorage !== null) {
        pageParametersFromStorage = pageSettingsFromStorage;
    }
    return pageParametersFromStorage;
}
/**
 * Sets the parameters currentPage property to the next page, then reloads the pictures list.
 * If there are no more pages, the first page is set and loaded instead.
 */
async function loadNextPage() {
    if (picturesPageParameters !== null) {
        if (picturesPageParameters.currentPageNumber < picturesPageParameters.totalPages) {
            picturesPageParameters.currentPageNumber++;
        }
        else {
            picturesPageParameters.currentPageNumber = 1;
        }
        clearPictureElements();
        await getPicturesList(picturesPageParameters);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets the parameters currentPage property to the previous page, then reloads the pictures list.
 * If there are no more pages, the last page is set and loaded instead.
 */
async function loadPreviousPage() {
    if (picturesPageParameters !== null) {
        if (picturesPageParameters.currentPageNumber > 1) {
            picturesPageParameters.currentPageNumber--;
        }
        else {
            picturesPageParameters.currentPageNumber = picturesPageParameters.totalPages;
        }
        clearPictureElements();
        await getPicturesList(picturesPageParameters);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates the navigation elements: Display page number, number of pages, sets the text of next/previous buttons.
 * If there is only one page, the next/previous buttons are hidden.
 * @param parameters
 */
function updateNavigationDiv(parameters) {
    const pageNumberSpan = document.querySelector('#page-number-span');
    const pageTotalSpan = document.querySelector('#page-total-span');
    if (pageNumberSpan !== null && pageTotalSpan !== null) {
        pageNumberSpan.innerHTML = parameters.currentPageNumber.toString();
        pageTotalSpan.innerHTML = parameters.totalPages.toString();
    }
    const navigationDiv = document.querySelector('#item-page-navigation-div');
    const navigationDivBottom = document.querySelector('#item-page-navigation-div-bottom');
    if (parameters.totalPages > 1) {
        const leftSpan = document.querySelector('#item-page-navigation-left-span');
        const rightSpan = document.querySelector('#item-page-navigation-right-span');
        const leftSpanBottom = document.querySelector('#item-page-navigation-left-span-bottom');
        const rightSpanBottom = document.querySelector('#item-page-navigation-right-span-bottom');
        const newerTextDiv = document.querySelector('#newer-text-div');
        const newerText = newerTextDiv?.dataset.newerText;
        const olderTextDiv = document.querySelector('#older-text-div');
        const olderText = olderTextDiv?.dataset.olderText;
        if (leftSpan !== null && rightSpan !== null && newerText && olderText) {
            if (parameters.sort === 1) {
                leftSpan.innerHTML = newerText;
                rightSpan.innerHTML = olderText;
            }
            else {
                leftSpan.innerHTML = olderText;
                rightSpan.innerHTML = newerText;
            }
            navigationDiv?.classList.remove('d-none');
        }
        if (leftSpanBottom !== null && rightSpanBottom !== null && newerText && olderText) {
            if (parameters.sort === 1) {
                leftSpanBottom.innerHTML = newerText;
                rightSpanBottom.innerHTML = olderText;
            }
            else {
                leftSpanBottom.innerHTML = olderText;
                rightSpanBottom.innerHTML = newerText;
            }
            navigationDivBottom?.classList.remove('d-none');
        }
    }
    else {
        navigationDiv?.classList.add('d-none');
        navigationDivBottom?.classList.add('d-none');
    }
}
/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup() {
    startLabelDiv = document.querySelector('#start-label-div');
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    const zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    startDateTimeFormatMoment = getLongDateTimeFormatMoment();
    settingsStartDateTimePicker = $('#settings-start-date-datetimepicker');
    settingsStartDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { setStartDate(startDateTimeFormatMoment); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    sortAscendingSettingsButton = document.querySelector('#settings-sort-ascending-button');
    sortDescendingSettingsButton = document.querySelector('#settings-sort-descending-button');
    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortPicturesAscending);
        sortDescendingSettingsButton.addEventListener('click', sortPicturesDescending);
    }
    const selectItemsPerPageElement = document.querySelector('#items-per-page-select');
    if (selectItemsPerPageElement !== null) {
        $(".selectpicker").selectpicker('refresh');
    }
    const pageSaveSettingsButton = document.querySelector('#page-save-settings-button');
    if (pageSaveSettingsButton !== null) {
        pageSaveSettingsButton.addEventListener('click', savePicturesPageSettings);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Adds eventlisteners for reset-tag-filter and next/previous button clicks. */
function addPageNavigationEventListeners() {
    const pageNextButton = document.querySelector('#next-page-button');
    if (pageNextButton !== null) {
        pageNextButton.addEventListener('click', loadNextPage);
    }
    const pagePreviousButton = document.querySelector('#previous-page-button');
    if (pagePreviousButton !== null) {
        pagePreviousButton.addEventListener('click', loadPreviousPage);
    }
    const pageNextButtonBottom = document.querySelector('#next-page-button-bottom');
    if (pageNextButtonBottom !== null) {
        pageNextButtonBottom.addEventListener('click', loadNextPage);
    }
    const pagePreviousButtonBottom = document.querySelector('#previous-page-button-bottom');
    if (pagePreviousButtonBottom !== null) {
        pagePreviousButtonBottom.addEventListener('click', loadPreviousPage);
    }
    const resetTagFilterButton = document.querySelector('#reset-tag-filter-button');
    if (resetTagFilterButton !== null) {
        resetTagFilterButton.addEventListener('click', resetActiveTagFilter);
    }
}
/** Adds event listeners for users reloading, navigating back or forward in the browser history. */
function addBrowserNavigationEventListeners() {
    window.onpopstate = function (event) {
        loadPageFromHistory();
    };
    window.onpageshow = function (event) {
        if (event.persisted) {
            loadPageFromHistory();
        }
    };
}
/** Select pickers don't always update when their values change, this ensures they show the correct items. */
function refreshSelectPickers() {
    const itemsPerPageInput = document.querySelector('#items-per-page-select');
    if (itemsPerPageInput !== null && picturesPageParameters !== null) {
        itemsPerPageInput.value = picturesPageParameters.itemsPerPage.toString();
        $(".selectpicker").selectpicker('refresh');
    }
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null && picturesPageParameters !== null) {
        sortTagsSelect.value = picturesPageParameters.sortTags.toString();
        $(".selectpicker").selectpicker('refresh');
    }
}
function getPopupPictureId() {
    let pictureIdDiv = document.querySelector('#popup-picture-id-div');
    if (pictureIdDiv !== null) {
        let pictureIdData = pictureIdDiv.dataset.popupPictureId;
        if (pictureIdData) {
            popupPictureId = parseInt(pictureIdData);
        }
    }
}
function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            if (picturesPageParameters !== null) {
                picturesPageParameters.progenies = getSelectedProgenies();
                picturesPageParameters = await getPicturesList(picturesPageParameters, false);
            }
        }
    });
}
/** Initialization and setup when page is loaded */
document.addEventListener('DOMContentLoaded', async function () {
    getPopupPictureId();
    await showPopupAtLoad(TimeLineType.Photo);
    picturesPageProgenyId = getCurrentProgenyId();
    languageId = getCurrentLanguageId();
    await initialSettingsPanelSetup();
    addPageNavigationEventListeners();
    addBrowserNavigationEventListeners();
    SettingsHelper.initPageSettings();
    addSelectedProgeniesChangedEventListener();
    picturesPageParameters = getPageParametersFromPageData();
    if (picturesPageParameters !== null) {
        picturesPageParameters.progenies = getSelectedProgenies();
        refreshSelectPickers();
        picturesPageParameters = await getPicturesList(picturesPageParameters, false);
        // If firstRun is still true the initial date of the first item, the total page count, or number of items, have changed. Run it again.
        if (firstRun) {
            picturesPageParameters = await getPicturesList(picturesPageParameters, false);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=pictures-index.js.map