import * as LocaleHelper from '../localization-v6.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v6.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v6.js';
import { hideBodyScrollbars, showBodyScrollbars } from './items-display.js';
import { addCopyLocationButtonEventListener, setupHereMaps } from '../locations/location-tools.js';
import { VideosPageParameters } from '../page-models-v6.js';

let videoDetailsTouchStartX: number = 0;
let videoDetailsTouchStartY: number = 0;
let videoDetailsTouchEndX: number = 0;
let videoDetailsTouchEndY: number = 0;

/**
 * Adds click event listeners to all elements with data-video-id with the videoId value on the page.
 * When clicked, the video details popup is displayed.
 * @param {string} videoId The ID of the video to display.
 */
export function addVideoItemEventListeners(videoId: string): void {
    const videoElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-video-id="' + videoId + '"]');
    if (videoElementsWithDataId) {
        videoElementsWithDataId.forEach((element) => {
            element.addEventListener('click', function () {
                displayVideoDetails(videoId);
            });
        });
    }
}

export function popupVideoDetails(videoId: string): void {
    displayVideoDetails(videoId);
}

/**
 * Overrides the comment submit form to send the comment data to the server and then refresh the video details popup.
 * Enables/disables the submit button when the comment textarea changes.
 */
async function addCommentEventListeners(): Promise<void> {
    const submitForm = document.getElementById('new-video-comment-form') as HTMLFormElement;
    if (submitForm !== null) {
        submitForm.addEventListener('submit', async function (event) {
            event.preventDefault();

            submitComment();
        });
    }

    const newCommentTextArea = document.getElementById('new-video-comment-text-area') as HTMLTextAreaElement;
    if (newCommentTextArea !== null) {
        newCommentTextArea.addEventListener('input', function () {
            const submitCommentButton = document.getElementById('submit-new-video-comment-button') as HTMLButtonElement;
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
 * Gets the form data from the comment form and sends it to the server to add a new comment to the video.
 */
async function submitComment(): Promise<void> {
    startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);

    const submitForm = document.getElementById('new-video-comment-form') as HTMLFormElement;
    if (submitForm !== null) {
        const formData = new FormData(submitForm);

        const response = await fetch('/Videos/AddVideoComment', {
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
            const currentVideoIdDiv = document.querySelector<HTMLDivElement>('#current-video-id-div');
            if (currentVideoIdDiv) {
                const currentVideoId = currentVideoIdDiv.getAttribute('data-current-video-id');
                if (currentVideoId) {
                    displayVideoDetails(currentVideoId, true);
                }
            }
        }
    }

    stopLoadingItemsSpinner('item-details-content');
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

        await setTagsAutoSuggestList(getCurrentProgenyId());
        await setLocationAutoSuggestList(getCurrentProgenyId());
        setupDateTimePicker();
        addCopyLocationButtonEventListener();
        const submitForm = document.getElementById('edit-video-form') as HTMLFormElement;
        if (submitForm !== null) {
            submitForm.addEventListener('submit', async function (event) {
                event.preventDefault();

                submitVideoEdit();
            });
        }

        ($(".selectpicker") as any).selectpicker("refresh");
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Gets the data from the edit video form and sends it to the server to update the video data.
 * Then refreshes the video details popup.
 */
async function submitVideoEdit(): Promise<void> {
    startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);

    const submitForm = document.getElementById('edit-video-form') as HTMLFormElement;
    if (submitForm !== null) {
        const formData = new FormData(submitForm);

        const response = await fetch('/Videos/EditVideo', {
            method: 'POST',
            body: formData,
            headers: {
                'Accept': 'application/json',
                'enctype': 'multipart/form-data'
            }
        }).catch(function (error) {
            console.log('Error editing Video. Error: ' + error);
        });

        if (response) {
            const currentVideoIdDiv = document.querySelector<HTMLDivElement>('#current-video-id-div');
            if (currentVideoIdDiv) {
                const currentVideoId = currentVideoIdDiv.getAttribute('data-current-video-id');
                if (currentVideoId) {
                    displayVideoDetails(currentVideoId, true);
                }
            }
        }
    }

    stopLoadingItemsSpinner('item-details-content');
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Configures the date time picker for the video edit form.
 */
async function setupDateTimePicker(): Promise<void> {
    setMomentLocale();
    const zebraDateTimeFormat = getZebraDateTimeFormat();
    const zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(getCurrentLanguageId());

    if (document.getElementById('video-date-time-picker') !== null) {
        const dateTimePicker: any = $('#video-date-time-picker');
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
 * Adds swipe navigation for the video details popup.
 */
function addNavigationEventListeners(): void {
    let previousLink = document.querySelector<HTMLAnchorElement>('#previous-video-link');
    if (previousLink) {
        previousLink.addEventListener('click', function () {
            let previousVideoId = previousLink.getAttribute('data-previous-video-id');
            if (previousVideoId) {
                startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);
                displayVideoDetails(previousVideoId, true);
            }
        });
    }
    let nextLink = document.querySelector<HTMLAnchorElement>('#next-video-link');
    if (nextLink) {
        nextLink.addEventListener('click', function () {
            let nextVideoId = nextLink.getAttribute('data-next-video-id');
            if (nextVideoId) {
                startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);
                displayVideoDetails(nextVideoId, true);
            }
        });
    }

    // Swipe navigation
    const videoDetailsDiv = document.querySelector<HTMLDivElement>('#video-details-div');
    if (videoDetailsDiv) {
        videoDetailsDiv.addEventListener('touchstart', event => {
            videoDetailsTouchStartX = event.touches[0].clientX;
            videoDetailsTouchStartY = event.touches[0].clientY;
        });
        videoDetailsDiv.addEventListener('touchend', event => {
            videoDetailsTouchEndX = event.changedTouches[0].clientX;
            videoDetailsTouchEndY = event.changedTouches[0].clientY;
            if (Math.abs(videoDetailsTouchEndY - videoDetailsTouchStartY) > 100) {
                return;
            }

            if (Math.abs(videoDetailsTouchEndX - videoDetailsTouchStartX) < 50) {
                return;
            }

            if (videoDetailsTouchEndX < videoDetailsTouchStartX) {
                let nextVideoId = nextLink?.getAttribute('data-next-video-id');
                if (nextVideoId) {
                    startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);
                    displayVideoDetails(nextVideoId, true);
                }
            }
            if (videoDetailsTouchEndX > videoDetailsTouchStartX) {
                let previousVideoId = previousLink?.getAttribute('data-previous-video-id');
                if (previousVideoId) {
                    startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);
                    displayVideoDetails(previousVideoId, true);
                }
            }
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
 * Fetches the HTML for video details and displays it in a popup.
 * Then adds the event listeners for the elements displayed.
 * @param {string} videoId The ID of the video to display.
 * @param {boolean} isPopupVisible If the popup is already visible. If true, the body-content spinner will not be shown.
 */
async function displayVideoDetails(videoId: string, isPopupVisible: boolean = false) {
    if (!isPopupVisible) {
        startLoadingItemsSpinner('body-content');
    }

    let tagFilter = '';
    const videoPageParameters = getVideoPageParametersFromPageData();
    if (videoPageParameters) {
        if (videoPageParameters.tagFilter) {
            tagFilter = videoPageParameters.tagFilter;
        }
    }

    let url = '/Videos/Video?id=' + videoId + "&tagFilter=" + tagFilter + "&partialView=true";
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
            console.error('Error getting video item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting video item. Error: ' + error);
    });

    if (!isPopupVisible) {
        stopLoadingItemsSpinner('body-content');
    }

}

/**
 * Gets the video page parameters from the page data.
 * Used to determine if a tag filter is set for the video page.
 * @returns {VideosPageParameters} The video page parameters.
 */
function getVideoPageParametersFromPageData(): VideosPageParameters | null {
    const videosPageParametersDiv = document.querySelector<HTMLDivElement>('#videos-page-parameters');
    let videosPageParametersResult: VideosPageParameters | null = new VideosPageParameters();
    if (videosPageParametersDiv !== null) {
        const pageParametersString: string | undefined = videosPageParametersDiv.dataset.videosPageParameters;
        if (!pageParametersString) {
            return null;
        }

        videosPageParametersResult = JSON.parse(pageParametersString);
    }
    return videosPageParametersResult;
}