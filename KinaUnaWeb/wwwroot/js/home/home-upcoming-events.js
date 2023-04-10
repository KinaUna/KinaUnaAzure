import { TimelineParameters, TimeLineItemViewModel } from '../page-models.js';
import { getCurrentProgenyId } from '../data-tools.js';
let upcomingEventsList = [];
const upcomingEventsParameters = new TimelineParameters();
let upcomingEventsProgenyId;
let moreUpcomingEventsButton;
function runWaitMeMoreUpcomingEventsButton() {
    const moreItemsButton = $('#loadingUpcomingEventsDiv');
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
function stopWaitMeMoreUpcomingEventsButton() {
    const moreItemsButton = $('#loadingUpcomingEventsDiv');
    moreItemsButton.waitMe("hide");
}
async function getUpcomingEventsList(parameters) {
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
            const newUpcomingEventsList = (await getUpcomingEventsListResult.json());
            console.log(newUpcomingEventsList);
            if (newUpcomingEventsList.timelineItems.length > 0) {
                const upcomingEventsParentDiv = document.querySelector('#upcomingEventsParentDiv');
                if (upcomingEventsParentDiv !== null) {
                    upcomingEventsParentDiv.classList.remove('d-none');
                }
                for await (const eventToAdd of newUpcomingEventsList.timelineItems) {
                    upcomingEventsList.push(eventToAdd);
                    await renderUpcomingEvent(eventToAdd);
                }
                ;
                if (newUpcomingEventsList.remainingItemsCount > 0 && moreUpcomingEventsButton !== null) {
                    moreUpcomingEventsButton.classList.remove('d-none');
                }
            }
        }
    }).catch(function (error) {
        console.log('Error loading TimelineList. Error: ' + error);
    });
    stopWaitMeMoreUpcomingEventsButton();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function renderUpcomingEvent(timelineItem) {
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
        const timelineDiv = document.querySelector('#upcomingEventsDiv');
        if (timelineDiv != null) {
            timelineDiv.insertAdjacentHTML('beforeend', timelineElementHtml);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
$(async function () {
    upcomingEventsProgenyId = getCurrentProgenyId();
    upcomingEventsParameters.count = 5;
    upcomingEventsParameters.skip = 0;
    upcomingEventsParameters.progenyId = upcomingEventsProgenyId;
    moreUpcomingEventsButton = document.querySelector('#moreUpcomingEventsButton');
    if (moreUpcomingEventsButton !== null) {
        moreUpcomingEventsButton.addEventListener('click', async () => {
            getUpcomingEventsList(upcomingEventsParameters);
        });
    }
    await getUpcomingEventsList(upcomingEventsParameters);
});
//# sourceMappingURL=home-upcoming-events.js.map