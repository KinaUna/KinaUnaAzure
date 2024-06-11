import * as LocaleHelper from '../localization-v2.js';
import { TimelineParameters, TimeLineItemViewModel } from '../page-models-v2.js';
import { getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, getTimeLineStartDate, getFormattedDateString } from '../data-tools-v2.js';
import * as SettingsHelper from '../settings-tools.js';
const timelinePageSettingsStorageKey = 'timeline_page_parameters';
let timelineItemsList = [];
const timeLineParameters = new TimelineParameters();
let latestPostsProgenyId;
let moreTimelineItemsButton;
let numberOfItemsDiv;
let firstRun = true;
const sortAscendingSettingsButton = document.querySelector('#sort-timeline-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#sort-timeline-descending-button');
const timelineStartDateTimePicker = $('#timeline-start-date-datetimepicker');
const timelineSettingsNotificationDiv = document.querySelector('#timeline-settings-notification-div');
const startLabelDiv = document.querySelector('#start-label-div');
function runWaitMeMoreTimelineItemsButton() {
    const moreItemsButton = $('#loadingTimeLineItemsDiv');
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
function stopWaitMeMoreTimelineItemsButton() {
    const moreItemsButton = $('#loadingTimeLineItemsDiv');
    moreItemsButton.waitMe("hide");
}
async function getTimelineList(parameters) {
    runWaitMeMoreTimelineItemsButton();
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.classList.add('d-none');
    }
    parameters.skip = timelineItemsList.length;
    if (firstRun) {
        const itemsCountDiv = document.querySelector('#itemsCountDiv');
        if (itemsCountDiv !== null) {
            const itemsCountDivData = itemsCountDiv.dataset.itemsCount;
            if (itemsCountDivData) {
                parameters.count = parseInt(itemsCountDivData);
            }
        }
        const startYearDiv = document.querySelector('#startYearDiv');
        if (startYearDiv !== null) {
            const startYearDivData = startYearDiv.dataset.startYear;
            if (startYearDivData) {
                parameters.year = parseInt(startYearDivData);
            }
        }
        const startMonthDiv = document.querySelector('#startMonthDiv');
        if (startMonthDiv !== null) {
            const startMonthDivData = startMonthDiv.dataset.startMonth;
            if (startMonthDivData) {
                parameters.month = parseInt(startMonthDivData);
            }
        }
        const startDayDiv = document.querySelector('#startDayDiv');
        if (startDayDiv !== null) {
            const startDayDivData = startDayDiv.dataset.startDay;
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
            const newTimeLineItemsList = (await getTimelineListResult.json());
            if (newTimeLineItemsList.timelineItems.length > 0) {
                timeLineParameters.firstItemYear = newTimeLineItemsList.firstItemYear;
                const latestPostsParentDiv = document.querySelector('#latestPostsParentDiv');
                if (latestPostsParentDiv !== null) {
                    latestPostsParentDiv.classList.remove('d-none');
                }
                for await (const timelineItemToAdd of newTimeLineItemsList.timelineItems) {
                    timelineItemsList.push(timelineItemToAdd);
                    await renderTimelineItem(timelineItemToAdd);
                    window.history.replaceState("state", "title", "TimeLine?sortBy=" + parameters.sortBy + "&items=" + timelineItemsList.length + "&year=" + parameters.year + "&month=" + parameters.month + "&day=" + parameters.day);
                }
                ;
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function updateSettingsNotificationDiv() {
    let timelineSettingsNotificationText;
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
        const timelineDiv = document.querySelector('#timelineItemsDiv');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function clearTimeLineElements() {
    timelineItemsList = [];
    firstRun = false;
    const timelineItemsDiv = document.querySelector('#timelineItemsDiv');
    if (timelineItemsDiv !== null) {
        timelineItemsDiv.innerHTML = '';
    }
}
async function setStartDate(longDateTimeFormatMoment) {
    let sTime = getTimeLineStartDate(longDateTimeFormatMoment);
    timeLineParameters.year = sTime.year();
    timeLineParameters.month = sTime.month() + 1;
    timeLineParameters.day = sTime.date();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function sortTimelineAscending() {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    timeLineParameters.sortBy = 0;
    let currentDate = new Date();
    if (timeLineParameters.year === currentDate.getFullYear() && timeLineParameters.month - 1 === currentDate.getMonth() && timeLineParameters.day === currentDate.getDate()) {
        timeLineParameters.year = timeLineParameters.firstItemYear;
        timeLineParameters.month = 1;
        timeLineParameters.day = 1;
        if (timelineStartDateTimePicker !== null) {
            const earliestDate = new Date(timeLineParameters.firstItemYear, 0, 1);
            const earlistDateString = getFormattedDateString(earliestDate, longDateTimeFormatMoment);
            timelineStartDateTimePicker.val(earlistDateString);
        }
    }
    return new Promise(function (resolve, reject) {
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function saveTimelinePageSettings() {
    const numberOfItemsToGetSelect = document.querySelector('#nextItemsCount');
    if (numberOfItemsToGetSelect !== null) {
        timeLineParameters.count = parseInt(numberOfItemsToGetSelect.value);
    }
    else {
        timeLineParameters.count = 10;
    }
    SettingsHelper.savePageSettings(timelinePageSettingsStorageKey, timeLineParameters);
    clearTimeLineElements();
    await getTimelineList(timeLineParameters);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function loadTimelinePageSettings() {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings(timelinePageSettingsStorageKey);
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
        const selectItemsPerPageElement = document.querySelector('#nextItemsCount');
        if (selectItemsPerPageElement !== null) {
            selectItemsPerPageElement.value = timeLineParameters.count.toString();
            $(".selectpicker").selectpicker('refresh');
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let longDateTimeFormatMoment;
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
        onSelect: function (a, b, c) { setStartDate(longDateTimeFormatMoment); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    SettingsHelper.initPageSettings();
    moreTimelineItemsButton = document.querySelector('#moreTimelineItemsButton');
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.addEventListener('click', async () => {
            getTimelineList(timeLineParameters);
        });
    }
    const timelineSaveSettingsButton = document.querySelector('#timeline-page-save-settings-button');
    if (timelineSaveSettingsButton !== null) {
        timelineSaveSettingsButton.addEventListener('click', saveTimelinePageSettings);
    }
    numberOfItemsDiv = document.querySelector('#numberOfItemsDiv');
    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortTimelineAscending);
        sortDescendingSettingsButton.addEventListener('click', sortTimelineDescending);
    }
    await loadTimelinePageSettings();
    await getTimelineList(timeLineParameters);
});
//# sourceMappingURL=timeline-index.js.map