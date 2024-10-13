import * as LocaleHelper from '../localization-v8.js';
import { TimeLineItemViewModel, TimelineRequest } from '../page-models-v8.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getFormattedDateString, setCategoriesAutoSuggestList, setContextAutoSuggestList, setTagsAutoSuggestList } from '../data-tools-v8.js';
import * as SettingsHelper from '../settings-tools-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v8.js';
import { getSelectedProgenies } from '../settings-tools-v8.js';
const timelinePageSettingsStorageKey = 'timeline_page_parameters';
let timelineItemsList = [];
let timeLineParameters = new TimelineRequest();
let timeLineProgenyId;
let languageId = 1;
let moreTimelineItemsButton;
let firstRun = true;
const sortAscendingSettingsButton = document.querySelector('#setting-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#setting-sort-descending-button');
const timelineStartDateTimePicker = $('#settings-start-date-datetimepicker');
const timelineSettingsNotificationDiv = document.querySelector('#settings-notification-div');
const startLabelDiv = document.querySelector('#start-label-div');
let zebraDatePickerTranslations;
let zebraDateTimeFormat;
let startDateTimeFormatMoment;
/** Shows the loading spinner in the loading-timeline-items-div.
 */
function startLoadingSpinner() {
    startLoadingItemsSpinner('loading-timeline-items-div');
}
/** Hides the loading spinner in the loading-timeline-items-div.
 */
function stopLoadingSpinner() {
    stopLoadingItemsSpinner('loading-timeline-items-div');
}
/**
 * Retrieves the list of timeline items, based on the parameters provided and the number of items already retrieved, then updates the page.
 * Hides the moreTimelineItemsButton while loading.
 * @param parameters  The parameters to use for retrieving the timeline items.
 * @param updateHistory If updateHistory is true the browser history is updated to reflect the current page. If false it is assumed the page was loaded from history or reload, and is already in the history stack.
 */
async function getTimelineList(parameters, updateHistory = true) {
    startLoadingSpinner();
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.classList.add('d-none');
    }
    parameters.skip = timelineItemsList.length;
    timeLineParameters.skip = parameters.skip;
    setBrowserUrl(parameters, true);
    await fetch('/Timeline/GetTimelineData', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(parameters)
    }).then(async function (getTimelineListResult) {
        if (getTimelineListResult != null) {
            updateSettingsNotificationDiv();
            const newTimelineItemsList = (await getTimelineListResult.json());
            if (newTimelineItemsList.timeLineItems.length > 0) {
                await processTimelineList(newTimelineItemsList);
                if (updateHistory) {
                    setBrowserUrl(parameters, true);
                }
            }
        }
    }).catch(function (error) {
        console.log('Error loading TimelineList. Error: ' + error);
    });
    firstRun = false;
    stopLoadingSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates the timeLineParameters with values from newTimelineItemsList, then adds the TimelineItems in the list of timelineItems to the page.
 * If there are no more remaining items the load more button is hidden.
 * @param newTimelineItemsList The TimelineList object with a list of items to add and data about remaining items.
 */
async function processTimelineList(timelineResponse) {
    const onThisDayPostsParentDiv = document.querySelector('#latest-posts-parent-div');
    if (onThisDayPostsParentDiv !== null) {
        onThisDayPostsParentDiv.classList.remove('d-none');
    }
    for await (const timelineItemToAdd of timelineResponse.timeLineItems) {
        timelineItemsList.push(timelineItemToAdd);
        await renderTimelineItem(timelineItemToAdd);
    }
    ;
    if (timelineResponse.remainingItemsCount > 0 && moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.classList.remove('d-none');
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Fetches the HTML for a given timeline item and renders it at the end of timeline-items-div.
* @param timelineItem The timelineItem object to add to the page.
*/
async function renderTimelineItem(timelineItem) {
    const timeLineItemViewModel = new TimeLineItemViewModel();
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
        const timelineDiv = document.querySelector('#timeline-items-div');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
            addTimelineItemEventListener(timelineItem);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Sets the url in the browser address bar to reflect the current page.
 * @param parameters The PicturesPageParameters currently in use.
 * @param replaceState If true, the current url will replace the url in the active one in history, if false the url will be added to the history.
 */
function setBrowserUrl(parameters, replaceState) {
    const url = new URL(window.location.href);
    url.searchParams.set('childId', parameters.progenyId.toString());
    url.searchParams.set('sortOrder', parameters.sortOrder.toString());
    url.searchParams.set('items', parameters.numberOfItems.toString());
    url.searchParams.set('skip', timelineItemsList.length.toString());
    url.searchParams.set('year', parameters.year.toString());
    url.searchParams.set('month', parameters.month.toString());
    url.searchParams.set('day', parameters.day.toString());
    url.searchParams.set('tagFilter', parameters.tagFilter.toString());
    url.searchParams.set('categoryFilter', parameters.categoryFilter.toString());
    url.searchParams.set('contextFilter', parameters.contextFilter.toString());
    if (replaceState) {
        window.history.replaceState({}, '', url);
    }
    else {
        window.history.pushState({}, '', url);
    }
}
/** Retrieves the parameters from the url in browser address bar.
 * Then loads the timeline list with the parameters retrived.
 */
async function loadPageFromHistory() {
    const url = new URL(window.location.href);
    if (timeLineParameters !== null) {
        timeLineParameters.progenyId = url.searchParams.get('childId') ? parseInt(url.searchParams.get('childId')) : 0;
        timeLineParameters.sortOrder = url.searchParams.get('sortOrder') ? parseInt(url.searchParams.get('sortOrder')) : 1;
        let initialCount = url.searchParams.get('items') ? parseInt(url.searchParams.get('items')) : 10;
        let skipValue = url.searchParams.get('skip') ? parseInt(url.searchParams.get('skip')) : 0;
        timeLineParameters.numberOfItems = skipValue;
        timeLineParameters.skip = 0;
        timeLineParameters.year = url.searchParams.get('year') ? parseInt(url.searchParams.get('year')) : 0;
        timeLineParameters.month = url.searchParams.get('month') ? parseInt(url.searchParams.get('month')) : 0;
        timeLineParameters.day = url.searchParams.get('day') ? parseInt(url.searchParams.get('day')) : 0;
        firstRun = false;
        await getTimelineList(timeLineParameters, false);
        timeLineParameters.numberOfItems = initialCount;
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Updates the div that shows the current sort order and start date.
*/
function updateSettingsNotificationDiv() {
    let timelineSettingsNotificationText;
    if (timeLineParameters.sortOrder === 0) {
        timelineSettingsNotificationText = sortAscendingSettingsButton?.innerHTML;
    }
    else {
        timelineSettingsNotificationText = sortDescendingSettingsButton?.innerHTML;
    }
    timelineSettingsNotificationText += '<br/>' + startLabelDiv?.innerHTML;
    timelineSettingsNotificationText += timelineStartDateTimePicker.val();
    const tagFilterSpan = document.querySelector('#tag-filter-span');
    if (timeLineParameters.tagFilter !== '') {
        timelineSettingsNotificationText += '<br/>' + tagFilterSpan?.innerHTML + timeLineParameters.tagFilter;
    }
    const categoryFilterSpan = document.querySelector('#category-filter-span');
    if (timeLineParameters.categoryFilter !== '') {
        timelineSettingsNotificationText += '<br/>' + categoryFilterSpan?.innerHTML + timeLineParameters.categoryFilter;
    }
    const contextFilterSpan = document.querySelector('#context-filter-span');
    if (timeLineParameters.contextFilter !== '') {
        timelineSettingsNotificationText += '<br/>' + contextFilterSpan?.innerHTML + timeLineParameters.contextFilter;
    }
    if (timelineSettingsNotificationDiv !== null && timelineSettingsNotificationText !== undefined) {
        timelineSettingsNotificationDiv.innerHTML = timelineSettingsNotificationText;
    }
}
/** Clears the list of timeline elements in the timeline-items-div and scrolls to above the timeline-items-div.
*/
function clearTimeLineElements() {
    const pageTitleDiv = document.querySelector('#page-title-div');
    if (pageTitleDiv !== null) {
        pageTitleDiv.scrollIntoView();
    }
    timelineItemsList = [];
    firstRun = false;
    const timelineItemsDiv = document.querySelector('#timeline-items-div');
    if (timelineItemsDiv !== null) {
        timelineItemsDiv.innerHTML = '';
    }
}
/** Gets the formatted date value from the start date picker and sets the start date in the parameters.
* @param dateTimeFormatMoment The Moment format of the date, which is used by the date picker.
*/
async function setStartDate(dateTimeFormatMoment) {
    let settingsStartValue = SettingsHelper.getPageSettingsStartDate(dateTimeFormatMoment);
    timeLineParameters.year = settingsStartValue.year();
    timeLineParameters.month = settingsStartValue.month() + 1;
    timeLineParameters.day = settingsStartValue.date();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets the value of the startDateTimePicker to the given date in the format defined by startDateTimeFormatMoment.
 * @param date The date to assign to the DateTimePicker.
 */
function updateStartDatePicker(date) {
    if (timelineStartDateTimePicker !== null) {
        const dateString = getFormattedDateString(date, startDateTimeFormatMoment);
        timelineStartDateTimePicker.val(dateString);
    }
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 * Updates the Start date if it hasn't been set so the list starts at the earliest date.
 */
async function sortTimelineAscending() {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    timeLineParameters.sortOrder = 0;
    let currentDate = new Date();
    if (timeLineParameters.year === currentDate.getFullYear() && timeLineParameters.month - 1 === currentDate.getMonth() && timeLineParameters.day === currentDate.getDate()) {
        timeLineParameters.year = timeLineParameters.firstItemYear;
        timeLineParameters.month = 1;
        timeLineParameters.day = 1;
        const earliestDate = new Date(timeLineParameters.firstItemYear, 0, 1);
        updateStartDatePicker(earliestDate);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the descending button as active, and the ascending button as inactive.
 * Updates the Start date if it hasn't been set, so the list starts at today's date.
 */
async function sortTimelineDescending() {
    sortDescendingSettingsButton?.classList.add('active');
    sortAscendingSettingsButton?.classList.remove('active');
    timeLineParameters.sortOrder = 1;
    let currentDate = new Date();
    if (timeLineParameters.year === timeLineParameters.firstItemYear && timeLineParameters.month === 1 && timeLineParameters.day === 1) {
        timeLineParameters.year = currentDate.getFullYear();
        timeLineParameters.month = currentDate.getMonth() + 1;
        timeLineParameters.day = currentDate.getDate();
        updateStartDatePicker(currentDate);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Toggles the active state of the tag filter button and updates the tag filter in the parameters.
 */
function toggleShowFilters() {
    const filtersElements = document.querySelectorAll('.timeline-filter-options');
    const toggleShowFiltersChevron = document.getElementById('show-filters-chevron');
    filtersElements.forEach(function (element) {
        if (element.classList.contains('d-none')) {
            element.classList.remove('d-none');
        }
        else {
            element.classList.add('d-none');
        }
    });
    if (toggleShowFiltersChevron !== null) {
        if (toggleShowFiltersChevron.classList.contains('chevron-right-rotate-down')) {
            toggleShowFiltersChevron.classList.remove('chevron-right-rotate-down');
        }
        else {
            toggleShowFiltersChevron.classList.add('chevron-right-rotate-down');
        }
    }
}
/**
 * Adds or removes the TimeLineType in the onThisDayParameters.timeLineTypeFilter.
 * @param type The TimeLineType to toggle.
 */
function toggleTimeLineType(type) {
    const index = timeLineParameters.timeLineTypeFilter.indexOf(type);
    if (index > -1) {
        timeLineParameters.timeLineTypeFilter.splice(index, 1);
    }
    else {
        timeLineParameters.timeLineTypeFilter.push(type);
    }
    updateTimeLineTypeButtons();
}
/**
 * Updates the TimeLineType buttons to show the currently selected types as active.
 */
function updateTimeLineTypeButtons() {
    const allButton = document.querySelector('#toggle-all-time-line-types-button');
    if (allButton !== null) {
        if (timeLineParameters.timeLineTypeFilter.length === 0) {
            allButton.classList.add('active');
        }
        else {
            allButton?.classList.remove('active');
        }
    }
    const typeButtons = document.querySelectorAll('.timeline-type-filter-button');
    typeButtons.forEach(function (button) {
        button.classList.remove('active');
        if (timeLineParameters.timeLineTypeFilter.includes(parseInt(button.dataset.type ?? '-1'))) {
            button.classList.add('active');
        }
    });
}
/**
 * Sets the TimeLineTypeFilter to empty, which means all types are included.
 */
function setTimeLineTypeFilterToAll() {
    timeLineParameters.timeLineTypeFilter = [];
    updateTimeLineTypeButtons();
}
/**
 * Saves the current page parameters to local storage and reloads the timeline items list.
 */
async function saveTimelinePageSettings() {
    const numberOfItemsToGetSelect = document.querySelector('#items-per-page-select');
    if (numberOfItemsToGetSelect !== null) {
        timeLineParameters.numberOfItems = parseInt(numberOfItemsToGetSelect.value);
    }
    else {
        timeLineParameters.numberOfItems = 10;
    }
    const tagFilterInput = document.querySelector('#tag-filter-input');
    if (tagFilterInput !== null) {
        timeLineParameters.tagFilter = tagFilterInput.value;
    }
    const categoryFilterInput = document.querySelector('#category-filter-input');
    if (categoryFilterInput !== null) {
        timeLineParameters.categoryFilter = categoryFilterInput.value;
    }
    const contextFilterInput = document.querySelector('#context-filter-input');
    if (contextFilterInput !== null) {
        timeLineParameters.contextFilter = contextFilterInput.value;
    }
    SettingsHelper.savePageSettings(timelinePageSettingsStorageKey, timeLineParameters);
    SettingsHelper.toggleShowPageSettings();
    clearTimeLineElements();
    await getTimelineList(timeLineParameters);
    if (timelineItemsList.length === 0) {
        await getTimelineList(timeLineParameters);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Retrieves timelineParameters saved in local storage.
 */
async function loadTimelinePageSettings() {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings(timelinePageSettingsStorageKey);
    if (pageSettingsFromStorage) {
        if (pageSettingsFromStorage.progenyId === timeLineProgenyId) {
            timeLineParameters.firstItemYear = pageSettingsFromStorage.firstItemYear ?? 1900;
        }
        timeLineParameters.sortOrder = pageSettingsFromStorage.sortOrder ?? 1;
        if (timeLineParameters.sortOrder === 0) {
            sortTimelineAscending();
        }
        else {
            sortTimelineDescending();
        }
        timeLineParameters.numberOfItems = pageSettingsFromStorage.numberOfItems ?? 10;
        const selectItemsPerPageElement = document.querySelector('#items-per-page-select');
        if (selectItemsPerPageElement !== null) {
            selectItemsPerPageElement.value = timeLineParameters.numberOfItems?.toString();
            $(".selectpicker").selectpicker('refresh');
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Reads the initial page parameters from json serialized data in the timeline-page-parameters-div element's data-timeline-page-parameters attribute.
 * If the page is navigated to without specific parameters, itemsPerPage, sort, and sortTags parameters are loaded from local storage.
 */
function getParametersFromPageProperties() {
    const timelineParametersDiv = document.querySelector('#timeline-page-parameters-div');
    if (timelineParametersDiv !== null) {
        const pageParameters = timelineParametersDiv.dataset.timelinePageParameters;
        if (pageParameters) {
            const parameters = JSON.parse(pageParameters);
            if (parameters !== null) {
                timeLineParameters.progenyId = parameters.progenyId;
                timeLineParameters.sortOrder = parameters.sortBy ?? 1;
                timeLineParameters.year = parameters.year ?? 0;
                timeLineParameters.month = parameters.month ?? 0;
                timeLineParameters.day = parameters.day ?? 0;
                timeLineParameters.firstItemYear = parameters.firstItemYear ?? 1900;
            }
        }
    }
}
/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    startDateTimeFormatMoment = getLongDateTimeFormatMoment();
    timelineStartDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { setStartDate(startDateTimeFormatMoment); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const timelineSaveSettingsButton = document.querySelector('#timeline-page-save-settings-button');
    if (timelineSaveSettingsButton !== null) {
        timelineSaveSettingsButton.addEventListener('click', saveTimelinePageSettings);
    }
    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortTimelineAscending);
        sortDescendingSettingsButton.addEventListener('click', sortTimelineDescending);
    }
    // Event listener for the show filters button.
    const toggleShowFiltersButton = document.querySelector('#timeline-toggle-filters-button');
    if (toggleShowFiltersButton !== null) {
        toggleShowFiltersButton.addEventListener('click', function (event) {
            event.preventDefault();
            toggleShowFilters();
        });
    }
    // Event listeners for TimeLineType buttons.
    const typeButtons = document.querySelectorAll('.timeline-type-filter-button');
    typeButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            toggleTimeLineType(parseInt(button.dataset.type ?? '-1'));
        });
    });
    const allButton = document.querySelector('#toggle-all-time-line-types-button');
    if (allButton !== null) {
        allButton.addEventListener('click', setTimeLineTypeFilterToAll);
    }
    await setTagsAutoSuggestList(timeLineParameters.progenies, 'tag-filter-input', true);
    await setCategoriesAutoSuggestList(timeLineParameters.progenies, 'category-filter-input', true);
    await setContextAutoSuggestList(timeLineParameters.progenies, 'context-filter-input', true);
    return new Promise(function (resolve, reject) {
        resolve();
    });
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
    const selectItemsPerPageElement = document.querySelector('#items-per-page-select');
    if (selectItemsPerPageElement !== null) {
        $(".selectpicker").selectpicker('refresh');
    }
}
function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            timeLineParameters.progenies = getSelectedProgenies();
            timelineItemsList = [];
            const timelineDiv = document.querySelector('#timeline-items-div');
            if (timelineDiv !== null) {
                timelineDiv.innerHTML = '';
            }
            await getTimelineList(timeLineParameters);
        }
    });
}
/** Initialization and setup when page is loaded */
document.addEventListener('DOMContentLoaded', async function () {
    languageId = getCurrentLanguageId();
    timeLineProgenyId = getCurrentProgenyId();
    initialSettingsPanelSetup();
    addSelectedProgeniesChangedEventListener();
    SettingsHelper.initPageSettings();
    timeLineParameters.progenies = getSelectedProgenies();
    moreTimelineItemsButton = document.querySelector('#more-timeline-items-button');
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.addEventListener('click', async () => {
            getTimelineList(timeLineParameters);
        });
    }
    addBrowserNavigationEventListeners();
    getParametersFromPageProperties();
    await loadTimelinePageSettings();
    refreshSelectPickers();
    await getTimelineList(timeLineParameters);
    if (firstRun) { // getTimelineList updated the parameters and exited early to reload with the new values.
        await getTimelineList(timeLineParameters);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=timeline-index.js.map