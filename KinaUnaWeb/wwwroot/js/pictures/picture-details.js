import * as LocaleHelper from '../localization-v8.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner, startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { addCopyLocationButtonEventListener, setupHereMaps } from '../locations/location-tools.js';
import { PicturesPageParameters } from '../page-models-v8.js';
let pictureDetailsTouchStartX = 0;
let pictureDetailsTouchStartY = 0;
let pictureDetailsTouchEndX = 0;
let pictureDetailsTouchEndY = 0;
/**
 * Adds click event listeners to all elements with data-picture-id with the pictureId value on the page.
 * When clicked, the picture details popup is displayed.
 * @param {string} pictureId The ID of the picture to display.
 */
export async function addPictureItemEventListeners(pictureId) {
    const pictureElementsWithDataId = document.querySelectorAll('[data-picture-id="' + pictureId + '"]');
    if (pictureElementsWithDataId) {
        pictureElementsWithDataId.forEach((element) => {
            element.addEventListener('click', async function () {
                await displayPictureDetails(pictureId);
            });
        });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Enable other scripts to call the displayPictureDetails function.
 * @param {string} pictureId The id of the picture to display.
 */
export function popupPictureDetails(pictureId) {
    displayPictureDetails(pictureId);
}
/**
 * Overrides the comment submit form to send the comment data to the server and then refresh the picture details popup.
 * Enables/disables the submit button when the comment textarea changes.
 */
async function addCommentEventListeners() {
    const submitForm = document.getElementById('new-picture-comment-form');
    if (submitForm !== null) {
        submitForm.addEventListener('submit', async function (event) {
            event.preventDefault();
            submitComment();
        });
    }
    const newCommentTextArea = document.getElementById('new-picture-comment-text-area');
    if (newCommentTextArea !== null) {
        newCommentTextArea.addEventListener('input', function () {
            const submitCommentButton = document.getElementById('submit-new-picture-comment-button');
            if (submitCommentButton) {
                if (newCommentTextArea.value.length > 0) {
                    submitCommentButton.disabled = false;
                    return;
                }
                submitCommentButton.disabled = true;
            }
        });
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Gets the form data from the comment form and sends it to the server to add a new comment to the picture.
 */
async function submitComment() {
    startLoadingItemsSpinner('item-details-content-wrapper', 0.25, 128, 128, 128);
    const submitForm = document.getElementById('new-picture-comment-form');
    if (submitForm !== null) {
        const formData = new FormData(submitForm);
        const response = await fetch('/Pictures/AddPictureComment', {
            method: 'POST',
            body: formData,
            headers: {
                'Accept': 'application/json',
                'enctype': 'multipart/form-data'
            }
        }).catch(function (error) {
            console.log('Error adding comment. Error: ' + error);
        });
        if (response) {
            const currentPictureIdDiv = document.querySelector('#current-picture-id-div');
            if (currentPictureIdDiv) {
                const currentPictureId = currentPictureIdDiv.getAttribute('data-current-picture-id');
                if (currentPictureId) {
                    displayPictureDetails(currentPictureId, true);
                }
            }
        }
    }
    stopLoadingItemsSpinner('item-details-content-wrapper');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * If the user has access, adds event listeners to the edit button and related elements in the item details popup.
 */
async function addEditEventListeners() {
    const toggleEditButton = document.querySelector('#toggle-edit-button');
    if (toggleEditButton !== null) {
        $("#toggle-edit-button").on('click', function () {
            $("#edit-section").toggle(500);
        });
        await setTagsAutoSuggestList(getCurrentProgenyId());
        await setLocationAutoSuggestList(getCurrentProgenyId());
        setupDateTimePicker();
        setupAccessLevelList();
        addCopyLocationButtonEventListener();
        const submitForm = document.getElementById('edit-picture-form');
        if (submitForm !== null) {
            submitForm.addEventListener('submit', async function (event) {
                event.preventDefault();
                submitPictureEdit();
            });
        }
        $(".selectpicker").selectpicker("refresh");
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Gets the data from the edit picture form and sends it to the server to update the picture data.
 * Then refreshes the picture details popup.
 */
async function submitPictureEdit() {
    startLoadingItemsSpinner('item-details-content-wrapper', 0.25, 128, 128, 128);
    const submitForm = document.getElementById('edit-picture-form');
    if (submitForm !== null) {
        const formData = new FormData(submitForm);
        const response = await fetch('/Pictures/EditPicture', {
            method: 'POST',
            body: formData,
            headers: {
                'Accept': 'application/json',
                'enctype': 'multipart/form-data'
            }
        }).catch(function (error) {
            console.log('Error editing Picture. Error: ' + error);
        });
        if (response) {
            const currentPictureIdDiv = document.querySelector('#current-picture-id-div');
            if (currentPictureIdDiv) {
                const currentPictureId = currentPictureIdDiv.getAttribute('data-current-picture-id');
                if (currentPictureId) {
                    displayPictureDetails(currentPictureId, true);
                }
            }
        }
    }
    stopLoadingItemsSpinner('item-details-content-wrapper');
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function setupAccessLevelList() {
}
/**
 * Configures the date time picker for the picture edit form.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    const zebraDateTimeFormat = getZebraDateTimeFormat();
    const zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(getCurrentLanguageId());
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
 * Adds event listeners to the previous and next links in the item details popup.
 * Adds event listners for swipe navigation and full screen image display.
 */
function addNavigationEventListeners() {
    let previousLink = document.querySelector('#previous-picture-link');
    if (previousLink) {
        previousLink.addEventListener('click', function () {
            let previousPictureId = previousLink.getAttribute('data-previous-picture-id');
            if (previousPictureId) {
                displayPictureDetails(previousPictureId, true);
            }
        });
    }
    let nextLink = document.querySelector('#next-picture-link');
    if (nextLink) {
        nextLink.addEventListener('click', function () {
            let nextPictureId = nextLink.getAttribute('data-next-picture-id');
            if (nextPictureId) {
                displayPictureDetails(nextPictureId, true);
            }
        });
    }
    // Swipe navigation
    const photoDetailsDiv = document.querySelector('#photo-details-div');
    if (photoDetailsDiv) {
        photoDetailsDiv.addEventListener('touchstart', event => {
            pictureDetailsTouchStartX = event.touches[0].clientX;
            pictureDetailsTouchStartY = event.touches[0].clientY;
        });
        photoDetailsDiv.addEventListener('touchend', event => {
            pictureDetailsTouchEndX = event.changedTouches[0].clientX;
            pictureDetailsTouchEndY = event.changedTouches[0].clientY;
            if (Math.abs(pictureDetailsTouchEndY - pictureDetailsTouchStartY) > 100) {
                return;
            }
            if (Math.abs(pictureDetailsTouchEndX - pictureDetailsTouchStartX) < 50) {
                return;
            }
            if (pictureDetailsTouchEndX < pictureDetailsTouchStartX) {
                let nextPictureId = nextLink?.getAttribute('data-next-picture-id');
                if (nextPictureId) {
                    displayPictureDetails(nextPictureId, true);
                }
            }
            if (pictureDetailsTouchEndX > pictureDetailsTouchStartX) {
                let previousPictureId = previousLink?.getAttribute('data-previous-picture-id');
                if (previousPictureId) {
                    displayPictureDetails(previousPictureId, true);
                }
            }
        });
    }
    // Todo: Add pinch/scroll zoom
    // Full screen image display
    const imageElements = document.querySelectorAll('.picture-details-image');
    if (imageElements) {
        imageElements.forEach((imageElement) => {
            imageElement.addEventListener('click', function (event) {
                const targetImage = event.target;
                if (targetImage) {
                    targetImage.parentElement?.classList.toggle('picture-details-image-full-screen');
                }
            });
        });
    }
}
/**
 * Adds an event listener to the close button in the item details popup.
 * When clicked, the popup is hidden and the body scrollbars are shown.
 */
function addCloseButtonEventListener() {
    let closeButtonsList = document.querySelectorAll('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            button.addEventListener('click', function () {
                const itemDetailsPopupDiv = document.querySelector('#item-details-div');
                if (itemDetailsPopupDiv) {
                    itemDetailsPopupDiv.innerHTML = '';
                    itemDetailsPopupDiv.classList.add('d-none');
                    showBodyScrollbars();
                }
            });
        });
    }
}
/**
 * Adds an event listener to the show map button in the item details popup.
 * When clicked, the map container is shown or hidden.
 */
function addShowMapButtonEventListener() {
    let showMapButton = document.querySelector('#show-here-maps-button');
    if (showMapButton) {
        const mapContainerDiv = document.getElementById('here-map-container-div');
        showMapButton.addEventListener('click', function () {
            if (mapContainerDiv === null) {
                return;
            }
            if (mapContainerDiv.classList.contains('d-none')) {
                mapContainerDiv.innerHTML = '';
                mapContainerDiv.classList.remove('d-none');
                setupHereMaps(getCurrentLanguageId());
            }
            else {
                mapContainerDiv.classList.add('d-none');
            }
        });
    }
}
/**
 * Fetches the HTML for picture details and displays it in a popup.
 * Then adds the event listeners for the elements displayed.
 * @param {string} pictureId The ID of the picture to display.
 * @param {boolean} isPopupVisible If the popup is already visible. If true, the body-content spinner will not be shown.
 */
async function displayPictureDetails(pictureId, isPopupVisible = false) {
    if (!isPopupVisible) {
        startFullPageSpinner();
    }
    else {
        startLoadingItemsSpinner('item-details-content-wrapper', 0.25, 128, 128, 128);
    }
    let tagFilter = '';
    const picturePageParameters = getPicturePageParametersFromPageData();
    if (picturePageParameters) {
        if (picturePageParameters.tagFilter) {
            tagFilter = picturePageParameters.tagFilter;
        }
    }
    let url = '/Pictures/Picture?id=' + pictureId + "&tagFilter=" + tagFilter + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const itemElementHtml = await response.text();
            const itemDetailsPopupDiv = document.querySelector('#item-details-div');
            if (itemDetailsPopupDiv) {
                itemDetailsPopupDiv.classList.remove('d-none');
                itemDetailsPopupDiv.innerHTML = itemElementHtml;
                hideBodyScrollbars();
                addCloseButtonEventListener();
                addNavigationEventListeners();
                addEditEventListeners();
                addCommentEventListeners();
                addShowMapButtonEventListener();
            }
        }
        else {
            console.error('Error getting picture item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting picture item. Error: ' + error);
    });
    if (!isPopupVisible) {
        stopFullPageSpinner();
    }
    else {
        stopLoadingItemsSpinner('item-details-content-wrapper');
    }
}
/**
 * Gets the picture page parameters from the page data div.
 * Used to determine if a tag filter is set for the pictures page.
 * @returns {PicturesPageParameters} The picture page parameters.
 */
function getPicturePageParametersFromPageData() {
    const picturesPageParametersDiv = document.querySelector('#pictures-page-parameters');
    let picturesPageParametersResult = new PicturesPageParameters();
    if (picturesPageParametersDiv !== null) {
        const pageParametersString = picturesPageParametersDiv.dataset.picturesPageParameters;
        if (!pageParametersString) {
            return null;
        }
        picturesPageParametersResult = JSON.parse(pageParametersString);
    }
    return picturesPageParametersResult;
}
//# sourceMappingURL=picture-details.js.map