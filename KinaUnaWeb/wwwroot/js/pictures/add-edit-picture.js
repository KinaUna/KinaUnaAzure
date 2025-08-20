import * as LocaleHelper from '../localization-v9.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v9.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v9.js';
import { PictureViewModel } from '../page-models-v9.js';
import { addCopyLocationButtonEventListener } from '../locations/location-tools.js';
import { setAddItemButtonEventListeners } from '../addItem/add-item.js';
let zebraDatePickerTranslations;
let languageId = 1;
let zebraDateTimeFormat;
let currentProgenyId;
let toggleEditButton;
let copyLocationButton;
let fileList = [];
let notSupportedFiles = [];
let imagesLoaded = 0;
/**
 * Configures the date time picker for the picture date input field.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    zebraDateTimeFormat = getZebraDateTimeFormat('#add-photo-zebra-date-time-format-div');
    zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(languageId);
    if (document.getElementById('picture-date-time-picker') !== null) {
        const dateTimePicker = $('#picture-date-time-picker');
        dateTimePicker.Zebra_DatePicker({
            format: zebraDateTimeFormat,
            open_icon_only: true,
            days: zebraDatePickerTranslations.daysArray,
            months: zebraDatePickerTranslations.monthsArray,
            lang_clear_date: zebraDatePickerTranslations.clearString,
            show_select_today: zebraDatePickerTranslations.todayString,
            select_other_months: true
        });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets up the Progeny select list and adds an event listener to update the tags and location auto suggest lists when the selected Progeny changes.
 */
function setupProgenySelectList() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        progenyIdSelect.addEventListener('change', onProgenySelectListChanged);
    }
}
async function onProgenySelectListChanged() {
    const progenyIdSelect = document.querySelector('#item-progeny-id-select');
    if (progenyIdSelect !== null) {
        currentProgenyId = parseInt(progenyIdSelect.value);
        await setTagsAutoSuggestList([currentProgenyId]);
        await setLocationAutoSuggestList([currentProgenyId]);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Adds an event listener to the edit button to toggle the edit section.
 * Shows/hides the edit section when the edit button is clicked.
 */
function addEditButtonEventListener() {
    toggleEditButton = document.querySelector('#toggle-edit-button');
    if (toggleEditButton !== null) {
        $("#toggle-edit-button").on('click', function () {
            $("#edit-section").toggle(500);
            $(".selectpicker").selectpicker("refresh");
        });
    }
}
/**
 * Hides the input fields and buttons when the pictures are being uploaded.
 */
function hideInputsWhenUploading() {
    const selectphotosButtonParentDiv = document.getElementById('select-photos-button-parent-div');
    if (selectphotosButtonParentDiv !== null) {
        selectphotosButtonParentDiv.classList.add('d-none');
    }
    const saveButton = document.getElementById('add-picture-save-button');
    if (saveButton !== null) {
        saveButton.disabled = true;
        saveButton.classList.add('d-none');
    }
    const selectPhotosButton = document.getElementById('select-photos-button');
    if (selectPhotosButton !== null) {
        selectPhotosButton.disabled = true;
        selectPhotosButton.classList.add('d-none');
    }
    const suggestTagsInputs = document.querySelectorAll('.amsify-suggestags-input');
    suggestTagsInputs.forEach((input) => {
        input.disabled = true;
    });
    const accessLevelButtons = document.querySelectorAll('.btn-kinaunaselect');
    accessLevelButtons.forEach((button) => {
        button.disabled = true;
    });
    const locationFormGroup = document.getElementById('location-form-group');
    if (locationFormGroup !== null) {
        locationFormGroup.classList.add('d-none');
    }
    const tagsFormGroup = document.getElementById('tags-form-group');
    if (tagsFormGroup !== null) {
        tagsFormGroup.classList.add('d-none');
    }
    const accessLevelFormGroup = document.getElementById('access-level-form-group');
    if (accessLevelFormGroup !== null) {
        accessLevelFormGroup.classList.add('d-none');
    }
    const actionsFormGroup = document.getElementById('actions-form-group');
    if (actionsFormGroup !== null) {
        actionsFormGroup.classList.add('d-none');
    }
    if (notSupportedFiles.length > 0) {
        for (let fileItem of notSupportedFiles) {
            const picturePreviewDiv = document.getElementById('picture-preview-div' + notSupportedFiles.indexOf(fileItem));
            if (picturePreviewDiv !== null) {
                picturePreviewDiv.innerHTML = '';
            }
        }
    }
    const removeFileButtonDivs = document.querySelectorAll('remove-button-parent-div');
    removeFileButtonDivs.forEach((buttonDiv) => {
        buttonDiv.classList.add('d-none');
    });
    const dropZone = document.getElementById('drop-files-div');
    if (dropZone !== null) {
        dropZone.classList.add('d-none');
        dropZone.removeEventListener('drop', async function (event) { });
    }
    // Elements are gone, scroll up to upload-file-div to keep image previews in view.
    const photoHeaderLabel = document.getElementById('photo-header-label');
    if (photoHeaderLabel !== null) {
        photoHeaderLabel.scrollIntoView();
    }
}
/**
 * Shows a loading spinner overlay for each picture being uploaded until the upload is completed.
 */
function showLoadingSpinners() {
    let itemNumber = 1;
    for (let fileItem of fileList) {
        const picturePreviewDiv = document.getElementById('picture-preview-div' + itemNumber);
        if (picturePreviewDiv !== null) {
            startLoadingItemsSpinner('picture-preview-div' + itemNumber, 0.5, 255, 255, 255);
        }
        itemNumber++;
    }
}
/**
 * Adds an event listener to the form to override the default submit event.
 * Prevents the form from being submitted and uploads the pictures in fileList one at a time.
  */
function addOverrideSubmitEvent() {
    const submitForm = document.getElementById('add-pictures-form');
    if (submitForm !== null) {
        submitForm.addEventListener('submit', onSubmitAddPicturesForm);
    }
}
async function onSubmitAddPicturesForm(event) {
    event.preventDefault();
    hideInputsWhenUploading();
    showLoadingSpinners();
    const filesInput = document.querySelector('#select-photos-button');
    if (filesInput !== null) {
        filesInput.value = '';
    }
    const submitForm = document.getElementById('add-pictures-form');
    const formData = new FormData(submitForm);
    let itemNumber = 1;
    for (let fileItem of fileList) {
        formData.delete('files');
        await uploadPicture(formData, fileItem, itemNumber);
        itemNumber++;
    }
    fileList = [];
    notSupportedFiles = [];
    const uploadCompletedDiv = document.getElementById('upload-completed-div');
    if (uploadCompletedDiv !== null) {
        uploadCompletedDiv.classList.remove('d-none');
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Uploads a picture file with the given form data.
 * @param {FormData} formData The Picture item form data (location, tags, accesslevel).
 * @param {File} pictureFile The picture file to upload.
 * @param {number} itemNumber The number of the picture item to upload in fileList, for identifying íts preview HTML element.
 */
async function uploadPicture(formData, pictureFile, itemNumber) {
    formData.append('files', pictureFile);
    const picturePreviewDiv = document.getElementById('picture-preview-div' + itemNumber);
    const response = await fetch('/Pictures/SavePicture', {
        method: 'POST',
        body: formData,
        headers: {
            'Accept': 'application/json',
            'enctype': 'multipart/form-data'
        }
    }).catch(function (error) {
        console.log('Error uploading Picture. Error: ' + error);
    });
    if (response) {
        const pictureItem = (await response.json());
        if (picturePreviewDiv !== null && pictureItem.pictureId > 0) {
            const pictureViewModel = new PictureViewModel();
            pictureViewModel.pictureId = pictureItem.pictureId;
            pictureViewModel.progenyId = pictureItem.progenyId;
            pictureViewModel.sortBy = 1;
            pictureViewModel.tagFilter = '';
            pictureViewModel.pictureNumber = itemNumber;
            const getPictureElementResponse = await fetch('/Pictures/GetPictureElement', {
                method: 'POST',
                body: JSON.stringify(pictureViewModel),
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            });
            if (getPictureElementResponse.ok && getPictureElementResponse.text !== null) {
                const pictureElementHtml = await getPictureElementResponse.text();
                picturePreviewDiv.innerHTML = '';
                picturePreviewDiv.innerHTML = pictureElementHtml;
            }
            const resultMessageWrapper = document.createElement('div');
            resultMessageWrapper.classList.add('col-auto');
            const successMessage = document.createElement('div');
            successMessage.classList.add('mt-4');
            successMessage.innerText = await LocaleHelper.getTranslation('Picture uploaded successfully.', 'Pictures', languageId);
            successMessage.classList.add('bg-success');
            successMessage.classList.add('text-white');
            successMessage.classList.add('p-3');
            resultMessageWrapper.appendChild(successMessage);
            picturePreviewDiv.insertAdjacentElement('beforeend', resultMessageWrapper);
        }
        stopLoadingItemsSpinner('picture-preview-div' + itemNumber);
    }
}
/**
 * Adds an event listener to the select photo button to open a file picker dialog.
 * As selectFiles uses showOpenFilePicker, it only works in some browsers, it is not supported for mobile browsers.
 */
function addSelectPhotoButtonEventListener() {
    const selectPhotoButton = document.querySelector('#select-photos-button');
    if (selectPhotoButton !== null) {
        selectPhotoButton.addEventListener('click', selectFiles);
    }
}
/**
 * Opens a file picker dialog and selects a file.
 * As it uses showOpenFilePicker, it only works in some browsers, it is not supported for mobile browsers.
 */
async function selectFiles() {
    const filePickerOptions = {
        multiple: true,
        types: [
            {
                description: 'Images',
                accept: {
                    'image/*': ['.jpg', '.jpeg', '.png', '.gif']
                }
            }
        ]
    };
    const uploadFileDiv = document.getElementById('upload-file-div');
    uploadFileDiv.innerHTML = '';
    const fileHandles = await window.showOpenFilePicker(filePickerOptions);
    for (const fileHandle of fileHandles) {
        const newFile = await fileHandle.getFile();
        fileList.push(newFile);
        displayFile(newFile, newFile.type);
    }
    if (fileList.length > 0) {
        const saveButton = document.getElementById('add-picture-save-button');
        if (saveButton !== null) {
            saveButton.disabled = false;
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Waits for a given number of milliseconds.
 * @param {number} milliseconds The number of milliseconds to wait.
 */
async function waitForDelay(milliseconds) {
    return new Promise(resolve => {
        setTimeout(resolve, milliseconds);
    });
}
/**
 * Processes the files returned by the file picker.
 * Adds each picture file to fileList and calls a function to display the preview for it.
 * When completed, enables the Save button.
 * @param {FileList} files The FileList returned by the file picker.
 */
async function handleFilesAdded(files) {
    const fileCount = files.length;
    let filesToDisplay = fileList.length + notSupportedFiles.length;
    for (let i = 0; i < fileCount; i++) {
        // Load pictures in order, so wait for the image load event to update imagesLoaded count.
        while (filesToDisplay > imagesLoaded) {
            // wait for 100 miliseconds
            await waitForDelay(100);
        }
        const file = files[i];
        if (file.type.indexOf('image') === -1) {
            notSupportedFiles.push(file);
            await displayNotSupportedFile(file);
            continue;
        }
        const reader = new FileReader();
        reader.onload = async function () {
            if (reader.result === null)
                return;
            let arrayBuffer = reader.result;
            if (arrayBuffer.byteLength === 0)
                return;
            let fileFromReader = new File([arrayBuffer], file.name);
            fileList.push(fileFromReader);
            await displayFile(fileFromReader, file.type);
        };
        reader.readAsArrayBuffer(file);
        filesToDisplay++;
    }
    // Ensure picture previews are loaded before enabling the Save button.
    while (filesToDisplay > imagesLoaded) {
        await waitForDelay(100);
    }
    if (fileList.length > 0) {
        const saveButton = document.getElementById('add-picture-save-button');
        if (saveButton !== null) {
            saveButton.disabled = false;
        }
    }
}
/**
 * Add drag and drop event listeners to the drop-files-div element.
 */
function addDropEventListener() {
    const dropZone = document.getElementById('drop-files-div');
    if (dropZone === null) {
        return;
    }
    dropZone.addEventListener('dragover', onDropFilesDivDragOver);
    dropZone.addEventListener('drop', onDropFilesDivDrop);
}
function onDropFilesDivDragOver(event) {
    event.stopPropagation();
    event.preventDefault();
    if (event.dataTransfer === null)
        return;
    event.dataTransfer.dropEffect = 'copy';
}
async function onDropFilesDivDrop(event) {
    event.stopPropagation(); // Stops some browsers from redirecting.
    event.preventDefault();
    if (event.dataTransfer === null)
        return;
    var files = event.dataTransfer.files;
    handleFilesAdded(files);
    return false;
}
/**
 * Adds an event listener to the file input element to handle the selected files.
 * Uses FileReader to get the raw data of the file, then creates a new File object from the raw data.
 * The input field should be set to a custom accept value to prevent mobile browsers from opening the Gallery/Photo Picker as a default
 * If the user choose to select files with the Gallery/Photo Picker on mobile, the files are stripped of some metadata and GPS data will not be available.
 */
function addFileInputEventListener() {
    const filesInput = document.querySelector('#select-photos-button');
    if (filesInput === null) {
        return;
    }
    filesInput.addEventListener('change', onFilesInputChanged);
    filesInput.value = '';
}
async function onFilesInputChanged(event) {
    const eventTargetAsHtmlInputElement = event.target;
    if (eventTargetAsHtmlInputElement !== null && eventTargetAsHtmlInputElement.files !== null) {
        handleFilesAdded(eventTargetAsHtmlInputElement.files);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Adds a div element with a preview of the picture file and basic file info.
 * As the file is read by the FileReader type information is not embedded, and needs to be provided.
 * @param {File} file The file to show a preview of.
 * @param {string} fileType The type of file (image/jpeg, image/png, etc.)
 */
async function displayFile(file, fileType) {
    const fileUrl = URL.createObjectURL(file);
    const picturePreviewDiv = document.createElement('div');
    const fileNumber = fileList.length;
    picturePreviewDiv.id = 'picture-preview-div' + fileNumber;
    picturePreviewDiv.classList.add('row');
    picturePreviewDiv.classList.add('row-cols-auto');
    const picturePreview = document.createElement('img');
    picturePreview.classList.add('photo-upload-picture');
    picturePreview.classList.add('ml-3');
    picturePreview.classList.add('mr-3');
    picturePreviewDiv.appendChild(picturePreview);
    picturePreview.addEventListener('load', function () { imagesLoaded++; });
    picturePreview.addEventListener('error', function () { imagesLoaded++; });
    const pictureFileInfoDiv = document.createElement('div');
    pictureFileInfoDiv.classList.add('col');
    pictureFileInfoDiv.classList.add('pt-3');
    const fileInfoNameDiv = document.createElement('div');
    const fileInfoSizeDiv = document.createElement('div');
    const fileInfoTypeDiv = document.createElement('div');
    const fileInfoRemoveFileDiv = document.createElement('div');
    fileInfoRemoveFileDiv.classList.add('w-100');
    fileInfoRemoveFileDiv.classList.add('p-5');
    fileInfoRemoveFileDiv.classList.add('remove-button-parent-div');
    const fileNameLabelSpan = document.createElement('span');
    fileNameLabelSpan.innerText = await LocaleHelper.getTranslation('File name', 'Pictures', languageId) + ': ';
    const fileNameSpan = document.createElement('span');
    fileNameSpan.innerText = file.name;
    const fileSizeLabelSpan = document.createElement('span');
    fileSizeLabelSpan.innerText = await LocaleHelper.getTranslation('File size', 'Pictures', languageId) + ': ';
    const fileSizeSpan = document.createElement('span');
    fileSizeSpan.innerText = (file.size / 1024).toFixed(0) + 'KB';
    const fileTypeLabelSpan = document.createElement('span');
    fileTypeLabelSpan.innerText = await LocaleHelper.getTranslation('File type', 'Pictures', languageId) + ': ';
    const fileTypeSpan = document.createElement('span');
    fileTypeSpan.innerText = fileType;
    const removeFileButton = document.createElement('button');
    removeFileButton.classList.add('btn');
    removeFileButton.classList.add('btn-danger');
    removeFileButton.classList.add('mr-auto');
    removeFileButton.classList.add('mt-auto');
    removeFileButton.innerText = await LocaleHelper.getTranslation('Remove file', 'Pictures', languageId);
    const removeFilePicturePreviewDivId = picturePreviewDiv.id;
    removeFileButton.addEventListener('click', async (event) => {
        event.preventDefault();
        fileList.splice(fileNumber - 1, 1);
        const picturePreviewDivToRemove = document.getElementById(removeFilePicturePreviewDivId);
        if (picturePreviewDivToRemove !== null) {
            picturePreviewDivToRemove.remove();
        }
    });
    fileInfoNameDiv.appendChild(fileNameLabelSpan);
    fileInfoNameDiv.appendChild(fileNameSpan);
    fileInfoSizeDiv.appendChild(fileSizeLabelSpan);
    fileInfoSizeDiv.appendChild(fileSizeSpan);
    fileInfoTypeDiv.appendChild(fileTypeLabelSpan);
    fileInfoTypeDiv.appendChild(fileTypeSpan);
    fileInfoRemoveFileDiv.appendChild(removeFileButton);
    pictureFileInfoDiv.appendChild(fileInfoNameDiv);
    pictureFileInfoDiv.appendChild(fileInfoSizeDiv);
    pictureFileInfoDiv.appendChild(fileInfoTypeDiv);
    pictureFileInfoDiv.appendChild(fileInfoRemoveFileDiv);
    picturePreviewDiv.appendChild(pictureFileInfoDiv);
    const uploadFileDiv = document.getElementById('upload-file-div');
    uploadFileDiv.appendChild(picturePreviewDiv);
    const spacerDiv = document.createElement('div');
    spacerDiv.classList.add('space-20');
    uploadFileDiv.appendChild(spacerDiv);
    picturePreview.src = fileUrl; // update the picture source last, so the load event is triggered and we can ensure they are all loaded before enabling the save button.
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Adds a div element that shows the file is not supported.
 * Shows an X icon and the basic file info.
 * @param {File} file The file to show data for.
 */
async function displayNotSupportedFile(file) {
    const picturePreviewDiv = document.createElement('div');
    const fileNumber = notSupportedFiles.length;
    picturePreviewDiv.id = 'picture-preview-div' + fileNumber;
    picturePreviewDiv.classList.add('row');
    picturePreviewDiv.classList.add('row-cols-auto');
    const picturePreview = document.createElement('img');
    picturePreview.classList.add('photo-upload-picture');
    picturePreview.classList.add('ml-3');
    picturePreview.classList.add('mr-3');
    picturePreviewDiv.appendChild(picturePreview);
    picturePreview.addEventListener('load', function () { imagesLoaded++; });
    picturePreview.addEventListener('error', function () { imagesLoaded++; });
    const pictureFileInfoDiv = document.createElement('div');
    pictureFileInfoDiv.classList.add('col');
    pictureFileInfoDiv.classList.add('pt-3');
    const fileInfoNameDiv = document.createElement('div');
    const fileInfoSizeDiv = document.createElement('div');
    const fileInfoTypeDiv = document.createElement('div');
    const fileTypeNotSupportedDiv = document.createElement('div');
    const fileInfoRemoveFileDiv = document.createElement('div');
    fileInfoRemoveFileDiv.classList.add('w-100');
    fileInfoRemoveFileDiv.classList.add('p-5');
    const fileNameLabelSpan = document.createElement('span');
    fileNameLabelSpan.innerText = await LocaleHelper.getTranslation('File name', 'Pictures', languageId) + ': ';
    const fileNameSpan = document.createElement('span');
    fileNameSpan.innerText = file.name;
    const fileSizeLabelSpan = document.createElement('span');
    fileSizeLabelSpan.innerText = await LocaleHelper.getTranslation('File size', 'Pictures', languageId) + ': ';
    const fileSizeSpan = document.createElement('span');
    fileSizeSpan.innerText = (file.size / 1024).toFixed(0) + 'KB';
    const fileTypeLabelSpan = document.createElement('span');
    fileTypeLabelSpan.innerText = await LocaleHelper.getTranslation('File type', 'Pictures', languageId) + ': ';
    const fileTypeSpan = document.createElement('span');
    fileTypeSpan.innerText = file.type;
    const notSupportedSpan = document.createElement('span');
    notSupportedSpan.innerText = await LocaleHelper.getTranslation('File type not supported.', 'Pictures', languageId);
    notSupportedSpan.classList.add('text-danger');
    const removeFileButton = document.createElement('button');
    removeFileButton.classList.add('btn');
    removeFileButton.classList.add('btn-danger');
    removeFileButton.classList.add('mr-auto');
    removeFileButton.classList.add('mt-auto');
    removeFileButton.innerText = await LocaleHelper.getTranslation('Remove file', 'Pictures', languageId);
    const removeFilePicturePreviewDivId = picturePreviewDiv.id;
    removeFileButton.addEventListener('click', async (event) => {
        event.preventDefault();
        notSupportedFiles.splice(fileNumber - 1, 1);
        const picturePreviewDivToRemove = document.getElementById(removeFilePicturePreviewDivId);
        if (picturePreviewDivToRemove !== null) {
            picturePreviewDivToRemove.remove();
        }
    });
    fileInfoNameDiv.appendChild(fileNameLabelSpan);
    fileInfoNameDiv.appendChild(fileNameSpan);
    fileInfoSizeDiv.appendChild(fileSizeLabelSpan);
    fileInfoSizeDiv.appendChild(fileSizeSpan);
    fileInfoTypeDiv.appendChild(fileTypeLabelSpan);
    fileInfoTypeDiv.appendChild(fileTypeSpan);
    fileTypeNotSupportedDiv.appendChild(notSupportedSpan);
    fileInfoRemoveFileDiv.appendChild(removeFileButton);
    pictureFileInfoDiv.appendChild(fileInfoNameDiv);
    pictureFileInfoDiv.appendChild(fileInfoSizeDiv);
    pictureFileInfoDiv.appendChild(fileInfoTypeDiv);
    pictureFileInfoDiv.appendChild(fileTypeNotSupportedDiv);
    pictureFileInfoDiv.appendChild(fileInfoRemoveFileDiv);
    picturePreviewDiv.appendChild(pictureFileInfoDiv);
    const uploadFileDiv = document.getElementById('upload-file-div');
    uploadFileDiv.appendChild(picturePreviewDiv);
    const spacerDiv = document.createElement('div');
    spacerDiv.classList.add('space-20');
    uploadFileDiv.appendChild(spacerDiv);
    picturePreview.src = '/images/clear.png';
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Setup of elements and event listeners.
 */
export async function initializeAddEditPicture() {
    languageId = getCurrentLanguageId();
    currentProgenyId = getCurrentProgenyId();
    fileList = [];
    notSupportedFiles = [];
    imagesLoaded = 0;
    await setupDateTimePicker();
    setupProgenySelectList();
    await setTagsAutoSuggestList([currentProgenyId]);
    await setLocationAutoSuggestList([currentProgenyId]);
    addEditButtonEventListener();
    addCopyLocationButtonEventListener();
    addFileInputEventListener();
    addDropEventListener();
    addOverrideSubmitEvent();
    setAddItemButtonEventListeners();
    $(".selectpicker").selectpicker('refresh');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-edit-picture.js.map