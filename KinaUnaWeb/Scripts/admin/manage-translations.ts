import * as pageModels from '../page-models-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';

let currentPageTranslationItem = new pageModels.TextTranslation();
let currentPageTranslations = new pageModels.TextTranslationPageListModel();

/**
 * Fetches all translations for the specified page, then calls the function to set up the table of translations.
 * @param pageName  The name of the page to get translations for.
 */
async function getPageTranslations(pageName: string): Promise<void> {
    startFullPageSpinner();

    currentPageTranslationItem.page = pageName;

    await fetch('/Admin/LoadPageTranslations', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(currentPageTranslationItem)
    }).then(async function (getPageTranslationsResult) {
        if (getPageTranslationsResult != null) {
            currentPageTranslations = (await getPageTranslationsResult.json()) as pageModels.TextTranslationPageListModel;
            setTranslationsListTableContent(currentPageTranslations);            
        }
    }).catch(function (error) {
        console.log('Error loading about text content. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Delay function
 * @param milliseconds The duration of the delay in miliseconds.
 * @returns
 */
function hideElementDelay(milliseconds: number): Promise<any> {
    return new Promise(resolve => {
        setTimeout(resolve, milliseconds);
    });
}

/**
 * Populates the translations-list-table with the data from the TextTranslationPageListModel.
 * Adds a header, then each TextTranslation item.
 * @param content The object containing the list of translations to add to the table. 
 */
function setTranslationsListTableContent(content: pageModels.TextTranslationPageListModel): void {
    const translationsListTable = document.querySelector<HTMLTableElement>('#translations-list-table');
    if (translationsListTable !== null) {
        const headerRow = document.querySelector<HTMLTableRowElement>('#translations-list-table-header');
        translationsListTable.innerHTML = '';
        if (headerRow !== null) {
            translationsListTable.appendChild(headerRow);
        }

        let translationSetCount = 0;
        content.translations.forEach((translationItem) => {
            addTranslationItemToTable(translationItem, translationSetCount);
            translationSetCount++;

            if (translationSetCount === currentPageTranslations.languagesList.length) {
                const spaceRow = createSpaceRow();
                translationsListTable.appendChild(spaceRow);
                translationSetCount = 0;
            }
        });
    }
    
}
/**
 * Creates space with a line to separate each group of word translations.
 * @returns The HTMLTableRowElement to append to the table.
 */
function createSpaceRow(): HTMLTableRowElement {
    const addEmptyRow = document.createElement('tr');
    const addEmptyRowColumn = document.createElement('td');
    addEmptyRowColumn.colSpan = 3;
    addEmptyRowColumn.innerHTML = '----------';
    addEmptyRow.appendChild(addEmptyRowColumn);

    return addEmptyRow;
}

/**
 * Creates a column that shows the Word/Key for a given translation item.
 * @param word The TextTranslation object's word value to create a column for.
 * @returns The HTMLTableCellElement to append to the table row.
 */
function createWordColumn(word: string): HTMLTableCellElement {
    let wordColumn = document.createElement('td');
    wordColumn.innerHTML = word;

    return wordColumn;
}

/**
 * Creates a column that shows the language for a given translation item.
 * @param languageId The TextTranslation object's language id to create a column for.
 * @returns The HTMLTableCellElement to append to the table row.
 */
function createLanguageColumn(languageId: number): HTMLTableCellElement {
    let languageColumn = document.createElement('td');
    const languageName = currentPageTranslations.languagesList.find(l => l.id === languageId)?.name;
    if (languageName) {
        languageColumn.innerHTML = languageName;
    }

    return languageColumn;
}

/**
 * Creates a column with an input field for the translation for a given translation item.
 * @param translationItem The TextTranslation object to create a column for.
 * @returns The HTMLTableCellElement to append to the table row.
 */
function createTranslationColumn(translationItem: pageModels.TextTranslation): HTMLTableCellElement {
    let translationColumn = document.createElement('td');

    let translationColumnInputGroup = document.createElement('div');
    translationColumnInputGroup.classList.add('input-group');

    let translationInput = document.createElement('input');
    translationInput.classList.add('form-control');
    translationInput.value = translationItem.translation;
    translationInput.dataset.translationId = translationItem.id.toString();

    translationInput.addEventListener('keydown', (keyPressed) => {
        if (keyPressed.key === 'Enter') {
            saveTranslationItem(translationItem.id);
        }
    });
    translationInput.addEventListener('focusin', () => {
        const saveButton = document.querySelector<HTMLButtonElement>('[data-save-button-id="' + translationItem.id + '"]');
        if (saveButton !== null) {
            saveButton.classList.remove('d-none');
        }
    });
    translationInput.addEventListener('focusout', async () => {

        const saveButton = document.querySelector<HTMLButtonElement>('[data-save-button-id="' + translationItem.id + '"]');
        if (saveButton !== null) {
            await hideElementDelay(500);
            saveButton.classList.add('d-none');
        }
    });

    let translationColumnInputGroupAppend = document.createElement('div');
    translationColumnInputGroupAppend.classList.add('input-group-append');

    let translationColumnInputSaveButton = document.createElement('button');
    translationColumnInputSaveButton.classList.add('btn');
    translationColumnInputSaveButton.classList.add('btn-sm');
    translationColumnInputSaveButton.classList.add('btn-success');
    translationColumnInputSaveButton.classList.add('mt-0');
    translationColumnInputSaveButton.classList.add('mb-0');
    translationColumnInputSaveButton.classList.add('d-none');
    translationColumnInputSaveButton.dataset.saveButtonId = translationItem.id.toString();
    translationColumnInputSaveButton.addEventListener('click', () => {
        saveTranslationItem(translationItem.id);
    });

    let saveButtonIcon = document.createElement('i');
    saveButtonIcon.classList.add('material-icons');
    saveButtonIcon.innerHTML = 'save';

    translationColumnInputSaveButton.appendChild(saveButtonIcon);

    let translationColumnInputSavedIndicator = document.createElement('span');
    translationColumnInputSavedIndicator.classList.add('input-group-text');
    translationColumnInputSavedIndicator.classList.add('d-none');
    translationColumnInputSavedIndicator.dataset.savedIndicatorId = translationItem.id.toString();
    let inputSavedIndicator = document.createElement('i');
    inputSavedIndicator.classList.add('material-icons');
    inputSavedIndicator.classList.add('text-success');
    inputSavedIndicator.innerHTML = 'task';
    translationColumnInputSavedIndicator.appendChild(inputSavedIndicator);

    translationColumnInputGroupAppend.appendChild(translationColumnInputSaveButton);
    translationColumnInputGroupAppend.appendChild(translationColumnInputSavedIndicator);
    translationColumnInputGroup.appendChild(translationInput);
    translationColumnInputGroup.appendChild(translationColumnInputGroupAppend);
    translationColumn.appendChild(translationColumnInputGroup);

    return translationColumn;
}

/**
 * Creates a column with a delete button for a given translation item.
 * @param id The Id of the TextTranslation to delete when clicked.
 * @returns The HTMLTableCellElement to append to the table row.
 */
function createDeleteColumn(id: number): HTMLTableCellElement {
    let translationDeleteColumn = document.createElement('td');
    translationDeleteColumn.rowSpan = currentPageTranslations.languagesList.length;

    let translationDeleteButton = document.createElement('button');
    translationDeleteButton.classList.add('btn');
    translationDeleteButton.classList.add('btn-sm');
    translationDeleteButton.classList.add('btn-danger');
    translationDeleteButton.innerHTML = 'Delete';
    translationDeleteButton.addEventListener('click', () => {
        deleteTranslationItem(id);
    });

    translationDeleteColumn.appendChild(translationDeleteButton);

    return translationDeleteColumn;
}

/**
 * Adds a row to the table for the given translation item, with columns for Word/Key, language, translation, and the delete action.
 * @param translationItem The TextTranslation object to add to the table.
 * @param translationSetCount The number of translations processed for this TextTranslation Word/Key so far.
 */
function addTranslationItemToTable(translationItem: pageModels.TextTranslation, translationSetCount: number): void {
    const translationsListTable = document.querySelector<HTMLTableElement>('#translations-list-table');
    if (translationsListTable === null) {
        return;
    }

    let rowToInsert = document.createElement('tr');

    let wordColumn = createWordColumn(translationItem.word);

    let languageColumn = createLanguageColumn(translationItem.languageId);

    let translationColumn = createTranslationColumn(translationItem);

    rowToInsert.appendChild(wordColumn);
    rowToInsert.appendChild(languageColumn);
    rowToInsert.appendChild(translationColumn);

    if (translationSetCount == 0) {
        let translationDeleteColumn = createDeleteColumn(translationItem.id);
        
        rowToInsert.appendChild(translationDeleteColumn);
    }
    
    translationsListTable.appendChild(rowToInsert);
}
/**
 * Saves the translation of the translation item with the given id.
 * When completed, calls a function to display a checkmark next to the item in the table.
 * @param translationId The id of the translation item to save.
 */
async function saveTranslationItem(translationId: number): Promise<void> {
    startFullPageSpinner();

    const tranlationInputElement = document.querySelector<HTMLInputElement>('[data-translation-id="' + translationId.toString() + '"]');

    if (tranlationInputElement !== null) {
        let translationToUpdate = currentPageTranslations.translations.find(t => t.id == translationId);
        if (translationToUpdate) {
            translationToUpdate.translation = tranlationInputElement.value;
            await fetch('/Admin/UpdatePageTranslation', {
                method: 'POST',
                body: JSON.stringify(translationToUpdate),
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            }).then(async function (saveTranslationResponse) {
                const responseTranslationItem: pageModels.TextTranslation = await saveTranslationResponse.json();
                if (responseTranslationItem.id !== 0) {
                    tranlationInputElement.value = responseTranslationItem.translation;
                    showSavedIndicator(translationId);
                }
            }).catch(function (error) {
                console.log('Error saving text translation. Error: ' + error);
            });
        }
    }

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates the translation field with a check mark icon to show the operation is completed.
 * @param translationId The translation item id to show the icon for.
 */
function showSavedIndicator(translationId: number): void {
    const checkElement = document.querySelector<HTMLSpanElement>('[data-saved-indicator-id="' + translationId.toString() + '"]');
    if (checkElement !== null) {
        checkElement.classList.remove('d-none');
    }
}

/**
 * Deletes the translation item with the given id.
 * @param translationId The id of the translation item to delete.
 */
async function deleteTranslationItem(translationId: number): Promise<void> {
    let translationToDelete = currentPageTranslations.translations.find(t => t.id == translationId);
    if (translationToDelete) {
        let confirmDelete = confirm("Are you sure to delete this translation?");
        if (confirmDelete) {
            startFullPageSpinner();

            await fetch('/Admin/DeletePageTranslation', {
                method: 'POST',
                body: JSON.stringify(translationToDelete),
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            }).then(async function (saveTranslationResponse) {
                const responseTranslationItem: pageModels.TextTranslation = await saveTranslationResponse.json();
                if (responseTranslationItem.id !== 0) {
                    const deletedTranslationItems = currentPageTranslations.translations.filter(t => t.page === responseTranslationItem.page && t.word === responseTranslationItem.word);
                    currentPageTranslations.translations = currentPageTranslations.translations.filter(t => !deletedTranslationItems.includes(t));
                    setTranslationsListTableContent(currentPageTranslations);
                }
            }).catch(function (error) {
                console.log('Error deleting translation. Error: ' + error);
            });

            stopFullPageSpinner();
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Initialization of the page: Set event listeners.
 */
document.addEventListener('DOMContentLoaded', function (): void {
    const selectPageElements = document.querySelectorAll('[data-viewid]');
    selectPageElements.forEach((selectPageElement) => {
        const pageLiElement = selectPageElement as HTMLLIElement;
        let pageName = 'Layout';
        if (pageLiElement.dataset.viewid) {
            pageName = pageLiElement.dataset.viewid;
        }

        selectPageElement.addEventListener('click', (event) => {
            const translationsPageSelectedDiv = document.querySelector<HTMLDivElement>('#translations-page-selected-div');
            if (translationsPageSelectedDiv !== null) {
                translationsPageSelectedDiv.innerHTML = pageName;
            }
            const allLiElements = document.querySelectorAll('[data-viewid]');
            allLiElements.forEach((element) => {
                element.classList.remove('select-view-item-selected');
            });

            const liElement = event.target as HTMLLIElement;
            liElement.classList.add('select-view-item-selected')
            getPageTranslations(pageName); 
        })
    });
});