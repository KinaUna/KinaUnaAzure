import * as LocaleHelper from '../localization-v9.js';
import { setTagsAutoSuggestList, setContextAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat, getLongDateTimeFormatMoment, validateDateValue, setLocationAutoSuggestList } from '../data-tools-v9.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
/**
 * Configures the date time picker for the todo due date and start date input fields.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-todo-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    const dueDateInput = document.getElementById('todo-due-date-time-picker');
    if (dueDateInput !== null) {
        dueDateInput.addEventListener('blur', validateInputs);
        const dueDateTimePicker = $('#todo-due-date-time-picker');
        dueDateTimePicker.Zebra_DatePicker({
            format: zebraDateTimeFormat,
            open_icon_only: true,
            onSelect: function (a, b, c) { validateInputs; },
            days: zebraDatePickerTranslations.daysArray,
            months: zebraDatePickerTranslations.monthsArray,
            lang_clear_date: zebraDatePickerTranslations.clearString,
            show_select_today: zebraDatePickerTranslations.todayString,
            select_other_months: true
        });
    }
    const startDateInput = document.getElementById('todo-start-date-time-picker');
    if (startDateInput !== null) {
        startDateInput.addEventListener('blur', validateInputs);
        const startDateTimePicker = $('#todo-start-date-time-picker');
        startDateTimePicker.Zebra_DatePicker({
            format: zebraDateTimeFormat,
            open_icon_only: true,
            onSelect: function (a, b, c) { validateInputs; },
            days: zebraDatePickerTranslations.daysArray,
            months: zebraDatePickerTranslations.monthsArray,
            lang_clear_date: zebraDatePickerTranslations.clearString,
            show_select_today: zebraDatePickerTranslations.todayString,
            select_other_months: true
        });
    }
    const completedDateInput = document.getElementById('todo-completed-date-time-picker');
    if (completedDateInput !== null) {
        completedDateInput.addEventListener('blur', validateInputs);
        const completedDateTimePicker = $('#todo-completed-date-time-picker');
        completedDateTimePicker.Zebra_DatePicker({
            format: zebraDateTimeFormat,
            open_icon_only: true,
            onSelect: function (a, b, c) { validateInputs; },
            days: zebraDatePickerTranslations.daysArray,
            months: zebraDatePickerTranslations.monthsArray,
            lang_clear_date: zebraDatePickerTranslations.clearString,
            show_select_today: zebraDatePickerTranslations.todayString,
            select_other_months: true
        });
    }
    ;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets up the Progeny select list and adds an event listener to update the tags and categories auto suggest lists when the selected Progeny changes.
 */
function setupProgenySelectList() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', async () => {
            currentProgenyId = parseInt(progenyIdSelect.value);
            await setTagsAutoSuggestList([currentProgenyId]);
            await setContextAutoSuggestList([currentProgenyId]);
            await setLocationAutoSuggestList([currentProgenyId]);
            $(".selectpicker").selectpicker('refresh');
        });
    }
}
/**
 * Sets up the Rich Text Editor for the todo description field and adds event listeners for image upload success and editor creation.
 * @returns {Promise<void>} A promise that resolves when the setup is complete.
 */
function setupRichTextEditor(containerElementId) {
    const containerElement = document.getElementById(containerElementId);
    if (containerElement !== null) {
        if (containerElement.querySelector('script') !== null) {
            eval(containerElement.querySelector('script').innerHTML);
        }
        const richTextEditor = document.getElementById('description-rich-text-editor');
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
        let rteElement = document.getElementById('description-rich-text-editor');
        if (rteElement) {
            if (rteElement.ej2_instances && rteElement.ej2_instances.length > 0) {
                rteElement.ej2_instances[0].refreshUI();
            }
        }
    }, 1000);
}
/**
 * Refreshes the Rich Text Editor UI when it receives focus.
 * This ensures that the editor is properly displayed and ready for user input.
 */
function onRichTextEditorFocus() {
    let rteElement = document.getElementById('description-rich-text-editor');
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
function validateInputs() {
    let isValid = true;
    const saveButton = document.getElementById('save-kanban-item-button');
    if (saveButton !== null) {
        const titleInput = document.getElementById('todo-title-input');
        const titleRequiredDiv = document.querySelector('#todo-title-required-div');
        if (titleInput && titleInput.value.trim() === '') {
            isValid = false;
            if (titleRequiredDiv) {
                titleRequiredDiv.classList.remove('d-none');
            }
        }
        else {
            if (titleRequiredDiv) {
                titleRequiredDiv.classList.add('d-none');
            }
        }
        const todoStartDateInput = document.getElementById('todo-start-date-time-picker');
        const todoStartDateRequiredDiv = document.querySelector('#todo-start-date-invalid-div');
        if (todoStartDateInput) {
            if (todoStartDateInput.value !== '' && !validateDateValue(todoStartDateInput.value, getLongDateTimeFormatMoment())) {
                isValid = false;
                if (todoStartDateRequiredDiv) {
                    todoStartDateRequiredDiv.classList.remove('d-none');
                }
            }
            else {
                if (todoStartDateRequiredDiv) {
                    todoStartDateRequiredDiv.classList.add('d-none');
                }
            }
        }
        const todoDueDateInput = document.getElementById('todo-due-date-time-picker');
        const todoDueDateRequiredDiv = document.querySelector('#todo-due-date-invalid-div');
        if (todoDueDateInput) {
            if (todoDueDateInput.value !== '' && !validateDateValue(todoDueDateInput.value, getLongDateTimeFormatMoment())) {
                isValid = false;
                if (todoDueDateRequiredDiv) {
                    todoDueDateRequiredDiv.classList.remove('d-none');
                }
            }
            else {
                if (todoDueDateRequiredDiv) {
                    todoDueDateRequiredDiv.classList.add('d-none');
                }
            }
        }
        const todoCompletedDateInput = document.getElementById('todo-completed-date-time-picker');
        const todoCompletedDateRequiredDiv = document.querySelector('#todo-completed-date-invalid-div');
        if (todoCompletedDateInput) {
            if (todoCompletedDateInput.value !== '' && !validateDateValue(todoCompletedDateInput.value, getLongDateTimeFormatMoment())) {
                isValid = false;
                if (todoCompletedDateRequiredDiv) {
                    todoCompletedDateRequiredDiv.classList.remove('d-none');
                }
            }
            else {
                if (todoCompletedDateRequiredDiv) {
                    todoCompletedDateRequiredDiv.classList.add('d-none');
                }
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
/**
 * Initializes the Add/Edit Todo page by setting up the date time picker, progeny select list, tags and context auto suggest lists, and the Rich Text Editor.
 * @returns {Promise<void>} A promise that resolves when the initialization is complete.
 * */
export async function initializeAddEditKanbanItem(containerElementId) {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentProgenyId();
    await setupDateTimePicker();
    setupProgenySelectList();
    await setTagsAutoSuggestList([currentProgenyId]);
    await setContextAutoSuggestList([currentProgenyId]);
    await setLocationAutoSuggestList([currentProgenyId]);
    $(".selectpicker").selectpicker('refresh');
    setupRichTextEditor(containerElementId);
    const titleInput = document.getElementById('todo-title-input');
    if (titleInput) {
        titleInput.addEventListener('input', validateInputs);
    }
    validateInputs();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-kanban-item.js.map