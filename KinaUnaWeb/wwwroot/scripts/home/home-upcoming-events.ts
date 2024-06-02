import { TimelineItem, TimelineParameters, TimeLineItemViewModel, TimelineList } from '../page-models-v1.js';
import { getCurrentProgenyId } from '../data-tools-v1.js';
let upcomingEventsList: TimelineItem[] = []
const upcomingEventsParameters: TimelineParameters = new TimelineParameters();
let upcomingEventsProgenyId: number;
let moreUpcomingEventsButton: HTMLButtonElement | null;

function runWaitMeMoreUpcomingEventsButton(): void {
    const moreItemsButton: any = $('#loadingUpcomingEventsDiv');
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

function stopWaitMeMoreUpcomingEventsButton(): void {
    const moreItemsButton: any = $('#loadingUpcomingEventsDiv');
    moreItemsButton.waitMe("hide");
}

async function getUpcomingEventsList(parameters: TimelineParameters) {
    runWaitMeMoreUpcomingEventsButton();
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
                const upcomingEventsParentDiv = document.querySelector<HTMLDivElement>('#upcomingEventsParentDiv');
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

    stopWaitMeMoreUpcomingEventsButton();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function renderUpcomingEvent(timelineItem: TimelineItem) {
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
        const timelineDiv = document.querySelector<HTMLDivElement>('#upcomingEventsDiv');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

$(async function () {
    upcomingEventsProgenyId = getCurrentProgenyId();
    upcomingEventsParameters.count = 5;
    upcomingEventsParameters.skip = 0;
    upcomingEventsParameters.progenyId = upcomingEventsProgenyId;

    moreUpcomingEventsButton = document.querySelector<HTMLButtonElement>('#moreUpcomingEventsButton');
    if (moreUpcomingEventsButton !== null) {
        moreUpcomingEventsButton.addEventListener('click', async () => {
            getUpcomingEventsList(upcomingEventsParameters);
        });
    }

    await getUpcomingEventsList(upcomingEventsParameters);
});