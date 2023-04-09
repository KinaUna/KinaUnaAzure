import { TimelineParameters, TimeLineItemViewModel } from '../page-models.js';
import { getCurrentProgenyId } from '../data-tools.js';
let timelineItemsList = [];
const timeLineParameters = new TimelineParameters();
let currentProgenyId;
let moreTimelineItemsButton;
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
            if (newTimeLineItemsList.length > 0) {
                for await (const timelineItemToAdd of newTimeLineItemsList) {
                    timelineItemsList.push(timelineItemToAdd);
                    await renderTimelineItem(timelineItemToAdd);
                }
                ;
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
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
$(async function () {
    currentProgenyId = getCurrentProgenyId();
    timeLineParameters.count = 5;
    timeLineParameters.skip = 0;
    timeLineParameters.progenyId = currentProgenyId;
    moreTimelineItemsButton = document.querySelector('#moreTimelineItemsButton');
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.addEventListener('click', async () => {
            getTimelineList(timeLineParameters);
        });
    }
    await getTimelineList(timeLineParameters);
});
//# sourceMappingURL=home-latest-posts.js.map