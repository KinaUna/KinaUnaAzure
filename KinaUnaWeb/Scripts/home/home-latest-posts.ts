import { TimelineItem, TimelineParameters, TimeLineItemViewModel } from '../page-models.js';
import {getCurrentProgenyId } from '../data-tools.js';
let timelineItemsList: TimelineItem[] = []
const timeLineParameters: TimelineParameters = new TimelineParameters();
let currentProgenyId: number;
let moreTimelineItemsButton: HTMLButtonElement | null;

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
                for await (const timelineItemToAdd of newTimeLineItemsList) {
                    timelineItemsList.push(timelineItemToAdd);
                    await renderTimelineItem(timelineItemToAdd);
                };
                if (moreTimelineItemsButton !== null) {
                    moreTimelineItemsButton.classList.remove('d-none');
                }
            }
            else {
                
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

    moreTimelineItemsButton = document.querySelector<HTMLButtonElement>('#moreTimelineItemsButton');
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.addEventListener('click', async () => {
            getTimelineList(timeLineParameters);
        });
    }

    await getTimelineList(timeLineParameters);
});