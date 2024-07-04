import { updateFilterButtonDisplay } from '../data-tools-v6.js';
import { addTimelineItemEventListener } from '../item-details/items-display.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v6.js';
import * as pageModels from '../page-models-v6.js';
import * as SettingsHelper from '../settings-tools-v6.js';

const friendsPageSettingsStorageKey = 'friends_page_parameters';
let friendsPageParameters = new pageModels.FriendsPageParameters();
const sortAscendingSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-descending-button');
const sortByFriendsSinceSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-by-friends-since-button');
const sortByNameSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-by-name-button');
/** Updates the friendsPageParameters object with the selected filter options.
 */
function getFriendsPageParameters(): void {
    const pageParametersDiv = document.querySelector<HTMLDivElement>('#friends-page-parameters');
    if (pageParametersDiv !== null) {
        const friendsPageParametersJson = pageParametersDiv.getAttribute('data-friends-page-parameters');
        if (friendsPageParametersJson !== null) {
            friendsPageParameters = JSON.parse(friendsPageParametersJson) as pageModels.FriendsPageParameters;
        }
    }
}

/** Shows the loading spinner in the loading-items-div.
 */
function runLoadingSpinner(): void {
    const loadingItemsParent: HTMLDivElement | null = document.querySelector<HTMLDivElement>('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.remove('d-none');
        startLoadingItemsSpinner('loading-items-div');
    }
}

/** Hides the loading spinner in the loading-items-div.
 */
function stopLoadingSpinner(): void {
    const loadingItemsParent: HTMLDivElement | null = document.querySelector<HTMLDivElement>('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.add('d-none');
        stopLoadingItemsSpinner('loading-items-div');
    }
}

/** Retrieves the list of friends, then updates the page.
 */
async function getFriendsList(): Promise<void> {
    runLoadingSpinner();
    
    await fetch('/Friends/FriendsList', {
        method: 'POST',
        body: JSON.stringify(friendsPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json'
        }
    }).then(async function (getFriendsResult) {
        const friendsPageResponse = await getFriendsResult.json() as pageModels.FriendsPageResponse;
        if (friendsPageResponse && friendsPageResponse.friendsList.length > 0) {
            const friendListDiv = document.querySelector<HTMLDivElement>('#friend-list-div');
            if (friendListDiv !== null) {
                friendListDiv.innerHTML = '';
            }

            for await (const friendId of friendsPageResponse.friendsList) {
                await getFriendElement(friendId);
            }

            updateTagsListDiv(friendsPageResponse.tagsList, friendsPageParameters.sortTags);
            updateActiveTagFilterDiv();
        }

    }).catch(function (error) {
        console.log('Error loading friends list. Error: ' + error);
    });

    stopLoadingSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function getFriendElement(id: number): Promise<void> {
    const getFriendElementParameters = new pageModels.FriendItemParameters();
    getFriendElementParameters.friendId = id;
    getFriendElementParameters.languageId = friendsPageParameters.languageId;

    await fetch('/Friends/FriendElement', {
        method: 'POST',
        body: JSON.stringify(getFriendElementParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (getFriendElementResult) {
        const friendHtml = await getFriendElementResult.text();
        const friendListDiv = document.querySelector<HTMLDivElement>('#friend-list-div');
        if (friendListDiv != null) {
            friendListDiv.insertAdjacentHTML('beforeend', friendHtml);
            const timelineItem = new pageModels.TimelineItem();
            timelineItem.itemId = id.toString();
            timelineItem.itemType = 6;
            addTimelineItemEventListener(timelineItem);
        }
    }).catch(function (error) {
        console.log('Error loading friends element. Error: ' + error);
    });
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets up the event listners for filtering buttons on the page, to select/deselect options and show/hide the associated contact items.
 */
function setupFilterButtons(): void {
    const filterButtons = document.querySelectorAll('.button-checkbox');
    filterButtons.forEach((filterButtonParentSpan) => {

        let filterButton = filterButtonParentSpan.querySelector('button');
        if (filterButton !== null) {
            filterButton.addEventListener('click', function (this: HTMLButtonElement) {
                updateFilterButtonDisplay(this);
            });
        }

        if (filterButton !== null) {
            updateFilterButtonDisplay(filterButton);
        }
    });

    const resetTagFilterButton = document.querySelector<HTMLButtonElement>('#reset-tag-filter-button');
    if (resetTagFilterButton !== null) {
        resetTagFilterButton.addEventListener('click', resetActiveTagFilter);
    }
}

/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup(): Promise<void> {
    const friendsPageSaveSettingsButton = document.querySelector<HTMLButtonElement>('#friends-page-save-settings-button');
    if (friendsPageSaveSettingsButton !== null) {
        friendsPageSaveSettingsButton.addEventListener('click', saveFriendsPageSettings);
    }

    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortFriendsAscending);
        sortDescendingSettingsButton.addEventListener('click', sortFriendsDescending);
    }

    if (sortByFriendsSinceSettingsButton !== null && sortByNameSettingsButton !== null) {
        sortByFriendsSinceSettingsButton.addEventListener('click', sortByFriendsSince);
        sortByNameSettingsButton.addEventListener('click', sortByName);
    }
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Saves the current page parameters to local storage and reloads the friends items list.
 */
async function saveFriendsPageSettings(): Promise<void> {
    const sortTagsSelect = document.querySelector<HTMLSelectElement>('#sort-tags-select');
    if (sortTagsSelect !== null) {
        friendsPageParameters.sortTags = sortTagsSelect.selectedIndex;
    }
    const saveAsDefaultCheckbox = document.querySelector<HTMLInputElement>('#settings-save-default-checkbox');
    if (saveAsDefaultCheckbox !== null && saveAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings<pageModels.FriendsPageParameters>(friendsPageSettingsStorageKey, friendsPageParameters);
    }
    
    SettingsHelper.toggleShowPageSettings();
    await getFriendsList();
    

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Retrieves FriendsPageParameters saved in local storage.
 */
async function loadFriendsPageSettings(): Promise<void> {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings<pageModels.FriendsPageParameters>(friendsPageSettingsStorageKey);
    if (pageSettingsFromStorage) {
        
        friendsPageParameters.sortBy = pageSettingsFromStorage.sortBy;
        if (friendsPageParameters.sortBy === 0) {
            sortFriendsAscending();
        }
        else {
            sortByName();
        }

        friendsPageParameters.sort = pageSettingsFromStorage.sort;
        if (friendsPageParameters.sort === 0) {
            sortFriendsAscending();
        }
        else {
            sortFriendsDescending();
        }

        friendsPageParameters.itemsPerPage = pageSettingsFromStorage.itemsPerPage;
        const selectItemsPerPageElement = document.querySelector<HTMLSelectElement>('#items-per-page-select');
        if (selectItemsPerPageElement !== null) {
            selectItemsPerPageElement.value = friendsPageParameters.itemsPerPage.toString();
            ($(".selectpicker") as any).selectpicker('refresh');
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortFriendsAscending(): Promise<void> {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    friendsPageParameters.sort = 0;
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortFriendsDescending(): Promise<void> {
    sortAscendingSettingsButton?.classList.remove('active');
    sortDescendingSettingsButton?.classList.add('active');
    friendsPageParameters.sort = 1;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortByFriendsSince(): Promise<void> {
    sortByFriendsSinceSettingsButton?.classList.add('active');
    sortByNameSettingsButton?.classList.remove('active');
    friendsPageParameters.sortBy = 0;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortByName(): Promise<void> {
    sortByFriendsSinceSettingsButton?.classList.remove('active');
    sortByNameSettingsButton?.classList.add('active');
    friendsPageParameters.sortBy = 1;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Renders a list of tag buttons in the tags-list-div, each with a link to filter the page.
* @param tagsList The list of strings for each tag.
*/
function updateTagsListDiv(tagsList: string[], sortOrder: number): void {
    const tagsListDiv = document.querySelector<HTMLDivElement>('#tags-list-div');
    if (tagsListDiv !== null) {
        tagsListDiv.innerHTML = '';

        if (sortOrder === 1) {
            tagsList.sort((a, b) => a.localeCompare(b));
        }

        tagsList.forEach(function (tag: string) {
            tagsListDiv.innerHTML += '<a class="btn tag-item" data-tag-link="' + tag + '">' + tag + '</a>';
        });

        const tagButtons = document.querySelectorAll('[data-tag-link]');
        tagButtons.forEach((tagButton) => {
            tagButton.addEventListener('click', tagButtonClick);
        });
    }
}

/** If a tag filter is active, show the tag in the active tag filter div and provide a button to clear it.
*/
function updateActiveTagFilterDiv(): void {
    const activeTagFilterDiv = document.querySelector<HTMLDivElement>('#active-tag-filter-div');
    const activeTagFilterSpan = document.querySelector<HTMLSpanElement>('#current-tag-filter-span');
    if (activeTagFilterDiv !== null && activeTagFilterSpan !== null && friendsPageParameters !== null) {
        if (friendsPageParameters.tagFilter !== '') {
            activeTagFilterDiv.classList.remove('d-none');
            activeTagFilterSpan.innerHTML = friendsPageParameters.tagFilter;
        }
        else {
            activeTagFilterDiv.classList.add('d-none');
            activeTagFilterSpan.innerHTML = '';
        }
    }
}

/** Clears the active tag filter and reloads the default full list of friends.
*/
async function resetActiveTagFilter(): Promise<void> {
    if (friendsPageParameters !== null) {
        friendsPageParameters.tagFilter = '';
        await getFriendsList();
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Event handler for tag buttons, sets the tag filter and reloads the list of friends.
*/
async function tagButtonClick(event: Event): Promise<void> {
    const target = event.target as HTMLElement;
    if (target !== null && friendsPageParameters !== null) {
        const tagLink = target.dataset.tagLink;
        if (tagLink !== undefined) {
            friendsPageParameters.tagFilter = tagLink;
            await getFriendsList();
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Select pickers don't always update when their values change, this ensures they show the correct items. */
function refreshSelectPickers(): void {
    const sortTagsSelect = document.querySelector<HTMLSelectElement>('#sort-tags-select');
    if (sortTagsSelect !== null && friendsPageParameters !== null) {
        sortTagsSelect.value = friendsPageParameters.sortTags.toString();
        ($(".selectpicker") as any).selectpicker('refresh');
    }
}

/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    initialSettingsPanelSetup();

    SettingsHelper.initPageSettings();
    setupFilterButtons();
    getFriendsPageParameters();
    refreshSelectPickers();
    await loadFriendsPageSettings();
    

    await getFriendsList();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});