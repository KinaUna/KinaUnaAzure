import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v8.js";
import { startFullPageSpinner, startLoadingItemsSpinner, stopFullPageSpinner, stopLoadingItemsSpinner } from "../navigation-tools-v8.js";
import { initializeAddEditNote } from "../notes/add-edit-note.js";
import { InitializeAddEditProgeny } from "../progeny/add-edit-progeny.js";

/**
 * Adds event listeners to all elements with the data-add-item-type attribute.
 */
export function setAddItemButtonEventListeners(): void {
    let addItemButtons = document.querySelectorAll<HTMLAnchorElement>('.add-item-button');
    addItemButtons.forEach(function (button) {
        button.addEventListener('click', async function (event) {
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
        });
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
        button.addEventListener('click', async function (event) {
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
        });
    });
}

/**
 * Shows the edit item modal for the specified item type and progeny.
 * @param editItemType
 * @param editItemItemId
 */
async function popupEditItemModal(editItemType: string, editItemItemId: string): Promise<void> {
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

        if (editItemType === 'user') {
            ($(".selectpicker") as any).selectpicker('refresh');
        }

        if (editItemType === 'progeny') {
            await InitializeAddEditProgeny();
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
        button.addEventListener('click', async function (event) {
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
        });
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
 * Adds an event listener to the close button in the item details popup.
 * When clicked, the popup is hidden and the body scrollbars are shown.
 */
function addCancelButtonEventListener(): void {
    let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-cancel-button');
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
 * Adds an event listener to the save item form.
 * When submitted, the form is sent to the server and the response is displayed in the item details popup.
 */
function setSaveItemFormEventListener(): void {
    let addItemForm = document.querySelector<HTMLFormElement>('#save-item-form');
    if (addItemForm) {
        addItemForm.addEventListener('submit', async function (event) {
            event.preventDefault();
            startFullPageSpinner();
            let itemDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
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

            return new Promise<void>(function (resolve, reject) {
                resolve();
            });
        });
    }
}
