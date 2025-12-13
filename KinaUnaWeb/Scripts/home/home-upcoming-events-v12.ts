import { TimelineItem, TimelineParameters, TimeLineItemViewModel, TimelineList } from '../page-models-v12.js';
import { getCurrentProgenyId } from '../data-tools-v12.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v12.js';
import { addTimelineItemEventListener } from '../item-details/items-display-v12.js';
import { getSelectedFamilies, getSelectedProgenies } from '../settings-tools-v12.js';

let upcomingEventsList: TimelineItem[] = []
const upcomingEventsParameters: TimelineParameters = new TimelineParameters();
let upcomingEventsProgenyId: number;
let moreUpcomingEventsButton: HTMLButtonElement | null;


/**
 * Starts the spinner for loading the timeline items.
 */
function startLoadingUpcomingItemsSpinner(): void {
    startLoadingItemsSpinner('loading-upcoming-events-div');
}

/**
 * Stops the spinner for loading the timeline items.
 */
function stopLoadingUpcomingItemsSpinner(): void {
    stopLoadingItemsSpinner('loading-upcoming-events-div');
}

/**
 * Retrieves the list of upcoming calendar items, based on the parameters provided and the number of items already retrieved, then updates the page.
 * Hides the moreUpcomingEventsButton while loading.
 * @param parameters The parameters to use for retrieving the calendar items.
 */
async function getUpcomingEventsList(parameters: TimelineParameters): Promise<void> {
    startLoadingUpcomingItemsSpinner();
    if (moreUpcomingEventsButton !== null) {
        moreUpcomingEventsButton.classList.add('d-none');
    }
        
    parameters.skip = upcomingEventsList.length;
    
    await fetch('/Calendar/GetUpcomingEventsList', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(parameters)
    }).then(async function (getUpcomingEventsListResult) {
        if (getUpcomingEventsListResult != null) {
            const newUpcomingEventsList = (await getUpcomingEventsListResult.json()) as TimelineList;
            if (newUpcomingEventsList.timelineItems.length > 0) {
                const upcomingEventsParentDiv = document.querySelector<HTMLDivElement>('#upcoming-events-parent-div');
                if (upcomingEventsParentDiv !== null) {
                    upcomingEventsParentDiv.classList.remove('d-none');
                }
                for await (const eventToAdd of newUpcomingEventsList.timelineItems) {
                    upcomingEventsList.push(eventToAdd);
                    await renderUpcomingEvent(eventToAdd);
                };
                if (newUpcomingEventsList.remainingItemsCount > 0 && moreUpcomingEventsButton !== null) {
                    moreUpcomingEventsButton.classList.remove('d-none');
                }
            }
        }
    }).catch(function (error) {
        console.log('Error loading TimelineList. Error: ' + error);
    });
    
    stopLoadingUpcomingItemsSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Fetches the HTML for a given calendar item and renders it at the end of upcoming-events-div.
 * @param timelineItem The timelineItem object to add to the div.
 */
async function renderUpcomingEvent(timelineItem: TimelineItem): Promise<void> {
    const timeLineItemViewModel: TimeLineItemViewModel = new TimeLineItemViewModel();
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
        const timelineDiv = document.querySelector<HTMLDivElement>('#upcoming-events-div');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
            addTimelineItemEventListener(timelineItem);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Adds the event listeners for the upcoming events page.
  */
function setUpcomingEventsEventListeners() {
    moreUpcomingEventsButton = document.querySelector<HTMLButtonElement>('#more-upcoming-events-button');
    if (moreUpcomingEventsButton !== null) {
        const moreUpcomingButtonAction = async () => {
            getUpcomingEventsList(upcomingEventsParameters);
        }
        moreUpcomingEventsButton.removeEventListener('click', moreUpcomingButtonAction);
        moreUpcomingEventsButton.addEventListener('click', moreUpcomingButtonAction);
    }
}

/**
 * Adds an event listener for the 'progeniesChanged' event to update the upcoming events list when the selected progenies change.
 */
function addSelectedProgeniesChangedEventListener() {
    const progeniesChangedAction = async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            upcomingEventsParameters.progenies = getSelectedProgenies();
            upcomingEventsParameters.families = getSelectedFamilies();
            upcomingEventsList = [];
            const timelineDiv = document.querySelector<HTMLDivElement>('#upcoming-events-div');
            if (timelineDiv !== null) {
                timelineDiv.innerHTML = '';
            }
            await getUpcomingEventsList(upcomingEventsParameters);
        }
    }
    window.removeEventListener('progeniesChanged', progeniesChangedAction);
    window.addEventListener('progeniesChanged', progeniesChangedAction);
}

function addSelectedFamiliesChangedEventListener() {
    const familiesChangedAction = async () => {
        let selectedFamilies = localStorage.getItem('selectedFamilies');
        if (selectedFamilies !== null) {
            upcomingEventsParameters.progenies = getSelectedProgenies();
            upcomingEventsParameters.families = getSelectedFamilies();
            upcomingEventsList = [];
            const timelineDiv = document.querySelector<HTMLDivElement>('#upcoming-events-div');
            if (timelineDiv !== null) {
                timelineDiv.innerHTML = '';
            }
            await getUpcomingEventsList(upcomingEventsParameters);
        }
    }
    window.removeEventListener('familiesChanged', familiesChangedAction);
    window.addEventListener('familiesChanged', familiesChangedAction);
}

export async function initializeUpcomingEvents() {
    upcomingEventsProgenyId = getCurrentProgenyId();
    upcomingEventsParameters.count = 8;
    upcomingEventsParameters.skip = 0;
    upcomingEventsParameters.progenyId = upcomingEventsProgenyId;
    setUpcomingEventsEventListeners();
    addSelectedProgeniesChangedEventListener();
    addSelectedFamiliesChangedEventListener();
    upcomingEventsParameters.progenies = getSelectedProgenies();
    upcomingEventsParameters.families = getSelectedFamilies();

    await getUpcomingEventsList(upcomingEventsParameters);
}