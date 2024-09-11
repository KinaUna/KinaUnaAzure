import * as LocaleHelper from '../localization-v6.js';
import { TimelineItem, TimelineParameters, TimeLineItemViewModel, TimelineList } from '../page-models-v6.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getFormattedDateString } from '../data-tools-v7.js';
import * as SettingsHelper from '../settings-tools-v6.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v6.js';
import { addTimelineItemEventListener } from '../item-details/items-display.js';

const timelinePageSettingsStorageKey = 'timeline_page_parameters';
let timelineItemsList: TimelineItem[] = []
let timeLineParameters: TimelineParameters = new TimelineParameters();
let timeLineProgenyId: number;
let languageId = 1;
let moreTimelineItemsButton: HTMLButtonElement | null;
let firstRun: boolean = true;

const sortAscendingSettingsButton = document.querySelector<HTMLButtonElement>('#setting-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector<HTMLButtonElement>('#setting-sort-descending-button');
const timelineStartDateTimePicker: any = $('#settings-start-date-datetimepicker');
const timelineSettingsNotificationDiv = document.querySelector<HTMLDivElement>('#settings-notification-div');
const startLabelDiv = document.querySelector<HTMLDivElement>('#start-label-div');

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let zebraDateTimeFormat: string;
let startDateTimeFormatMoment: string;

/** Shows the loading spinner in the loading-timeline-items-div.
 */
function startLoadingSpinner(): void {
    startLoadingItemsSpinner('loading-timeline-items-div');
}

/** Hides the loading spinner in the loading-timeline-items-div.
 */
function stopLoadingSpinner(): void {
    stopLoadingItemsSpinner('loading-timeline-items-div');
}

/**
 * Retrieves the list of timeline items, based on the parameters provided and the number of items already retrieved, then updates the page.
 * Hides the moreTimelineItemsButton while loading.
 * @param parameters  The parameters to use for retrieving the timeline items.
 * @param updateHistory If updateHistory is true the browser history is updated to reflect the current page. If false it is assumed the page was loaded from history or reload, and is already in the history stack.
 */
async function getTimelineList(parameters: TimelineParameters, updateHistory: Boolean = true): Promise<void> {
    startLoadingSpinner();
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.classList.add('d-none');
    }
    
    parameters.skip = timelineItemsList.length;
    timeLineParameters.skip = parameters.skip;
    setBrowserUrl(parameters, true);
    await fetch('/Timeline/GetTimelineList', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(parameters)
    }).then(async function (getTimelineListResult) {
        if (getTimelineListResult != null) {
            updateSettingsNotificationDiv();
            const newTimelineItemsList = (await getTimelineListResult.json()) as TimelineList;
            if (newTimelineItemsList.timelineItems.length > 0) {

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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates the timeLineParameters with values from newTimelineItemsList, then adds the TimelineItems in the list of timelineItems to the page.
 * If there are no more remaining items the load more button is hidden.
 * @param newTimelineItemsList The TimelineList object with a list of items to add and data about remaining items.
 */
async function processTimelineList(newTimelineItemsList: TimelineList): Promise<void> {
    if (firstRun && timeLineParameters.firstItemYear !== newTimelineItemsList.firstItemYear) {
        timeLineParameters.firstItemYear = newTimelineItemsList.firstItemYear;

        if (timeLineParameters.sortBy === 0) {
            timeLineParameters.year = newTimelineItemsList.firstItemYear;
            timeLineParameters.month = 1;
            timeLineParameters.day = 1;
            updateStartDatePicker(new Date(timeLineParameters.year, timeLineParameters.month - 1, timeLineParameters.day));
            return new Promise<void>(function (resolve, reject) {
                resolve();
            });
        }
    }

    const latestPostsParentDiv = document.querySelector<HTMLDivElement>('#latest-posts-parent-div');
    if (latestPostsParentDiv !== null) {
        latestPostsParentDiv.classList.remove('d-none');
    }
    for await (const timelineItemToAdd of newTimelineItemsList.timelineItems) {
        timelineItemsList.push(timelineItemToAdd);
        await renderTimelineItem(timelineItemToAdd);
    };
    if (newTimelineItemsList.remainingItemsCount > 0 && moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.classList.remove('d-none');
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
        const timelineDiv = document.querySelector<HTMLDivElement>('#timeline-items-div');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
            addTimelineItemEventListener(timelineItem);
        }


    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Sets the url in the browser address bar to reflect the current page.
 * @param parameters The PicturesPageParameters currently in use.
 * @param replaceState If true, the current url will replace the url in the active one in history, if false the url will be added to the history.
 */
function setBrowserUrl(parameters: TimelineParameters, replaceState: boolean): void {
    const url = new URL(window.location.href);
    url.searchParams.set('childId', parameters.progenyId.toString());
    url.searchParams.set('sortBy', parameters.sortBy.toString());
    url.searchParams.set('items', parameters.count.toString());
    url.searchParams.set('skip', timelineItemsList.length.toString());
    url.searchParams.set('year', parameters.year.toString());
    url.searchParams.set('month', parameters.month.toString());
    url.searchParams.set('day', parameters.day.toString());
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
async function loadPageFromHistory(): Promise<void> {
    const url = new URL(window.location.href);

    if (timeLineParameters !== null) {
        timeLineParameters.progenyId = url.searchParams.get('childId') ? parseInt(url.searchParams.get('childId') as string) : 0;
        timeLineParameters.sortBy = url.searchParams.get('sortBy') ? parseInt(url.searchParams.get('sortBy') as string) : 1;
        let initialCount: number = url.searchParams.get('items') ? parseInt(url.searchParams.get('items') as string) : 10;
        let skipValue: number = url.searchParams.get('skip') ? parseInt(url.searchParams.get('skip') as string) : 0;
        timeLineParameters.count = skipValue;
        timeLineParameters.skip = 0;
        timeLineParameters.year = url.searchParams.get('year') ? parseInt(url.searchParams.get('year') as string) : 0;
        timeLineParameters.month = url.searchParams.get('month') ? parseInt(url.searchParams.get('month') as string) : 0;
        timeLineParameters.day = url.searchParams.get('day') ? parseInt(url.searchParams.get('day') as string) : 0;
        
        firstRun = false;
        await getTimelineList(timeLineParameters, false);
        timeLineParameters.count = initialCount;
    }
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Updates the div that shows the current sort order and start date.
*/
function updateSettingsNotificationDiv(): void {
    let timelineSettingsNotificationText: string | undefined;
    if (timeLineParameters.sortBy === 0) {
        timelineSettingsNotificationText = sortAscendingSettingsButton?.innerHTML;
    }
    else {
        timelineSettingsNotificationText = sortDescendingSettingsButton?.innerHTML;
    }
    timelineSettingsNotificationText += '<br/>' + startLabelDiv?.innerHTML;
    timelineSettingsNotificationText += timelineStartDateTimePicker.val();

    if (timelineSettingsNotificationDiv !== null && timelineSettingsNotificationText !== undefined) {
        timelineSettingsNotificationDiv.innerHTML = timelineSettingsNotificationText;
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
    const timelineItemsDiv = document.querySelector<HTMLDivElement>('#timeline-items-div');
    if (timelineItemsDiv !== null) {
        timelineItemsDiv.innerHTML = '';
    }
}

/** Gets the formatted date value from the start date picker and sets the start date in the parameters.
* @param dateTimeFormatMoment The Moment format of the date, which is used by the date picker.
*/
async function setStartDate(dateTimeFormatMoment: string): Promise<void> {
    let settingsStartValue: any = SettingsHelper.getPageSettingsStartDate(dateTimeFormatMoment);
    
    timeLineParameters.year = settingsStartValue.year();
    timeLineParameters.month = settingsStartValue.month() + 1;
    timeLineParameters.day = settingsStartValue.date();
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets the value of the startDateTimePicker to the given date in the format defined by startDateTimeFormatMoment.
 * @param date The date to assign to the DateTimePicker.
 */
function updateStartDatePicker(date: Date): void {
    if (timelineStartDateTimePicker !== null) {
        const dateString = getFormattedDateString(date, startDateTimeFormatMoment);
        timelineStartDateTimePicker.val(dateString);
    }
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 * Updates the Start date if it hasn't been set so the list starts at the earliest date.
 */
async function sortTimelineAscending(): Promise<void> {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    timeLineParameters.sortBy = 0;
    let currentDate = new Date();
    if (timeLineParameters.year === currentDate.getFullYear() && timeLineParameters.month -1 === currentDate.getMonth() && timeLineParameters.day === currentDate.getDate()) {
        timeLineParameters.year = timeLineParameters.firstItemYear;
        timeLineParameters.month = 1;
        timeLineParameters.day = 1;

        const earliestDate = new Date(timeLineParameters.firstItemYear, 0, 1);
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
    timeLineParameters.sortBy = 1;
    let currentDate = new Date();
    if (timeLineParameters.year === timeLineParameters.firstItemYear && timeLineParameters.month === 1 && timeLineParameters.day === 1) {
        timeLineParameters.year = currentDate.getFullYear();
        timeLineParameters.month = currentDate.getMonth() + 1;
        timeLineParameters.day = currentDate.getDate();
                
        updateStartDatePicker(currentDate);
    }
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Saves the current page parameters to local storage and reloads the timeline items list.
 */
async function saveTimelinePageSettings(): Promise<void> {
    const numberOfItemsToGetSelect = document.querySelector<HTMLSelectElement>('#items-per-page-select');
    if (numberOfItemsToGetSelect !== null) {
        timeLineParameters.count = parseInt(numberOfItemsToGetSelect.value);
    }
    else {
        timeLineParameters.count = 10;
    }

    SettingsHelper.savePageSettings<TimelineParameters>(timelinePageSettingsStorageKey, timeLineParameters);
    SettingsHelper.toggleShowPageSettings();
    clearTimeLineElements();
    await getTimelineList(timeLineParameters);
    if (timelineItemsList.length === 0) {
        await getTimelineList(timeLineParameters);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Retrieves timelineParameters saved in local storage.
 */
async function loadTimelinePageSettings(): Promise<void> {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings<TimelineParameters>(timelinePageSettingsStorageKey);
    if (pageSettingsFromStorage) {
        if (pageSettingsFromStorage.progenyId === timeLineProgenyId) {
            timeLineParameters.firstItemYear = pageSettingsFromStorage.firstItemYear;
        }        
        timeLineParameters.sortBy = pageSettingsFromStorage.sortBy;
        if (timeLineParameters.sortBy === 0) {
            sortTimelineAscending();
        }
        else {
            sortTimelineDescending();
        }

        timeLineParameters.count = pageSettingsFromStorage.count;
        const selectItemsPerPageElement = document.querySelector<HTMLSelectElement>('#items-per-page-select');
        if (selectItemsPerPageElement !== null) {
            selectItemsPerPageElement.value = timeLineParameters.count.toString();
            ($(".selectpicker") as any).selectpicker('refresh');
        }
    }
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Reads the initial page parameters from json serialized data in the timeline-page-parameters-div element's data-timeline-page-parameters attribute.
 * If the page is navigated to without specific parameters, itemsPerPage, sort, and sortTags parameters are loaded from local storage.
 */ 
function getParametersFromPageProperties(): void {
    const timelineParametersDiv = document.querySelector<HTMLDivElement>('#timeline-page-parameters-div');
    if (timelineParametersDiv !== null) {
        const pageParameters: string | undefined = timelineParametersDiv.dataset.timelinePageParameters;
        if (pageParameters) {
            const parameters = JSON.parse(pageParameters);
            if (parameters !== null) {
                timeLineParameters.progenyId = parameters.progenyId;
                timeLineParameters.sortBy = parameters.sortBy;
                timeLineParameters.year = parameters.year;
                timeLineParameters.month = parameters.month;
                timeLineParameters.day = parameters.day;
                timeLineParameters.firstItemYear = parameters.firstItemYear;
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

    timelineStartDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { setStartDate(startDateTimeFormatMoment); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    const timelineSaveSettingsButton = document.querySelector<HTMLButtonElement>('#timeline-page-save-settings-button');
    if (timelineSaveSettingsButton !== null) {
        timelineSaveSettingsButton.addEventListener('click', saveTimelinePageSettings);
    }

    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortTimelineAscending);
        sortDescendingSettingsButton.addEventListener('click', sortTimelineDescending);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Adds event listeners for users reloading, navigating back or forward in the browser history. */
function addBrowserNavigationEventListeners(): void {
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
function refreshSelectPickers(): void {
    const selectItemsPerPageElement = document.querySelector<HTMLSelectElement>('#items-per-page-select');
    if (selectItemsPerPageElement !== null) {
        ($(".selectpicker") as any).selectpicker('refresh');
    }
}


/** Initialization and setup when page is loaded */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    languageId = getCurrentLanguageId();
    timeLineProgenyId = getCurrentProgenyId();

    initialSettingsPanelSetup();

    SettingsHelper.initPageSettings();


    moreTimelineItemsButton = document.querySelector<HTMLButtonElement>('#more-timeline-items-button');
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});