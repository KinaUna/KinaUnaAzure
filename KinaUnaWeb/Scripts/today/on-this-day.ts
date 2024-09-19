import * as LocaleHelper from '../localization-v8.js';
import { TimelineItem, TimelineParameters, TimeLineItemViewModel, TimelineList, OnThisDayRequest, OnThisDayResponse, OnThisDayPeriod, TimeLineType } from '../page-models-v8.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getFormattedDateString } from '../data-tools-v8.js';
import * as SettingsHelper from '../settings-tools-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v8.js';

const onThisDayPageSettingsStorageKey = 'on_this_day_page_parameters';
const onThisDayParameters: OnThisDayRequest = new OnThisDayRequest();
let onThisDayProgenyId: number;
let languageId = 1;
let moreOnThisDayItemsButton: HTMLButtonElement | null;
let firstRun: boolean = true;
let timelineItemsList: TimelineItem[] = []

const sortAscendingSettingsButton = document.querySelector<HTMLButtonElement>('#setting-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector<HTMLButtonElement>('#setting-sort-descending-button');
const onThisDayStartDateTimePicker: any = $('#settings-start-date-datetimepicker');
const onThisDaySettingsNotificationDiv = document.querySelector<HTMLDivElement>('#settings-notification-div');
const startLabelDiv = document.querySelector<HTMLDivElement>('#start-label-div');
const periodLabelDiv = document.querySelector<HTMLDivElement>('#period-label-div');
let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let zebraDateTimeFormat: string;
let startDateTimeFormatMoment: string;

/** Shows the loading spinner in the loading-on-this-day-items-div.
 */
function startLoadingSpinner(): void {
    startLoadingItemsSpinner('loading-on-this-day-items-div');
}

/** Hides the loading spinner in the loading-on-this-day-items-div.
 */
function stopLoadingSpinner(): void {
    stopLoadingItemsSpinner('loading-on-this-day-items-div');
}

/**
 * Retrieves a OnThisDayResponse with a list of TimeLineItems and number of items remaining, based on the parameters provided and the number of items already retrieved, then updates the page.
 * Hides the moreTimelineItemsButton while loading.
 * @param parameters  The parameters to use for retrieving the timeline items.
 */
async function getOnThisDayData(parameters: OnThisDayRequest) {
    startLoadingSpinner();
    if (moreOnThisDayItemsButton !== null) {
        moreOnThisDayItemsButton.classList.add('d-none');
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
            const newOnThisDayResponse = (await getOnThisDayListResult.json()) as OnThisDayResponse;
            if (newOnThisDayResponse.timeLineItems.length > 0) {
                await processOnThisDayList(newOnThisDayResponse);               
            }
        }
    }).catch(function (error) {
        console.log('Error loading OnThisDay data. Error: ' + error);
    });
    firstRun = false;
    stopLoadingSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function processOnThisDayList(onThisDayResponse: OnThisDayResponse) {
    const onThisDayPostsParentDiv = document.querySelector<HTMLDivElement>('#on-this-day-posts-parent-div');
    if (onThisDayPostsParentDiv !== null) {
        onThisDayPostsParentDiv.classList.remove('d-none');
    }
    for await (const timelineItemToAdd of onThisDayResponse.timeLineItems) {
        timelineItemsList.push(timelineItemToAdd);
        await renderTimelineItem(timelineItemToAdd);
    };
    if (onThisDayResponse.remainingItemsCount > 0 && moreOnThisDayItemsButton !== null) {
        moreOnThisDayItemsButton.classList.remove('d-none');
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
        const timelineDiv = document.querySelector<HTMLDivElement>('#on-this-day-items-div');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
            addTimelineItemEventListener(timelineItem);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Updates the div that shows the current sort order and start date.
*/
function updateSettingsNotificationDiv(): void {
    let onThisDaySettingsNotificationText: string | undefined;
    if (onThisDayParameters.sortOrder === 0) {
        onThisDaySettingsNotificationText = sortAscendingSettingsButton?.innerHTML;
    }
    else {
        onThisDaySettingsNotificationText = sortDescendingSettingsButton?.innerHTML;
    }

    onThisDaySettingsNotificationText += '<br/>' + startLabelDiv?.innerHTML;
    onThisDaySettingsNotificationText += onThisDayStartDateTimePicker.val();

    onThisDaySettingsNotificationText += '<br/>' + periodLabelDiv?.innerHTML;
    const periodButtons = document.querySelectorAll<HTMLButtonElement>('.on-this-day-period-button');
    periodButtons.forEach(function (button: HTMLButtonElement) {
        if (parseInt(button.dataset.period ?? '-1') === onThisDayParameters.onThisDayPeriod) {
            onThisDaySettingsNotificationText += button.innerHTML;
        }
    });
    
    if (onThisDaySettingsNotificationDiv !== null && onThisDaySettingsNotificationText !== undefined) {
        onThisDaySettingsNotificationDiv.innerHTML = onThisDaySettingsNotificationText;
    }

}

/** Clears the list of timeline elements in the timeline-items-div and scrolls to above the timeline-items-div.
*/
function clearTimeLineElements(): void {
    const pageTitleDiv = document.querySelector<HTMLDivElement>('#page-title-div');
    if (pageTitleDiv !== null) {
        pageTitleDiv.scrollIntoView();
    }
    timelineItemsList = [];
    firstRun = false;
    const timelineItemsDiv = document.querySelector<HTMLDivElement>('#on-this-day-items-div');
    if (timelineItemsDiv !== null) {
        timelineItemsDiv.innerHTML = '';
    }
}

/** Gets the formatted date value from the start date picker and sets the start date in the parameters.
* @param dateTimeFormatMoment The Moment format of the date, which is used by the date picker.
*/
async function setStartDate(dateTimeFormatMoment: string): Promise<void> {
    let settingsStartValue: any = SettingsHelper.getPageSettingsStartDate(dateTimeFormatMoment);

    onThisDayParameters.year = settingsStartValue.year();
    onThisDayParameters.month = settingsStartValue.month() + 1;
    onThisDayParameters.day = settingsStartValue.date();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets the value of the startDateTimePicker to the given date in the format defined by startDateTimeFormatMoment.
 * @param date The date to assign to the DateTimePicker.
 */
function updateStartDatePicker(date: Date): void {
    if (onThisDayStartDateTimePicker !== null) {
        const dateString = getFormattedDateString(date, startDateTimeFormatMoment);
        onThisDayStartDateTimePicker.val(dateString);
    }
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 * Updates the Start date if it hasn't been set so the list starts at the earliest date.
 */
async function sortTimelineAscending(): Promise<void> {
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the descending button as active, and the ascending button as inactive.
 * Updates the Start date if it hasn't been set, so the list starts at today's date.
 */
async function sortTimelineDescending(): Promise<void> {
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets the OnThisDayPeriod, and updates the period buttons to show the selected period as active.
 * @param period The period to set as active.
 * @returns Promise<void>
 */
async function setPeriod(period: OnThisDayPeriod): Promise<void> {
    onThisDayParameters.onThisDayPeriod = period;
    // update settings panel.
    updatePeriodButtons();
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates the period buttons to show the currently selected period as active.
 */
function updatePeriodButtons(): void {
    const periodButtons = document.querySelectorAll<HTMLButtonElement>('.on-this-day-period-button');
    periodButtons.forEach(function (button: HTMLButtonElement) {
        button.classList.remove('active');
        if (parseInt(button.dataset.period ?? '-1') === onThisDayParameters.onThisDayPeriod) {
            button.classList.add('active');
        }
    });
}

function toggleShowFilters(): void {
    const filtersElements = document.querySelectorAll<HTMLDivElement>('.timeline-filter-options');
    const toggleShowFiltersChevron = document.getElementById('show-filters-chevron');
    filtersElements.forEach(function (element: HTMLDivElement) {
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
function toggleTimeLineType(type: TimeLineType): void {
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
function updateTimeLineTypeButtons(): void {
    const allButton = document.querySelector<HTMLButtonElement>('#toggle-all-time-line-types-button');
    if (allButton !== null) {

        if (onThisDayParameters.timeLineTypeFilter.length === 0) {
            allButton.classList.add('active');
            }
        else {
            allButton?.classList.remove('active');
        }    
    }
    
    const typeButtons = document.querySelectorAll<HTMLButtonElement>('.timeline-type-filter-button');
    typeButtons.forEach(function (button: HTMLButtonElement) {
        button.classList.remove('active');
        if (onThisDayParameters.timeLineTypeFilter.includes(parseInt(button.dataset.type ?? '-1'))) {
            button.classList.add('active');
        }
    });
}

/**
 * Sets the TimeLineTypeFilter to empty, which means all types are included.
 */
function setTimeLineTypeFilterToAll(): void {
    onThisDayParameters.timeLineTypeFilter = [];
    updateTimeLineTypeButtons();
}


/**
 * Saves the current page parameters to local storage and reloads the timeline items list.
 */
async function saveOnThisDayPageSettings(): Promise<void> {
    const numberOfItemsToGetSelect = document.querySelector<HTMLSelectElement>('#items-per-page-select');
    if (numberOfItemsToGetSelect !== null) {
        onThisDayParameters.numberOfItems = parseInt(numberOfItemsToGetSelect.value);
    }
    else {
        onThisDayParameters.numberOfItems = 10;
    }

    SettingsHelper.savePageSettings<OnThisDayRequest>(onThisDayPageSettingsStorageKey, onThisDayParameters);
    SettingsHelper.toggleShowPageSettings();
    clearTimeLineElements();
    await getOnThisDayData(onThisDayParameters);
    if (timelineItemsList.length === 0) {
        await getOnThisDayData(onThisDayParameters);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Retrieves onThisDayParameters saved in local storage.
 */
async function loadOnThisDayPageSettings(): Promise<void> {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings<OnThisDayRequest>(onThisDayPageSettingsStorageKey);
    if (pageSettingsFromStorage) {
        if (pageSettingsFromStorage.progenyId === onThisDayProgenyId) {
            onThisDayParameters.firstItemYear = pageSettingsFromStorage.firstItemYear;
        }
        onThisDayParameters.sortOrder= pageSettingsFromStorage.sortOrder;
        if (onThisDayParameters.sortOrder === 0) {
            sortTimelineAscending();
        }
        else {
            sortTimelineDescending();
        }

        onThisDayParameters.numberOfItems = pageSettingsFromStorage.numberOfItems;
        const selectItemsPerPageElement = document.querySelector<HTMLSelectElement>('#items-per-page-select');
        if (selectItemsPerPageElement !== null) {
            selectItemsPerPageElement.value = onThisDayParameters.numberOfItems.toString();
            ($(".selectpicker") as any).selectpicker('refresh');
        }

        setPeriod(pageSettingsFromStorage.onThisDayPeriod);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Reads the initial page parameters from json serialized data in the on-this-day-page-parameters-div element's data-timeline-page-parameters attribute.
 * If the page is navigated to without specific parameters, itemsPerPage, sort, and sortTags parameters are loaded from local storage.
 */
function getParametersFromPageProperties(): void {
    const onThisDayParametersDiv = document.querySelector<HTMLDivElement>('#on-this-day-page-parameters-div');
    if (onThisDayParametersDiv !== null) {
        const pageParameters: string | undefined = onThisDayParametersDiv.dataset.onThisDayPageParameters;
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
async function initialSettingsPanelSetup(): Promise<void> {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    startDateTimeFormatMoment = getLongDateTimeFormatMoment();

    onThisDayStartDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { setStartDate(startDateTimeFormatMoment); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    const onThisDaySaveSettingsButton = document.querySelector<HTMLButtonElement>('#on-this-day-page-save-settings-button');
    if (onThisDaySaveSettingsButton !== null) {
        onThisDaySaveSettingsButton.addEventListener('click', saveOnThisDayPageSettings);
    }

    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortTimelineAscending);
        sortDescendingSettingsButton.addEventListener('click', sortTimelineDescending);
    }
    // Event listeners for period buttons.
    const periodButtons = document.querySelectorAll<HTMLButtonElement>('.on-this-day-period-button');
    periodButtons.forEach(function (button: HTMLButtonElement) {
        button.addEventListener('click', function () {
            setPeriod(parseInt(button.dataset.period ?? '-1'));
        });
    });

    // Event listener for the show filters button.
    const toggleShowFiltersButton = document.querySelector<HTMLButtonElement>('#timeline-toggle-filters-button');
    if (toggleShowFiltersButton !== null) {
        toggleShowFiltersButton.addEventListener('click', function (event) {
            event.preventDefault();
            toggleShowFilters();
        });
    }

    // Event listeners for TimeLineType buttons.
    const typeButtons = document.querySelectorAll<HTMLButtonElement>('.timeline-type-filter-button');
    typeButtons.forEach(function (button: HTMLButtonElement) {
        button.addEventListener('click', function () {
            toggleTimeLineType(parseInt(button.dataset.type ?? '-1'));
        });
    });

    const allButton = document.querySelector<HTMLButtonElement>('#toggle-all-time-line-types-button');
    if (allButton !== null) {
        allButton.addEventListener('click', setTimeLineTypeFilterToAll);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Select pickers don't always update when their values change, this ensures they show the correct items. */
function refreshSelectPickers(): void {
    const selectItemsPerPageElement = document.querySelector<HTMLSelectElement>('#items-per-page-select');
    if (selectItemsPerPageElement !== null) {
        ($(".selectpicker") as any).selectpicker('refresh');
    }
}


/** Initialization and setup when page is loaded */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    languageId = getCurrentLanguageId();
    onThisDayProgenyId = getCurrentProgenyId();

    initialSettingsPanelSetup();

    SettingsHelper.initPageSettings();

    moreOnThisDayItemsButton = document.querySelector<HTMLButtonElement>('#more-on-this-day-items-button');
    if (moreOnThisDayItemsButton !== null) {
        moreOnThisDayItemsButton.addEventListener('click', async () => {
            getOnThisDayData(onThisDayParameters);
        });
    }

    getParametersFromPageProperties();
    await loadOnThisDayPageSettings();
    refreshSelectPickers();

    await getOnThisDayData(onThisDayParameters);
    if (firstRun) { // getOnThisDayData updated the parameters and exited early to reload with the new values.
        await getOnThisDayData(onThisDayParameters);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});