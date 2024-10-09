import { TimelineItem, TimelineParameters, TimeLineItemViewModel, TimelineList } from '../page-models-v8.js';
import { getCurrentProgenyId } from '../data-tools-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v8.js';

let timelineItemsList: TimelineItem[] = []
const timeLineParameters: TimelineParameters = new TimelineParameters();
let latestPostsProgenyId: number;
let moreTimelineItemsButton: HTMLButtonElement | null;

/**
 * Starts the spinner for loading the timeline items.
 */
function startLoadingTimelineItemsSpinner(): void {
    startLoadingItemsSpinner('loading-latest-posts-items-div');
}

/**
 * Stops the spinner for loading the timeline items.
 */
function stopLoadingTimelineItemsSpinner(): void {
    stopLoadingItemsSpinner('loading-latest-posts-items-div');
}

/**
 * Retrieves the list of timeline items, based on the parameters provided and the number of items already retrieved, then updates the page.
 * Hides the moreTimelineItemsButton while loading.
 * @param parameters The parameters to use for retrieving the timeline items.
 */
async function getTimelineList(parameters: TimelineParameters, reset: boolean = false): Promise<void> {
    startLoadingTimelineItemsSpinner();

    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.classList.add('d-none');
    }

    if (reset) {
        timelineItemsList = [];
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
            const newTimeLineItemsList = (await getTimelineListResult.json()) as TimelineList;
            if (newTimeLineItemsList.timelineItems.length > 0) {
                const latestPostsParentDiv = document.querySelector<HTMLDivElement>('#latest-posts-parent-div');
                if (latestPostsParentDiv !== null) {
                    latestPostsParentDiv.classList.remove('d-none');
                    if (reset) {
                        const timelineDiv = document.querySelector<HTMLDivElement>('#timeline-items-div');
                        if (timelineDiv != null) {
                            timelineDiv.innerHTML = '';
                        }
                    }
                }
                for await (const timelineItemToAdd of newTimeLineItemsList.timelineItems) {
                    timelineItemsList.push(timelineItemToAdd);
                    await renderTimelineItem(timelineItemToAdd);
                };
                if (newTimeLineItemsList.remainingItemsCount > 0 && moreTimelineItemsButton !== null) {
                    moreTimelineItemsButton.classList.remove('d-none');
                }
            }
        }
    }).catch(function (error) {
        console.log('Error loading TimelineList. Error: ' + error);
    });

    stopLoadingTimelineItemsSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Fetches the HTML for a given timeline item and renders it at the end of timeline-items-div.
 * Adds an event listener to the item, to display a popup with more details when clicked.
 * @param timelineItem The timelineItem object to add to the div.
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

function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            getSelectedProgenies();
            await getTimelineList(timeLineParameters, true);
        }

    });
}

function getSelectedProgenies() {
    let selectedProgenies = localStorage.getItem('selectedProgenies');
    if (selectedProgenies !== null) {
        let selectedProgenyIds: string[] = JSON.parse(selectedProgenies);
        let progeniesIds = selectedProgenyIds.map(function (id) {
            return parseInt(id);
        });
        timeLineParameters.progenies = progeniesIds;
    }
}

/**
 * Initializes page settings and sets up event listeners when page is first loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    latestPostsProgenyId = getCurrentProgenyId();
    timeLineParameters.count = 5;
    timeLineParameters.skip = 0;
    timeLineParameters.progenyId = latestPostsProgenyId;
    getSelectedProgenies();

    moreTimelineItemsButton = document.querySelector<HTMLButtonElement>('#more-latest-posts-items-button');
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.addEventListener('click', async () => {
            getTimelineList(timeLineParameters);
        });
    }

    await getTimelineList(timeLineParameters);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});