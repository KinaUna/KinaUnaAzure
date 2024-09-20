import * as LocaleHelper from '../localization-v8.js';
import { VideoViewModel, VideosPageParameters, TimelineItem } from '../page-models-v8.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getFormattedDateString } from '../data-tools-v8.js';
import * as SettingsHelper from '../settings-tools-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v8.js';
let videosPageParameters = new VideosPageParameters();
const videosPageSettingsStorageKey = 'videos_page_parameters';
let languageId = 1;
let videosPageProgenyId;
let zebraDateTimeFormat;
let startDateTimeFormatMoment;
let firstRun = true;
let currentSort = 1;
let currentStartYear = 0;
let currentStartMonth = 0;
let currentStartDay = 0;
const videosListDiv = document.querySelector('#video-list-div');
let sortAscendingSettingsButton = document.querySelector('#settings-sort-ascending-button');
let sortDescendingSettingsButton = document.querySelector('#setting-sort-descending-button');
let settingsStartDateTimePicker = $('#settings-start-date-datetimepicker');
let startLabelDiv = document.querySelector('#start-label-div');
/** Reads the initial page parameters from json serialized data in the videos-page-parameters div elements data-videos-page-parameters attribute.
 * If the page is navigated to without specific parameters, itemsPerPage, sort, and sortTags parameters are loaded from local storage.
 * @returns The VideosPageParameters object with the initial page parameters.
 */
function getPageParametersFromPageData() {
    const videosPageParametersDiv = document.querySelector('#videos-page-parameters');
    let videosPageParametersResult = new VideosPageParameters();
    if (videosPageParametersDiv !== null) {
        const pageParametersString = videosPageParametersDiv.dataset.videosPageParameters;
        if (pageParametersString) {
            let videosPageParametersFromPageData = JSON.parse(pageParametersString);
            if (videosPageParametersFromPageData !== null) {
                videosPageParametersResult = videosPageParametersFromPageData;
                // If year, month and day are all 0, it means the page was navigated to without specifying parameters to use, so load them from storage.
                if (videosPageParametersResult.year === 0 && videosPageParametersResult.month === 0 && videosPageParametersResult.day === 0) {
                    const pageSettingsFromStorage = loadVideosPageParametersFromStorage();
                    if (pageSettingsFromStorage !== null) {
                        pageSettingsFromStorage.currentPageNumber = 1;
                        pageSettingsFromStorage.progenyId = videosPageParametersFromPageData.progenyId;
                        pageSettingsFromStorage.languageId = videosPageParametersFromPageData.languageId;
                        pageSettingsFromStorage.tagFilter = videosPageParametersFromPageData.tagFilter;
                        pageSettingsFromStorage.year = videosPageParametersFromPageData.year;
                        pageSettingsFromStorage.month = videosPageParametersFromPageData.month;
                        pageSettingsFromStorage.day = videosPageParametersFromPageData.day;
                        if (pageSettingsFromStorage.itemsPerPage === null) {
                            pageSettingsFromStorage.itemsPerPage = 10;
                        }
                        if (pageSettingsFromStorage.sort === null) {
                            pageSettingsFromStorage.sort = 1;
                        }
                        if (pageSettingsFromStorage.sortTags === null) {
                            pageSettingsFromStorage.sortTags = 0;
                        }
                        videosPageParametersResult = pageSettingsFromStorage;
                    }
                }
                // Override progenyId, it should never be retrieved from storage on initial page loads.
                videosPageParametersResult.progenyId = videosPageParametersFromPageData.progenyId;
            }
            if (videosPageParametersResult === null) {
                videosPageParametersResult = new VideosPageParameters();
            }
        }
    }
    if (videosPageParametersResult.sort > 1) {
        videosPageParametersResult.sort = 1;
    }
    if (videosPageParametersResult.sort === 0) {
        sortVideosAscending();
    }
    else {
        sortVideosDescending();
    }
    return videosPageParametersResult;
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
//
/** Retrieves the list of videos, based on the parameters provided, then updates the page.
 *
 * @param parameters The VideosPageParameters object with the parameters to use for the query.
 * @param updateHistory If updateHistory is true the browser history is updated to reflect the current page. If false it is assumed the page was loaded from history or reload, and is already in the history stack.
 * @returns The parameters object, with updated data for firstItemYear, totalPages, totalItems,
 */
async function getVideosList(parameters, updateHistory = true) {
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
    await fetch('/Videos/GetVideoList', {
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
            if (newItemsList.videoItems.length > 0) {
                parameters = await processVideosList(newItemsList, parameters);
                if (updateHistory) {
                    setBrowserUrl(parameters, false);
                }
            }
        }
    }).catch(function (error) {
        console.log('Error loading Videos list. Error: ' + error);
    });
    stopLoadingSpinner();
    return new Promise(function (resolve, reject) {
        resolve(parameters);
    });
}
/**
 * Updates the parameters with the data from the newItemsList, then replaces the videos on the page with the items in the newItemsList.
 * Also makes calls to update the navigation elements and tags list.
 * @param newItemsList The VideosList object with an array of pictures to render, and updated data for firstItemYear.
 * @param parameters The current VideoPageParameters for the page.
 * @returns The updated VideoPageParameters object.
 */
async function processVideosList(newItemsList, parameters) {
    parameters.totalPages = newItemsList.totalPages;
    parameters.totalItems = newItemsList.allItemsCount;
    parameters.currentPageNumber = newItemsList.currentPageNumber;
    // If this is the initial page load and videos are sorted in ascending order, check if year for the earliest video needs to be updated.
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
    const videoListParentDiv = document.querySelector('#video-list-parent-div');
    if (videoListParentDiv !== null) {
        videoListParentDiv.classList.remove('d-none');
    }
    for await (const itemToAdd of newItemsList.videoItems) {
        await renderVideoItem(itemToAdd);
    }
    ;
    updateTagsListDiv(newItemsList.tagsList, parameters.sortTags);
    return new Promise(function (resolve, reject) {
        resolve(parameters);
    });
}
/** Sets the url in the browser address bar to reflect the current page.
 * @param parameters The VideosPageParameters currently in use.
 * @param replaceState If true, the current url will replace the url in the active one in history, if false the url will be added to the history.
 */
function setBrowserUrl(parameters, replaceState) {
    const url = new URL(window.location.href);
    url.pathname = '/Videos/Index/' + parameters.currentPageNumber.toString();
    url.searchParams.set('childId', parameters.progenyId.toString());
    url.searchParams.set('sortBy', parameters.sort.toString());
    url.searchParams.set('pageSize', parameters.itemsPerPage.toString());
    url.searchParams.set('tagFilter', parameters.tagFilter.toString());
    url.searchParams.set('year', parameters.year.toString());
    url.searchParams.set('month', parameters.month.toString());
    url.searchParams.set('day', parameters.day.toString());
    url.searchParams.set('sortTags', parameters.sortTags.toString());
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
 * Then loads the videosList with the parameters retrived.
 */
async function loadPageFromHistory() {
    const url = new URL(window.location.href);
    if (videosPageParameters !== null) {
        videosPageParameters.currentPageNumber = parseInt(url.pathname.replace('/Pictures/Index/', ''));
        videosPageParameters.progenyId = url.searchParams.get('childId') ? parseInt(url.searchParams.get('childId')) : 0;
        videosPageParameters.sort = url.searchParams.get('sortBy') ? parseInt(url.searchParams.get('sortBy')) : 1;
        videosPageParameters.itemsPerPage = url.searchParams.get('pageSize') ? parseInt(url.searchParams.get('pageSize')) : 10;
        videosPageParameters.tagFilter = url.searchParams.get('tagFilter') ? url.searchParams.get('tagFilter') : '';
        videosPageParameters.year = url.searchParams.get('year') ? parseInt(url.searchParams.get('year')) : 0;
        videosPageParameters.month = url.searchParams.get('month') ? parseInt(url.searchParams.get('month')) : 0;
        videosPageParameters.day = url.searchParams.get('day') ? parseInt(url.searchParams.get('day')) : 0;
        videosPageParameters.sortTags = url.searchParams.get('sortTags') ? parseInt(url.searchParams.get('sortTags')) : 0;
        firstRun = false;
        clearVideoElements();
        videosPageParameters = await getVideosList(videosPageParameters, false);
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
/** Clears the active tag filter and reloads the default full list of videos.
*/
async function resetActiveTagFilter() {
    if (videosPageParameters !== null) {
        videosPageParameters.tagFilter = '';
        clearVideoElements();
        videosPageParameters = await getVideosList(videosPageParameters);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Event handler for tag buttons, sets the tag filter and reloads the list of videos.
*/
async function tagButtonClick(event) {
    const target = event.target;
    if (target !== null && videosPageParameters !== null) {
        const tagLink = target.dataset.tagLink;
        if (tagLink !== undefined) {
            videosPageParameters.tagFilter = tagLink;
            videosPageParameters.currentPageNumber = 1;
            clearVideoElements();
            await getVideosList(videosPageParameters);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Fetches the HTML for a given video and renders it at the end of video-list-div.
* @param videoItem The video object to add to the page.
*/
async function renderVideoItem(videoItem) {
    if (videosPageParameters !== null) {
        const videoViewModel = new VideoViewModel();
        videoViewModel.videoId = videoItem.videoId;
        videoViewModel.progenyId = videoItem.progenyId;
        videoViewModel.sortBy = videosPageParameters.sort;
        videoViewModel.tagFilter = videosPageParameters.tagFilter;
        videoViewModel.videoNumber = videoItem.videoNumber;
        const getVideoElementResponse = await fetch('/Videos/GetVideoElement', {
            method: 'POST',
            body: JSON.stringify(videoViewModel),
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        });
        if (getVideoElementResponse.ok && getVideoElementResponse.text !== null) {
            const videoElementHtml = await getVideoElementResponse.text();
            if (videosListDiv != null) {
                videosListDiv.insertAdjacentHTML('beforeend', videoElementHtml);
                const timelineItem = new TimelineItem();
                timelineItem.itemId = videoItem.videoId.toString();
                timelineItem.itemType = 2;
                addTimelineItemEventListener(timelineItem);
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Clears the list of video elements in the videoListDiv and scrolls to above the videos-list-div.
*/
function clearVideoElements() {
    const pageTitleDiv = document.querySelector('#page-title-div');
    if (pageTitleDiv !== null) {
        pageTitleDiv.scrollIntoView();
    }
    const videoListDiv = document.querySelector('#video-list-div');
    if (videoListDiv !== null) {
        videoListDiv.innerHTML = '';
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
    if (videosPageParameters !== null) {
        videosPageParameters.year = settingsStartValue.year();
        videosPageParameters.month = settingsStartValue.month() + 1;
        videosPageParameters.day = settingsStartValue.date();
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
function sortVideosAscending() {
    if (videosPageParameters === null) {
        return;
    }
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    videosPageParameters.sort = 0;
    let currentDate = new Date();
    if (videosPageParameters.year === 0) {
        videosPageParameters.year = currentDate.getFullYear();
    }
    if (videosPageParameters.month === 0) {
        videosPageParameters.month = currentDate.getMonth() + 1;
    }
    if (videosPageParameters.day === 0) {
        videosPageParameters.day = currentDate.getDay();
    }
    if (videosPageParameters.year === currentDate.getFullYear() && videosPageParameters.month - 1 === currentDate.getMonth() && videosPageParameters.day === currentDate.getDate()) {
        videosPageParameters.year = videosPageParameters.firstItemYear;
        videosPageParameters.month = 1;
        videosPageParameters.day = 1;
        currentStartYear = videosPageParameters.year;
        currentStartMonth = videosPageParameters.month;
        currentStartDay = videosPageParameters.day;
        const earliestDate = new Date(videosPageParameters.firstItemYear, 0, 1);
        setStartDatePicker(earliestDate);
    }
}
/**
 * Updates parameters sort value, sets the sort buttons to show the descending button as active, and the ascending button as inactive.
 * Updates the Start date if it hasn't been set, so the list starts at today's date.
 */
function sortVideosDescending() {
    if (videosPageParameters === null) {
        return;
    }
    sortDescendingSettingsButton?.classList.add('active');
    sortAscendingSettingsButton?.classList.remove('active');
    videosPageParameters.sort = 1;
    const currentDate = new Date();
    if (videosPageParameters.year === videosPageParameters.firstItemYear && videosPageParameters.month === 1 && videosPageParameters.day === 1) {
        videosPageParameters.year = currentDate.getFullYear();
        videosPageParameters.month = currentDate.getMonth() + 1;
        videosPageParameters.day = currentDate.getDate();
        currentStartYear = videosPageParameters.year;
        currentStartMonth = videosPageParameters.month;
        currentStartDay = videosPageParameters.day;
        setStartDatePicker(currentDate);
    }
}
/**
 * Saves the current page parameters to local storage and reloads the pictures list.
 */
async function saveVideosPageSettings() {
    if (videosPageParameters === null) {
        return new Promise(function (resolve, reject) {
            reject();
        });
    }
    const firstVideoNumber = (videosPageParameters.currentPageNumber - 1) * videosPageParameters.itemsPerPage + 1;
    const itemsPerPageInput = document.querySelector('#items-per-page-select');
    if (itemsPerPageInput !== null) {
        videosPageParameters.itemsPerPage = parseInt(itemsPerPageInput.value);
    }
    else {
        videosPageParameters.itemsPerPage = 10;
    }
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null) {
        videosPageParameters.sortTags = sortTagsSelect.selectedIndex;
    }
    const saveAsDefaultCheckbox = document.querySelector('#settings-save-default-checkbox');
    if (saveAsDefaultCheckbox !== null && saveAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings(videosPageSettingsStorageKey, videosPageParameters);
    }
    if (videosPageParameters.year === currentStartYear && videosPageParameters.month === currentStartMonth && videosPageParameters.day === currentStartDay) {
        const pageOfFirstVideo = Math.ceil(firstVideoNumber / videosPageParameters.itemsPerPage);
        if (videosPageParameters.sort === currentSort) {
            videosPageParameters.currentPageNumber = pageOfFirstVideo;
        }
        else {
            const newTotalPages = Math.ceil(videosPageParameters.totalItems / videosPageParameters.itemsPerPage);
            videosPageParameters.currentPageNumber = newTotalPages - pageOfFirstVideo + 1;
        }
    }
    else {
        videosPageParameters.currentPageNumber = 1;
    }
    SettingsHelper.toggleShowPageSettings();
    clearVideoElements();
    videosPageParameters = await getVideosList(videosPageParameters);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Retrieves the videosPageParameters saved in local storage.
 * @returns The VideosPageParameters object retrieved from local storage.
 */
function loadVideosPageParametersFromStorage() {
    let pageParametersFromStorage = new VideosPageParameters();
    let pageSettingsFromStorage = SettingsHelper.getPageSettings(videosPageSettingsStorageKey);
    if (pageSettingsFromStorage !== null) {
        pageParametersFromStorage = pageSettingsFromStorage;
    }
    return pageParametersFromStorage;
}
/**
 * Sets the parameters currentPage property to the next page, then reloads the videos list.
 * If there are no more pages, the first page is set and loaded instead.
 */
async function loadNextPage() {
    if (videosPageParameters !== null) {
        if (videosPageParameters.currentPageNumber < videosPageParameters.totalPages) {
            videosPageParameters.currentPageNumber++;
        }
        else {
            videosPageParameters.currentPageNumber = 1;
        }
        clearVideoElements();
        await getVideosList(videosPageParameters);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets the parameters currentPage property to the previous page, then reloads the videos list.
 * If there are no more pages, the last page is set and loaded instead.
 */
async function loadPreviousPage() {
    if (videosPageParameters !== null) {
        if (videosPageParameters.currentPageNumber > 1) {
            videosPageParameters.currentPageNumber--;
        }
        else {
            videosPageParameters.currentPageNumber = videosPageParameters.totalPages;
        }
        clearVideoElements();
        await getVideosList(videosPageParameters);
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
        sortAscendingSettingsButton.addEventListener('click', sortVideosAscending);
        sortDescendingSettingsButton.addEventListener('click', sortVideosDescending);
    }
    const selectItemsPerPageElement = document.querySelector('#items-per-page-select');
    if (selectItemsPerPageElement !== null) {
        $(".selectpicker").selectpicker('refresh');
    }
    const pageSaveSettingsButton = document.querySelector('#page-save-settings-button');
    if (pageSaveSettingsButton !== null) {
        pageSaveSettingsButton.addEventListener('click', saveVideosPageSettings);
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
    if (itemsPerPageInput !== null && videosPageParameters !== null) {
        itemsPerPageInput.value = videosPageParameters.itemsPerPage.toString();
        $(".selectpicker").selectpicker('refresh');
    }
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null && videosPageParameters !== null) {
        sortTagsSelect.value = videosPageParameters.sortTags.toString();
        $(".selectpicker").selectpicker('refresh');
    }
}
/** Initialization and setup when page is loaded */
document.addEventListener('DOMContentLoaded', async function () {
    videosPageProgenyId = getCurrentProgenyId();
    languageId = getCurrentLanguageId();
    await initialSettingsPanelSetup();
    addPageNavigationEventListeners();
    addBrowserNavigationEventListeners();
    SettingsHelper.initPageSettings();
    videosPageParameters = getPageParametersFromPageData();
    if (videosPageParameters !== null) {
        refreshSelectPickers();
        videosPageParameters = await getVideosList(videosPageParameters, false);
        // If firstRun is still true the initial date of the first item, the total page count, or number of items, have changed. Run it again.
        if (firstRun) {
            videosPageParameters = await getVideosList(videosPageParameters, false);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=videos-index.js.map