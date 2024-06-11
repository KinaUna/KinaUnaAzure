import * as pageModels from '../page-models-v2.js';
let editTextTranslationCurrentTextItem = new pageModels.KinaUnaTextParameters();
async function loadManageTextsEditTextModal(textItem) {
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
            const responseItem = await editTextTranslationResponse.json();
            await showManageTextsEditModal(responseItem);
        }
    }).catch(function (error) {
        console.log('Error loading about text content. Error: ' + error);
    });
    const waitMeStopEvent = new Event('waitMeStop');
    window.dispatchEvent(waitMeStopEvent);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function showManageTextsEditModal(textItem) {
    return new Promise(function (resolve, reject) {
        $('#manageTextsPageEditTextModalDiv .modal-title').html(textItem.title);
        setManageTextsEditForm(textItem);
        resolve();
    });
}
function setManageTextsEditForm(textItem) {
    const editTextTranslationTitleInput = document.querySelector('#editTextTranslationTitleInput');
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
    const editTextTranslationPageDiv = document.querySelector('#editTextTranslationPageDiv');
    if (editTextTranslationPageDiv !== null) {
        editTextTranslationPageDiv.innerHTML = textItem.page;
    }
    const editTextTranslationLanguageList = document.querySelector('#editTextTranslationLanguageList');
    if (editTextTranslationLanguageList !== null) {
        editTextTranslationLanguageList.ej2_instances[0].value = textItem.languageId; // Syncfusion DropDownList element.
    }
    const editTextTranslationRichTextEditor = document.querySelector('#editTextTranslationRichTextEditor');
    if (editTextTranslationRichTextEditor !== null) {
        editTextTranslationRichTextEditor.ej2_instances[0].value = textItem.text; // Syncfusion RichTextEditor element.
        editTextTranslationRichTextEditor.ej2_instances[0].refreshUI();
    }
    const editTextTranslationPageInput = document.querySelector('#editTextTranslationPageInput');
    if (editTextTranslationPageInput !== null) {
        editTextTranslationPageInput.value = textItem.page;
    }
    const editTextTranslationIdInput = document.querySelector('#editTextTranslationIdInput');
    if (editTextTranslationIdInput !== null) {
        editTextTranslationIdInput.value = textItem.id.toString();
    }
    const editTextTranslationTextIdInput = document.querySelector('#editTextTranslationTextIdInput');
    if (editTextTranslationTextIdInput !== null) {
        editTextTranslationTextIdInput.value = textItem.textId.toString();
    }
    const editTextTranslationLanguageIdInput = document.querySelector('#editTextTranslationLanguageIdInput');
    if (editTextTranslationLanguageIdInput !== null) {
        editTextTranslationLanguageIdInput.value = textItem.languageId.toString();
    }
}
async function saveManageTextsContent() {
    const waitMeStartEvent = new Event('waitMeStart');
    window.dispatchEvent(waitMeStartEvent);
    const editTextTranslationForm = document.querySelector('#manageTextsEditForm');
    if (editTextTranslationForm !== null) {
        await fetch(editTextTranslationForm.action, {
            method: 'POST',
            body: new FormData(editTextTranslationForm),
        }).then(async function (saveManageTextResponse) {
            const responseTextItem = await saveManageTextResponse.json();
            if (responseTextItem.id !== 0) {
                const buttonToUpdate = document.querySelector('[data-manage-texts-edit-text-id="' + responseTextItem.id + '"]');
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function getManageTextsReturnUrl() {
    let manageTextsPageReturnUrl = '';
    const manageTextsPageReturnUrlDiv = document.querySelector('#manageTextsPageReturnUrlDiv');
    if (manageTextsPageReturnUrlDiv !== null) {
        const manageTextsPageReturnUrlData = manageTextsPageReturnUrlDiv.dataset.manageTextsPageReturnUrl;
        if (manageTextsPageReturnUrlData) {
            manageTextsPageReturnUrl = manageTextsPageReturnUrlData;
        }
    }
    return manageTextsPageReturnUrl;
}
function editTextTranslationLanguageListChanged() {
    const editTextTranslationLanguageList = document.querySelector('#editTextTranslationLanguageList');
    if (editTextTranslationLanguageList !== null) {
        const selectedLanguageId = editTextTranslationLanguageList.ej2_instances[0].value;
        editTextTranslationCurrentTextItem.languageId = parseInt(selectedLanguageId);
        loadManageTextsEditTextModal(editTextTranslationCurrentTextItem);
    }
}
$(function () {
    const editButtons = document.querySelectorAll('[data-manage-texts-edit-text-id]');
    editButtons.forEach(function (element) {
        const editButton = element;
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
    const manageTextsEditForm = document.querySelector('#manageTextsEditForm');
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
//# sourceMappingURL=manage-texts.js.map