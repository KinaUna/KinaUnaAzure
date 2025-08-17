import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v9.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v9.js';

/**
 * Adds event listeners to all elements with the data-friend-id attribute.
 * When clicked, the DisplayFriendItem function is called.
 * @param {string} itemId The id of the Friend to add event listeners for.
 */
export function addFriendItemListeners(itemId: string): void {
    const elementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-friend-id="' + itemId + '"]');
    if (elementsWithDataId) {
        elementsWithDataId.forEach((element) => {
            element.addEventListener('click', onFriendItemDivClicked);
        });
    }
}

async function onFriendItemDivClicked(event: MouseEvent): Promise<void> {
    const friendElement: HTMLDivElement = event.currentTarget as HTMLDivElement;
    if (friendElement !== null) {
        const friendId = friendElement.dataset.friendId;
        if (friendId) {
            await displayFriendItem(friendId);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Enable other scripts to call the DisplayFriendItem function.
 * @param {string} friendId The id of the friend item to display.
 */
export async function popupFriendItem(friendId: string): Promise<void> {
    await displayFriendItem(friendId);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Displays a friend item in a popup.
 * @param {string} friendId The id of the friend item to display.
 */
async function displayFriendItem(friendId: string): Promise<void> {
    startFullPageSpinner();
    let url = '/Friends/ViewFriend?friendId=' + friendId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const friendElementHtml = await response.text();
            const friendDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (friendDetailsPopupDiv) {
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = friendElementHtml;
                friendDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                friendDetailsPopupDiv.classList.remove('d-none');
                let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
                if (closeButtonsList) {
                    closeButtonsList.forEach((button) => {
                        button.addEventListener('click', function () {
                            friendDetailsPopupDiv.innerHTML = '';
                            friendDetailsPopupDiv.classList.add('d-none');
                            showBodyScrollbars();
                        });
                    });
                }
                setEditItemButtonEventListeners();
            }
        } else {
            console.error('Error getting friend item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting friend item. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}