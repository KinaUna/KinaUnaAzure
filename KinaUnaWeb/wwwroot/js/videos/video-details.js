import * as LocaleHelper from '../localization-v8.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner, startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { addCopyLocationButtonEventListener, setupHereMaps } from '../locations/location-tools.js';
import { VideosPageParameters } from '../page-models-v8.js';
import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
let videoDetailsTouchStartX = 0;
let videoDetailsTouchStartY = 0;
let videoDetailsTouchEndX = 0;
let videoDetailsTouchEndY = 0;
/**
 * Adds click event listeners to all elements with data-video-id with the videoId value on the page.
 * When clicked, the video details popup is displayed.
 * @param {string} videoId The ID of the video to display.
 */
export function addVideoItemEventListeners(videoId) {
    const videoElementsWithDataId = document.querySelectorAll('[data-video-id="' + videoId + '"]');
    if (videoElementsWithDataId) {
        videoElementsWithDataId.forEach((element) => {
            element.addEventListener('click', onVideoItemDivClicked);
        });
    }
}
async function onVideoItemDivClicked(event) {
    const videoElement = event.currentTarget;
    if (videoElement !== null) {
        const videoId = videoElement.dataset.videoId;
        if (videoId) {
            await displayVideoDetails(videoId);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function popupVideoDetails(videoId) {
    await displayVideoDetails(videoId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Overrides the comment submit form to send the comment data to the server and then refresh the video details popup.
 * Enables/disables the submit button when the comment textarea changes.
 */
async function addCommentEventListeners() {
    const submitForm = document.getElementById('new-video-comment-form');
    if (submitForm !== null) {
        submitForm.addEventListener('submit', onSubmitComment);
    }
    const newCommentTextArea = document.getElementById('new-video-comment-text-area');
    if (newCommentTextArea !== null) {
        newCommentTextArea.addEventListener('input', onCommentInput);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function onSubmitComment(event) {
    event.preventDefault();
    await submitComment();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function onCommentInput() {
    const newCommentTextArea = document.getElementById('new-video-comment-text-area');
    const submitCommentButton = document.getElementById('submit-new-video-comment-button');
    if (submitCommentButton) {
        if (newCommentTextArea.value.length > 0) {
            submitCommentButton.disabled = false;
            return;
        }
        submitCommentButton.disabled = true;
    }
}
/**
 * Gets the form data from the comment form and sends it to the server to add a new comment to the video.
 */
async function submitComment() {
    startLoadingItemsSpinner('item-details-content-wrapper', 0.25, 128, 128, 128);
    const submitForm = document.getElementById('new-video-comment-form');
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
            const currentVideoIdDiv = document.querySelector('#current-video-id-div');
            if (currentVideoIdDiv) {
                const currentVideoId = currentVideoIdDiv.getAttribute('data-current-video-id');
                if (currentVideoId) {
                    displayVideoDetails(currentVideoId, true);
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
        await setTagsAutoSuggestList([getCurrentProgenyId()]);
        await setLocationAutoSuggestList([getCurrentProgenyId()]);
        setupDateTimePicker();
        addCopyLocationButtonEventListener();
        const submitForm = document.getElementById('edit-video-form');
        if (submitForm !== null) {
            submitForm.addEventListener('submit', onSubmitEditVideoForm);
        }
        $(".selectpicker").selectpicker("refresh");
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function onSubmitEditVideoForm(event) {
    event.preventDefault();
    submitVideoEdit();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Gets the data from the edit video form and sends it to the server to update the video data.
 * Then refreshes the video details popup.
 */
async function submitVideoEdit() {
    startLoadingItemsSpinner('item-details-content-wrapper', 0.25, 128, 128, 128);
    const submitForm = document.getElementById('edit-video-form');
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
            const currentVideoIdDiv = document.querySelector('#current-video-id-div');
            if (currentVideoIdDiv) {
                const currentVideoId = currentVideoIdDiv.getAttribute('data-current-video-id');
                if (currentVideoId) {
                    await displayVideoDetails(currentVideoId, true);
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
 * Configures the date time picker for the video edit form.
 */
async function setupDateTimePicker() {
    setMomentLocale();
    const zebraDateTimeFormat = getZebraDateTimeFormat();
    const zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(getCurrentLanguageId());
    if (document.getElementById('video-date-time-picker') !== null) {
        const dateTimePicker = $('#video-date-time-picker');
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
 * Adds swipe navigation for the video details popup.
 */
function addNavigationEventListeners() {
    let previousLink = document.querySelector('#previous-video-link');
    if (previousLink) {
        previousLink.addEventListener('click', onPreviousLinkClicked);
    }
    let nextLink = document.querySelector('#next-video-link');
    if (nextLink) {
        nextLink.addEventListener('click', onNextLinkClicked);
    }
    // Swipe navigation
    const videoDetailsDiv = document.querySelector('#video-details-div');
    if (videoDetailsDiv) {
        videoDetailsDiv.addEventListener('touchstart', onVideoDetailsTouchStart);
        videoDetailsDiv.addEventListener('touchend', onVideoDetailsTouchEnd);
    }
}
async function onPreviousLinkClicked() {
    let previousLink = document.querySelector('#previous-video-link');
    if (previousLink === null) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    let previousVideoId = previousLink.getAttribute('data-previous-video-id');
    if (previousVideoId) {
        await displayVideoDetails(previousVideoId, true);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function onNextLinkClicked() {
    let nextLink = document.querySelector('#next-video-link');
    if (nextLink === null) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    let nextVideoId = nextLink.getAttribute('data-next-video-id');
    if (nextVideoId) {
        await displayVideoDetails(nextVideoId, true);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function onVideoDetailsTouchStart(event) {
    videoDetailsTouchStartX = event.touches[0].clientX;
    videoDetailsTouchStartY = event.touches[0].clientY;
}
async function onVideoDetailsTouchEnd(event) {
    videoDetailsTouchEndX = event.changedTouches[0].clientX;
    videoDetailsTouchEndY = event.changedTouches[0].clientY;
    if (Math.abs(videoDetailsTouchEndY - videoDetailsTouchStartY) > 100) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    if (Math.abs(videoDetailsTouchEndX - videoDetailsTouchStartX) < 75) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    let nextLink = document.querySelector('#next-video-link');
    if (videoDetailsTouchEndX < videoDetailsTouchStartX) {
        let nextVideoId = nextLink?.getAttribute('data-next-video-id');
        if (nextVideoId) {
            await displayVideoDetails(nextVideoId, true);
        }
    }
    let previousLink = document.querySelector('#previous-video-link');
    if (videoDetailsTouchEndX > videoDetailsTouchStartX) {
        let previousVideoId = previousLink?.getAttribute('data-previous-video-id');
        if (previousVideoId) {
            await displayVideoDetails(previousVideoId, true);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
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
        showMapButton.addEventListener('click', onShowHereMapsButtonClicked);
    }
}
function onShowHereMapsButtonClicked() {
    const mapContainerDiv = document.getElementById('here-map-container-div');
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
}
/**
 * Fetches the HTML for video details and displays it in a popup.
 * Then adds the event listeners for the elements displayed.
 * @param {string} videoId The ID of the video to display.
 * @param {boolean} isPopupVisible If the popup is already visible. If true, the body-content spinner will not be shown.
 */
async function displayVideoDetails(videoId, isPopupVisible = false) {
    if (!isPopupVisible) {
        startFullPageSpinner();
    }
    else {
        startLoadingItemsSpinner('item-details-content-wrapper', 0.25, 128, 128, 128);
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
                setEditItemButtonEventListeners();
            }
        }
        else {
            console.error('Error getting video item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting video item. Error: ' + error);
    });
    if (!isPopupVisible) {
        stopFullPageSpinner();
    }
    else {
        stopLoadingItemsSpinner('item-details-content-wrapper');
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Gets the video page parameters from the page data.
 * Used to determine if a tag filter is set for the video page.
 * @returns {VideosPageParameters} The video page parameters.
 */
function getVideoPageParametersFromPageData() {
    const videosPageParametersDiv = document.querySelector('#videos-page-parameters');
    let videosPageParametersResult = new VideosPageParameters();
    if (videosPageParametersDiv !== null) {
        const pageParametersString = videosPageParametersDiv.dataset.videosPageParameters;
        if (!pageParametersString) {
            return null;
        }
        videosPageParametersResult = JSON.parse(pageParametersString);
    }
    return videosPageParametersResult;
}
//# sourceMappingURL=video-details.js.map