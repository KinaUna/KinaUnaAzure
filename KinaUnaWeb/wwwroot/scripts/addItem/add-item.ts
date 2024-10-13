import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v8.js";
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from "../navigation-tools-v8.js";

export function setAddItemButtonEventListeners(): void {
    let addItemButtons = document.querySelectorAll<HTMLAnchorElement>('.add-item-button');
    addItemButtons.forEach(function (button) {
        button.addEventListener('click', async function (event) {
            event.preventDefault();
            let addItemButton = event.currentTarget as HTMLAnchorElement;
            console.log(addItemButton);
            let addItemType = addItemButton.getAttribute('data-add-item-type');
            console.log(addItemType);
            if (addItemType !== null) {
                await popupAddItemModal(addItemType as string);
            }
        });
    });
}

async function popupAddItemModal(addItemType: string): Promise<void> {
    let popup = document.getElementById('item-details-div');
    if (popup !== null) {
        await fetch('/AddItem/GetAddItemModalContent?itemType=' + addItemType, {
            method: 'GET',
            headers: {
                'Accept': 'text/html',
            }
        }).then(async function (response) {
            if (response.ok) {
                let modalContent = await response.text();
                popup.innerHTML = modalContent;
            }
        });

        // hide main-modal
        $('#main-modal').modal('toggle');

        // show item-details-div
        popup.classList.remove('d-none');
        ($(".selectpicker") as any).selectpicker('refresh');
        hideBodyScrollbars();
        addCloseButtonEventListener();
        setSaveAddItemFormEventListener();
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

function setSaveAddItemFormEventListener(): void {
    let addItemForm = document.querySelector<HTMLFormElement>('#add-item-form');
    if (addItemForm) {
        addItemForm.addEventListener('submit', async function (event) {
            event.preventDefault();
            startLoadingItemsSpinner('item-details-div')
            let formData = new FormData(addItemForm);
            let formAction = addItemForm.getAttribute('action');
            if (formAction) {
                await fetch(formAction, {
                    method: 'POST',
                    body: formData
                }).then(async function (response) {
                    if (response.ok) {
                        let itemDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
                        if (itemDetailsPopupDiv) {
                            let modalContent = await response.text();
                            itemDetailsPopupDiv.innerHTML = modalContent;
                            itemDetailsPopupDiv.classList.remove('d-none');
                            hideBodyScrollbars();
                            addCloseButtonEventListener();
                        }
                    }
                });
            }

            stopLoadingItemsSpinner('item-details-div');
        });
    }
}