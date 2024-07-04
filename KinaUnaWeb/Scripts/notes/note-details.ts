import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v6.js';

/**
 * Adds event listeners to all elements with the data-note-id attribute.
 * When clicked, the DisplayNoteItem function is called.
 * @param {string} itemId The id of the note to add event listeners for.
 */
export function addNoteEventListeners(itemId: string): void {
    const noteElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-note-id="' + itemId + '"]');
    if (noteElementsWithDataId) {
        noteElementsWithDataId.forEach((element) => {
            element.addEventListener('click', function () {
                displayNoteItem(itemId);
            });
        });
    }
}

/**
 * Enable other scripts to call the DisplayNoteItem function.
 * @param {string} noteId The id of the note to display.
 */
export function popupNoteItem(noteId: string): void {
    displayNoteItem(noteId);

}

/**
 * Displays a note item in a popup.
 * @param {string} noteId The id of the note to display.
 */
async function displayNoteItem(noteId: string): Promise<void> {
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
            const noteDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (noteDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = noteElementHtml;
                noteDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                noteDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            noteDetailsPopupDiv.innerHTML = '';
                            noteDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }

            }
        } else {
            console.error('Error getting note item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting note item. Error: ' + error);
    });
    
    stopFullPageSpinner();
}