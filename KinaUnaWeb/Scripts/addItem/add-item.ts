import { initializeAddEditEvent } from "../calendar/add-edit-event.js";
import { initializeAddEditContact } from "../contacts/add-edit-contact.js";
import { initializeAddEditFriend } from "../friends/add-edit-friend.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v8.js";
import { initializeAddEditLocation } from "../locations/add-edit-location.js";
import { initializeAddEditMeasurement } from "../measurements/add-edit-measurement.js";
import { startFullPageSpinner, startLoadingItemsSpinner, stopFullPageSpinner, stopLoadingItemsSpinner } from "../navigation-tools-v8.js";
import { initializeAddEditNote } from "../notes/add-edit-note.js";
import { initializeAddEditPicture } from "../pictures/add-edit-picture.js";
import { popupPictureDetails } from "../pictures/picture-details.js";
import { InitializeAddEditProgeny } from "../progeny/add-edit-progeny.js";
import { initializeAddEditSkill } from "../skills/add-edit-skill.js";
import { initializeAddEditSleep } from "../sleep/add-edit-sleep.js";
import { initializeAddEditVaccination } from "../vaccinations/add-edit-vaccination.js";
import { initializeAddEditVideo } from "../videos/add-edit-video.js";
import { popupVideoDetails } from "../videos/video-details.js";
import { initializeAddEditVocabulary } from "../vocabulary/add-edit-vocabulary.js";
import { initializeAddEditTodo } from "../todos/add-edit-todo.js";

/**
 * Adds event listeners to all elements with the data-add-item-type attribute.
 */
export function setAddItemButtonEventListeners(): void {
    let addItemButtons = document.querySelectorAll<HTMLAnchorElement>('.add-item-button');
    addItemButtons.forEach(function (button) {
        button.addEventListener('click', onAddItemButtonClicked);
    });
}

async function onAddItemButtonClicked(event: MouseEvent): Promise<void> {
    event.preventDefault();
    startFullPageSpinner();
    let addItemButton = event.currentTarget as HTMLAnchorElement;
    let addItemType = addItemButton.getAttribute('data-add-item-type');
    let addItemProgenyId = addItemButton.getAttribute('data-add-item-progeny-id');
    if (addItemType !== null) {
        if (addItemProgenyId === null) {
            addItemProgenyId = '0';
        }
        await popupAddItemModal(addItemType as string, addItemProgenyId);
    }
    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Shows the add item modal for the specified item type and progeny.
 * @param addItemType
 * @param addItemProgenyId
 */
async function popupAddItemModal(addItemType: string, addItemProgenyId: string): Promise<void> {
    let popup = document.getElementById('item-details-div');
    if (popup !== null) {
        popup.innerHTML = '';
        
        await fetch('/AddItem/GetAddItemModalContent?itemType=' + addItemType + '&progenyId=' + addItemProgenyId, {
            method: 'GET',
            headers: {
                'Accept': 'text/html',
            }
        }).then(async function (response) {
            if (response.ok) {
                let modalContent = await response.text();
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.id = 'full-screen-overlay-div';
                fullScreenOverlay.innerHTML = modalContent;
                popup.appendChild(fullScreenOverlay);
            }
        }).catch(function (error) {
            console.error('Error getting add item popup content:', error);
        });

        // hide main-modal
        $('#main-modal').modal('hide');

        // show item-details-div
        popup.classList.remove('d-none');
        if (addItemType === 'user') {
            ($(".selectpicker") as any).selectpicker('refresh');
        }

        if (addItemType === 'progeny') {
            await InitializeAddEditProgeny();
        }

        if (addItemType === 'note') {
            await initializeAddEditNote();
        }

        if (addItemType === 'calendar') {
            await initializeAddEditEvent();
        }

        if (addItemType === 'sleep') {
            await initializeAddEditSleep();
        }

        if (addItemType === 'picture') {
            await initializeAddEditPicture();
        }

        if (addItemType === 'video') {
            await initializeAddEditVideo();
        }

        if (addItemType === 'vocabulary') {
            await initializeAddEditVocabulary();
        }

        if (addItemType === 'friend') {
            await initializeAddEditFriend();
        }

        if (addItemType === 'measurement') {
            await initializeAddEditMeasurement();
        }

        if (addItemType === 'contact') {
            await initializeAddEditContact();
        }

        if (addItemType === 'skill') {
            await initializeAddEditSkill();
        }

        if (addItemType === 'vaccination') {
            await initializeAddEditVaccination();
        }

        if (addItemType === 'location') {
            await initializeAddEditLocation();
        }

        if (addItemType === 'todo') {
            await initializeAddEditTodo();
        }
       
        hideBodyScrollbars();
        addCloseButtonEventListener();
        addCancelButtonEventListener();
        setSaveItemFormEventListener();
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
* Adds event listeners to all elements with the data-add-item-type attribute.
*/
export function setEditItemButtonEventListeners(): void {
    let editItemButtons = document.querySelectorAll<HTMLAnchorElement>('.edit-item-button');
    editItemButtons.forEach(function (button) {
        button.addEventListener('click', onEditItemButtonClicked);
    });

    let copyItemButtons = document.querySelectorAll<HTMLButtonElement>('.copy-item-button');
    copyItemButtons.forEach(function (button) {
        button.addEventListener('click', onCopyItemButtonClicked);
    });
}

async function onEditItemButtonClicked(event: MouseEvent): Promise<void> {
    event.preventDefault();
    startFullPageSpinner();
    let editItemButton = event.currentTarget as HTMLAnchorElement;
    let editItemType = editItemButton.getAttribute('data-edit-item-type');
    let editItemItemId = editItemButton.getAttribute('data-edit-item-item-id');

    if (editItemType !== null && editItemItemId !== null && editItemItemId !== '0') {
        await popupEditItemModal(editItemType, editItemItemId);
    }

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function onCopyItemButtonClicked(event: MouseEvent): Promise<void> {
    event.preventDefault();
    startFullPageSpinner();
    let copyItemButton = event.currentTarget as HTMLButtonElement;
    let copyItemType = copyItemButton.getAttribute('data-copy-item-type');
    let copyItemItemId = copyItemButton.getAttribute('data-copy-item-item-id');

    if (copyItemType !== null && copyItemItemId !== null && copyItemItemId !== '0') {
        await popupCopyItemModal(copyItemType, copyItemItemId);
    }

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Shows the edit item modal for the specified item type and item id.
 * @param editItemType
 * @param editItemItemId
 */
async function popupEditItemModal(editItemType: string, editItemItemId: string): Promise<void> {

    // Picture and video items are handled differently.
    if (editItemType === 'picture') {
        await popupPictureDetails(editItemItemId);
        return new Promise<void>(function (resolve, reject) {
            resolve();
        });
    }
    // Picture and video items are handled differently.
    if (editItemType === 'video') {
        await popupVideoDetails(editItemItemId);
        return new Promise<void>(function (resolve, reject) {
            resolve();
        });
    }

    let popup = document.getElementById('item-details-div');
    if (popup !== null) {
        popup.innerHTML = '';
        await fetch('/AddItem/GetEditItemModalContent?itemType=' + editItemType + '&itemId=' + editItemItemId, {
            method: 'GET',
            headers: {
                'Accept': 'text/html',
            }
        }).then(async function (response) {
            if (response.ok) {
                let modalContent = await response.text();
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.id = 'full-screen-overlay-div';
                fullScreenOverlay.innerHTML = modalContent;
                popup.appendChild(fullScreenOverlay);
            }
        }).catch(function (error) {
            console.error('Error getting edit item popup content:', error);
        });

        // hide main-modal
        $('#main-modal').modal('hide');

        // show item-details-div
        popup.classList.remove('d-none');

        if (editItemType === 'user') {
            ($(".selectpicker") as any).selectpicker('refresh');
        }

        if (editItemType === 'progeny') {
            await InitializeAddEditProgeny();
        }

        if (editItemType === 'note') {
            await initializeAddEditNote();
        }

        if (editItemType === 'calendar') {
            await initializeAddEditEvent();
        }

        if (editItemType === 'sleep') {
            await initializeAddEditSleep();
        }

        if (editItemType === 'vocabulary') {
            await initializeAddEditVocabulary();
        }

        if (editItemType === 'friend') {
            await initializeAddEditFriend();
        }

        if (editItemType === 'measurement') {
            await initializeAddEditMeasurement();
        }

        if (editItemType === 'contact') {
            await initializeAddEditContact();
        }

        if (editItemType === 'skill') {
            await initializeAddEditSkill();
        }

        if (editItemType === 'vaccination') {
            await initializeAddEditVaccination();
        }

        if (editItemType === 'location') {
            await initializeAddEditLocation();
        }

        if (editItemType === 'todo') {
            await initializeAddEditTodo();
        }
        
        hideBodyScrollbars();
        addCloseButtonEventListener();
        addCancelButtonEventListener();
        setSaveItemFormEventListener();
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Shows the copy item modal for the specified item type and item id.
 * @param copyItemType
 * @param copyItemItemId
 */
async function popupCopyItemModal(copyItemType: string, copyItemItemId: string): Promise<void> {
    let popup = document.getElementById('item-details-div');
    if (popup !== null) {
        popup.innerHTML = '';
        await fetch('/CopyItem/GetCopyItemModalContent?itemType=' + copyItemType + '&itemId=' + copyItemItemId, {
            method: 'GET',
            headers: {
                'Accept': 'text/html',
            }
        }).then(async function (response) {
            if (response.ok) {
                let modalContent = await response.text();
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.id = 'full-screen-overlay-div';
                fullScreenOverlay.innerHTML = modalContent;
                popup.appendChild(fullScreenOverlay);
            }
        }).catch(function (error) {
            console.error('Error getting copy item popup content:', error);
        });

        // hide main-modal
        $('#main-modal').modal('hide');

        // show item-details-div
        popup.classList.remove('d-none');
                
        if (copyItemType === 'note') {
            await initializeAddEditNote();
        }

        if (copyItemType === 'calendar') {
            await initializeAddEditEvent();
        }

        if (copyItemType === 'sleep') {
            await initializeAddEditSleep();
        }

        if (copyItemType === 'vocabulary') {
            await initializeAddEditVocabulary();
        }

        if (copyItemType === 'friend') {
            await initializeAddEditFriend();
        }

        if (copyItemType === 'measurement') {
            await initializeAddEditMeasurement();
        }

        if (copyItemType === 'contact') {
            await initializeAddEditContact();
        }

        if (copyItemType === 'skill') {
            await initializeAddEditSkill();
        }

        if (copyItemType === 'vaccination') {
            await initializeAddEditVaccination();
        }

        if (copyItemType === 'location') {
            await initializeAddEditLocation();
        }

        if (copyItemType === 'picture') {
            await initializeAddEditPicture();
        }

        if (copyItemType === 'video') {
            await initializeAddEditVideo();
        }

        if (copyItemType === 'todo') {
            await initializeAddEditTodo();
        }

        hideBodyScrollbars();
        addCloseButtonEventListener();
        addCancelButtonEventListener();
        setSaveItemFormEventListener();
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
* Adds event listeners to all elements with the data-add-item-type attribute.
*/
export function setDeleteItemButtonEventListeners(): void {
    let deleteItemButtons = document.querySelectorAll<HTMLAnchorElement>('.delete-item-button');
    deleteItemButtons.forEach(function (button) {
        button.addEventListener('click', onDeleteItemButtonClicked);
    });
}

async function onDeleteItemButtonClicked(event: MouseEvent): Promise<void> {
    event.preventDefault();
    startFullPageSpinner();
    let deleteItemButton = event.currentTarget as HTMLAnchorElement;
    let deleteItemType = deleteItemButton.getAttribute('data-delete-item-type');
    let deleteItemItemId = deleteItemButton.getAttribute('data-delete-item-item-id');

    if (deleteItemType !== null && deleteItemItemId !== null && deleteItemItemId !== '0') {
        await popupDeleteItemModal(deleteItemType, deleteItemItemId);
    }

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}
/**
 * Shows the edit item modal for the specified item type and progeny.
 * @param editItemType
 * @param editItemItemId
 */
async function popupDeleteItemModal(deleteItemType: string, deleteItemItemId: string): Promise<void> {
    let popup = document.getElementById('item-details-div');
    if (popup !== null) {
        popup.innerHTML = '';
        await fetch('/AddItem/GetDeleteItemModalContent?itemType=' + deleteItemType + '&itemId=' + deleteItemItemId, {
            method: 'GET',
            headers: {
                'Accept': 'text/html',
            }
        }).then(async function (response) {
            if (response.ok) {
                let modalContent = await response.text();
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = modalContent;
                popup.appendChild(fullScreenOverlay);
            }
        }).catch(function (error) {
            console.error('Error getting delete item popup content:', error);
        });

        // hide main-modal
        $('#main-modal').modal('hide');

        // show item-details-div
        popup.classList.remove('d-none');

        if (deleteItemType === 'user') {
            ($(".selectpicker") as any).selectpicker('refresh');
        }

        hideBodyScrollbars();
        addCloseButtonEventListener();
        addCancelButtonEventListener();
        setSaveItemFormEventListener();
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Adds an event listener to the close button in the item details popup.
 * When clicked, the popup is hidden and the body scrollbars are shown.
 */
function addCloseButtonEventListener(): void {
    let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            button.addEventListener('click', onCloseButtonClicked);
        });
    }
}

function onCloseButtonClicked() {
    const itemDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
    if (itemDetailsPopupDiv) {
        itemDetailsPopupDiv.innerHTML = '';
        itemDetailsPopupDiv.classList.add('d-none');
        showBodyScrollbars();
    }
}

/**
 * Adds an event listener to the close button in the item details popup.
 * When clicked, the popup is hidden and the body scrollbars are shown.
 */
function addCancelButtonEventListener(): void {
    let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-cancel-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            button.addEventListener('click', onCancelButtonClicked);
        });
    }
}

function onCancelButtonClicked() {
    const itemDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
    if (itemDetailsPopupDiv) {
        itemDetailsPopupDiv.innerHTML = '';
        itemDetailsPopupDiv.classList.add('d-none');
        showBodyScrollbars();
    }
}

/**
 * Adds an event listener to the save item form.
 * When submitted, the form is sent to the server and the response is displayed in the item details popup.
 */
function setSaveItemFormEventListener(): void {
    let addItemForm = document.querySelector<HTMLFormElement>('#save-item-form');
    if (addItemForm) {
        addItemForm.addEventListener('submit', onSaveItemFormSubmit);
    }
}

async function onSaveItemFormSubmit(event: SubmitEvent): Promise<void> {
    event.preventDefault();
    startFullPageSpinner();
    let itemDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
    if (itemDetailsPopupDiv) {
        itemDetailsPopupDiv.classList.add('d-none');
    }
    let addItemForm = document.querySelector<HTMLFormElement>('#save-item-form');
    
    if (!addItemForm) {
        return new Promise<void>(function (resolve, reject) {
            resolve();
        });
    }

    let formData = new FormData(addItemForm);
    let formAction = addItemForm.getAttribute('action');

    if (itemDetailsPopupDiv) {
        itemDetailsPopupDiv.innerHTML = '';
    }

    if (formAction) {
        await fetch(formAction, {
            method: 'POST',
            body: formData
        }).then(async function (response) {
            if (response.ok) {
                if (formAction.includes('/Calendar/')){
                    const calendarDataChangedEvent = new Event('calendarDataChanged');
                    window.dispatchEvent(calendarDataChangedEvent);
                }
                if (itemDetailsPopupDiv) {
                    let modalContent = await response.text();
                    const fullScreenOverlay = document.createElement('div');
                    fullScreenOverlay.classList.add('full-screen-bg');
                    fullScreenOverlay.innerHTML = modalContent;
                    itemDetailsPopupDiv.appendChild(fullScreenOverlay);
                    itemDetailsPopupDiv.classList.remove('d-none');
                    hideBodyScrollbars();
                    addCloseButtonEventListener();
                    setEditItemButtonEventListeners();
                    setAddItemButtonEventListeners();
                }
            }
        }).catch(function (error) {
            console.error('Error saving item:', error);
        });
    }
    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}
