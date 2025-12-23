/**
 * Sets up the Rich Text Editor for the note content field and adds event listeners for image upload success and editor creation.
 */
function setupRichTextEditor() {
    const fullScreenOverlay = document.getElementById('full-screen-overlay-div');
    if (fullScreenOverlay !== null) {
        if (fullScreenOverlay.querySelector('script') !== null) {
            eval(fullScreenOverlay.querySelector('script').innerHTML);
        }
        const richTextEditor = document.getElementById('content-rich-text-editor');
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
 * @param args The event arguments containing the uploaded file information.
 */
function onImageUploadSuccess(args) {
    if (args.e.currentTarget.getResponseHeader('name') != null) {
        args.file.name = args.e.currentTarget.getResponseHeader('name');
        let filename = document.querySelectorAll(".e-file-name")[0];
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
        let rteElement = document.getElementById('content-rich-text-editor');
        if (rteElement) {
            if (rteElement.ej2_instances && rteElement.ej2_instances.length > 0) {
                rteElement.ej2_instances[0].refreshUI();
            }
        }
    }, 1000);
}
/**
 * Refreshes the Rich Text Editor UI when it receives focus.
 * This ensures that the editor is properly initialized and displayed when focused.
 */
function onRichTextEditorFocus() {
    let rteElement = document.getElementById('content-rich-text-editor');
    if (rteElement) {
        if (rteElement.ej2_instances && rteElement.ej2_instances.length > 0) {
            rteElement.ej2_instances[0].refreshUI();
        }
    }
}
/**
* Initializes the Add/Edit Note page by setting up the date time picker, progeny select list, tags and categories auto suggest lists, and the Rich Text Editor.
* @returns A promise that resolves when the initialization is complete.
*/
export async function initializeAddEditHelpContent(itemId) {
    $(".selectpicker").selectpicker('refresh');
    setupRichTextEditor();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-help.js.map