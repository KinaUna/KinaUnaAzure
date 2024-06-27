import * as LocaleHelper from '../localization-v6.js';
import { setTagsAutoSuggestList, setLocationAutoSuggestList, getCurrentProgenyId, getCurrentLanguageId, setMomentLocale, getZebraDateTimeFormat } from '../data-tools-v6.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v6.js';
export function addPictureItemEventListeners(pictureId: string): void {
    const pictureElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-picture-id="' + pictureId + '"]');
    if (pictureElementsWithDataId) {
        pictureElementsWithDataId.forEach((element) => {
            element.addEventListener('click', function () {
                displayPictureDetails(pictureId);
            });
        });
    }
}

export function popupPictureDetails(pictureId: string): void {
    displayPictureDetails(pictureId);
}

async function addCommentEventListeners(): Promise<void> {
    const submitForm = document.getElementById('new-picture-comment-form') as HTMLFormElement;
    if (submitForm !== null) {
        submitForm.addEventListener('submit', async function (event) {
            event.preventDefault();

            submitComment();
        });
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });

}

async function submitComment(): Promise<void> {
    startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);

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
                    displayPictureDetails(currentPictureId, true);
                }
            }
        }
    }

    stopLoadingItemsSpinner('item-details-content');
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function addEditEventListeners(): Promise<void> {
    const toggleEditButton = document.querySelector<HTMLButtonElement>('#toggle-edit-button');
    if (toggleEditButton !== null) {
        $("#toggle-edit-button").on('click', function () {
            $("#edit-section").toggle(500);
        });
    }

    const submitForm = document.getElementById('edit-picture-form') as HTMLFormElement;
    if (submitForm !== null) {
        submitForm.addEventListener('submit', async function (event) {
            event.preventDefault();

            submitPictureEdit();
        });
    }

    await setTagsAutoSuggestList(getCurrentProgenyId());
    await setLocationAutoSuggestList(getCurrentProgenyId());
    setupDateTimePicker();
    addCopyLocationButtonEventListener();

    ($(".selectpicker") as any).selectpicker("refresh");

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function submitPictureEdit(): Promise<void> {
    startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);

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
                    displayPictureDetails(currentPictureId, true);
                }
            }
        }
    }

    stopLoadingItemsSpinner('item-details-content');
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

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

function addCopyLocationButtonEventListener(): void {
    const copyLocationButton = document.querySelector<HTMLButtonElement>('#copy-location-button');
    if (copyLocationButton !== null) {
        copyLocationButton.addEventListener('click', function () {
            const latitudeInput = document.getElementById('latitude') as HTMLInputElement;
            const longitudeInput = document.getElementById('longitude') as HTMLInputElement;
            const locationSelect = document.getElementById('copy-location') as HTMLSelectElement;

            if (latitudeInput !== null && longitudeInput !== null && locationSelect !== null) {
                let locId = parseInt(locationSelect.value);
                // let selectedLocation = copyLocationList.find((obj: { id: number; name: string; lat: number, lng: number }) => { return obj.id === locId });

                //latitudeInput.setAttribute('value', selectedLocation.lat);
                //longitudeInput.setAttribute('value', selectedLocation.lng);
            }
        });
    }
}

async function displayPictureDetails(pictureId: string, isPopupVisible: boolean = false) {
    if (!isPopupVisible) {
        startLoadingItemsSpinner('body-content');
    }

    let url = '/Pictures/Picture?id=' + pictureId + "&partialView=true";
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
                
                let bodyElement = document.querySelector<HTMLBodyElement>('body');
                if (bodyElement) {
                    bodyElement.style.overflow = 'hidden';
                }

                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            itemDetailsPopupDiv.innerHTML = '';
                            itemDetailsPopupDiv.classList.add('d-none');
                            let bodyElement = document.querySelector<HTMLBodyElement>('body');
                            if (bodyElement) {
                                bodyElement.style.removeProperty('overflow');
                            }
                        });
                    });
                }
                let previousLink = document.querySelector<HTMLAnchorElement>('#previous-picture-link');
                if (previousLink) {
                    previousLink.addEventListener('click', function () {
                        let previousPictureId = previousLink.getAttribute('data-previous-picture-id');
                        if (previousPictureId) {
                            startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);
                            displayPictureDetails(previousPictureId, true);
                        }
                    });
                }
                let nextLink = document.querySelector<HTMLAnchorElement>('#next-picture-link');
                if (nextLink) {
                    nextLink.addEventListener('click', function () {
                        let nextPictureId = nextLink.getAttribute('data-next-picture-id');
                        if (nextPictureId) {
                            startLoadingItemsSpinner('item-details-content', 0.5, 1, 1, 1);
                            displayPictureDetails(nextPictureId, true);
                        }
                    });
                }

                addEditEventListeners();
                addCommentEventListeners();
            }
        } else {
            console.error('Error getting picture item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting picture item. Error: ' + error);
    });

    if (!isPopupVisible) {
        stopLoadingItemsSpinner('body-content');
    }

}