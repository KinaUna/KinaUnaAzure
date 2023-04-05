import * as pageModels from '../page-models.js';

let currentPageTranslationItem = new pageModels.TextTranslation();
let currentPageTranslations = new pageModels.TextTranslationPageListModel();

async function getPageTranslations(pageName: string) {
    const waitMeStartEvent = new Event('waitMeStart');
    window.dispatchEvent(waitMeStartEvent);

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
    
    const waitMeStopEvent = new Event('waitMeStop');
    window.dispatchEvent(waitMeStopEvent);
}

function setTranslationsListTableContent(content: pageModels.TextTranslationPageListModel) {
    const translationsListTable = document.querySelector<HTMLTableElement>('#translationsListTable');
    if (translationsListTable !== null) {
        const firstRow = document.querySelector<HTMLTableRowElement>('#translationsListTableHeader');
        translationsListTable.innerHTML = '';
        if (firstRow !== null) {
            translationsListTable.appendChild(firstRow);
        }

        let translationSetCount = 0;
        content.translations.forEach((translationItem) => {
            let rowToInsert = document.createElement('tr');
            let wordColumn = document.createElement('td');
            wordColumn.innerHTML = translationItem.word;
            let languageColumn = document.createElement('td');
            const languageName = currentPageTranslations.languagesList.find(l => l.id === translationItem.languageId)?.name;
            if (languageName) {
                languageColumn.innerHTML = languageName;
            }

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
            let translationColumnInputGroupAppend = document.createElement('div');
            translationColumnInputGroupAppend.classList.add('input-group-append');
            let translationColumnInputSavedIndicator = document.createElement('span');
            translationColumnInputSavedIndicator.classList.add('input-group-text');
            let inputSavedIndicator = document.createElement('i');
            inputSavedIndicator.classList.add('material-icons');
            inputSavedIndicator.innerHTML = 'task';
            translationColumnInputSavedIndicator.appendChild(inputSavedIndicator);
            translationColumnInputGroupAppend.appendChild(translationColumnInputSavedIndicator);
            translationColumnInputGroup.appendChild(translationInput);
            translationColumnInputGroup.appendChild(translationColumnInputGroupAppend);
            translationColumn.appendChild(translationColumnInputGroup);

            rowToInsert.appendChild(wordColumn);
            rowToInsert.appendChild(languageColumn);
            rowToInsert.appendChild(translationInput);

            if (translationSetCount == 0) {
                let translationDeleteColumn = document.createElement('td');
                translationDeleteColumn.rowSpan = content.languagesList.length;
                let translationDeleteButton = document.createElement('button');
                translationDeleteButton.classList.add('btn');
                translationDeleteButton.classList.add('btn-danger');
                translationDeleteButton.innerHTML = 'Delete';
                translationDeleteButton.addEventListener('click', () => {
                    deleteTranslationItem(translationItem.id);
                });
                translationDeleteColumn.appendChild(translationDeleteButton);
                rowToInsert.appendChild(translationDeleteColumn);
            }

            translationsListTable.appendChild(rowToInsert);
            
            translationSetCount++;
            if (translationSetCount === content.languagesList.length) {
                const addEmptyRow = document.createElement('tr');
                const addEmptyRowColumn = document.createElement('td');
                addEmptyRowColumn.colSpan = 3;
                addEmptyRowColumn.innerHTML = '----------';
                addEmptyRow.appendChild(addEmptyRowColumn);
                translationsListTable.appendChild(addEmptyRow);
                translationSetCount = 0;
            }
        });
    }
    
}

async function saveTranslationItem(translationId: number) {
    const waitMeStartEvent = new Event('waitMeStart');
    window.dispatchEvent(waitMeStartEvent);

    const tranlationInputElement = document.querySelector<HTMLInputElement>('[data-translation-id="' + translationId.toString() + '"]')
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
                const responseTranslationItem: pageModels.TextTranslation = JSON.parse(await saveTranslationResponse.text());
                if (responseTranslationItem.id !== 0) {
                    tranlationInputElement.value = responseTranslationItem.translation;
                    //Todo: show check or some other icon to indicate the translation was updated.
                }
            }).catch(function (error) {
                console.log('Error saving text translation. Error: ' + error);
            });
        }
    }

    const waitMeStopEvent = new Event('waitMeStop');
    window.dispatchEvent(waitMeStopEvent);
}

async function deleteTranslationItem(translationId: number) {
    let translationToDelete = currentPageTranslations.translations.find(t => t.id == translationId);
    if (translationToDelete) {
        let confirmDelete = confirm("Are you sure to delete this translation?");
        if (confirmDelete) {
            const waitMeStartEvent = new Event('waitMeStart');
            window.dispatchEvent(waitMeStartEvent);

            await fetch('/Admin/DeletePageTranslation', {
                method: 'POST',
                body: JSON.stringify(translationToDelete),
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            }).then(async function (saveTranslationResponse) {
                const responseTranslationItem: pageModels.TextTranslation = JSON.parse(await saveTranslationResponse.text());
                if (responseTranslationItem.id !== 0) {
                    const deletedTranslationItems = currentPageTranslations.translations.filter(t => t.page === responseTranslationItem.page && t.word === responseTranslationItem.word);
                    currentPageTranslations.translations = currentPageTranslations.translations.filter(t => !deletedTranslationItems.includes(t));
                    setTranslationsListTableContent(currentPageTranslations);
                }
            }).catch(function (error) {
                console.log('Error deleting translation. Error: ' + error);
            });

            const waitMeStopEvent = new Event('waitMeStop');
            window.dispatchEvent(waitMeStopEvent);
        }
    }    
}

$(function (): void {
    const selectPageElements = document.querySelectorAll('[data-viewid]');
    selectPageElements.forEach((selectPageElement) => {
        const pageLiElement = selectPageElement as HTMLLIElement;
        let pageName = 'Layout';
        if (pageLiElement.dataset.viewid) {
            pageName = pageLiElement.dataset.viewid;
        }

        selectPageElement.addEventListener('click', (event) => {
            const translationsPageSelectedDiv = document.querySelector<HTMLDivElement>('#translationsPageSelectedDiv');
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