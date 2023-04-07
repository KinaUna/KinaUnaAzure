import * as pageModels from '../page-models.js';

let editTextTranslationCurrentTextItem = new pageModels.KinaUnaTextParameters();

async function loadManageTextsEditTextModal(textItem: pageModels.KinaUnaTextParameters): Promise<void> {
    const waitMeStartEvent = new Event('waitMeStart');
    window.dispatchEvent(waitMeStartEvent);
    await fetch('/Admin/EditTextTranslation?textId=' + textItem.textId + '&languageId=' + textItem.languageId + '&returnUrl=' + getManageTextsReturnUrl(), {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (editTextTranslationResponse) {
        if (editTextTranslationResponse != null) {
            const responseItem: pageModels.KinaUnaTextResponse = await editTextTranslationResponse.json();
            await showManageTextsEditModal(responseItem);
        }
    }).catch(function (error) {
        console.log('Error loading about text content. Error: ' + error);
    });
    const waitMeStopEvent = new Event('waitMeStop');
    window.dispatchEvent(waitMeStopEvent);
}

function showManageTextsEditModal(textItem: pageModels.KinaUnaTextResponse) {
    return new Promise<void>(function (resolve, reject) {
        $('#manageTextsPageEditTextModalDiv .modal-title').html(textItem.title);
        setManageTextsEditForm(textItem);
        resolve();
    });
}

function setManageTextsEditForm(textItem: pageModels.KinaUnaTextResponse) {
    const editTextTranslationTitleInput = document.querySelector<HTMLInputElement>('#editTextTranslationTitleInput');
    if (editTextTranslationTitleInput !== null) {
        editTextTranslationTitleInput.value = textItem.title;
        if (textItem.title.startsWith('__')) {
            editTextTranslationTitleInput.readOnly = true;
            editTextTranslationTitleInput.disabled = true;
        }
        else {
            editTextTranslationTitleInput.readOnly = false;
            editTextTranslationTitleInput.disabled = false;
        }
    }

    const editTextTranslationPageDiv = document.querySelector<HTMLDivElement>('#editTextTranslationPageDiv');
    if (editTextTranslationPageDiv !== null) {
        editTextTranslationPageDiv.innerHTML = textItem.page;
    }

    const editTextTranslationLanguageList = document.querySelector('#editTextTranslationLanguageList');
    if (editTextTranslationLanguageList !== null) {
        (editTextTranslationLanguageList as any).ej2_instances[0].value = textItem.languageId; // Syncfusion DropDownList element.
    }

    const editTextTranslationRichTextEditor = document.querySelector('#editTextTranslationRichTextEditor');
    if (editTextTranslationRichTextEditor !== null) {
        (editTextTranslationRichTextEditor as any).ej2_instances[0].value = textItem.text; // Syncfusion RichTextEditor element.
        (editTextTranslationRichTextEditor as any).ej2_instances[0].refreshUI();
    }

    const editTextTranslationPageInput = document.querySelector<HTMLInputElement>('#editTextTranslationPageInput');
    if (editTextTranslationPageInput !== null) {
        editTextTranslationPageInput.value = textItem.page;
    }

    const editTextTranslationIdInput = document.querySelector<HTMLInputElement>('#editTextTranslationIdInput');
    if (editTextTranslationIdInput !== null) {
        editTextTranslationIdInput.value = textItem.id.toString();
    }

    const editTextTranslationTextIdInput = document.querySelector<HTMLInputElement>('#editTextTranslationTextIdInput');
    if (editTextTranslationTextIdInput !== null) {
        editTextTranslationTextIdInput.value = textItem.textId.toString();
    }

    const editTextTranslationLanguageIdInput = document.querySelector<HTMLInputElement>('#editTextTranslationLanguageIdInput');
    if (editTextTranslationLanguageIdInput !== null) {
        editTextTranslationLanguageIdInput.value = textItem.languageId.toString();
    }
}

async function saveManageTextsContent() {
    const waitMeStartEvent = new Event('waitMeStart');
    window.dispatchEvent(waitMeStartEvent);
    const editTextTranslationForm = document.querySelector<HTMLFormElement>('#manageTextsEditForm');
    
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
                $("#manageTextsPageEditTextModalDiv").modal("hide");
            }
        }).catch(function (error) {
            console.log('Error saving edit text translation. Error: ' + error);
        });
    }
    const waitMeStopEvent = new Event('waitMeStop');
    window.dispatchEvent(waitMeStopEvent);
}

function getManageTextsReturnUrl(): string {
    let manageTextsPageReturnUrl: string = '';
    const manageTextsPageReturnUrlDiv: HTMLDivElement | null = document.querySelector<HTMLDivElement>('#manageTextsPageReturnUrlDiv');
    if (manageTextsPageReturnUrlDiv !== null) {
        const manageTextsPageReturnUrlData: string | undefined = manageTextsPageReturnUrlDiv.dataset.manageTextsPageReturnUrl;
        if (manageTextsPageReturnUrlData) {
            manageTextsPageReturnUrl = manageTextsPageReturnUrlData;
        }
    }

    return manageTextsPageReturnUrl;
}

function editTextTranslationLanguageListChanged() {
    const editTextTranslationLanguageList = document.querySelector('#editTextTranslationLanguageList');
    if (editTextTranslationLanguageList !== null) {
        const selectedLanguageId = (editTextTranslationLanguageList as any).ej2_instances[0].value;
        editTextTranslationCurrentTextItem.languageId = parseInt(selectedLanguageId);
        loadManageTextsEditTextModal(editTextTranslationCurrentTextItem);
    }
}

$(function (): void {
    
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

    const manageTextsEditForm = document.querySelector<HTMLFormElement>('#manageTextsEditForm');
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