import { addTimelineItemEventListener, showPopupAtLoad } from '../item-details/items-display-v8.js';
import * as pageModels from '../page-models-v8.js';
const notesPageSettingsStorageKey = 'notes_page_parameters';
let notesPageParameters = new pageModels.NotesPageParameters();
let notesPageParametersHistory = [];
const notesIndexPageParametersDiv = document.querySelector('#notes-index-page-parameters');
const notesListDiv = document.querySelector('#notes-list-div');
const nextNotesItemsPageButton = document.querySelector('#next-note-items-page-button');
const previousNotesItemsPageButton = document.querySelector('#previous-note-items-page-button');
const headerNextNotesItemsPageButton = document.querySelector('#header-next-note-items-page-button');
const headerPreviousNotesItemsPageButton = document.querySelector('#header-previous-note-items-page-button');
const pageNumberSpan = document.querySelector('#page-number-span');
const headerPageNumberSpan = document.querySelector('#header-page-number-span');
const pageTotalSpan = document.querySelector('#page-total-span');
const headerPageTotalSpan = document.querySelector('#header-page-total-span');
const notesPageSettingsButton = document.querySelector('#notes-index-page-settings-button');
const notesPageSettingsDiv = document.querySelector('#page-settings-div');
const notesPageMainDiv = document.querySelector('#kinauna-main-div');
const itemsPerPageInput = document.querySelector('#notes-items-per-page-input');
const sortAscendingSettingsButton = document.querySelector('#sort-notes-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#sort-notes-descending-button');
function setNotesPageParametersFromPageData() {
    if (notesIndexPageParametersDiv !== null) {
        const pageParameters = notesIndexPageParametersDiv.dataset.notesIndexPageParameters;
        if (pageParameters) {
            notesPageParameters = JSON.parse(pageParameters);
        }
    }
}
async function getNotes() {
    const getMoreNotesResponse = await fetch('/Notes/NotesList', {
        method: 'POST',
        body: JSON.stringify(notesPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
        }
    });
    if (getMoreNotesResponse.ok && getMoreNotesResponse.body !== null) {
        const notesPageResponse = await getMoreNotesResponse.json();
        if (notesPageResponse) {
            notesPageParameters.currentPageNumber = notesPageResponse.pageNumber;
            notesPageParameters.totalPages = notesPageResponse.totalPages;
            notesPageParameters.totalItems = notesPageResponse.totalItems;
            if (notesPageResponse.totalItems < 1) {
                getNoteElement(0);
            }
            else {
                updateNotesPageNavigation();
                if (notesListDiv != null) {
                    notesListDiv.innerHTML = '';
                }
                for await (const noteItemId of notesPageResponse.notesList) {
                    await getNoteElement(noteItemId);
                    const timelineItem = new pageModels.TimelineItem();
                    timelineItem.itemId = noteItemId.toString();
                    timelineItem.itemType = 9;
                    addTimelineItemEventListener(timelineItem);
                }
                ;
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function getNoteElement(id) {
    const getNoteElementParameters = new pageModels.NoteItemParameters();
    getNoteElementParameters.noteId = id;
    getNoteElementParameters.languageId = notesPageParameters.languageId;
    const getNotesElementResponse = await fetch('/Notes/NoteElement', {
        method: 'POST',
        body: JSON.stringify(getNoteElementParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    });
    if (getNotesElementResponse.ok && getNotesElementResponse.text !== null) {
        const noteHtml = await getNotesElementResponse.text();
        if (notesListDiv != null) {
            notesListDiv.insertAdjacentHTML('beforeend', noteHtml);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function getNextNotesPage() {
    notesPageParametersHistory.push(structuredClone(notesPageParameters));
    notesPageParameters.currentPageNumber++;
    getNotes();
    setNotesPageHistory();
}
function getPreviousNotesPage() {
    notesPageParametersHistory.push(structuredClone(notesPageParameters));
    notesPageParameters.currentPageNumber--;
    getNotes();
    setNotesPageHistory();
}
function setNotesPageHistory() {
    const url = new URL(window.location.href);
    url.searchParams.set('childId', notesPageParameters.progenyId.toString());
    url.searchParams.set('page', notesPageParameters.currentPageNumber.toString());
    url.searchParams.set('sort', notesPageParameters.sort.toString());
    url.searchParams.set('itemsPerPage', notesPageParameters.itemsPerPage.toString());
    window.history.pushState({}, '', url);
}
function getNotesPageFromHistory() {
    if (notesPageParametersHistory.length > 0) {
        const lastParameters = notesPageParametersHistory.pop();
        if (lastParameters) {
            notesPageParameters = lastParameters;
            getNotes();
        }
    }
}
function updateNotesPageNavigation() {
    if (pageNumberSpan !== null) {
        pageNumberSpan.innerHTML = notesPageParameters.currentPageNumber.toString();
    }
    if (pageTotalSpan !== null) {
        pageTotalSpan.innerHTML = notesPageParameters.totalPages.toString();
    }
    if (headerPageNumberSpan !== null) {
        headerPageNumberSpan.innerHTML = notesPageParameters.currentPageNumber.toString();
    }
    if (headerPageTotalSpan !== null) {
        headerPageTotalSpan.innerHTML = notesPageParameters.totalPages.toString();
    }
    if (notesPageParameters.totalItems < notesPageParameters.currentPageNumber * notesPageParameters.itemsPerPage || notesPageParameters.totalPages < 2) {
        nextNotesItemsPageButton?.classList.add('d-none');
        headerNextNotesItemsPageButton?.classList.add('d-none');
    }
    else {
        nextNotesItemsPageButton?.classList.remove('d-none');
        headerNextNotesItemsPageButton?.classList.remove('d-none');
    }
    if (notesPageParameters.currentPageNumber === 1 || notesPageParameters.totalPages < 2) {
        previousNotesItemsPageButton?.classList.add('d-none');
        headerPreviousNotesItemsPageButton?.classList.add('d-none');
    }
    else {
        previousNotesItemsPageButton?.classList.remove('d-none');
        headerPreviousNotesItemsPageButton?.classList.remove('d-none');
    }
}
function toggleShowNotesPageSettings() {
    notesPageMainDiv?.classList.toggle('main-show-page-settings');
    notesPageSettingsDiv?.classList.toggle('d-none');
    notesPageSettingsButton?.classList.toggle('d-none');
    notesPageParameters.showSettings = !notesPageParameters.showSettings;
    saveNotesPageSettings(false);
}
function saveNotesPageSettings(reload) {
    localStorage.setItem(notesPageSettingsStorageKey, JSON.stringify(notesPageParameters));
    if (reload) {
        notesPageParameters.currentPageNumber = 1;
        getNotes();
    }
    setNotesPageHistory();
}
function saveNotesPageSettingsReload() {
    saveNotesPageSettings(true);
}
function loadNotesPageSettingsFromLocalStorage() {
    const localStorageNotesPageParametersString = localStorage.getItem(notesPageSettingsStorageKey);
    if (localStorageNotesPageParametersString) {
        const pageParams = JSON.parse(localStorageNotesPageParametersString);
        if (pageParams !== null) {
            notesPageParameters.itemsPerPage = pageParams.itemsPerPage;
            notesPageParameters.sort = pageParams.sort;
            notesPageParameters.showSettings = pageParams.showSettings;
            if (itemsPerPageInput !== null) {
                itemsPerPageInput.value = notesPageParameters.itemsPerPage.toString();
            }
        }
    }
}
function decreaseNotesItemsPerPage() {
    if (itemsPerPageInput !== null) {
        let itemsPerPageInputValue = itemsPerPageInput.valueAsNumber;
        itemsPerPageInputValue--;
        if (itemsPerPageInputValue > 0) {
            itemsPerPageInput.value = itemsPerPageInputValue.toString();
        }
        notesPageParameters.itemsPerPage = itemsPerPageInput.valueAsNumber;
    }
}
function increaseNotesItemsPerPage() {
    if (itemsPerPageInput !== null) {
        let itemsPerPageInputValue = itemsPerPageInput.valueAsNumber;
        itemsPerPageInputValue++;
        if (itemsPerPageInputValue < 101) {
            itemsPerPageInput.value = itemsPerPageInputValue.toString();
        }
        notesPageParameters.itemsPerPage = itemsPerPageInput.valueAsNumber;
    }
}
function sortNotesPageAscending() {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    notesPageParameters.sort = 0;
}
function sortNotesPageDescending() {
    sortDescendingSettingsButton?.classList.add('active');
    sortAscendingSettingsButton?.classList.remove('active');
    notesPageParameters.sort = 1;
}
document.addEventListener('DOMContentLoaded', async function () {
    const notesPageSettingsContentDiv = document.querySelector('#page-settings-content-div');
    if (notesPageSettingsContentDiv !== null) {
        notesPageSettingsDiv?.appendChild(notesPageSettingsContentDiv);
    }
    const notesShowPageSettingsButtonDiv = document.querySelector('#notes-show-page-settings-button-div');
    if (notesShowPageSettingsButtonDiv !== null) {
        notesPageSettingsDiv?.parentElement?.appendChild(notesShowPageSettingsButtonDiv);
    }
    setNotesPageParametersFromPageData();
    loadNotesPageSettingsFromLocalStorage();
    if (notesPageParameters.showSettings) {
        notesPageMainDiv?.classList.toggle('main-show-page-settings');
        notesPageSettingsDiv?.classList.toggle('d-none');
        notesPageSettingsButton?.classList.toggle('d-none');
    }
    if (notesPageParameters.sort === 0) {
        sortAscendingSettingsButton?.classList.add('active');
        sortDescendingSettingsButton?.classList.remove('active');
    }
    await showPopupAtLoad(pageModels.TimeLineType.Note);
    getNotes();
    if (nextNotesItemsPageButton !== null && previousNotesItemsPageButton !== null) {
        nextNotesItemsPageButton.addEventListener('click', getNextNotesPage);
        previousNotesItemsPageButton.addEventListener('click', getPreviousNotesPage);
    }
    if (headerNextNotesItemsPageButton !== null && headerPreviousNotesItemsPageButton !== null) {
        headerNextNotesItemsPageButton.addEventListener('click', getNextNotesPage);
        headerPreviousNotesItemsPageButton.addEventListener('click', getPreviousNotesPage);
    }
    if (notesPageSettingsButton !== null) {
        notesPageSettingsButton.addEventListener('click', toggleShowNotesPageSettings);
    }
    const decreaseNotesItemsPerPageButton = document.querySelector('#decrease-notes-items-per-page-button');
    const increaseNotesItemsPerPageButton = document.querySelector('#increase-notes-items-per-page-button');
    const notesSaveSettingsButton = document.querySelector('#notes-page-save-settings-button');
    const closeNotesPageSettingsButton = document.querySelector('#close-notes-page-settings-button');
    if (decreaseNotesItemsPerPageButton !== null && increaseNotesItemsPerPageButton !== null &&
        notesSaveSettingsButton !== null && closeNotesPageSettingsButton !== null && sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        decreaseNotesItemsPerPageButton.addEventListener('click', decreaseNotesItemsPerPage);
        increaseNotesItemsPerPageButton.addEventListener('click', increaseNotesItemsPerPage);
        notesSaveSettingsButton.addEventListener('click', saveNotesPageSettingsReload);
        closeNotesPageSettingsButton.addEventListener('click', toggleShowNotesPageSettings);
        sortAscendingSettingsButton.addEventListener('click', sortNotesPageAscending);
        sortDescendingSettingsButton.addEventListener('click', sortNotesPageDescending);
    }
    window.onpopstate = function (event) {
        getNotesPageFromHistory();
    };
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=notes-index.js.map