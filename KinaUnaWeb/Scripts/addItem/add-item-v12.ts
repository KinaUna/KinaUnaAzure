import { initializeAddEditEvent } from "../calendar/add-edit-event-v12.js";
import { initializeAddEditContact } from "../contacts/add-edit-contact-v12.js";
import { initializeAddEditFriend } from "../friends/add-edit-friend-v12.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v12.js";
import { initializeAddEditLocation } from "../locations/add-edit-location-v12.js";
import { initializeAddEditMeasurement } from "../measurements/add-edit-measurement-v12.js";
import { startFullPageSpinner, startLoadingItemsSpinner, stopFullPageSpinner, stopLoadingItemsSpinner } from "../navigation-tools-v12.js";
import { initializeAddEditNote } from "../notes/add-edit-note-v12.js";
import { initializeAddEditPicture } from "../pictures/add-edit-picture-v12.js";
import { popupPictureDetails } from "../pictures/picture-details-v12.js";
import { InitializeAddEditProgeny } from "../progeny/add-edit-progeny-v12.js";
import { initializeAddEditSkill } from "../skills/add-edit-skill-v12.js";
import { initializeAddEditSleep } from "../sleep/add-edit-sleep-v12.js";
import { initializeAddEditVaccination } from "../vaccinations/add-edit-vaccination-v12.js";
import { initializeAddEditVideo } from "../videos/add-edit-video-v12.js";
import { popupVideoDetails } from "../videos/video-details-v12.js";
import { initializeAddEditVocabulary } from "../vocabulary/add-edit-vocabulary-v12.js";
import { initializeAddEditTodo } from "../todos/add-edit-todo-v12.js";
import { TimelineChangedEvent } from "../data-tools-v12.js";
import { KanbanBoard, Picture, TimelineItem, TimeLineType, TodoItem } from "../page-models-v12.js";
import { popupTodoItem } from "../todos/todo-details-v12.js";
import { initializeAddEditKanbanBoard } from "../kanbans/add-edit-kanban-board-v12.js";
import { dispatchKanbanBoardChangedEvent, popupKanbanBoard } from "../kanbans/kanban-board-details-v12.js";
import { editKanbanItemFunction, removeKanbanItemFunction } from "../kanbans/kanban-items-v12.js";
import { refreshSubtasks } from "../todos/subtasks-v12.js";
import { setPermissions } from "../item-permissions-v12.js";

/**
 * Adds event listeners to all elements with the data-add-item-type attribute.
 */
export function setAddItemButtonEventListeners(): void {
    let addItemButtons = document.querySelectorAll<HTMLAnchorElement>('.add-item-button');
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
                history.pushState(null, document.title, window.location.href);
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
            await initializeAddEditNote('0');
        }

        if (addItemType === 'calendar') {
            await initializeAddEditEvent('0');
        }

        if (addItemType === 'sleep') {
            await initializeAddEditSleep('0');
        }

        if (addItemType === 'picture') {
            await initializeAddEditPicture('0');
        }

        if (addItemType === 'video') {
            await initializeAddEditVideo('0');
        }

        if (addItemType === 'vocabulary') {
            await initializeAddEditVocabulary('0');
        }

        if (addItemType === 'friend') {
            await initializeAddEditFriend('0');
        }

        if (addItemType === 'measurement') {
            await initializeAddEditMeasurement('0');
        }

        if (addItemType === 'contact') {
            await initializeAddEditContact('0');
        }

        if (addItemType === 'skill') {
            await initializeAddEditSkill('0');
        }

        if (addItemType === 'vaccination') {
            await initializeAddEditVaccination('0');
        }

        if (addItemType === 'location') {
            await initializeAddEditLocation('0');
        }

        if (addItemType === 'todo') {
            await initializeAddEditTodo('0');
        }

        if (addItemType === 'kanbanboard') {
            await initializeAddEditKanbanBoard('0');
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
        button.removeEventListener('click', onEditItemButtonClicked);
        button.addEventListener('click', onEditItemButtonClicked);
    });

    let copyItemButtons = document.querySelectorAll<HTMLButtonElement>('.copy-item-button');
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
export async function onEditItemButtonClicked(event: MouseEvent): Promise<void> {
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

/**
 * Handles the click event for the copy item button.
 * It retrieves the item type and item id from the button's data attributes,
 * then opens the copy item modal for the specified type and item id.
 * @param event The mouse event that triggered the click.
 */
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

    // Kanban item editing is handled differently.
    if (editItemType === 'kanbanitem') {
        await editKanbanItemFunction(editItemItemId);
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
                history.pushState(null, document.title, window.location.href);
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
            await initializeAddEditNote(editItemItemId);
        }

        if (editItemType === 'calendar') {
            await initializeAddEditEvent(editItemItemId);
        }

        if (editItemType === 'sleep') {
            await initializeAddEditSleep(editItemItemId);
        }

        if (editItemType === 'vocabulary') {
            await initializeAddEditVocabulary(editItemItemId);
        }

        if (editItemType === 'friend') {
            await initializeAddEditFriend(editItemItemId);
        }

        if (editItemType === 'measurement') {
            await initializeAddEditMeasurement(editItemItemId);
        }

        if (editItemType === 'contact') {
            await initializeAddEditContact(editItemItemId);
        }

        if (editItemType === 'skill') {
            await initializeAddEditSkill(editItemItemId);
        }

        if (editItemType === 'vaccination') {
            await initializeAddEditVaccination(editItemItemId);
        }

        if (editItemType === 'location') {
            await initializeAddEditLocation(editItemItemId);
        }

        if (editItemType === 'todo') {
            await initializeAddEditTodo(editItemItemId);
        }

        if (editItemType === 'video') {
            await initializeAddEditVideo(editItemItemId);
        }

        if (editItemType === 'picture') {
            await initializeAddEditPicture(editItemItemId);
        }

        if (editItemType === 'subtask') {
            await initializeAddEditTodo(editItemItemId);
        }

        if (editItemType === 'kanbanboard') {
            await initializeAddEditKanbanBoard(editItemItemId);
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
                history.pushState(null, document.title, window.location.href);
            }
        }).catch(function (error) {
            console.error('Error getting copy item popup content:', error);
        });

        // hide main-modal
        $('#main-modal').modal('hide');

        // show item-details-div
        popup.classList.remove('d-none');
                
        if (copyItemType === 'note') {
            await initializeAddEditNote('0');
        }

        if (copyItemType === 'calendar') {
            await initializeAddEditEvent('0');
        }

        if (copyItemType === 'sleep') {
            await initializeAddEditSleep('0');
        }

        if (copyItemType === 'vocabulary') {
            await initializeAddEditVocabulary('0');
        }

        if (copyItemType === 'friend') {
            await initializeAddEditFriend('0');
        }

        if (copyItemType === 'measurement') {
            await initializeAddEditMeasurement('0');
        }

        if (copyItemType === 'contact') {
            await initializeAddEditContact('0');
        }

        if (copyItemType === 'skill') {
            await initializeAddEditSkill('0');
        }

        if (copyItemType === 'vaccination') {
            await initializeAddEditVaccination('0');
        }

        if (copyItemType === 'location') {
            await initializeAddEditLocation('0');
        }

        if (copyItemType === 'picture') {
            await initializeAddEditPicture('0');
        }

        if (copyItemType === 'video') {
            await initializeAddEditVideo('0');
        }

        if (copyItemType === 'todo') {
            await initializeAddEditTodo('0');
        }

        if (copyItemType === 'kanbanboard') {
            await initializeAddEditKanbanBoard('0');
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
    let deleteItemButtons = document.querySelectorAll<HTMLAnchorElement>('.item-details-delete-button');
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
export async function onDeleteItemButtonClicked(event: MouseEvent): Promise<void> {
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
    if (deleteItemType === 'kanbanitem') {
        await removeKanbanItemFunction(deleteItemItemId);
        return new Promise<void>(function (resolve, reject) {
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
                history.pushState(null, document.title, window.location.href);
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
            button.removeEventListener('click', onCloseButtonClicked);
            button.addEventListener('click', onCloseButtonClicked);
        });
    }
}

/**
 * Handles the click event for the close button in the item details popup.
 * It hides the popup and shows the body scrollbars.
 */
async function onCloseButtonClicked(event: MouseEvent): Promise<void> {
    let closeButton = event.currentTarget as HTMLElement;
    await popupPreviousItem(closeButton);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Adds an event listener to the close button in the item details popup.
 * When clicked, the popup is hidden and the body scrollbars are shown.
 */
function addCancelButtonEventListener(): void {
    let cancelButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-cancel-button');
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
async function onCancelButtonClicked(event: MouseEvent): Promise<void> {
    let cancelButton = event.currentTarget as HTMLElement;
    
    await popupPreviousItem(cancelButton);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function popupPreviousItem(buttonClicked: HTMLElement): Promise<void> {
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

        if (previousItemType === 'picture') {
            await popupPictureDetails(previousItemId);
        }

        if (previousItemType === 'video') {
            await popupVideoDetails(previousItemId);
        }
    }
    else {
        const itemDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
        if (itemDetailsPopupDiv) {
            itemDetailsPopupDiv.innerHTML = '';
            itemDetailsPopupDiv.classList.add('d-none');
            showBodyScrollbars();
            history.back();
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}


/**
 * Adds an event listener to the save item form.
 * When submitted, the form is sent to the server and the response is displayed in the item details popup.
 */
function setSaveItemFormEventListener(): void {
    let addItemForm = document.querySelector<HTMLFormElement>('#save-item-form');
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

    // If there is an item permissions editor div, set the permissions for the item before saving.
    const permissionsEditorDiv = document.querySelector<HTMLDivElement>('#item-permissions-editor-div');
    if (permissionsEditorDiv) {
         setPermissions();
    }

    let formData = new FormData(addItemForm);
    let formAction = addItemForm.getAttribute('action');
    let returnItemId: string;
    if (formAction !== null && formAction.includes('/Subtasks/')) {
        const parentTodoItemIdInput = document.querySelector<HTMLInputElement>('#subtask-parent-todo-item-id-input');
        if (parentTodoItemIdInput) {
            returnItemId = parentTodoItemIdInput.value;
        }
    }
    
    if (formAction) {
        await fetch(formAction, {
            method: 'POST',
            body: formData
        }).then(async function (response) {
            if (response.ok) {
                dispatchTimelineItemChangedEvent();
                if (itemDetailsPopupDiv) {
                    itemDetailsPopupDiv.innerHTML = '';
                }
                if (formAction.includes('/Calendar/')){
                    const calendarDataChangedEvent = new Event('calendarDataChanged');
                    window.dispatchEvent(calendarDataChangedEvent);
                }

                if (formAction.includes('/Subtasks/')) {
                    dispatchTimelineItemChangedEvent(TimeLineType.TodoItem.toString(), returnItemId);
                    await popupTodoItem(returnItemId);
                    return new Promise<void>(function (resolve, reject) {
                        resolve();
                    });
                }

                if (formAction.includes('/Kanbans/')) {
                    const updatedKanbanBoard: KanbanBoard = await response.json() as KanbanBoard;
                    returnItemId = updatedKanbanBoard.kanbanBoardId.toString();
                    dispatchKanbanBoardChangedEvent(returnItemId);
                    await popupKanbanBoard(returnItemId);
                    return new Promise<void>(function (resolve, reject) {
                        resolve();
                    });
                }

                if (formAction.includes('/DeleteTodo')) {
                    // Todo: reload the todos list.
                    let todoItem = await response.json() as TodoItem;
                    dispatchTimelineItemChangedEvent(TimeLineType.TodoItem.toString(), todoItem.todoItemId.toString());
                    return new Promise<void>(function (resolve, reject) {
                        resolve();
                    });
                }

                if (formAction.includes('/DeletePicture')) {
                    // Todo: reload the pictures list.
                    let pictureItem = await response.json() as Picture;
                    dispatchTimelineItemChangedEvent(TimeLineType.Photo.toString(), pictureItem.pictureId.toString());
                    return new Promise<void>(function (resolve, reject) {
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
                }
                return new Promise<void>(function (resolve, reject) {
                    resolve();
                });
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

/**
 * Dispatches a TimelineItemChangedEvent if the timeline update div has the necessary attributes.
 * This is used to notify other parts of the application that a timeline item has changed.
 */
function dispatchTimelineItemChangedEvent(itemType: string = '', itemId: string = ''): void {
    let changedItemType: string | null = '';
    let changedItemItemId: string | null = '';

    if (itemType !== '') {
        changedItemType = itemType;
        changedItemItemId = itemId;
        const timelineItem = new TimelineItem();
        timelineItem.itemType = parseInt(changedItemType);
        timelineItem.itemId = changedItemItemId;
        const timelineItemChangedEvent = new TimelineChangedEvent(timelineItem);
        window.dispatchEvent(timelineItemChangedEvent);
    }
    else {
        const timelineUpdateDataDivs = document.querySelectorAll<HTMLDivElement>('.timeline-update-data-div');
        if (timelineUpdateDataDivs === null) {
            // If the timeline update div is not found, do not dispatch the event.
            return;
        }

        // Iterate through the NodeList to dispatch the event for each element found.
        if (timelineUpdateDataDivs.length === 0) {
            // If the timeline update div is not found, do not dispatch the event.
            return;
        }
        timelineUpdateDataDivs.forEach(timelineUpdateDataDiv => {
            changedItemType = timelineUpdateDataDiv.getAttribute('data-changed-item-type');
            changedItemItemId = timelineUpdateDataDiv.getAttribute('data-changed-item-item-id');

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
        });        
    }
}
