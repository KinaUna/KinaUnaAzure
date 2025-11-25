import { setEditItemButtonEventListeners } from '../addItem/add-item-v11.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v11.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v11.js';
/**
 * Adds event listeners to all elements with the data-note-id attribute.
 * When clicked, the DisplayNoteItem function is called.
 * @param {string} itemId The id of the note to add event listeners for.
 */
export function addNoteEventListeners(itemId) {
    const noteElementsWithDataId = document.querySelectorAll('[data-note-id="' + itemId + '"]');
    if (noteElementsWithDataId) {
        noteElementsWithDataId.forEach((element) => {
            const noteClickedAction = async function () {
                await displayNoteItem(itemId);
            };
            element.removeEventListener('click', noteClickedAction);
            element.addEventListener('click', noteClickedAction);
        });
    }
}
async function onNoteItemDivClicked(event) {
    const noteElement = event.currentTarget;
    if (noteElement !== null) {
        const noteId = noteElement.dataset.noteId;
        if (noteId) {
            await displayNoteItem(noteId);
        }
    }
}
/**
 * Enable other scripts to call the DisplayNoteItem function.
 * @param {string} noteId The id of the note to display.
 */
export async function popupNoteItem(noteId) {
    await displayNoteItem(noteId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Displays a note item in a popup.
 * @param {string} noteId The id of the note to display.
 */
async function displayNoteItem(noteId) {
    startFullPageSpinner();
    let url = '/Notes/ViewNote?noteId=' + noteId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const noteElementHtml = await response.text();
            const noteDetailsPopupDiv = document.querySelector('#item-details-div');
            if (noteDetailsPopupDiv) {
                noteDetailsPopupDiv.innerHTML = '';
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = noteElementHtml;
                noteDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                noteDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        const closeButtonAction = function () {
                            noteDetailsPopupDiv.innerHTML = '';
                            noteDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        };
                        button.removeEventListener('click', closeButtonAction);
                        button.addEventListener('click', closeButtonAction);
                    });
                }
                setEditItemButtonEventListeners();
            }
        }
        else {
            console.error('Error getting note item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting note item. Error: ' + error);
    });
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=note-details-v11.js.map