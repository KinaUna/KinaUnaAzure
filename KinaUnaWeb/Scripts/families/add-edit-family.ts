import { getCurrentLanguageId } from "../data-tools-v9.js";
import { hideBodyScrollbars } from "../item-details/items-display-v9.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9.js";
import { GetFamiliesList } from "./families-index.js";

let languageId = 1;

export async function displayAddFamilyModal() {
    startFullPageSpinner();

    const response = await fetch('/Families/AddFamily', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }

    });
    if (response.ok) {
        const familyDetailsDiv = document.querySelector<HTMLDivElement>('#item-details-div');
        if (familyDetailsDiv) {
            const familyDetailsHTML = await response.text();
            familyDetailsDiv.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            familyDetailsDiv.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            familyDetailsDiv.classList.remove('d-none');
            addAddFamilyModalEventListeners();
            await initializeAddEditFamily();
        }
        else {
            return Promise.reject('Family details div not found in the document.');
        }

    } else {
        console.error('Failed to fetch add family element:', response.statusText);
        return Promise.reject('Failed to fetch add family element: ' + response.statusText);
    }

    stopFullPageSpinner();

    return Promise.resolve();
}

function addAddFamilyModalEventListeners(): void {
    const closeButton = document.querySelector<HTMLButtonElement>('#close-add-family-modal-button');
    if (closeButton) {
        const closeButtonClickedAction = function (event: MouseEvent) {
            event.preventDefault();
            event.stopPropagation();
            const familyDetailsDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (familyDetailsDiv) {
                familyDetailsDiv.innerHTML = '';
                familyDetailsDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
    }

    const addFamilyForm = document.querySelector<HTMLFormElement>('#add-family-form');
    if (addFamilyForm) {
        const addFamilyFormSubmitAction = async function (event: Event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(addFamilyForm);
            const response = await fetch('/Families/AddFamily', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const familyDetailsDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (familyDetailsDiv) {
                    familyDetailsDiv.innerHTML = '';
                    familyDetailsDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the families list on the main page.
                    await GetFamiliesList();
                }

            } else {
                console.error('Failed to add family:', response.statusText);
                return Promise.reject('Failed to add family: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        addFamilyForm.removeEventListener('submit', addFamilyFormSubmitAction);
        addFamilyForm.addEventListener('submit', addFamilyFormSubmitAction);
    }    
}

export async function displayEditFamilyModal(familyId: number): Promise<void> {
    startFullPageSpinner();

    const response = await fetch('/Families/EditFamily?familyId=' + familyId, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }

    });
    if (response.ok) {
        const familyDetailsDiv = document.querySelector<HTMLDivElement>('#item-details-div');
        if (familyDetailsDiv) {
            const familyDetailsHTML = await response.text();
            familyDetailsDiv.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = familyDetailsHTML;
            familyDetailsDiv.appendChild(fullScreenOverlay);
            hideBodyScrollbars();
            familyDetailsDiv.classList.remove('d-none');
            addEditFamilyModalEventListeners();
            await initializeAddEditFamily();
        }
        else {
            return Promise.reject('Family details div not found in the document.');
        }

    } else {
        console.error('Failed to fetch edit family element:', response.statusText);
        return Promise.reject('Failed to fetch edit family element: ' + response.statusText);
    }

    stopFullPageSpinner();

    return Promise.resolve();
}

function addEditFamilyModalEventListeners(): void {
    const closeButton = document.querySelector<HTMLButtonElement>('#close-family-modal-button');
    if (closeButton) {
        const closeButtonClickedAction = function (event: MouseEvent) {
            event.preventDefault();
            event.stopPropagation();
            const familyDetailsDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (familyDetailsDiv) {
                familyDetailsDiv.innerHTML = '';
                familyDetailsDiv.classList.add('d-none');
                document.body.style.overflow = 'auto';
            }
        };
        closeButton.removeEventListener('click', closeButtonClickedAction);
        closeButton.addEventListener('click', closeButtonClickedAction);
    }

    const editFamilyForm = document.querySelector<HTMLFormElement>('#edit-family-form');
    if (editFamilyForm) {
        const editFamilyFormSubmitAction = async function (event: Event) {
            event.preventDefault();
            event.stopPropagation();
            startFullPageSpinner();
            const formData = new FormData(editFamilyForm);
            const response = await fetch('/Families/EditFamily', {
                method: 'POST',
                body: formData
            });
            if (response.ok) {
                const familyDetailsDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                if (familyDetailsDiv) {
                    familyDetailsDiv.innerHTML = '';
                    familyDetailsDiv.classList.add('d-none');
                    document.body.style.overflow = 'auto';
                    // Refresh the families list on the main page.
                    await GetFamiliesList();
                }

            } else {
                console.error('Failed to update family:', response.statusText);
                return Promise.reject('Failed to update family: ' + response.statusText);
            }
            stopFullPageSpinner();
            return Promise.resolve();
        };
        editFamilyForm.removeEventListener('submit', editFamilyFormSubmitAction);
        editFamilyForm.addEventListener('submit', editFamilyFormSubmitAction);
    }
}

/**
* Sets up the Rich Text Editor for the todo description field and adds event listeners for image upload success and editor creation.
* @returns {Promise<void>} A promise that resolves when the setup is complete.
*/
function setupRichTextEditor() {
    const fullScreenOverlay = document.getElementById('full-screen-overlay-div');
    if (fullScreenOverlay !== null) {
        if (fullScreenOverlay.querySelector('script') !== null) {
            eval((fullScreenOverlay.querySelector('script') as HTMLElement).innerHTML);
        }
        const richTextEditor: any = document.getElementById('description-rich-text-editor');
        if (richTextEditor && richTextEditor.ej2_instances) {

            richTextEditor.ej2_instances[0].addEventListener('imageUploadSuccess', onImageUploadSuccess);

            richTextEditor.ej2_instances[0].addEventListener('created', onRichTextEditorCreated);

            richTextEditor.ej2_instances[0].addEventListener('focus', onRichTextEditorFocus);
        }
    }
}

/**
 * Handles the image upload success event for the Rich Text Editor.
 * Updates the file name in the editor after a successful image upload.
 * @param {any} args - The event arguments containing the uploaded file information.
 */
function onImageUploadSuccess(args: any) {
    if (args.e.currentTarget.getResponseHeader('name') != null) {
        args.file.name = args.e.currentTarget.getResponseHeader('name');
        let filename: any = document.querySelectorAll(".e-file-name")[0];
        filename.innerHTML = args.file.name.replace(document.querySelectorAll(".e-file-type")[0].innerHTML, '');
        filename.title = args.file.name;
    }
}

/**
 * Refreshes the Rich Text Editor UI after it has been created.
 * This is necessary to ensure that the editor is properly initialized and displayed.
 */
function onRichTextEditorCreated() {
    setTimeout(function () {
        let rteElement: any = document.getElementById('description-rich-text-editor');
        if (rteElement) {
            if (rteElement.ej2_instances && rteElement.ej2_instances.length > 0) {
                rteElement.ej2_instances[0].refreshUI();
            }

        }
    },
        1000);
}

/**
 * Refreshes the Rich Text Editor UI when it receives focus.
 * This ensures that the editor is properly displayed and ready for user input.
 */
function onRichTextEditorFocus() {
    let rteElement: any = document.getElementById('description-rich-text-editor');
    if (rteElement) {
        if (rteElement.ej2_instances && rteElement.ej2_instances.length > 0) {
            rteElement.ej2_instances[0].refreshUI();
        }
    }
}

/**
 * Validates the inputs on the Add/Edit Todo page.
 * Checks if the title is empty, and if the date inputs are valid.
 * Enables or disables the save button based on the validation results.
 */
function validateInputs(): void {
    let isValid = true;
    const saveButton = document.getElementById('save-family-button');
    if (saveButton !== null) {
        const nameInput = document.getElementById('family-name-input') as HTMLInputElement;
        const nameRequiredDiv = document.querySelector<HTMLDivElement>('#name-required-div');
        if (nameInput && nameInput.value.trim() === '') {
            isValid = false;
            if (nameRequiredDiv) {
                nameRequiredDiv.classList.remove('d-none');
            }
        }
        else {
            if (nameRequiredDiv) {
                nameRequiredDiv.classList.add('d-none');
            }
        }
        
        if (isValid) {
            saveButton.removeAttribute('disabled');
        }
        else {
            saveButton.setAttribute('disabled', 'disabled');
        }
    }
}

export async function initializeAddEditFamily(): Promise<void> {
    languageId = getCurrentLanguageId();

    setupRichTextEditor();

    const nameInput = document.getElementById('family-name-input') as HTMLInputElement;
    if (nameInput) {
        nameInput.addEventListener('input', validateInputs);
    }

    validateInputs();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}
