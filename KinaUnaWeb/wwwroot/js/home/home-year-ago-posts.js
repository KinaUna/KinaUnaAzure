import { TimelineParameters, TimeLineItemViewModel } from '../page-models-v2.js';
import { getCurrentProgenyId } from '../data-tools-v2.js';
let yearAgoItemsList = [];
const yearAgoParameters = new TimelineParameters();
let yearAgoProgenyId;
let moreYearAgoItemsButton;
function runWaitMeMoreYearAgoItemsButton() {
    const moreItemsButton = $('#loadingYearAgoItemsDiv');
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
function stopWaitMeMoreYearAgoItemsButton() {
    const moreItemsButton = $('#loadingYearAgoItemsDiv');
    moreItemsButton.waitMe("hide");
}
async function getYearAgoList(parameters) {
    runWaitMeMoreYearAgoItemsButton();
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
                const yearAgoPostsParentDiv = document.querySelector('#yearAgoPostsParentDiv');
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
    stopWaitMeMoreYearAgoItemsButton();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function renderYearAgoItem(timelineItem) {
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
        const yearAgoElementHtml = await getTimelineElementResponse.text();
        const yearAgoItemsDiv = document.querySelector('#yearAgoItemsDiv');
        if (yearAgoItemsDiv != null) {
            yearAgoItemsDiv.insertAdjacentHTML('beforeend', yearAgoElementHtml);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
$(async function () {
    yearAgoProgenyId = getCurrentProgenyId();
    yearAgoParameters.count = 5;
    yearAgoParameters.skip = 0;
    yearAgoParameters.progenyId = yearAgoProgenyId;
    moreYearAgoItemsButton = document.querySelector('#moreYearAgoPostsButton');
    if (moreYearAgoItemsButton !== null) {
        moreYearAgoItemsButton.addEventListener('click', async () => {
            getYearAgoList(yearAgoParameters);
        });
    }
    await getYearAgoList(yearAgoParameters);
});
//# sourceMappingURL=home-year-ago-posts.js.map