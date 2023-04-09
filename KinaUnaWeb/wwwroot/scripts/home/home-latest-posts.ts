import { TimelineItem, TimelineParameters, TimeLineItemViewModel } from '../page-models.js';
import {getCurrentProgenyId } from '../data-tools.js';
let timelineItemsList: TimelineItem[] = []
const timeLineParameters: TimelineParameters = new TimelineParameters();
let currentProgenyId: number;

async function getTimelineList(parameters: TimelineParameters) {

    await fetch('/Timeline/GetTimelineList', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(parameters)
    }).then(async function (getTimelineListResult) {
        if (getTimelineListResult != null) {
            const newTimeLineItemsList = (await getTimelineListResult.json()) as TimelineItem[];
            if (newTimeLineItemsList.length > 0) {
                await appendTimelineItems(newTimeLineItemsList);
            }
        }
    }).catch(function (error) {
        console.log('Error loading TimelineList. Error: ' + error);
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function appendTimelineItems(timelineItems: TimelineItem[]) {
    timelineItems.forEach( async (timelineItem) => {
        timelineItemsList.push(timelineItem);
        await renderTimelineItem(timelineItem);
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
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

$(async function () {
    currentProgenyId = getCurrentProgenyId();
    timeLineParameters.count = 5;
    timeLineParameters.skip = 0;
    timeLineParameters.progenyId = currentProgenyId;

    await getTimelineList(timeLineParameters);
});