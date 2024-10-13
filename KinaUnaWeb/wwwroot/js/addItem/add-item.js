import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v8.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v8.js";
export function setAddItemButtonEventListeners() {
    let addItemButtons = document.querySelectorAll('.add-item-button');
    addItemButtons.forEach(function (button) {
        button.addEventListener('click', async function (event) {
            event.preventDefault();
            startFullPageSpinner();
            let addItemButton = event.currentTarget;
            let addItemType = addItemButton.getAttribute('data-add-item-type');
            if (addItemType !== null) {
                await popupAddItemModal(addItemType);
            }
            stopFullPageSpinner();
        });
    });
}
async function popupAddItemModal(addItemType) {
    let popup = document.getElementById('item-details-div');
    if (popup !== null) {
        popup.innerHTML = '';
        await fetch('/AddItem/GetAddItemModalContent?itemType=' + addItemType, {
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
        });
        // hide main-modal
        $('#main-modal').modal('toggle');
        // show item-details-div
        popup.classList.remove('d-none');
        $(".selectpicker").selectpicker('refresh');
        hideBodyScrollbars();
        addCloseButtonEventListener();
        addCancelButtonEventListener();
        setSaveAddItemFormEventListener();
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
function setSaveAddItemFormEventListener() {
    let addItemForm = document.querySelector('#add-item-form');
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
                });
            }
            stopFullPageSpinner();
        });
    }
}
//# sourceMappingURL=add-item.js.map