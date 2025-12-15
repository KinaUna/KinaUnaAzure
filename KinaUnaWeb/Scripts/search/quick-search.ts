import { addTimelineItemEventListener } from "../item-details/items-display-v12.js";
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from "../navigation-tools-v12.js";
import { SearchResponse, SearchViewModel, TimelineItem, TimeLineItemViewModel } from "../page-models-v12.js";

let searchViewModel: SearchViewModel = new SearchViewModel();

export function addQuickSearchButtonEventListener() {
    const quickSearchButton = document.getElementById('quick-search-button');
    if (quickSearchButton) {
        async function handleQuickSearchClick(event: MouseEvent) {
            event.preventDefault();
            // Fetch the partial view and show the quick search modal.
            const response = await fetch('/Search/QuickSearch');
            if (response.ok) {
                const modalHtml = await response.text();
                const quickSearchModal = document.querySelector<HTMLDivElement>('#quick-search-modal-div');
                if (!quickSearchModal) return;
                quickSearchModal.innerHTML = modalHtml;
                quickSearchModal.classList.remove('d-none');
                addQuickSearchFormEventListener();
                addQuickSearchCloseButtonEventListener();
            }
        }
        quickSearchButton.removeEventListener('click', handleQuickSearchClick);
        quickSearchButton.addEventListener('click', handleQuickSearchClick);
    }
}

function addQuickSearchFormEventListener() {
    const quickSearchForm = document.querySelector<HTMLFormElement>('#quick-search-form');
    if (quickSearchForm) {
        async function handleQuickSearchFormSubmit(event: Event) {
            event.preventDefault();
            const queryInput = document.querySelector<HTMLInputElement>('#quick-search-query-input');
            if (queryInput !== null) {
                searchViewModel.query = queryInput.value;
            }
            searchViewModel.skip = 0;
            searchViewModel.numberOfItems = 10;

            // Clear the results list
            const quickSearchResultsDiv = document.querySelector<HTMLDivElement>('#quick-search-results-div');
            if (quickSearchResultsDiv) {
                quickSearchResultsDiv.innerHTML = '';
                await getQuickSearchResults();
            }
            
        }
        quickSearchForm.removeEventListener('submit', handleQuickSearchFormSubmit);
        quickSearchForm.addEventListener('submit', handleQuickSearchFormSubmit);
    }
}

async function moreSearchResultsButtonClicked(event: MouseEvent) {
    event.preventDefault();
    await getQuickSearchResults();
}

async function getQuickSearchResults(): Promise<void> {
    const moreTimelineItemsButton = document.querySelector<HTMLButtonElement>('#more-quick-search-results-button')
    if (moreTimelineItemsButton !== null) {
        moreTimelineItemsButton.classList.add('d-none');
    }

    startLoadingItemsSpinner('loading-quick-search-results-div');
        
    const response = await fetch('/Search/QuickSearch', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(searchViewModel)
    });
    if (response.ok) {
        let searchResults: SearchResponse<TimelineItem> = await response.json();
        // Process and display search results
        for (let i = 0; i < searchResults.results.length; i++) {
            await renderTimelineItem(searchResults.results[i]);

        }
        // Update skip for next batch
        searchViewModel.skip += searchResults.results.length;
        console.log('skip: ' + searchViewModel.skip);
        if (searchResults.remainingItems > 0) {
            console.log('remaining items: ' + searchResults.remainingItems);
            if (moreTimelineItemsButton !== null) {
                moreTimelineItemsButton.classList.remove('d-none');
                moreTimelineItemsButton.removeEventListener('click', moreSearchResultsButtonClicked);
                moreTimelineItemsButton.addEventListener('click', moreSearchResultsButtonClicked);
            }
        }
    }

    stopLoadingItemsSpinner('loading-quick-search-results-div');
}

/** Fetches the HTML for a given timeline item and renders it at the end of timeline-items-div.
* @param timelineItem The timelineItem object to add to the page.
*/
async function renderTimelineItem(timelineItem: TimelineItem): Promise<void> {
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
        const quickSearchResultsDiv = document.querySelector<HTMLDivElement>('#quick-search-results-div');
        if (quickSearchResultsDiv != null) {
            const timelineItemDiv = document.createElement('div');
            timelineItemDiv.innerHTML = timelineElementHtml;
            timelineItemDiv.classList.add('col-12');
            timelineItemDiv.classList.add('col-sm-6');
            timelineItemDiv.classList.add('col-md-4');
            timelineItemDiv.classList.add('mb-2');
            timelineItemDiv.style.maxHeight = '400px';
            timelineItemDiv.style.overflowY = 'hidden';
            quickSearchResultsDiv.insertAdjacentHTML('beforeend', timelineItemDiv.outerHTML);
            await addTimelineItemEventListener(timelineItem);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function addQuickSearchCloseButtonEventListener() {
    const quickSearchCloseButton = document.querySelector<HTMLButtonElement>('#quick-search-close-button');
    if (quickSearchCloseButton) {
        function handleQuickSearchCloseButtonClick() {
            const quickSearchModal = document.querySelector<HTMLDivElement>('#quick-search-modal-div');
            if (quickSearchModal) {
                quickSearchModal.classList.add('d-none');
                quickSearchModal.innerHTML = '';
            }
        }
        quickSearchCloseButton.removeEventListener('click', handleQuickSearchCloseButtonClick);
        quickSearchCloseButton.addEventListener('click', handleQuickSearchCloseButtonClick);
    }
}