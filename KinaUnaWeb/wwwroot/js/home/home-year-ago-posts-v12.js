import { TimelineParameters, TimeLineItemViewModel } from '../page-models-v12.js';
import { getCurrentProgenyId } from '../data-tools-v12.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v12.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v12.js';
import { getSelectedFamilies, getSelectedProgenies } from '../settings-tools-v12.js';
let yearAgoItemsList = [];
const yearAgoParameters = new TimelineParameters();
let yearAgoProgenyId;
let moreYearAgoItemsButton;
/**
 * Starts the spinner for loading the timeline items.
 */
function startLoadingYearAgoItemsSpinner() {
    startLoadingItemsSpinner('loading-year-ago-items-div');
}
/**
 * Stops the spinner for loading the timeline items.
 */
function stopLoadingYearAgoItemsSpinner() {
    stopLoadingItemsSpinner('loading-year-ago-items-div');
}
/**
 * Retrieves the list of timeline items, based on the parameters provided and the number of items already retrieved, then updates the page.
 * Hides the moreYearAgoItemsButton while loading.
 * @param parameters The parameters to use for retrieving the timeline items.
 */
async function getYearAgoList(parameters) {
    startLoadingYearAgoItemsSpinner();
    if (moreYearAgoItemsButton !== null) {
        moreYearAgoItemsButton.classList.add('d-none');
    }
    parameters.skip = yearAgoItemsList.length;
    await fetch('/Timeline/GetYearAgoList', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(parameters)
    }).then(async function (getYearAgoListResult) {
        if (getYearAgoListResult != null) {
            const newYearAgoItemsList = (await getYearAgoListResult.json());
            if (newYearAgoItemsList.timelineItems.length > 0) {
                const yearAgoPostsParentDiv = document.querySelector('#year-ago-posts-parent-div');
                if (yearAgoPostsParentDiv !== null) {
                    yearAgoPostsParentDiv.classList.remove('d-none');
                }
                for await (const yearAgoItemToAdd of newYearAgoItemsList.timelineItems) {
                    yearAgoItemsList.push(yearAgoItemToAdd);
                    await renderYearAgoItem(yearAgoItemToAdd);
                }
                ;
                if (newYearAgoItemsList.remainingItemsCount > 0 && moreYearAgoItemsButton !== null) {
                    moreYearAgoItemsButton.classList.remove('d-none');
                }
            }
        }
    }).catch(function (error) {
        console.log('Error loading TimelineList. Error: ' + error);
    });
    stopLoadingYearAgoItemsSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Fetches the HTML for a given timeline item and renders it at the end of year-ago-items-div.
 * @param timelineItem The timelineItem object to add to the div.
 */
async function renderYearAgoItem(timelineItem) {
    const timeLineItemViewModel = new TimeLineItemViewModel();
    timeLineItemViewModel.typeId = timelineItem.itemType;
    timeLineItemViewModel.itemId = parseInt(timelineItem.itemId);
    timeLineItemViewModel.itemYear = timelineItem.itemYear;
    timeLineItemViewModel.itemMonth = timelineItem.itemMonth;
    timeLineItemViewModel.itemDay = timelineItem.itemDay;
    const getTimelineElementResponse = await fetch('/Timeline/GetTimelineItemElement', {
        method: 'POST',
        body: JSON.stringify(timeLineItemViewModel),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    });
    if (getTimelineElementResponse.ok && getTimelineElementResponse.text !== null) {
        const yearAgoElementHtml = await getTimelineElementResponse.text();
        const yearAgoItemsDiv = document.querySelector('#year-ago-items-div');
        if (yearAgoItemsDiv != null) {
            yearAgoItemsDiv.insertAdjacentHTML('beforeend', yearAgoElementHtml);
            addTimelineItemEventListener(timelineItem);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets the event listeners for the moreYearAgoItemsButton.
 */
function setYearAgoEventListeners() {
    moreYearAgoItemsButton = document.querySelector('#more-year-ago-posts-button');
    if (moreYearAgoItemsButton !== null) {
        const yearAgoButtonAction = async () => {
            getYearAgoList(yearAgoParameters);
        };
        moreYearAgoItemsButton.removeEventListener('click', yearAgoButtonAction);
        moreYearAgoItemsButton.addEventListener('click', yearAgoButtonAction);
    }
}
function addSelectedProgeniesChangedEventListener() {
    const progeniesChangedAction = async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            yearAgoParameters.progenies = getSelectedProgenies();
            yearAgoParameters.families = getSelectedFamilies();
            yearAgoItemsList = [];
            const yearAgoItemsDiv = document.querySelector('#year-ago-items-div');
            if (yearAgoItemsDiv !== null) {
                yearAgoItemsDiv.innerHTML = '';
            }
            await getYearAgoList(yearAgoParameters);
        }
    };
    window.removeEventListener('progeniesChanged', progeniesChangedAction);
    window.addEventListener('progeniesChanged', progeniesChangedAction);
}
function addSelectedFamiliesChangedEventListener() {
    const familiesChangedAction = async () => {
        let selectedFamilies = localStorage.getItem('selectedFamilies');
        if (selectedFamilies !== null) {
            yearAgoParameters.progenies = getSelectedProgenies();
            yearAgoParameters.families = getSelectedFamilies();
            yearAgoItemsList = [];
            const yearAgoItemsDiv = document.querySelector('#year-ago-items-div');
            if (yearAgoItemsDiv !== null) {
                yearAgoItemsDiv.innerHTML = '';
            }
            await getYearAgoList(yearAgoParameters);
        }
    };
    window.removeEventListener('familiesChanged', familiesChangedAction);
    window.addEventListener('familiesChanged', familiesChangedAction);
}
export async function initializeYearAgo() {
    yearAgoProgenyId = getCurrentProgenyId();
    yearAgoParameters.count = 5;
    yearAgoParameters.skip = 0;
    yearAgoParameters.progenyId = yearAgoProgenyId;
    yearAgoParameters.progenies = getSelectedProgenies();
    yearAgoParameters.families = getSelectedFamilies();
    addSelectedProgeniesChangedEventListener();
    addSelectedFamiliesChangedEventListener();
    setYearAgoEventListeners();
    await getYearAgoList(yearAgoParameters);
}
//# sourceMappingURL=home-year-ago-posts-v12.js.map