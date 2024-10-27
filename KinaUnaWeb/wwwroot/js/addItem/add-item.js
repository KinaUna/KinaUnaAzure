import { initializeAddEditEvent } from "../calendar/add-edit-event.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v8.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v8.js";
import { initializeAddEditNote } from "../notes/add-edit-note.js";
import { initializeAddEditPicture } from "../pictures/add-edit-picture.js";
import { InitializeAddEditProgeny } from "../progeny/add-edit-progeny.js";
import { initializeAddEditSleep } from "../sleep/add-edit-sleep.js";
import { initializeAddEditVideo } from "../videos/add-edit-video.js";
/**
 * Adds event listeners to all elements with the data-add-item-type attribute.
 */
export function setAddItemButtonEventListeners() {
    let addItemButtons = document.querySelectorAll('.add-item-button');
    addItemButtons.forEach(function (button) {
        button.addEventListener('click', onAddItemButtonClicked);
    });
}
async function onAddItemButtonClicked(event) {
    event.preventDefault();
    startFullPageSpinner();
    let addItemButton = event.currentTarget;
    let addItemType = addItemButton.getAttribute('data-add-item-type');
    let addItemProgenyId = addItemButton.getAttribute('data-add-item-progeny-id');
    if (addItemType !== null) {
        if (addItemProgenyId === null) {
            addItemProgenyId = '0';
        }
        await popupAddItemModal(addItemType, addItemProgenyId);
    }
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Shows the add item modal for the specified item type and progeny.
 * @param addItemType
 * @param addItemProgenyId
 */
async function popupAddItemModal(addItemType, addItemProgenyId) {
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
            $(".selectpicker").selectpicker('refresh');
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
        hideBodyScrollbars();
        addCloseButtonEventListener();
        addCancelButtonEventListener();
        setSaveItemFormEventListener();
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
* Adds event listeners to all elements with the data-add-item-type attribute.
*/
export function setEditItemButtonEventListeners() {
    let editItemButtons = document.querySelectorAll('.edit-item-button');
    editItemButtons.forEach(function (button) {
        button.addEventListener('click', onEditItemButtonClicked);
    });
}
async function onEditItemButtonClicked(event) {
    event.preventDefault();
    startFullPageSpinner();
    let editItemButton = event.currentTarget;
    let editItemType = editItemButton.getAttribute('data-edit-item-type');
    let editItemItemId = editItemButton.getAttribute('data-edit-item-item-id');
    if (editItemType !== null && editItemItemId !== null && editItemItemId !== '0') {
        await popupEditItemModal(editItemType, editItemItemId);
    }
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Shows the edit item modal for the specified item type and progeny.
 * @param editItemType
 * @param editItemItemId
 */
async function popupEditItemModal(editItemType, editItemItemId) {
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
            $(".selectpicker").selectpicker('refresh');
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
        hideBodyScrollbars();
        addCloseButtonEventListener();
        addCancelButtonEventListener();
        setSaveItemFormEventListener();
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
* Adds event listeners to all elements with the data-add-item-type attribute.
*/
export function setDeleteItemButtonEventListeners() {
    let deleteItemButtons = document.querySelectorAll('.delete-item-button');
    deleteItemButtons.forEach(function (button) {
        button.addEventListener('click', onDeleteItemButtonClicked);
    });
}
async function onDeleteItemButtonClicked(event) {
    event.preventDefault();
    startFullPageSpinner();
    let deleteItemButton = event.currentTarget;
    let deleteItemType = deleteItemButton.getAttribute('data-delete-item-type');
    let deleteItemItemId = deleteItemButton.getAttribute('data-delete-item-item-id');
    if (deleteItemType !== null && deleteItemItemId !== null && deleteItemItemId !== '0') {
        await popupDeleteItemModal(deleteItemType, deleteItemItemId);
    }
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Shows the edit item modal for the specified item type and progeny.
 * @param editItemType
 * @param editItemItemId
 */
async function popupDeleteItemModal(deleteItemType, deleteItemItemId) {
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
            $(".selectpicker").selectpicker('refresh');
        }
        hideBodyScrollbars();
        addCloseButtonEventListener();
        addCancelButtonEventListener();
        setSaveItemFormEventListener();
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
            button.addEventListener('click', onCloseButtonClicked);
        });
    }
}
function onCloseButtonClicked() {
    const itemDetailsPopupDiv = document.querySelector('#item-details-div');
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
function addCancelButtonEventListener() {
    let closeButtonsList = document.querySelectorAll('.item-details-cancel-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            button.addEventListener('click', onCancelButtonClicked);
        });
    }
}
function onCancelButtonClicked() {
    const itemDetailsPopupDiv = document.querySelector('#item-details-div');
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
function setSaveItemFormEventListener() {
    let addItemForm = document.querySelector('#save-item-form');
    if (addItemForm) {
        addItemForm.addEventListener('submit', onSaveItemFormSubmit);
    }
}
async function onSaveItemFormSubmit(event) {
    event.preventDefault();
    startFullPageSpinner();
    let itemDetailsPopupDiv = document.querySelector('#item-details-div');
    if (itemDetailsPopupDiv) {
        itemDetailsPopupDiv.classList.add('d-none');
    }
    let addItemForm = document.querySelector('#save-item-form');
    console.log(addItemForm);
    if (!addItemForm) {
        return new Promise(function (resolve, reject) {
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=add-item.js.map