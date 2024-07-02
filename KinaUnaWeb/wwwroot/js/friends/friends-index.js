import { updateFilterButtonDisplay } from '../data-tools-v6.js';
import { addTimelineItemEventListener } from '../item-details/items-display.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v6.js';
import * as pageModels from '../page-models-v6.js';
import * as SettingsHelper from '../settings-tools-v6.js';
const friendsPageSettingsStorageKey = 'friends_page_parameters';
let friendsPageParameters = new pageModels.FriendsPageParameters();
const sortAscendingSettingsButton = document.querySelector('#setting-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#setting-sort-descending-button');
const sortByFriendsSinceSettingsButton = document.querySelector('#settings-sort-by-friends-since-button');
const sortByNameSettingsButton = document.querySelector('#settings-sort-by-name-button');
/** Updates the friendsPageParameters object with the selected filter options.
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
 * Sets up the event listners for filtering buttons on the page, to select/deselect options and show/hide the associated contact items.
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
}
/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup() {
    const timelineSaveSettingsButton = document.querySelector('#timeline-page-save-settings-button');
    if (timelineSaveSettingsButton !== null) {
        timelineSaveSettingsButton.addEventListener('click', saveFriendsPageSettings);
    }
    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortFriendsAscending);
        sortDescendingSettingsButton.addEventListener('click', sortFriendsDescending);
    }
    if (sortByFriendsSinceSettingsButton !== null && sortByNameSettingsButton !== null) {
        sortByFriendsSinceSettingsButton.addEventListener('click', sortByFriendsSince);
        sortByNameSettingsButton.addEventListener('click', sortByName);
    }
    const friendsSaveSettingsButton = document.querySelector('#page-save-settings-button');
    if (friendsSaveSettingsButton !== null) {
        friendsSaveSettingsButton.addEventListener('click', saveFriendsPageSettings);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Saves the current page parameters to local storage and reloads the timeline items list.
 */
async function saveFriendsPageSettings() {
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
 * Retrieves timelineParameters saved in local storage.
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
        friendsPageParameters.itemsPerPage = pageSettingsFromStorage.itemsPerPage;
        const selectItemsPerPageElement = document.querySelector('#items-per-page-select');
        if (selectItemsPerPageElement !== null) {
            selectItemsPerPageElement.value = friendsPageParameters.itemsPerPage.toString();
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
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    initialSettingsPanelSetup();
    SettingsHelper.initPageSettings();
    setupFilterButtons();
    getFriendsPageParameters();
    await loadFriendsPageSettings();
    await getFriendsList();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=friends-index.js.map