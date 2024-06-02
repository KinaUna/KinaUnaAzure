import { TimelineParameters, TimeLineItemViewModel } from '../page-models-v1.js';
import { getCurrentProgenyId } from '../data-tools-v1.js';
let timelineItemsList = [];
const timeLineParameters = new TimelineParameters();
let latestPostsProgenyId;
let moreTimelineItemsButton;
let numberOfItemsDiv;
let firstRun = true;
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
    if (numberOfItemsDiv !== null) {
        numberOfItemsDiv.classList.add('d-none');
    }
    parameters.skip = timelineItemsList.length;
    parameters.sortBy = sortBy;
    if (firstRun) {
        const itemsCountDiv = document.querySelector('#itemsCountDiv');
        if (itemsCountDiv !== null) {
            const itemsCountDivData = itemsCountDiv.dataset.itemsCount;
            if (itemsCountDivData) {
                parameters.count = parseInt(itemsCountDivData);
            }
        }
    }
    else {
        const numberOfItemsToGetSelect = document.querySelector('#nextItemsCount');
        if (numberOfItemsToGetSelect !== null) {
            parameters.count = parseInt(numberOfItemsToGetSelect.value);
        }
        else {
            parameters.count = 10;
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
            const newTimeLineItemsList = (await getTimelineListResult.json());
            if (newTimeLineItemsList.timelineItems.length > 0) {
                const latestPostsParentDiv = document.querySelector('#latestPostsParentDiv');
                if (latestPostsParentDiv !== null) {
                    latestPostsParentDiv.classList.remove('d-none');
                }
                for await (const timelineItemToAdd of newTimeLineItemsList.timelineItems) {
                    timelineItemsList.push(timelineItemToAdd);
                    await renderTimelineItem(timelineItemToAdd);
                    window.history.replaceState("state", "title", "TimeLine?sortBy=" + sortBy + "&items=" + timelineItemsList.length);
                }
                ;
                if (newTimeLineItemsList.remainingItemsCount > 0 && moreTimelineItemsButton !== null) {
                    moreTimelineItemsButton.classList.remove('d-none');
                    if (numberOfItemsDiv !== null) {
                        numberOfItemsDiv.classList.remove('d-none');
                    }
                }
                window.history.replaceState("state", "title", "TimeLine?sortBy=" + sortBy + "&items=" + timelineItemsList.length);
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
    latestPostsProgenyId = getCurrentProgenyId();
    timeLineParameters.count = 10;
    timeLineParameters.skip = 0;
    timeLineParameters.progenyId = latestPostsProgenyId;
    moreTimelineItemsButton = document.querySelector('#moreTimelineItemsButton');
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.addEventListener('click', async () => {
            getTimelineList(timeLineParameters);
        });
    }
    numberOfItemsDiv = document.querySelector('#numberOfItemsDiv');
    await getTimelineList(timeLineParameters);
});
//# sourceMappingURL=timeline-index.js.map