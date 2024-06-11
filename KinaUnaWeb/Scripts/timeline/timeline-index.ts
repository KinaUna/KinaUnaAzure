import * as LocaleHelper from '../localization-v2.js';
import { TimelineItem, TimelineParameters, TimeLineItemViewModel, TimelineList } from '../page-models-v2.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getTimeLineStartDate, getFormattedDateString } from '../data-tools-v2.js';
import * as SettingsHelper from '../settings-tools.js';

const timelinePageSettingsStorageKey = 'timeline_page_parameters';

declare let sortBy: number;
declare let startYear: number;
declare let startMonth: number;
declare let startDay: number;

let timelineItemsList: TimelineItem[] = []
const timeLineParameters: TimelineParameters = new TimelineParameters();
let latestPostsProgenyId: number;
let moreTimelineItemsButton: HTMLButtonElement | null;
let numberOfItemsDiv: HTMLDivElement | null;
let firstRun: boolean = true;

const sortAscendingSettingsButton = document.querySelector<HTMLButtonElement>('#sort-timeline-ascending-button');
const sortDescendingSettingsButton = document.querySelector<HTMLButtonElement>('#sort-timeline-descending-button');
const timelineStartDateTimePicker: any = $('#timeline-start-date-datetimepicker');
const timelineSettingsNotificationDiv = document.querySelector<HTMLDivElement>('#timeline-settings-notification-div');
const startLabelDiv = document.querySelector<HTMLDivElement>('#start-label-div');

function runWaitMeMoreTimelineItemsButton(): void {
    const moreItemsButton: any = $('#loadingTimeLineItemsDiv');
    moreItemsButton.waitMe({
        effect: 'bounce',
        text: '',
        bg: 'rgba(177, 77, 227, 0.0)',
        color: '#9011a1',
        maxSize: '',
        waitTime: -1,
        source: '',
        textPos: 'vertical',
        fontSize: '',
        onClose: function () { }
    });
}

function stopWaitMeMoreTimelineItemsButton(): void {
    const moreItemsButton: any = $('#loadingTimeLineItemsDiv');
    moreItemsButton.waitMe("hide");
}

async function getTimelineList(parameters: TimelineParameters) {
    runWaitMeMoreTimelineItemsButton();
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.classList.add('d-none');
    }
    
    parameters.skip = timelineItemsList.length;
    
    if (firstRun) {
        const itemsCountDiv = document.querySelector<HTMLDivElement>('#itemsCountDiv');
        if (itemsCountDiv !== null) {
            const itemsCountDivData: string | undefined = itemsCountDiv.dataset.itemsCount;
            if (itemsCountDivData) {
                parameters.count = parseInt(itemsCountDivData);
            }
            
        }
        const startYearDiv = document.querySelector<HTMLDivElement>('#startYearDiv');
        if (startYearDiv !== null) {
            const startYearDivData: string | undefined = startYearDiv.dataset.startYear;
            if (startYearDivData) {
                parameters.year = parseInt(startYearDivData);
            }
        }
        
        const startMonthDiv = document.querySelector<HTMLDivElement>('#startMonthDiv');
        if (startMonthDiv !== null) {
            const startMonthDivData: string | undefined = startMonthDiv.dataset.startMonth;
            if (startMonthDivData) {
                parameters.month = parseInt(startMonthDivData);
            }
        }

        const startDayDiv = document.querySelector<HTMLDivElement>('#startDayDiv');
        if (startDayDiv !== null) {
            const startDayDivData: string | undefined = startDayDiv.dataset.startDay;
            if (startDayDivData) {
                parameters.day = parseInt(startDayDivData);
            }
        }
    }
    
    firstRun = false;
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
            const newTimeLineItemsList = (await getTimelineListResult.json()) as TimelineList;
            if (newTimeLineItemsList.timelineItems.length > 0) {
                timeLineParameters.firstItemYear = newTimeLineItemsList.firstItemYear;
                const latestPostsParentDiv = document.querySelector<HTMLDivElement>('#latestPostsParentDiv');
                if (latestPostsParentDiv !== null) {
                    latestPostsParentDiv.classList.remove('d-none');
                }
                for await (const timelineItemToAdd of newTimeLineItemsList.timelineItems) {
                    timelineItemsList.push(timelineItemToAdd);
                    await renderTimelineItem(timelineItemToAdd);
                    window.history.replaceState("state", "title", "TimeLine?sortBy=" + parameters.sortBy + "&items=" + timelineItemsList.length + "&year=" + parameters.year + "&month=" + parameters.month + "&day=" + parameters.day);
                };
                if (newTimeLineItemsList.remainingItemsCount > 0 && moreTimelineItemsButton !== null) {
                    moreTimelineItemsButton.classList.remove('d-none');
                }

                window.history.replaceState("state", "title", "TimeLine?sortBy=" + parameters.sortBy + "&items=" + timelineItemsList.length + "&year=" + parameters.year + "&month=" + parameters.month + "&day=" + parameters.day);
            }
        }
    }).catch(function (error) {
        console.log('Error loading TimelineList. Error: ' + error);
    });

    stopWaitMeMoreTimelineItemsButton();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

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

async function renderTimelineItem(timelineItem: TimelineItem) {
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
        const timelineDiv = document.querySelector<HTMLDivElement>('#timelineItemsDiv');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function clearTimeLineElements(): void {
    timelineItemsList = [];
    firstRun = false;
    const timelineItemsDiv = document.querySelector<HTMLDivElement>('#timelineItemsDiv');
    if (timelineItemsDiv !== null) {
        timelineItemsDiv.innerHTML = '';
    }
}

async function setStartDate(longDateTimeFormatMoment: string) {
    let sTime: any = getTimeLineStartDate(longDateTimeFormatMoment);
    
    timeLineParameters.year = sTime.year();
    timeLineParameters.month = sTime.month() + 1;
    timeLineParameters.day = sTime.date();
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function sortTimelineAscending() {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    timeLineParameters.sortBy = 0;
    let currentDate = new Date();
    if (timeLineParameters.year === currentDate.getFullYear() && timeLineParameters.month -1 === currentDate.getMonth() && timeLineParameters.day === currentDate.getDate()) {
        timeLineParameters.year = timeLineParameters.firstItemYear;
        timeLineParameters.month = 1;
        timeLineParameters.day = 1;
                
        if (timelineStartDateTimePicker !== null) {
            const earliestDate = new Date(timeLineParameters.firstItemYear, 0, 1);
            const earlistDateString = getFormattedDateString(earliestDate, longDateTimeFormatMoment);
            timelineStartDateTimePicker.val(earlistDateString);
        }
    }
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function sortTimelineDescending() {
    sortDescendingSettingsButton?.classList.add('active');
    sortAscendingSettingsButton?.classList.remove('active');
    timeLineParameters.sortBy = 1;
    let currentDate = new Date();
    if (timeLineParameters.year === timeLineParameters.firstItemYear && timeLineParameters.month === 1 && timeLineParameters.day === 1) {
        timeLineParameters.year = currentDate.getFullYear();
        timeLineParameters.month = currentDate.getMonth() + 1;
        timeLineParameters.day = currentDate.getDate();
        
        if (timelineStartDateTimePicker !== null) {
            const currentDateString = getFormattedDateString(currentDate, longDateTimeFormatMoment);
            timelineStartDateTimePicker.val(currentDateString);
        }
    }
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function saveTimelinePageSettings() {
    const numberOfItemsToGetSelect = document.querySelector<HTMLSelectElement>('#nextItemsCount');
    if (numberOfItemsToGetSelect !== null) {
        timeLineParameters.count = parseInt(numberOfItemsToGetSelect.value);
    }
    else {
        timeLineParameters.count = 10;
    }

    SettingsHelper.savePageSettings<TimelineParameters>(timelinePageSettingsStorageKey, timeLineParameters);
    clearTimeLineElements();
    await getTimelineList(timeLineParameters);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function loadTimelinePageSettings() {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings<TimelineParameters>(timelinePageSettingsStorageKey);
    if (pageSettingsFromStorage) {
        timeLineParameters.firstItemYear = pageSettingsFromStorage.firstItemYear;
        timeLineParameters.sortBy = pageSettingsFromStorage.sortBy;
        if (timeLineParameters.sortBy === 0) {
            sortTimelineAscending();
            
        }
        else {
            sortTimelineDescending();
        }

        timeLineParameters.count = pageSettingsFromStorage.count;
        const selectItemsPerPageElement = document.querySelector<HTMLSelectElement>('#nextItemsCount');
        if (selectItemsPerPageElement !== null) {
            selectItemsPerPageElement.value = timeLineParameters.count.toString();
            ($(".selectpicker") as any).selectpicker('refresh');
        }
    }
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

let zebraDatePickerTranslations: LocaleHelper.ZebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat: string;
let longDateTimeFormatMoment: string;

$(async function () {
    latestPostsProgenyId = getCurrentProgenyId();
    timeLineParameters.count = 10;
    timeLineParameters.skip = 0;
    timeLineParameters.progenyId = latestPostsProgenyId;
    timeLineParameters.sortBy = sortBy;
    timeLineParameters.year = startYear;
    timeLineParameters.month = startMonth;
    timeLineParameters.day = startDay;
    
    languageId = getCurrentLanguageId();
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat();
    
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);

    longDateTimeFormatMoment = getLongDateTimeFormatMoment();
    
    timelineStartDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a: any, b: any, c: any) { setStartDate(longDateTimeFormatMoment); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });

    SettingsHelper.initPageSettings();
    
    moreTimelineItemsButton = document.querySelector<HTMLButtonElement>('#moreTimelineItemsButton');
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.addEventListener('click', async () => {
            getTimelineList(timeLineParameters);
        });
    }

    const timelineSaveSettingsButton = document.querySelector<HTMLButtonElement>('#timeline-page-save-settings-button');
    if (timelineSaveSettingsButton !== null) {
        timelineSaveSettingsButton.addEventListener('click', saveTimelinePageSettings);
    }
    
    numberOfItemsDiv = document.querySelector<HTMLDivElement>('#numberOfItemsDiv');

    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortTimelineAscending);
        sortDescendingSettingsButton.addEventListener('click', sortTimelineDescending);
    }

    await loadTimelinePageSettings();

    await getTimelineList(timeLineParameters);
});