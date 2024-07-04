import { updateFilterButtonDisplay } from '../data-tools-v6.js';
import { addTimelineItemEventListener } from '../item-details/items-display.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v6.js';
import * as pageModels from '../page-models-v6.js';
import * as SettingsHelper from '../settings-tools-v6.js';

const contactsPageSettingsStorageKey = 'contacts_page_parameters';
let contactsPageParameters = new pageModels.ContactsPageParameters();
const sortAscendingSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-descending-button');
const sortByContactAddedSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-by-contact-added-button');
const sortByDisplayNameSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-by-display-name-button');
const sortByFirstNameSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-by-first-name-button');
const sortByLastNameSettingsButton = document.querySelector<HTMLButtonElement>('#settings-sort-by-last-name-button');

/** Updates the contactsPageParameters object with the selected filter options.
 */
function getContactsPageParameters(): void {
    const pageParametersDiv = document.querySelector<HTMLDivElement>('#contacts-page-parameters');
    if (pageParametersDiv !== null) {
        const contactsPageParametersJson = pageParametersDiv.getAttribute('data-contacts-page-parameters');
        if (contactsPageParametersJson !== null) {
            contactsPageParameters = JSON.parse(contactsPageParametersJson) as pageModels.ContactsPageParameters;
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

/** Retrieves the list of contacts, then updates the page.
 */
async function getContactsList(): Promise<void> {
    runLoadingSpinner();

    await fetch('/Contacts/ContactsList', {
        method: 'POST',
        body: JSON.stringify(contactsPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json'
        }
    }).then(async function (getContactsResult) {
        const contactsPageResponse = await getContactsResult.json() as pageModels.ContactsPageResponse;
        if (contactsPageResponse && contactsPageResponse.contactsList.length > 0) {
            const contactListDiv = document.querySelector<HTMLDivElement>('#contact-list-div');
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function getContactElement(id: number): Promise<void> {
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
        const contactListDiv = document.querySelector<HTMLDivElement>('#contact-list-div');
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
    const contactsPageSaveSettingsButton = document.querySelector<HTMLButtonElement>('#contacts-page-save-settings-button');
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

    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Saves the current page parameters to local storage and reloads the contacts items list.
 */
async function saveContactsPageSettings(): Promise<void> {
    const sortTagsSelect = document.querySelector<HTMLSelectElement>('#sort-tags-select');
    if (sortTagsSelect !== null) {
        contactsPageParameters.sortTags = sortTagsSelect.selectedIndex;
    }

    const saveAsDefaultCheckbox = document.querySelector<HTMLInputElement>('#settings-save-default-checkbox');
    if (saveAsDefaultCheckbox !== null && saveAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings<pageModels.ContactsPageParameters>(contactsPageSettingsStorageKey, contactsPageParameters);
    }

    SettingsHelper.toggleShowPageSettings();
    await getContactsList();


    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Retrieves ContactsPageParameters saved in local storage.
 */
async function loadContactsPageSettings(): Promise<void> {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings<pageModels.ContactsPageParameters>(contactsPageSettingsStorageKey);
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

        contactsPageParameters.itemsPerPage = pageSettingsFromStorage.itemsPerPage;
        const selectItemsPerPageElement = document.querySelector<HTMLSelectElement>('#items-per-page-select');
        if (selectItemsPerPageElement !== null) {
            selectItemsPerPageElement.value = contactsPageParameters.itemsPerPage.toString();
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
async function sortContactsAscending(): Promise<void> {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    contactsPageParameters.sort = 0;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortContactsDescending(): Promise<void> {
    sortAscendingSettingsButton?.classList.remove('active');
    sortDescendingSettingsButton?.classList.add('active');
    contactsPageParameters.sort = 1;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort by value.
 */
async function sortByContactAdded(): Promise<void> {
    sortByContactAddedSettingsButton?.classList.add('active');
    sortByDisplayNameSettingsButton?.classList.remove('active');
    sortByFirstNameSettingsButton?.classList.remove('active');
    sortByLastNameSettingsButton?.classList.remove('active');
    contactsPageParameters.sortBy = 0;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort by value.
 */
async function sortByDisplayName(): Promise<void> {
    sortByContactAddedSettingsButton?.classList.remove('active');
    sortByDisplayNameSettingsButton?.classList.add('active');
    sortByFirstNameSettingsButton?.classList.remove('active');
    sortByLastNameSettingsButton?.classList.remove('active');
    contactsPageParameters.sortBy = 1;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort by value.
 */
async function sortByFirstName(): Promise<void> {
    sortByContactAddedSettingsButton?.classList.remove('active');
    sortByDisplayNameSettingsButton?.classList.remove('active');
    sortByFirstNameSettingsButton?.classList.add('active');
    sortByLastNameSettingsButton?.classList.remove('active');
    contactsPageParameters.sortBy = 2;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort by value.
 */
async function sortByLastName(): Promise<void> {
    sortByContactAddedSettingsButton?.classList.remove('active');
    sortByDisplayNameSettingsButton?.classList.remove('active');
    sortByFirstNameSettingsButton?.classList.remove('active');
    sortByLastNameSettingsButton?.classList.add('active');
    contactsPageParameters.sortBy = 3;

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
async function resetActiveTagFilter(): Promise<void> {
    if (contactsPageParameters !== null) {
        contactsPageParameters.tagFilter = '';
        await getContactsList();
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Event handler for tag buttons, sets the tag filter and reloads the list of contacts.
*/
async function tagButtonClick(event: Event): Promise<void> {
    const target = event.target as HTMLElement;
    if (target !== null && contactsPageParameters !== null) {
        const tagLink = target.dataset.tagLink;
        if (tagLink !== undefined) {
            contactsPageParameters.tagFilter = tagLink;
            await getContactsList();
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/** Select pickers don't always update when their values change, this ensures they show the correct items. */
function refreshSelectPickers(): void {
    const sortTagsSelect = document.querySelector<HTMLSelectElement>('#sort-tags-select');
    if (sortTagsSelect !== null && contactsPageParameters !== null) {
        sortTagsSelect.value = contactsPageParameters.sortTags.toString();
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
    getContactsPageParameters();
    refreshSelectPickers();
    await loadContactsPageSettings();


    await getContactsList();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});