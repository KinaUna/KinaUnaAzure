import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
/**
 * Adds event listeners to all elements with the data-vocabulary-id attribute.
 * When clicked, the DisplayVocabularyItem function is called.
 * @param {string} itemId The id of the VocabularyItem to add event listeners for.
 */
export function addVocabularyItemListeners(itemId) {
    const elementsWithDataId = document.querySelectorAll('[data-vocabulary-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', async function () {
                await DisplayVocabularyItem(itemId);
            });
        });
    }
}
/**
 * Enable other scripts to call the DisplayVocabularyItem function.
 * @param {string} vocabularyId The id of the vocabulary item to display.
 */
export async function popupVocabularyItem(vocabularyId) {
    await DisplayVocabularyItem(vocabularyId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Displays a vocabulary item in a popup.
 * @param {string} vocabularyId The id of the vocabulary item to display.
 */
async function DisplayVocabularyItem(vocabularyId) {
    startFullPageSpinner();
    let url = '/Vocabulary/ViewVocabularyItem?vocabularyId=' + vocabularyId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const vocabularyElementHtml = await response.text();
            const vocabularyDetailsPopupDiv = document.querySelector('#item-details-div');
            if (vocabularyDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = vocabularyElementHtml;
                vocabularyDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                vocabularyDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            vocabularyDetailsPopupDiv.innerHTML = '';
                            vocabularyDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
            }
        }
        else {
            console.error('Error getting vocabulary item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting vocabulary item. Error: ' + error);
    });
    stopFullPageSpinner();
}
//# sourceMappingURL=vocabulary-details.js.map