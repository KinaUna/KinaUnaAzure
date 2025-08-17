import { TimelineParameters, TimeLineItemViewModel } from '../page-models-v9.js';
import { getCurrentProgenyId } from '../data-tools-v9.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v9.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v9.js';
import { getSelectedProgenies } from '../settings-tools-v9.js';
let timelineItemsList = [];
const timeLineParameters = new TimelineParameters();
let latestPostsProgenyId;
let moreTimelineItemsButton;
/**
 * Starts the spinner for loading the timeline items.
 */
function startLoadingTimelineItemsSpinner() {
    startLoadingItemsSpinner('loading-latest-posts-items-div');
}
/**
 * Stops the spinner for loading the timeline items.
 */
function stopLoadingTimelineItemsSpinner() {
    stopLoadingItemsSpinner('loading-latest-posts-items-div');
}
/**
 * Retrieves the list of timeline items, based on the parameters provided and the number of items already retrieved, then updates the page.
 * Hides the moreTimelineItemsButton while loading.
 * @param parameters The parameters to use for retrieving the timeline items.
 */
async function getTimelineList(parameters) {
    startLoadingTimelineItemsSpinner();
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.classList.add('d-none');
    }
    parameters.skip = timelineItemsList.length;
    await fetch('/Timeline/GetTimelineList', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(parameters)
    }).then(async function (getTimelineListResult) {
        if (getTimelineListResult != null) {
            const newTimeLineItemsList = (await getTimelineListResult.json());
            if (newTimeLineItemsList.timelineItems.length > 0) {
                const latestPostsParentDiv = document.querySelector('#latest-posts-parent-div');
                if (latestPostsParentDiv !== null) {
                    latestPostsParentDiv.classList.remove('d-none');
                }
                for await (const timelineItemToAdd of newTimeLineItemsList.timelineItems) {
                    timelineItemsList.push(timelineItemToAdd);
                    await renderTimelineItem(timelineItemToAdd);
                }
                ;
                if (newTimeLineItemsList.remainingItemsCount > 0 && moreTimelineItemsButton !== null) {
                    moreTimelineItemsButton.classList.remove('d-none');
                }
            }
        }
    }).catch(function (error) {
        console.log('Error loading TimelineList. Error: ' + error);
    });
    stopLoadingTimelineItemsSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Fetches the HTML for a given timeline item and renders it at the end of timeline-items-div.
 * Adds an event listener to the item, to display a popup with more details when clicked.
 * @param timelineItem The timelineItem object to add to the div.
 */
async function renderTimelineItem(timelineItem) {
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
function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            timeLineParameters.progenies = getSelectedProgenies();
            timelineItemsList = [];
            const timelineDiv = document.querySelector('#timeline-items-div');
            if (timelineDiv != null) {
                timelineDiv.innerHTML = '';
            }
            await getTimelineList(timeLineParameters);
        }
    });
}
/**
 * Initializes page settings and sets up event listeners when page is first loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    latestPostsProgenyId = getCurrentProgenyId();
    timeLineParameters.count = 5;
    timeLineParameters.skip = 0;
    timeLineParameters.progenyId = latestPostsProgenyId;
    timeLineParameters.progenies = getSelectedProgenies();
    addSelectedProgeniesChangedEventListener();
    moreTimelineItemsButton = document.querySelector('#more-latest-posts-items-button');
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.addEventListener('click', async () => {
            getTimelineList(timeLineParameters);
        });
    }
    await getTimelineList(timeLineParameters);
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=home-latest-posts.js.map