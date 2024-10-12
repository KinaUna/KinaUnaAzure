import { updateFilterButtonDisplay } from '../data-tools-v8.js';
import { addTimelineItemEventListener, showPopupAtLoad } from '../item-details/items-display-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import * as pageModels from '../page-models-v8.js';
import * as SettingsHelper from '../settings-tools-v8.js';
import { getSelectedProgenies } from '../settings-tools-v8.js';
const friendsPageSettingsStorageKey = 'friends_page_parameters';
let friendsPageParameters = new pageModels.FriendsPageParameters();
const sortAscendingSettingsButton = document.querySelector('#settings-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#settings-sort-descending-button');
const sortByFriendsSinceSettingsButton = document.querySelector('#settings-sort-by-friends-since-button');
const sortByNameSettingsButton = document.querySelector('#settings-sort-by-name-button');
/** Gets the FriendsPageParameters from the page's data attribute.
 */
function getFriendsPageParameters() {
    const pageParametersDiv = document.querySelector('#friends-page-parameters');
    if (pageParametersDiv !== null) {
        const friendsPageParametersJson = pageParametersDiv.getAttribute('data-friends-page-parameters');
        if (friendsPageParametersJson !== null) {
            friendsPageParameters = JSON.parse(friendsPageParametersJson);
        }
    }
}
/** Shows the loading spinner in the loading-items-div.
 */
function runLoadingSpinner() {
    const loadingItemsParent = document.querySelector('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.remove('d-none');
        startLoadingItemsSpinner('loading-items-div');
    }
}
/** Hides the loading spinner in the loading-items-div.
 */
function stopLoadingSpinner() {
    const loadingItemsParent = document.querySelector('#loading-items-parent-div');
    if (loadingItemsParent !== null) {
        loadingItemsParent.classList.add('d-none');
        stopLoadingItemsSpinner('loading-items-div');
    }
}
/** Retrieves the list of friends, then updates the page.
 */
async function getFriendsList() {
    runLoadingSpinner();
    await fetch('/Friends/FriendsList', {
        method: 'POST',
        body: JSON.stringify(friendsPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json'
        }
    }).then(async function (getFriendsResult) {
        const friendsPageResponse = await getFriendsResult.json();
        if (friendsPageResponse && friendsPageResponse.friendsList.length > 0) {
            const friendListDiv = document.querySelector('#friend-list-div');
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function getFriendElement(id) {
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
        const friendListDiv = document.querySelector('#friend-list-div');
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets up the event listners for filtering buttons on the page, to select/deselect options and show/hide the associated friend items.
 */
function setupFilterButtons() {
    const filterButtons = document.querySelectorAll('.button-checkbox');
    filterButtons.forEach((filterButtonParentSpan) => {
        let filterButton = filterButtonParentSpan.querySelector('button');
        if (filterButton !== null) {
            filterButton.addEventListener('click', function () {
                updateFilterButtonDisplay(this);
            });
        }
        if (filterButton !== null) {
            updateFilterButtonDisplay(filterButton);
        }
    });
    const resetTagFilterButton = document.querySelector('#reset-tag-filter-button');
    if (resetTagFilterButton !== null) {
        resetTagFilterButton.addEventListener('click', resetActiveTagFilter);
    }
}
/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup() {
    const friendsPageSaveSettingsButton = document.querySelector('#friends-page-save-settings-button');
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Saves the current page parameters to local storage and reloads the friends items list.
 */
async function saveFriendsPageSettings() {
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null) {
        friendsPageParameters.sortTags = sortTagsSelect.selectedIndex;
    }
    const saveAsDefaultCheckbox = document.querySelector('#settings-save-default-checkbox');
    if (saveAsDefaultCheckbox !== null && saveAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings(friendsPageSettingsStorageKey, friendsPageParameters);
    }
    SettingsHelper.toggleShowPageSettings();
    await getFriendsList();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Retrieves FriendsPageParameters saved in local storage.
 */
async function loadFriendsPageSettings() {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings(friendsPageSettingsStorageKey);
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
        friendsPageParameters.sortTags = pageSettingsFromStorage.sortTags;
        const sortTagsElement = document.querySelector('#sort-tags-select');
        if (sortTagsElement !== null && friendsPageParameters.sortTags) {
            sortTagsElement.value = friendsPageParameters.sortTags.toString();
            $(".selectpicker").selectpicker('refresh');
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortFriendsAscending() {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    friendsPageParameters.sort = 0;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortFriendsDescending() {
    sortAscendingSettingsButton?.classList.remove('active');
    sortDescendingSettingsButton?.classList.add('active');
    friendsPageParameters.sort = 1;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortByFriendsSince() {
    sortByFriendsSinceSettingsButton?.classList.add('active');
    sortByNameSettingsButton?.classList.remove('active');
    friendsPageParameters.sortBy = 0;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortByName() {
    sortByFriendsSinceSettingsButton?.classList.remove('active');
    sortByNameSettingsButton?.classList.add('active');
    friendsPageParameters.sortBy = 1;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Renders a list of tag buttons in the tags-list-div, each with a link to filter the page.
* @param tagsList The list of strings for each tag.
*/
function updateTagsListDiv(tagsList, sortOrder) {
    const tagsListDiv = document.querySelector('#tags-list-div');
    if (tagsListDiv !== null) {
        tagsListDiv.innerHTML = '';
        if (sortOrder === 1) {
            tagsList.sort((a, b) => a.localeCompare(b));
        }
        tagsList.forEach(function (tag) {
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
function updateActiveTagFilterDiv() {
    const activeTagFilterDiv = document.querySelector('#active-tag-filter-div');
    const activeTagFilterSpan = document.querySelector('#current-tag-filter-span');
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
async function resetActiveTagFilter() {
    if (friendsPageParameters !== null) {
        friendsPageParameters.tagFilter = '';
        await getFriendsList();
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Adds an event listener to the reset tag filter button.
 */
function addResetActiveTagFilterEventListener() {
    const resetTagFilterButton = document.querySelector('#reset-tag-filter-button');
    if (resetTagFilterButton !== null) {
        resetTagFilterButton.addEventListener('click', resetActiveTagFilter);
    }
}
/** Event handler for tag buttons, sets the tag filter and reloads the list of friends.
*/
async function tagButtonClick(event) {
    const target = event.target;
    if (target !== null && friendsPageParameters !== null) {
        const tagLink = target.dataset.tagLink;
        if (tagLink !== undefined) {
            friendsPageParameters.tagFilter = tagLink;
            await getFriendsList();
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Select pickers don't always update when their values change, this ensures they show the correct items. */
function refreshSelectPickers() {
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null && friendsPageParameters !== null) {
        sortTagsSelect.value = friendsPageParameters.sortTags.toString();
        $(".selectpicker").selectpicker('refresh');
    }
}
function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            friendsPageParameters.progenies = getSelectedProgenies();
            friendsPageParameters.currentPageNumber = 1;
            await getFriendsList();
        }
    });
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    initialSettingsPanelSetup();
    SettingsHelper.initPageSettings();
    setupFilterButtons();
    getFriendsPageParameters();
    refreshSelectPickers();
    addResetActiveTagFilterEventListener();
    await loadFriendsPageSettings();
    await showPopupAtLoad(pageModels.TimeLineType.Friend);
    addSelectedProgeniesChangedEventListener();
    friendsPageParameters.progenies = getSelectedProgenies();
    await getFriendsList();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=friends-index.js.map