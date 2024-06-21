import * as pageModels from '../page-models-v6.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v6.js';

let editTextTranslationCurrentTextItem = new pageModels.KinaUnaTextParameters();

/**
 * Loads data for the text editing modal.
 * @param textItem  The parameters for text item to edit: the textId of the text, the language to edit.
 * @returns
 */
async function loadManageTextsEditTextModal(textItem: pageModels.KinaUnaTextParameters): Promise<void> {
    startFullPageSpinner();

    await fetch('/Admin/EditTextTranslation?textId=' + textItem.textId + '&languageId=' + textItem.languageId + '&returnUrl=' + getManageTextsReturnUrl(), {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (editTextTranslationResponse) {
        if (editTextTranslationResponse != null) {
            const responseItem: pageModels.KinaUnaTextResponse = await editTextTranslationResponse.json();
            setTextEditModalTitle(responseItem.title);
            updateTextEditForm(responseItem);
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
 * Sets the title of the modal.
 * @param title The title to display. 
 */
function setTextEditModalTitle(title: string): void {
    $('#manage-texts-page-edit-text-modal-div .modal-title').html(title);
}

/**
 * Sets the title element of the edit form.
 * If the title starts with __ it is considered a special system page, and the title should never change.
 * @param title
 */
function setEditFormTitleElement(title: string): void {
    const editTextTranslationTitleInput = document.querySelector<HTMLInputElement>('#edit-text-translation-title-input');
    if (editTextTranslationTitleInput !== null) {
        editTextTranslationTitleInput.value = title;
        if (title.startsWith('__')) {
            editTextTranslationTitleInput.readOnly = true;
            editTextTranslationTitleInput.disabled = true;
        }
        else {
            editTextTranslationTitleInput.readOnly = false;
            editTextTranslationTitleInput.disabled = false;
        }
    }
}

/**
 * Sets the Page element. 
 * @param page The name of the page the text appears on.
 */
function setEditFormPageElement(page: string): void {
    const editTextTranslationPageDiv = document.querySelector<HTMLDivElement>('#edit-text-translation-page-div');
    if (editTextTranslationPageDiv !== null) {
        editTextTranslationPageDiv.innerHTML = page;
    }
}

/**
 * Sets the language list to the languageId of the current text item.
 * @param languageId The languageId of the current text item.
 */
function setTranslationLanguageList(languageId: number): void {
    const editTextTranslationLanguageList = document.querySelector('#edit-text-translation-language-list');
    if (editTextTranslationLanguageList !== null) {
        (editTextTranslationLanguageList as any).ej2_instances[0].value = languageId; // Syncfusion DropDownList element.
    }
}

/**
 * Updates the rich text editor's content.
 * @param text the HTML string with the text to be edited.
 */
function setEditFormTextElement(text: string): void {
    const editTextTranslationRichTextEditor = document.querySelector('#edit-text-translation-rich-text-editor');
    if (editTextTranslationRichTextEditor !== null) {
        (editTextTranslationRichTextEditor as any).ej2_instances[0].value = text; // Syncfusion RichTextEditor element.
        (editTextTranslationRichTextEditor as any).ej2_instances[0].refreshUI();
    }
}

/**
 * Updates the hidden input values for the form data.
 * @param textItem The text object containing the data.
 */
function setHiddenFormInputFields(textItem: pageModels.KinaUnaTextResponse ): void {
    const editTextTranslationPageInput = document.querySelector<HTMLInputElement>('#edit-text-translation-page-input');
    if (editTextTranslationPageInput !== null) {
        editTextTranslationPageInput.value = textItem.page;
    }

    const editTextTranslationIdInput = document.querySelector<HTMLInputElement>('#edit-text-translation-id-input');
    if (editTextTranslationIdInput !== null) {
        editTextTranslationIdInput.value = textItem.id.toString();
    }

    const editTextTranslationTextIdInput = document.querySelector<HTMLInputElement>('#edit-text-translation-text-id-input');
    if (editTextTranslationTextIdInput !== null) {
        editTextTranslationTextIdInput.value = textItem.textId.toString();
    }

    const editTextTranslationLanguageIdInput = document.querySelector<HTMLInputElement>('#edit-text-translation-language-id-input');
    if (editTextTranslationLanguageIdInput !== null) {
        editTextTranslationLanguageIdInput.value = textItem.languageId.toString();
    }
}

/**
 * Populates the form fields in the modal.
 * @param textItem The Text data to populate the fields with.
 */
function updateTextEditForm(textItem: pageModels.KinaUnaTextResponse): void {
    
    setEditFormTitleElement(textItem.title);

    setEditFormPageElement(textItem.page);

    setTranslationLanguageList(textItem.languageId);

    setEditFormTextElement(textItem.text);

    setHiddenFormInputFields(textItem);
}

/**
 * Submits the form data, saving the updated text data.
 */
async function saveManageTextsContent(): Promise<void> {
    startFullPageSpinner();
    const editTextTranslationForm = document.querySelector<HTMLFormElement>('#manage-texts-edit-form');
    
    if (editTextTranslationForm !== null) {
        await fetch(editTextTranslationForm.action, {
            method: 'POST',
            body: new FormData(editTextTranslationForm),
        }).then(async function (saveManageTextResponse) {
            const responseTextItem: pageModels.KinaUnaTextResponse = await saveManageTextResponse.json();
            if (responseTextItem.id !== 0) {
                const buttonToUpdate = document.querySelector<HTMLButtonElement>('[data-manage-texts-edit-text-id="' + responseTextItem.id + '"]');
                if (buttonToUpdate !== null) {
                    buttonToUpdate.innerHTML = 'Edit ' + responseTextItem.title;
                }
                editTextTranslationCurrentTextItem.title = responseTextItem.title;
                $("#manage-texts-page-edit-text-modal-div").modal("hide");

            }
        }).catch(function (error) {
            console.log('Error saving edit text translation. Error: ' + error);
        });
    }
    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Gets the return URL from the hidden data field.
 * @returns the URL.
 */
function getManageTextsReturnUrl(): string {
    let manageTextsPageReturnUrl: string = '';
    const manageTextsPageReturnUrlDiv: HTMLDivElement | null = document.querySelector<HTMLDivElement>('#manage-texts-page-return-url-div');
    if (manageTextsPageReturnUrlDiv !== null) {
        const manageTextsPageReturnUrlData: string | undefined = manageTextsPageReturnUrlDiv.dataset.manageTextsPageReturnUrl;
        if (manageTextsPageReturnUrlData) {
            manageTextsPageReturnUrl = manageTextsPageReturnUrlData;
        }
    }

    return manageTextsPageReturnUrl;
}

/**
 * Updates the text edit form when a different language is selected.
 */
function editTextTranslationLanguageListChanged() {
    const editTextTranslationLanguageList = document.querySelector('#edit-text-translation-language-list');
    if (editTextTranslationLanguageList !== null) {
        const selectedLanguageId = (editTextTranslationLanguageList as any).ej2_instances[0].value;
        editTextTranslationCurrentTextItem.languageId = parseInt(selectedLanguageId);
        loadManageTextsEditTextModal(editTextTranslationCurrentTextItem);
    }
}

/**
 * Initial setup when the page is loaded.
 */
document.addEventListener('DOMContentLoaded', function (): void {
    
    const editButtons = document.querySelectorAll('[data-manage-texts-edit-text-id]');
    editButtons.forEach(function (element) {
        const editButton = element as HTMLButtonElement;
        if (editButton !== null) {
            editButton.addEventListener('click', () => {
                const senderTextId = editButton.dataset.manageTextsEditTextId;
                const senderLanguageId = editButton.dataset.manageTextsEditLanguageId;
                if (senderTextId && senderLanguageId) {
                    editTextTranslationCurrentTextItem.textId = parseInt(senderTextId);
                    editTextTranslationCurrentTextItem.languageId = parseInt(senderLanguageId);
                    loadManageTextsEditTextModal(editTextTranslationCurrentTextItem);
                }                
            });
        }        
    });

    const manageTextsEditForm = document.querySelector<HTMLFormElement>('#manage-texts-edit-form');
    if (manageTextsEditForm !== null) {
        manageTextsEditForm.addEventListener('submit', async (submitEvent) => {
            submitEvent.preventDefault();
            await saveManageTextsContent();            
        });
    }
    
    window.addEventListener('languageChanged', () => {
        editTextTranslationLanguageListChanged();
    });
});