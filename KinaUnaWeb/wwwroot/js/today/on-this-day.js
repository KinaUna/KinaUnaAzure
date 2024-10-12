import * as LocaleHelper from '../localization-v8.js';
import { TimeLineItemViewModel, OnThisDayRequest } from '../page-models-v8.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getFormattedDateString, setTagsAutoSuggestList, setCategoriesAutoSuggestList, setContextAutoSuggestList } from '../data-tools-v8.js';
import * as SettingsHelper from '../settings-tools-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v8.js';
import { getSelectedProgenies } from '../settings-tools-v8.js';
const onThisDayPageSettingsStorageKey = 'on_this_day_page_parameters';
const onThisDayParameters = new OnThisDayRequest();
let onThisDayProgenyId;
let languageId = 1;
let moreOnThisDayItemsButton;
let firstRun = true;
let timelineItemsList = [];
const sortAscendingSettingsButton = document.querySelector('#setting-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#setting-sort-descending-button');
const onThisDayStartDateTimePicker = $('#settings-start-date-datetimepicker');
const onThisDaySettingsNotificationDiv = document.querySelector('#settings-notification-div');
const startLabelDiv = document.querySelector('#start-label-div');
const periodLabelDiv = document.querySelector('#period-label-div');
let zebraDatePickerTranslations;
let zebraDateTimeFormat;
let startDateTimeFormatMoment;
/** Shows the loading spinner in the loading-on-this-day-items-div.
 */
function startLoadingSpinner() {
    startLoadingItemsSpinner('loading-on-this-day-items-div');
}
/** Hides the loading spinner in the loading-on-this-day-items-div.
 */
function stopLoadingSpinner() {
    stopLoadingItemsSpinner('loading-on-this-day-items-div');
}
/**
 * Retrieves a OnThisDayResponse with a list of TimeLineItems and number of items remaining, based on the parameters provided and the number of items already retrieved, then updates the page.
 * Hides the moreTimelineItemsButton while loading.
 * @param parameters  The parameters to use for retrieving the timeline items.
 */
async function getOnThisDayData(parameters, reset = false) {
    startLoadingSpinner();
    if (moreOnThisDayItemsButton !== null) {
        moreOnThisDayItemsButton.classList.add('d-none');
    }
    if (reset) {
        timelineItemsList = [];
        const timelineDiv = document.querySelector('#on-this-day-items-div');
        if (timelineDiv !== null) {
            timelineDiv.innerHTML = '';
        }
    }
    parameters.skip = timelineItemsList.length;
    await fetch('/Today/GetOnThisDayList', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(parameters)
    }).then(async function (getOnThisDayListResult) {
        if (getOnThisDayListResult != null) {
            updateSettingsNotificationDiv();
            const newOnThisDayResponse = (await getOnThisDayListResult.json());
            if (newOnThisDayResponse.timeLineItems.length > 0) {
                await processOnThisDayList(newOnThisDayResponse);
            }
        }
    }).catch(function (error) {
        console.log('Error loading OnThisDay data. Error: ' + error);
    });
    firstRun = false;
    stopLoadingSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function processOnThisDayList(onThisDayResponse) {
    const onThisDayPostsParentDiv = document.querySelector('#on-this-day-posts-parent-div');
    if (onThisDayPostsParentDiv !== null) {
        onThisDayPostsParentDiv.classList.remove('d-none');
    }
    for await (const timelineItemToAdd of onThisDayResponse.timeLineItems) {
        timelineItemsList.push(timelineItemToAdd);
        await renderTimelineItem(timelineItemToAdd);
    }
    ;
    if (onThisDayResponse.remainingItemsCount > 0 && moreOnThisDayItemsButton !== null) {
        moreOnThisDayItemsButton.classList.remove('d-none');
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
        const timelineDiv = document.querySelector('#on-this-day-items-div');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
            addTimelineItemEventListener(timelineItem);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Updates the div that shows the current sort order and start date.
*/
function updateSettingsNotificationDiv() {
    let onThisDaySettingsNotificationText;
    if (onThisDayParameters.sortOrder === 0) {
        onThisDaySettingsNotificationText = sortAscendingSettingsButton?.innerHTML;
    }
    else {
        onThisDaySettingsNotificationText = sortDescendingSettingsButton?.innerHTML;
    }
    onThisDaySettingsNotificationText += '<br/>' + startLabelDiv?.innerHTML;
    onThisDaySettingsNotificationText += onThisDayStartDateTimePicker.val();
    onThisDaySettingsNotificationText += '<br/>' + periodLabelDiv?.innerHTML;
    const periodButtons = document.querySelectorAll('.on-this-day-period-button');
    periodButtons.forEach(function (button) {
        if (parseInt(button.dataset.period ?? '-1') === onThisDayParameters.onThisDayPeriod) {
            onThisDaySettingsNotificationText += button.innerHTML;
        }
    });
    const tagFilterSpan = document.querySelector('#tag-filter-span');
    if (onThisDayParameters.tagFilter !== '') {
        onThisDaySettingsNotificationText += '<br/>' + tagFilterSpan?.innerHTML + onThisDayParameters.tagFilter;
    }
    const categoryFilterSpan = document.querySelector('#category-filter-span');
    if (onThisDayParameters.categoryFilter !== '') {
        onThisDaySettingsNotificationText += '<br/>' + categoryFilterSpan?.innerHTML + onThisDayParameters.categoryFilter;
    }
    const contextFilterSpan = document.querySelector('#context-filter-span');
    if (onThisDayParameters.contextFilter !== '') {
        onThisDaySettingsNotificationText += '<br/>' + contextFilterSpan?.innerHTML + onThisDayParameters.contextFilter;
    }
    if (onThisDaySettingsNotificationDiv !== null && onThisDaySettingsNotificationText !== undefined) {
        onThisDaySettingsNotificationDiv.innerHTML = onThisDaySettingsNotificationText;
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
    const timelineItemsDiv = document.querySelector('#on-this-day-items-div');
    if (timelineItemsDiv !== null) {
        timelineItemsDiv.innerHTML = '';
    }
}
/** Gets the formatted date value from the start date picker and sets the start date in the parameters.
* @param dateTimeFormatMoment The Moment format of the date, which is used by the date picker.
*/
async function setStartDate(dateTimeFormatMoment) {
    let settingsStartValue = SettingsHelper.getPageSettingsStartDate(dateTimeFormatMoment);
    onThisDayParameters.year = settingsStartValue.year();
    onThisDayParameters.month = settingsStartValue.month() + 1;
    onThisDayParameters.day = settingsStartValue.date();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets the value of the startDateTimePicker to the given date in the format defined by startDateTimeFormatMoment.
 * @param date The date to assign to the DateTimePicker.
 */
function updateStartDatePicker(date) {
    if (onThisDayStartDateTimePicker !== null) {
        const dateString = getFormattedDateString(date, startDateTimeFormatMoment);
        onThisDayStartDateTimePicker.val(dateString);
    }
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 * Updates the Start date if it hasn't been set so the list starts at the earliest date.
 */
async function sortTimelineAscending() {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    onThisDayParameters.sortOrder = 0;
    let currentDate = new Date();
    if (onThisDayParameters.year === currentDate.getFullYear() && onThisDayParameters.month - 1 === currentDate.getMonth() && onThisDayParameters.day === currentDate.getDate()) {
        onThisDayParameters.year = onThisDayParameters.firstItemYear;
        onThisDayParameters.month = 1;
        onThisDayParameters.day = 1;
        const earliestDate = new Date(onThisDayParameters.firstItemYear, 0, 1);
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
    onThisDayParameters.sortOrder = 1;
    let currentDate = new Date();
    if (onThisDayParameters.year === onThisDayParameters.firstItemYear && onThisDayParameters.month === 1 && onThisDayParameters.day === 1) {
        onThisDayParameters.year = currentDate.getFullYear();
        onThisDayParameters.month = currentDate.getMonth() + 1;
        onThisDayParameters.day = currentDate.getDate();
        updateStartDatePicker(currentDate);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets the OnThisDayPeriod, and updates the period buttons to show the selected period as active.
 * @param period The period to set as active.
 * @returns Promise<void>
 */
async function setPeriod(period) {
    onThisDayParameters.onThisDayPeriod = period;
    // update settings panel.
    updatePeriodButtons();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates the period buttons to show the currently selected period as active.
 */
function updatePeriodButtons() {
    const periodButtons = document.querySelectorAll('.on-this-day-period-button');
    periodButtons.forEach(function (button) {
        button.classList.remove('active');
        if (parseInt(button.dataset.period ?? '-1') === onThisDayParameters.onThisDayPeriod) {
            button.classList.add('active');
        }
    });
}
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
    const index = onThisDayParameters.timeLineTypeFilter.indexOf(type);
    if (index > -1) {
        onThisDayParameters.timeLineTypeFilter.splice(index, 1);
    }
    else {
        onThisDayParameters.timeLineTypeFilter.push(type);
    }
    updateTimeLineTypeButtons();
}
/**
 * Updates the TimeLineType buttons to show the currently selected types as active.
 */
function updateTimeLineTypeButtons() {
    const allButton = document.querySelector('#toggle-all-time-line-types-button');
    if (allButton !== null) {
        if (onThisDayParameters.timeLineTypeFilter.length === 0) {
            allButton.classList.add('active');
        }
        else {
            allButton?.classList.remove('active');
        }
    }
    const typeButtons = document.querySelectorAll('.timeline-type-filter-button');
    typeButtons.forEach(function (button) {
        button.classList.remove('active');
        if (onThisDayParameters.timeLineTypeFilter.includes(parseInt(button.dataset.type ?? '-1'))) {
            button.classList.add('active');
        }
    });
}
/**
 * Sets the TimeLineTypeFilter to empty, which means all types are included.
 */
function setTimeLineTypeFilterToAll() {
    onThisDayParameters.timeLineTypeFilter = [];
    updateTimeLineTypeButtons();
}
/**
 * Saves the current page parameters to local storage and reloads the timeline items list.
 */
async function saveOnThisDayPageSettings() {
    const numberOfItemsToGetSelect = document.querySelector('#items-per-page-select');
    if (numberOfItemsToGetSelect !== null) {
        onThisDayParameters.numberOfItems = parseInt(numberOfItemsToGetSelect.value);
    }
    else {
        onThisDayParameters.numberOfItems = 10;
    }
    const tagFilterInput = document.querySelector('#tag-filter-input');
    if (tagFilterInput !== null) {
        onThisDayParameters.tagFilter = tagFilterInput.value;
    }
    const categoryFilterInput = document.querySelector('#category-filter-input');
    if (categoryFilterInput !== null) {
        onThisDayParameters.categoryFilter = categoryFilterInput.value;
    }
    const contextFilterInput = document.querySelector('#context-filter-input');
    if (contextFilterInput !== null) {
        onThisDayParameters.contextFilter = contextFilterInput.value;
    }
    SettingsHelper.savePageSettings(onThisDayPageSettingsStorageKey, onThisDayParameters);
    SettingsHelper.toggleShowPageSettings();
    clearTimeLineElements();
    await getOnThisDayData(onThisDayParameters);
    if (timelineItemsList.length === 0) {
        await getOnThisDayData(onThisDayParameters);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Retrieves onThisDayParameters saved in local storage.
 */
async function loadOnThisDayPageSettings() {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings(onThisDayPageSettingsStorageKey);
    if (pageSettingsFromStorage) {
        if (pageSettingsFromStorage.progenyId === onThisDayProgenyId) {
            onThisDayParameters.firstItemYear = pageSettingsFromStorage.firstItemYear;
        }
        onThisDayParameters.sortOrder = pageSettingsFromStorage.sortOrder;
        if (onThisDayParameters.sortOrder === 0) {
            sortTimelineAscending();
        }
        else {
            sortTimelineDescending();
        }
        onThisDayParameters.numberOfItems = pageSettingsFromStorage.numberOfItems;
        const selectItemsPerPageElement = document.querySelector('#items-per-page-select');
        if (selectItemsPerPageElement !== null) {
            selectItemsPerPageElement.value = onThisDayParameters.numberOfItems.toString();
            $(".selectpicker").selectpicker('refresh');
        }
        setPeriod(pageSettingsFromStorage.onThisDayPeriod);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Reads the initial page parameters from json serialized data in the on-this-day-page-parameters-div element's data-timeline-page-parameters attribute.
 * If the page is navigated to without specific parameters, itemsPerPage, sort, and sortTags parameters are loaded from local storage.
 */
function getParametersFromPageProperties() {
    const onThisDayParametersDiv = document.querySelector('#on-this-day-page-parameters-div');
    if (onThisDayParametersDiv !== null) {
        const pageParameters = onThisDayParametersDiv.dataset.onThisDayPageParameters;
        if (pageParameters) {
            const parameters = JSON.parse(pageParameters);
            if (parameters !== null) {
                onThisDayParameters.progenyId = parameters.progenyId;
                onThisDayParameters.sortOrder = parameters.sortOrder;
                onThisDayParameters.year = parameters.year;
                onThisDayParameters.month = parameters.month;
                onThisDayParameters.day = parameters.day;
                onThisDayParameters.firstItemYear = parameters.firstItemYear;
                onThisDayParameters.onThisDayPeriod = parameters.onThisDayPeriod;
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
    onThisDayStartDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { setStartDate(startDateTimeFormatMoment); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const onThisDaySaveSettingsButton = document.querySelector('#on-this-day-page-save-settings-button');
    if (onThisDaySaveSettingsButton !== null) {
        onThisDaySaveSettingsButton.addEventListener('click', saveOnThisDayPageSettings);
    }
    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortTimelineAscending);
        sortDescendingSettingsButton.addEventListener('click', sortTimelineDescending);
    }
    // Event listeners for period buttons.
    const periodButtons = document.querySelectorAll('.on-this-day-period-button');
    periodButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            setPeriod(parseInt(button.dataset.period ?? '-1'));
        });
    });
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
    await setTagsAutoSuggestList(onThisDayParameters.progenies, 'tag-filter-input', true);
    await setCategoriesAutoSuggestList(onThisDayParameters.progenies, 'category-filter-input', true);
    await setContextAutoSuggestList(onThisDayParameters.progenies, 'context-filter-input', true);
    return new Promise(function (resolve, reject) {
        resolve();
    });
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
            onThisDayParameters.progenies = getSelectedProgenies();
            await getOnThisDayData(onThisDayParameters, true);
        }
    });
}
/** Initialization and setup when page is loaded */
document.addEventListener('DOMContentLoaded', async function () {
    languageId = getCurrentLanguageId();
    onThisDayProgenyId = getCurrentProgenyId();
    initialSettingsPanelSetup();
    SettingsHelper.initPageSettings();
    moreOnThisDayItemsButton = document.querySelector('#more-on-this-day-items-button');
    if (moreOnThisDayItemsButton !== null) {
        moreOnThisDayItemsButton.addEventListener('click', async () => {
            getOnThisDayData(onThisDayParameters);
        });
    }
    getParametersFromPageProperties();
    await loadOnThisDayPageSettings();
    refreshSelectPickers();
    addSelectedProgeniesChangedEventListener();
    onThisDayParameters.progenies = getSelectedProgenies();
    await getOnThisDayData(onThisDayParameters);
    if (firstRun) { // getOnThisDayData updated the parameters and exited early to reload with the new values.
        await getOnThisDayData(onThisDayParameters);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=on-this-day.js.map