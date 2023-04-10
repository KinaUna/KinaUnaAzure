import { TimelineItem, TimelineParameters, TimeLineItemViewModel, TimelineList } from '../page-models.js';
import { getCurrentProgenyId } from '../data-tools.js';
let yearAgoItemsList: TimelineItem[] = []
const yearAgoParameters: TimelineParameters = new TimelineParameters();
let yearAgoProgenyId: number;
let moreYearAgoItemsButton: HTMLButtonElement | null;

function runWaitMeMoreYearAgoItemsButton(): void {
    const moreItemsButton: any = $('#loadingYearAgoItemsDiv');
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

function stopWaitMeMoreYearAgoItemsButton(): void {
    const moreItemsButton: any = $('#loadingYearAgoItemsDiv');
    moreItemsButton.waitMe("hide");
}

async function getYearAgoList(parameters: TimelineParameters) {
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
            const newYearAgoItemsList = (await getYearAgoListResult.json()) as TimelineList;
            if (newYearAgoItemsList.timelineItems.length > 0) {
                const yearAgoPostsParentDiv = document.querySelector<HTMLDivElement>('#yearAgoPostsParentDiv');
                if (yearAgoPostsParentDiv !== null) {
                    yearAgoPostsParentDiv.classList.remove('d-none');
                }
                for await (const yearAgoItemToAdd of newYearAgoItemsList.timelineItems) {
                    yearAgoItemsList.push(yearAgoItemToAdd);
                    await renderYearAgoItem(yearAgoItemToAdd);
                };
                if (newYearAgoItemsList.remainingItemsCount > 0 && moreYearAgoItemsButton !== null) {
                    moreYearAgoItemsButton.classList.remove('d-none');
                }
            }
        }
    }).catch(function (error) {
        console.log('Error loading TimelineList. Error: ' + error);
    });

    stopWaitMeMoreYearAgoItemsButton();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function renderYearAgoItem(timelineItem: TimelineItem) {
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
        const yearAgoElementHtml = await getTimelineElementResponse.text();
        const yearAgoItemsDiv = document.querySelector<HTMLDivElement>('#yearAgoItemsDiv');
        if (yearAgoItemsDiv != null) {
            yearAgoItemsDiv.insertAdjacentHTML('beforeend', yearAgoElementHtml);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

$(async function () {
    yearAgoProgenyId = getCurrentProgenyId();
    yearAgoParameters.count = 5;
    yearAgoParameters.skip = 0;
    yearAgoParameters.progenyId = yearAgoProgenyId;

    moreYearAgoItemsButton = document.querySelector<HTMLButtonElement>('#moreYearAgoPostsButton');
    if (moreYearAgoItemsButton !== null) {
        moreYearAgoItemsButton.addEventListener('click', async () => {
            getYearAgoList(yearAgoParameters);
        });
    }

    await getYearAgoList(yearAgoParameters);
});