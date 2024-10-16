﻿import * as LocaleHelper from '../localization-v8.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner, startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { addCopyLocationButtonEventListener, setupHereMaps } from '../locations/location-tools.js';
import { PicturesPageParameters } from '../page-models-v8.js';

let pictureDetailsTouchStartX: number = 0;
let pictureDetailsTouchStartY: number = 0;
let pictureDetailsTouchEndX: number = 0;
let pictureDetailsTouchEndY: number = 0;

/**
 * Adds click event listeners to all elements with data-picture-id with the pictureId value on the page.
 * When clicked, the picture details popup is displayed.
 * @param {string} pictureId The ID of the picture to display.
 */
export async function addPictureItemEventListeners(pictureId: string): Promise<void> {
    const pictureElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-picture-id="' + pictureId + '"]');
    if (pictureElementsWithDataId) {
        pictureElementsWithDataId.forEach((element) => {
            element.addEventListener('click', async function () {
                await displayPictureDetails(pictureId);
            });
        });
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Enable other scripts to call the displayPictureDetails function.
 * @param {string} pictureId The id of the picture to display.
 */
export async function popupPictureDetails(pictureId: string): Promise<void> {
    await displayPictureDetails(pictureId);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Overrides the comment submit form to send the comment data to the server and then refresh the picture details popup.
 * Enables/disables the submit button when the comment textarea changes.
 */
async function addCommentEventListeners(): Promise<void> {
    const submitForm = document.getElementById('new-picture-comment-form') as HTMLFormElement;
    if (submitForm !== null) {
        submitForm.addEventListener('submit', async function (event) {
            event.preventDefault();

            submitComment();
        });
    }

    const newCommentTextArea = document.getElementById('new-picture-comment-text-area') as HTMLTextAreaElement;
    if (newCommentTextArea !== null) {
        newCommentTextArea.addEventListener('input', function () {
            const submitCommentButton = document.getElementById('submit-new-picture-comment-button') as HTMLButtonElement;
            if (submitCommentButton) {
                if (newCommentTextArea.value.length > 0) {
                    submitCommentButton.disabled = false;
                    return;
                }
                submitCommentButton.disabled = true;
            }
        });
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });

}

/**
 * Gets the form data from the comment form and sends it to the server to add a new comment to the picture.
 */
async function submitComment(): Promise<void> {
    startLoadingItemsSpinner('item-details-content-wrapper', 0.25, 128, 128, 128);
    const submitForm = document.getElementById('new-picture-comment-form') as HTMLFormElement;
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
            const currentPictureIdDiv = document.querySelector<HTMLDivElement>('#current-picture-id-div');
            if (currentPictureIdDiv) {
                const currentPictureId = currentPictureIdDiv.getAttribute('data-current-picture-id');
                if (currentPictureId) {
                    await displayPictureDetails(currentPictureId, true);
                }
            }
        }
    }

    stopLoadingItemsSpinner('item-details-content-wrapper');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * If the user has access, adds event listeners to the edit button and related elements in the item details popup.
 */
async function addEditEventListeners(): Promise<void> {
    const toggleEditButton = document.querySelector<HTMLButtonElement>('#toggle-edit-button');
    if (toggleEditButton !== null) {
        $("#toggle-edit-button").on('click', function () {
            $("#edit-section").toggle(500);
        });

        await setTagsAutoSuggestList([getCurrentProgenyId()]);
        await setLocationAutoSuggestList([getCurrentProgenyId()]);
        setupDateTimePicker();
        setupAccessLevelList();
        addCopyLocationButtonEventListener();
        const submitForm = document.getElementById('edit-picture-form') as HTMLFormElement;
        if (submitForm !== null) {
            submitForm.addEventListener('submit', async function (event) {
                event.preventDefault();

                submitPictureEdit();
            });
        }

        ($(".selectpicker") as any).selectpicker("refresh");
    }
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Gets the data from the edit picture form and sends it to the server to update the picture data.
 * Then refreshes the picture details popup.
 */
async function submitPictureEdit(): Promise<void> {
    startLoadingItemsSpinner('item-details-content-wrapper', 0.25, 128, 128, 128);

    const submitForm = document.getElementById('edit-picture-form') as HTMLFormElement;
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
            const currentPictureIdDiv = document.querySelector<HTMLDivElement>('#current-picture-id-div');
            if (currentPictureIdDiv) {
                const currentPictureId = currentPictureIdDiv.getAttribute('data-current-picture-id');
                if (currentPictureId) {
                    await displayPictureDetails(currentPictureId, true);
                }
            }
        }
    }

    stopLoadingItemsSpinner('item-details-content-wrapper');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function setupAccessLevelList(): void {

}
/**
 * Configures the date time picker for the picture edit form.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    const zebraDateTimeFormat = getZebraDateTimeFormat();
    const zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(getCurrentLanguageId());

    if (document.getElementById('picture-date-time-picker') !== null) {
        const dateTimePicker: any = $('#picture-date-time-picker');
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Adds event listeners to the previous and next links in the item details popup.
 * Adds event listners for swipe navigation and full screen image display.
 */
function addNavigationEventListeners(): void {
    let previousLink = document.querySelector<HTMLAnchorElement>('#previous-picture-link');
    if (previousLink) {
        previousLink.addEventListener('click', async function () {
            let previousPictureId = previousLink.getAttribute('data-previous-picture-id');
            if (previousPictureId) {
                await displayPictureDetails(previousPictureId, true);
            }
        });
    }
    let nextLink = document.querySelector<HTMLAnchorElement>('#next-picture-link');
    if (nextLink) {
        nextLink.addEventListener('click', async function () {
            let nextPictureId = nextLink.getAttribute('data-next-picture-id');
            if (nextPictureId) {
                await displayPictureDetails(nextPictureId, true);
            }
        });
    }

    // Swipe navigation
    const photoDetailsDiv = document.querySelector<HTMLDivElement>('#photo-details-div');
    if (photoDetailsDiv) {
        photoDetailsDiv.addEventListener('touchstart', event => {
            pictureDetailsTouchStartX = event.touches[0].clientX;
            pictureDetailsTouchStartY = event.touches[0].clientY;
        });
        photoDetailsDiv.addEventListener('touchend', async event => {
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
                    await displayPictureDetails(nextPictureId, true);
                }
            }
            if (pictureDetailsTouchEndX > pictureDetailsTouchStartX) {
                let previousPictureId = previousLink?.getAttribute('data-previous-picture-id');
                if (previousPictureId) {
                    await displayPictureDetails(previousPictureId, true);
                }
            }
        });
    }

    // Todo: Add pinch/scroll zoom

    // Full screen image display
    const imageElements = document.querySelectorAll<HTMLImageElement>('.picture-details-image');
    if (imageElements) {
        imageElements.forEach((imageElement) => {
            imageElement.addEventListener('click', function (event) {
                const targetImage = event.target as HTMLImageElement;
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
function addCloseButtonEventListener(): void {
    let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            button.addEventListener('click', function () {
                const itemDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
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
function addShowMapButtonEventListener(): void {
    let showMapButton = document.querySelector<HTMLButtonElement>('#show-here-maps-button');
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
async function displayPictureDetails(pictureId: string, isPopupVisible: boolean = false): Promise<void> {
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
            const itemDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
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
        } else {
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

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Gets the picture page parameters from the page data div.
 * Used to determine if a tag filter is set for the pictures page.
 * @returns {PicturesPageParameters} The picture page parameters.
 */
function getPicturePageParametersFromPageData(): PicturesPageParameters | null {
    const picturesPageParametersDiv = document.querySelector<HTMLDivElement>('#pictures-page-parameters');
    let picturesPageParametersResult: PicturesPageParameters | null = new PicturesPageParameters();
    if (picturesPageParametersDiv !== null) {
        const pageParametersString: string | undefined = picturesPageParametersDiv.dataset.picturesPageParameters;
        if (!pageParametersString) {
            return null;
        }

        picturesPageParametersResult = JSON.parse(pageParametersString);
    }
    return picturesPageParametersResult;
}