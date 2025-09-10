import { initializeAddEditEvent } from "../calendar/add-edit-event.js";
import { initializeAddEditContact } from "../contacts/add-edit-contact.js";
import { initializeAddEditFriend } from "../friends/add-edit-friend.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v9.js";
import { initializeAddEditLocation } from "../locations/add-edit-location.js";
import { initializeAddEditMeasurement } from "../measurements/add-edit-measurement.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9.js";
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
import { TimelineChangedEvent } from "../data-tools-v9.js";
import { TimelineItem } from "../page-models-v9.js";
import { popupTodoItem } from "../todos/todo-details.js";
import { initializeAddEditKanbanBoard } from "../kanbans/add-edit-kanban-board.js";
import { dispatchKanbanBoardChangedEvent, popupKanbanBoard } from "../kanbans/kanban-board-details.js";
import { editKanbanItemFunction, removeKanbanItemFunction } from "../kanbans/kanban-items.js";
/**
 * Adds event listeners to all elements with the data-add-item-type attribute.
 */
export function setAddItemButtonEventListeners() {
    let addItemButtons = document.querySelectorAll('.add-item-button');
    addItemButtons.forEach(function (button) {
        button.removeEventListener('click', onAddItemButtonClicked);
        button.addEventListener('click', onAddItemButtonClicked);
    });
}
/**
 * Handles the click event for the add item button.
 * It retrieves the item type and progeny id from the button's data attributes,
 * then opens the add item modal for the specified type and progeny.
 * @param event The mouse event that triggered the click.
 */
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
        if (addItemType === 'kanbanboard') {
            await initializeAddEditKanbanBoard();
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
        button.removeEventListener('click', onEditItemButtonClicked);
        button.addEventListener('click', onEditItemButtonClicked);
    });
    let copyItemButtons = document.querySelectorAll('.copy-item-button');
    copyItemButtons.forEach(function (button) {
        button.removeEventListener('click', onCopyItemButtonClicked);
        button.addEventListener('click', onCopyItemButtonClicked);
    });
}
/**
 * Handles the click event for the edit item button.
 * It retrieves the item type and progeny id from the button's data attributes,
 * then opens the edit item modal for the specified type and progeny.
 * @param event The mouse event that triggered the click.
 */
export async function onEditItemButtonClicked(event) {
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
 * Handles the click event for the copy item button.
 * It retrieves the item type and item id from the button's data attributes,
 * then opens the copy item modal for the specified type and item id.
 * @param event The mouse event that triggered the click.
 */
async function onCopyItemButtonClicked(event) {
    event.preventDefault();
    startFullPageSpinner();
    let copyItemButton = event.currentTarget;
    let copyItemType = copyItemButton.getAttribute('data-copy-item-type');
    let copyItemItemId = copyItemButton.getAttribute('data-copy-item-item-id');
    if (copyItemType !== null && copyItemItemId !== null && copyItemItemId !== '0') {
        await popupCopyItemModal(copyItemType, copyItemItemId);
    }
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Shows the edit item modal for the specified item type and item id.
 * @param editItemType
 * @param editItemItemId
 */
async function popupEditItemModal(editItemType, editItemItemId) {
    // Picture and video items are handled differently.
    if (editItemType === 'picture') {
        await popupPictureDetails(editItemItemId);
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    // Picture and video items are handled differently.
    if (editItemType === 'video') {
        await popupVideoDetails(editItemItemId);
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    if (editItemType === 'kanbanitem') {
        await editKanbanItemFunction(editItemItemId);
        return new Promise(function (resolve, reject) {
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
        if (editItemType === 'subtask') {
            await initializeAddEditTodo();
        }
        if (editItemType === 'kanbanboard') {
            await initializeAddEditKanbanBoard();
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
 * Shows the copy item modal for the specified item type and item id.
 * @param copyItemType
 * @param copyItemItemId
 */
async function popupCopyItemModal(copyItemType, copyItemItemId) {
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
        if (copyItemType === 'kanbanboard') {
            await initializeAddEditKanbanBoard();
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
    let deleteItemButtons = document.querySelectorAll('.item-details-delete-button');
    deleteItemButtons.forEach(function (button) {
        button.removeEventListener('click', onDeleteItemButtonClicked);
        button.addEventListener('click', onDeleteItemButtonClicked);
    });
}
/**
 * Handles the click event for the delete item button.
 * It retrieves the item type and item id from the button's data attributes,
 * then opens the delete item modal for the specified type and item id.
 * @param event The mouse event that triggered the click.
 */
export async function onDeleteItemButtonClicked(event) {
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
    if (deleteItemType === 'kanbanitem') {
        await removeKanbanItemFunction(deleteItemItemId);
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
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
            button.removeEventListener('click', onCloseButtonClicked);
            button.addEventListener('click', onCloseButtonClicked);
        });
    }
}
/**
 * Handles the click event for the close button in the item details popup.
 * It hides the popup and shows the body scrollbars.
 */
async function onCloseButtonClicked(event) {
    let closeButton = event.currentTarget;
    await popupPreviousItem(closeButton);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Adds an event listener to the close button in the item details popup.
 * When clicked, the popup is hidden and the body scrollbars are shown.
 */
function addCancelButtonEventListener() {
    let cancelButtonsList = document.querySelectorAll('.item-details-cancel-button');
    if (cancelButtonsList) {
        cancelButtonsList.forEach((button) => {
            button.removeEventListener('click', onCancelButtonClicked);
            button.addEventListener('click', onCancelButtonClicked);
        });
    }
}
/**
 * Handles the click event for the cancel button in the add or edit item popup.
 * It hides the popup and shows the body scrollbars.
 */
async function onCancelButtonClicked(event) {
    let cancelButton = event.currentTarget;
    await popupPreviousItem(cancelButton);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function popupPreviousItem(buttonClicked) {
    // If the button has a 'data-previous-item-type' attribute, popup that item again.
    let previousItemType = buttonClicked.getAttribute('data-previous-item-type');
    let previousItemId = buttonClicked.getAttribute('data-previous-item-id');
    if (previousItemType !== null && previousItemId !== null && previousItemId !== '0') {
        if (previousItemType === 'todo') {
            await popupTodoItem(previousItemId);
        }
        if (previousItemType === 'kanbanboard') {
            await popupKanbanBoard(previousItemId);
        }
    }
    else {
        const itemDetailsPopupDiv = document.querySelector('#item-details-div');
        if (itemDetailsPopupDiv) {
            itemDetailsPopupDiv.innerHTML = '';
            itemDetailsPopupDiv.classList.add('d-none');
            showBodyScrollbars();
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Adds an event listener to the save item form.
 * When submitted, the form is sent to the server and the response is displayed in the item details popup.
 */
function setSaveItemFormEventListener() {
    let addItemForm = document.querySelector('#save-item-form');
    if (addItemForm) {
        addItemForm.removeEventListener('submit', onSaveItemFormSubmit);
        addItemForm.addEventListener('submit', onSaveItemFormSubmit);
    }
}
/**
 * Handles the submission of the save item form.
 * It prevents the default form submission, sends the form data to the server,
 * and displays the response in the item details popup.
 * @param event The submit event triggered by the form submission.
 */
async function onSaveItemFormSubmit(event) {
    event.preventDefault();
    startFullPageSpinner();
    let itemDetailsPopupDiv = document.querySelector('#item-details-div');
    if (itemDetailsPopupDiv) {
        itemDetailsPopupDiv.classList.add('d-none');
    }
    let addItemForm = document.querySelector('#save-item-form');
    if (!addItemForm) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    let formData = new FormData(addItemForm);
    let formAction = addItemForm.getAttribute('action');
    let returnItemId;
    if (formAction !== null && formAction.includes('/Subtasks/')) {
        const parentTodoItemIdInput = document.querySelector('#subtask-parent-todo-item-id-input');
        if (parentTodoItemIdInput) {
            returnItemId = parentTodoItemIdInput.value;
        }
    }
    if (itemDetailsPopupDiv) {
        itemDetailsPopupDiv.innerHTML = '';
    }
    if (formAction) {
        await fetch(formAction, {
            method: 'POST',
            body: formData
        }).then(async function (response) {
            if (response.ok) {
                if (formAction.includes('/Calendar/')) {
                    const calendarDataChangedEvent = new Event('calendarDataChanged');
                    window.dispatchEvent(calendarDataChangedEvent);
                }
                if (formAction.includes('/Subtasks/')) {
                    console.log('returnItemId: ' + returnItemId);
                    await popupTodoItem(returnItemId);
                    return new Promise(function (resolve, reject) {
                        resolve();
                    });
                }
                if (formAction.includes('/Kanbans/')) {
                    const updatedKanbanBoard = await response.json();
                    returnItemId = updatedKanbanBoard.kanbanBoardId.toString();
                    dispatchKanbanBoardChangedEvent(returnItemId);
                    await popupKanbanBoard(returnItemId);
                    return new Promise(function (resolve, reject) {
                        resolve();
                    });
                }
                if (formAction.includes('/DeleteTodo')) {
                    // Todo: reload the todos list.
                    return new Promise(function (resolve, reject) {
                        resolve();
                    });
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
                    dispatchTimelineItemChangedEvent();
                }
                return new Promise(function (resolve, reject) {
                    resolve();
                });
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
/**
 * Dispatches a TimelineItemChangedEvent if the timeline update div has the necessary attributes.
 * This is used to notify other parts of the application that a timeline item has changed.
 */
function dispatchTimelineItemChangedEvent() {
    console.log('dispatchTimelineItemChangedEvent');
    const timelineUpdateDataDiv = document.querySelector('#timeline-update-data-div');
    if (timelineUpdateDataDiv === null) {
        // If the timeline update div is not found, do not dispatch the event.
        return;
    }
    let changedItemType = timelineUpdateDataDiv.getAttribute('data-changed-item-type');
    let changedItemItemId = timelineUpdateDataDiv.getAttribute('data-changed-item-item-id');
    if (changedItemType === null || changedItemItemId === null) {
        // If the item type or item id is null, do not dispatch the event.
        return;
    }
    if (changedItemType === '0' || changedItemItemId === '0') {
        // If the item type or item id is 0, do not dispatch the event.
        return;
    }
    if (isNaN(parseInt(changedItemType)) || isNaN(parseInt(changedItemItemId))) {
        // If the item type or item id is not a number, do not dispatch the event.
        return;
    }
    const timelineItem = new TimelineItem();
    timelineItem.itemType = parseInt(changedItemType);
    timelineItem.itemId = changedItemItemId;
    const timelineItemChangedEvent = new TimelineChangedEvent(timelineItem);
    window.dispatchEvent(timelineItemChangedEvent);
}
//# sourceMappingURL=add-item.js.map