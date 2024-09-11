import { updateFilterButtonDisplay } from '../data-tools-v7.js';
import { addTimelineItemEventListener } from '../item-details/items-display.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v6.js';
import * as pageModels from '../page-models-v6.js';
import * as SettingsHelper from '../settings-tools-v6.js';
const contactsPageSettingsStorageKey = 'contacts_page_parameters';
let contactsPageParameters = new pageModels.ContactsPageParameters();
const sortAscendingSettingsButton = document.querySelector('#settings-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#settings-sort-descending-button');
const sortByContactAddedSettingsButton = document.querySelector('#settings-sort-by-contact-added-button');
const sortByDisplayNameSettingsButton = document.querySelector('#settings-sort-by-display-name-button');
const sortByFirstNameSettingsButton = document.querySelector('#settings-sort-by-first-name-button');
const sortByLastNameSettingsButton = document.querySelector('#settings-sort-by-last-name-button');
/** Gets the ContactsPageParameters from the page's data-contacts-page-parameters attribute.
 */
function getContactsPageParameters() {
    const pageParametersDiv = document.querySelector('#contacts-page-parameters');
    if (pageParametersDiv !== null) {
        const contactsPageParametersJson = pageParametersDiv.getAttribute('data-contacts-page-parameters');
        if (contactsPageParametersJson !== null) {
            contactsPageParameters = JSON.parse(contactsPageParametersJson);
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
/** Retrieves the list of contacts, then updates the page.
 */
async function getContactsList() {
    runLoadingSpinner();
    await fetch('/Contacts/ContactsList', {
        method: 'POST',
        body: JSON.stringify(contactsPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json'
        }
    }).then(async function (getContactsResult) {
        const contactsPageResponse = await getContactsResult.json();
        if (contactsPageResponse && contactsPageResponse.contactsList.length > 0) {
            const contactListDiv = document.querySelector('#contact-list-div');
            if (contactListDiv !== null) {
                contactListDiv.innerHTML = '';
            }
            for await (const contactId of contactsPageResponse.contactsList) {
                await getContactElement(contactId);
            }
            updateTagsListDiv(contactsPageResponse.tagsList, contactsPageParameters.sortTags);
            updateActiveTagFilterDiv();
        }
    }).catch(function (error) {
        console.log('Error loading contacts list. Error: ' + error);
    });
    stopLoadingSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function getContactElement(id) {
    const getContactElementParameters = new pageModels.ContactItemParameters();
    getContactElementParameters.contactId = id;
    getContactElementParameters.languageId = getContactElementParameters.languageId;
    await fetch('/Contacts/ContactElement', {
        method: 'POST',
        body: JSON.stringify(getContactElementParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (getContactElementResult) {
        const contactHtml = await getContactElementResult.text();
        const contactListDiv = document.querySelector('#contact-list-div');
        if (contactListDiv != null) {
            contactListDiv.insertAdjacentHTML('beforeend', contactHtml);
            const timelineItem = new pageModels.TimelineItem();
            timelineItem.itemId = id.toString();
            timelineItem.itemType = 10;
            addTimelineItemEventListener(timelineItem);
        }
    }).catch(function (error) {
        console.log('Error loading contact element. Error: ' + error);
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
    const resetTagFilterButton = document.querySelector('#reset-tag-filter-button');
    if (resetTagFilterButton !== null) {
        resetTagFilterButton.addEventListener('click', resetActiveTagFilter);
    }
}
/**
* Configures the elements in the settings panel.
*/
async function initialSettingsPanelSetup() {
    const contactsPageSaveSettingsButton = document.querySelector('#contacts-page-save-settings-button');
    if (contactsPageSaveSettingsButton !== null) {
        contactsPageSaveSettingsButton.addEventListener('click', saveContactsPageSettings);
    }
    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortContactsAscending);
        sortDescendingSettingsButton.addEventListener('click', sortContactsDescending);
    }
    if (sortByContactAddedSettingsButton !== null) {
        sortByContactAddedSettingsButton.addEventListener('click', sortByContactAdded);
    }
    if (sortByDisplayNameSettingsButton !== null) {
        sortByDisplayNameSettingsButton.addEventListener('click', sortByDisplayName);
    }
    if (sortByFirstNameSettingsButton !== null) {
        sortByFirstNameSettingsButton.addEventListener('click', sortByFirstName);
    }
    if (sortByLastNameSettingsButton !== null) {
        sortByLastNameSettingsButton.addEventListener('click', sortByLastName);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Saves the current page parameters to local storage and reloads the contacts items list.
 */
async function saveContactsPageSettings() {
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null) {
        contactsPageParameters.sortTags = sortTagsSelect.selectedIndex;
    }
    const saveAsDefaultCheckbox = document.querySelector('#settings-save-default-checkbox');
    if (saveAsDefaultCheckbox !== null && saveAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings(contactsPageSettingsStorageKey, contactsPageParameters);
    }
    SettingsHelper.toggleShowPageSettings();
    await getContactsList();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Retrieves ContactsPageParameters saved in local storage.
 */
async function loadContactsPageSettings() {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings(contactsPageSettingsStorageKey);
    if (pageSettingsFromStorage) {
        contactsPageParameters.sortBy = pageSettingsFromStorage.sortBy;
        if (contactsPageParameters.sortBy === 0) {
            sortContactsAscending();
        }
        if (contactsPageParameters.sortBy === 1) {
            sortByDisplayName();
        }
        if (contactsPageParameters.sortBy === 2) {
            sortByFirstName();
        }
        if (contactsPageParameters.sortBy === 3) {
            sortByLastName();
        }
        contactsPageParameters.sort = pageSettingsFromStorage.sort;
        if (contactsPageParameters.sort === 0) {
            sortContactsAscending();
        }
        else {
            sortContactsDescending();
        }
        contactsPageParameters.sortTags = pageSettingsFromStorage.sortTags;
        const sortTagsElement = document.querySelector('#sort-tags-select');
        if (sortTagsElement !== null && contactsPageParameters.sortTags) {
            sortTagsElement.value = contactsPageParameters.sortTags.toString();
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
async function sortContactsAscending() {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    contactsPageParameters.sort = 0;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortContactsDescending() {
    sortAscendingSettingsButton?.classList.remove('active');
    sortDescendingSettingsButton?.classList.add('active');
    contactsPageParameters.sort = 1;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort by value.
 */
async function sortByContactAdded() {
    sortByContactAddedSettingsButton?.classList.add('active');
    sortByDisplayNameSettingsButton?.classList.remove('active');
    sortByFirstNameSettingsButton?.classList.remove('active');
    sortByLastNameSettingsButton?.classList.remove('active');
    contactsPageParameters.sortBy = 0;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort by value.
 */
async function sortByDisplayName() {
    sortByContactAddedSettingsButton?.classList.remove('active');
    sortByDisplayNameSettingsButton?.classList.add('active');
    sortByFirstNameSettingsButton?.classList.remove('active');
    sortByLastNameSettingsButton?.classList.remove('active');
    contactsPageParameters.sortBy = 1;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort by value.
 */
async function sortByFirstName() {
    sortByContactAddedSettingsButton?.classList.remove('active');
    sortByDisplayNameSettingsButton?.classList.remove('active');
    sortByFirstNameSettingsButton?.classList.add('active');
    sortByLastNameSettingsButton?.classList.remove('active');
    contactsPageParameters.sortBy = 2;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort by value.
 */
async function sortByLastName() {
    sortByContactAddedSettingsButton?.classList.remove('active');
    sortByDisplayNameSettingsButton?.classList.remove('active');
    sortByFirstNameSettingsButton?.classList.remove('active');
    sortByLastNameSettingsButton?.classList.add('active');
    contactsPageParameters.sortBy = 3;
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
    if (activeTagFilterDiv !== null && activeTagFilterSpan !== null && contactsPageParameters !== null) {
        if (contactsPageParameters.tagFilter !== '') {
            activeTagFilterDiv.classList.remove('d-none');
            activeTagFilterSpan.innerHTML = contactsPageParameters.tagFilter;
        }
        else {
            activeTagFilterDiv.classList.add('d-none');
            activeTagFilterSpan.innerHTML = '';
        }
    }
}
/** Clears the active tag filter and reloads the default full list of contacts.
*/
async function resetActiveTagFilter() {
    if (contactsPageParameters !== null) {
        contactsPageParameters.tagFilter = '';
        await getContactsList();
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Adds an event listener to the reset tag filter button.
*/
function addResetActiveTagFilterEventListener() {
    const resetTagFilterButton = document.querySelector('#reset-tag-filter-button');
    if (resetTagFilterButton !== null) {
        resetTagFilterButton.addEventListener('click', resetActiveTagFilter);
    }
}
/** Event handler for tag buttons, sets the tag filter and reloads the list of contacts.
*/
async function tagButtonClick(event) {
    const target = event.target;
    if (target !== null && contactsPageParameters !== null) {
        const tagLink = target.dataset.tagLink;
        if (tagLink !== undefined) {
            contactsPageParameters.tagFilter = tagLink;
            await getContactsList();
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Select pickers don't always update when their values change, this ensures they show the correct items. */
function refreshSelectPickers() {
    const sortTagsSelect = document.querySelector('#sort-tags-select');
    if (sortTagsSelect !== null && contactsPageParameters !== null) {
        sortTagsSelect.value = contactsPageParameters.sortTags.toString();
        $(".selectpicker").selectpicker('refresh');
    }
}
/**
 * Initializes the page elements when it is loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    initialSettingsPanelSetup();
    SettingsHelper.initPageSettings();
    setupFilterButtons();
    getContactsPageParameters();
    refreshSelectPickers();
    addResetActiveTagFilterEventListener();
    await loadContactsPageSettings();
    await getContactsList();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=contacts-index.js.map