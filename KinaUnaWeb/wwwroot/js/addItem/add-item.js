import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v8.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v8.js";
/**
 * Adds event listeners to all elements with the data-add-item-type attribute.
 */
export function setAddItemButtonEventListeners() {
    let addItemButtons = document.querySelectorAll('.add-item-button');
    addItemButtons.forEach(function (button) {
        button.addEventListener('click', async function (event) {
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
        });
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
        $(".selectpicker").selectpicker('refresh');
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
    let addItemButtons = document.querySelectorAll('.edit-item-button');
    addItemButtons.forEach(function (button) {
        button.addEventListener('click', async function (event) {
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
        });
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
        $(".selectpicker").selectpicker('refresh');
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
 * Adds an event listener to the close button in the item details popup.
 * When clicked, the popup is hidden and the body scrollbars are shown.
 */
function addCancelButtonEventListener() {
    let closeButtonsList = document.querySelectorAll('.item-details-cancel-button');
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
 * Adds an event listener to the save item form.
 * When submitted, the form is sent to the server and the response is displayed in the item details popup.
 */
function setSaveItemFormEventListener() {
    let addItemForm = document.querySelector('#save-item-form');
    if (addItemForm) {
        addItemForm.addEventListener('submit', async function (event) {
            event.preventDefault();
            startFullPageSpinner();
            let itemDetailsPopupDiv = document.querySelector('#item-details-div');
            if (itemDetailsPopupDiv) {
                itemDetailsPopupDiv.classList.add('d-none');
                itemDetailsPopupDiv.innerHTML = '';
            }
            let formData = new FormData(addItemForm);
            let formAction = addItemForm.getAttribute('action');
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
        });
    }
}
//# sourceMappingURL=add-item.js.map