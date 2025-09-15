import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item.js";
import { getCurrentLanguageId, TimelineChangedEvent } from "../data-tools-v9.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v9.js";
import { getTranslation } from "../localization-v9.js";
import { startFullPageSpinner, startLoadingItemsSpinner, stopFullPageSpinner, stopLoadingItemsSpinner } from "../navigation-tools-v9.js";
import { KanbanBoard, KanbanBoardColumn, KanbanItem, TimelineItem, TodoStatusType } from "../page-models-v9.js";
import { getStatusIconForTodoItems } from "../todos/todo-details.js";
import { initializeAddEditKanbanItem } from "./add-edit-kanban-item.js";
import { addCardDividerEventListeners, addCardEventListeners, createKanbanItemCardHTML, loadKanbanItemsTranslations } from "./kanban-cards.js";
import { displayKanbanItemDetails, getAddKanbanItemForm, getEditKanbanItemForm, getKanbanItemsForBoard, getRemoveKanbanItemForm, updateKanbanItem } from "./kanban-items.js";

let kanbanBoardMainDiv = document.querySelector<HTMLDivElement>('#kanban-board-main-div');
let kanbanBoard: KanbanBoard;
let kanbanItems: KanbanItem[] = [];
const defaultColumnTitle = 'To do';
let userCanEdit: boolean = false;
let renameString = '';
let setLimitString = '';
let changeStatusString = '';
let moveLeftString = '';
let moveRightString = '';
let setLimitHeaderString = '';
let noStatusString = '';
let statusNotStartedString = '';
let statusInProgressString = '';
let statusDoneString = '';
let statusCancelledString = '';

export function getKanbanBoard(): KanbanBoard {
    return kanbanBoard;
}

export function setKanbanBoard(board: KanbanBoard): void {
    kanbanBoard = board;
}
export function getKanbanItems(): KanbanItem[] {
    return kanbanItems;
}

export function setKanbanItems(kanbanItemsList: KanbanItem[]): void {
    kanbanItems = kanbanItemsList;
}

function addTimelineChangedEventListener(): void {
    // Subscribe to the timelineChanged event to refresh the KanbanBoards list when a KanbanBoard is added, updated, or deleted.
    window.addEventListener('timelineChanged', async (event: TimelineChangedEvent) => {
        let changedItem = event.TimelineItem;
        if (changedItem !== null && changedItem.itemType === 16) { // 16 is the item type for KanbanBoards.
            if (changedItem.itemId === kanbanBoard.kanbanBoardId.toString()) {
                await renderKanbanBoard(true);
            }
        }
    });
}

/**
 * Trigger a timelineChanged event for the specified KanbanBoard id.
 * @param kanbanBoardId
 */
export function dispatchKanbanBoardChangedEvent(kanbanBoardId: string): void {
    const timelineItem = new TimelineItem();
    timelineItem.itemType = 16;
    timelineItem.itemId = kanbanBoardId;
    const timelineItemChangedEvent = new TimelineChangedEvent(timelineItem);
    window.dispatchEvent(timelineItemChangedEvent);
}

/**
 * Adds event listeners to all elements with the data-kanban-board-id attribute.
 * When clicked, the DisplayKanbanBoard function is called.
 * @param {string} itemId The id of the KanbanBoard item to add event listeners for.
 */
export function addKanbanBoardListeners(itemId: string): void {
    const kanbanBoardElementsWithDataId = document.querySelectorAll<HTMLDivElement>('[data-kanban-board-id="' + itemId + '"]');
    if (kanbanBoardElementsWithDataId) {
        kanbanBoardElementsWithDataId.forEach((element) => {
            element.removeEventListener('click', onKanbanBoardDivClicked);
            element.addEventListener('click', onKanbanBoardDivClicked);
        });
    }
}

async function onKanbanBoardDivClicked(event: MouseEvent): Promise<void> {
    const kanbanBoardElement: HTMLDivElement = event.currentTarget as HTMLDivElement;
    if (kanbanBoardElement !== null) {
        const kanbanBoardId = kanbanBoardElement.dataset.kanbanBoardId;
        if (kanbanBoardId) {
            await displayKanbanBoard(kanbanBoardId);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
* Enable other scripts to call the DisplayKanbanBoard function.
* @param {string} kanbanBoardId The id of the KanbanBoard to display.
*/
export async function popupKanbanBoard(kanbanBoardId: string): Promise<void> {
    await displayKanbanBoard(kanbanBoardId);

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Displays a KanbanBoard in a popup.
 * @param {string} todoId The id of the KanbanBoard to display.
 */
async function displayKanbanBoard(kanbanBoardId: string): Promise<void> {
    startLoadingItemsSpinner('kanban-board-main-div');
    await loadKanbanItemsTranslations();
    addTimelineChangedEventListener();
    let url = '/Kanbans/ViewKanbanBoard?kanbanBoardId=' + kanbanBoardId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const kanbanBoardElementHtml = await response.text();
            const kanbanBoardDetailsPopupDiv = document.querySelector<HTMLDivElement>('#item-details-div');
            if (kanbanBoardDetailsPopupDiv) {
                kanbanBoardDetailsPopupDiv.innerHTML = '';
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = kanbanBoardElementHtml;
                kanbanBoardDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                kanbanBoardDetailsPopupDiv.classList.remove('d-none');
                setKanbanBoardDetailsEventListeners(kanbanBoardId, kanbanBoardDetailsPopupDiv);
                setEditItemButtonEventListeners();
                setDeleteItemButtonEventListeners();
                await renderKanbanBoard(true);

            }
        } else {
            console.error('Error getting Kanban Board. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting Kanban Board. Error: ' + error);
    });

    stopLoadingItemsSpinner('kanban-board-main-div');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets up event listeners for the Kanban Board details popup.
 * @param {string} itemId The id of the Kanban Board item to set event listeners for.
 * @param {HTMLDivElement} kanbanBoardDetailsPopupDiv The div element for the Kanban Board details popup.
 */
function setKanbanBoardDetailsEventListeners(itemId: string, kanbanBoardDetailsPopupDiv: HTMLDivElement):void {
    let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function () {
                kanbanBoardDetailsPopupDiv.innerHTML = '';
                kanbanBoardDetailsPopupDiv.classList.add('d-none');
                showBodyScrollbars();
            }
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }
}

/**
 * Gets the KanbanBoard with the specified id from the server.
 * Stores the result in the kanbanBoard variable and updates the columnsList property.
 * @param kanbanBoardId The id of the KanbanBoard to get.
 * @returns The KanbanBoard with the specified id.
 */
async function loadKanbanBoard(kanbanBoardId: number): Promise<KanbanBoard> {
    let url = '/Kanbans/GetKanbanBoard?kanbanBoardId=' + kanbanBoardId;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            kanbanBoard = await response.json();
            if (!kanbanBoard.columns) {
                kanbanBoard.columns = '[]';
            }
            kanbanBoard.columnsList = JSON.parse(kanbanBoard.columns) as KanbanBoardColumn[];
        } else {
            console.error('Error getting Kanban Board. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting Kanban Board. Error: ' + error);
    });

    return new Promise<KanbanBoard>(function (resolve, reject) {
        resolve(kanbanBoard);
    });
}

/**
 * Renders the KanbanBoard in the kanban-board-main-div element.
 * @param reloadKanbanItems Whether to reload the KanbanItems from the server or use the existing kanbanItems array.
 * @returns A promise that resolves when the KanbanBoard has been rendered.
 */
async function renderKanbanBoard(reloadKanbanItems: boolean): Promise<void> {
    kanbanBoardMainDiv = document.querySelector<HTMLDivElement>('#kanban-board-main-div');
    
    if (kanbanBoardMainDiv) {
        const kanbanBoardId = kanbanBoardMainDiv.dataset.viewKanbanBoardId;
        const userCanEditData = kanbanBoardMainDiv.dataset.userCanEdit;
        if (userCanEditData && userCanEditData === 'True') {
            userCanEdit = true;
        }
        if (kanbanBoardId) {
            const kanbanBoard: KanbanBoard = await loadKanbanBoard(parseInt(kanbanBoardId));
            if (kanbanBoard) {
                if (kanbanItems.length === 0 || reloadKanbanItems) {
                    kanbanItems = await getKanbanItemsForBoard(kanbanBoard.kanbanBoardId);
                    const columnIds = kanbanBoard.columnsList.map(c => c.id);
                    columnIds.forEach((columnId) => {
                        ensureColumnRowIndexesAreSequential(columnId);
                    });
                }

                kanbanBoardMainDiv.innerHTML = await createKanbanBoardContainer(kanbanBoard);

                // If the KanbanBoard has no columns, add a default column.
                if (kanbanBoard.columnsList !== null && kanbanBoard.columnsList.length === 0) {

                    let defaultKanbanBoardColumn: KanbanBoardColumn = new KanbanBoardColumn();
                    defaultKanbanBoardColumn.id = 1;
                    defaultKanbanBoardColumn.title = await getTranslation(defaultColumnTitle, 'Todos', getCurrentLanguageId());
                    defaultKanbanBoardColumn.columnIndex = 0;
                    defaultKanbanBoardColumn.wipLimit = 0;
                    kanbanBoard.columnsList.push(defaultKanbanBoardColumn);
                    kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                    // Save the updated KanbanBoard to the server.
                    await updateKanbanBoardColumns(kanbanBoard);
                }

                // Render kanban items in the appropriate columns.
                for (const column of kanbanBoard.columnsList) {
                    renderKanbanItemsInColumn(column.id);
                }
                                
                addColumnEventListeners();
                addCardButtonsEventListners();

                // Hide all column menus when clicking outside of them.
                document.removeEventListener('click', hideAllMenusAndModals);
                document.addEventListener('click', hideAllMenusAndModals);
            }
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function addCardButtonsEventListners(): void {
    // Set up event listeners for adding cards, editing cards, deleting cards, dragging and dropping cards, etc.
    const addCardButtons = document.querySelectorAll<HTMLButtonElement>('.add-card-button');
    addCardButtons.forEach((button) => {
        const addCardButtonFunction = async function () {
            const columnId = button.dataset.columnId;
            if (columnId) {
                const addCardModalDiv = document.querySelector<HTMLDivElement>('#kanban-item-details-div');
                if (addCardModalDiv) {
                    addCardModalDiv.innerHTML = '';
                    let rowIndex = 0;
                    const columnBodyDiv = document.querySelector<HTMLDivElement>('#kanban-column-body-' + columnId);
                    if (columnBodyDiv) {
                        const kanbanItemsInColumn = columnBodyDiv.querySelectorAll<HTMLDivElement>('.kanban-card');
                        rowIndex = kanbanItemsInColumn.length;
                    }
                    const formHtml = await getAddKanbanItemForm(kanbanBoard.kanbanBoardId, parseInt(columnId), rowIndex);
                    addCardModalDiv.innerHTML = formHtml;
                    addCardModalDiv.classList.remove('d-none');
                    hideBodyScrollbars();
                    const cancelButton = addCardModalDiv.querySelector<HTMLButtonElement>('.add-kanban-item-cancel-button');
                    if (cancelButton) {
                        const closeButtonFunction = function () {
                            addCardModalDiv.innerHTML = '';
                            addCardModalDiv.classList.add('d-none');
                        }
                        cancelButton.removeEventListener('click', closeButtonFunction);
                        cancelButton.addEventListener('click', closeButtonFunction);

                        const closeButton = addCardModalDiv.querySelector<HTMLButtonElement>('.modal-close-button');
                        if (closeButton) {
                            closeButton.removeEventListener('click', closeButtonFunction);
                            closeButton.addEventListener('click', closeButtonFunction);
                        }
                    }

                    const addKanbanItemForm = addCardModalDiv.querySelector<HTMLFormElement>('#save-kanban-card-form');
                    if (addKanbanItemForm) {
                        const addKanbanItemFormFunction = async function (event: Event) {
                            event.preventDefault();
                            startFullPageSpinner();
                            const formData = new FormData(addKanbanItemForm);
                            const url = '/KanbanItems/AddKanbanItem';
                            await fetch(url, {
                                method: 'POST',
                                body: formData
                            }).then(async function (response) {
                                if (response.ok) {
                                    // Successfully added the KanbanItem. Re-render the KanbanBoard.
                                    addCardModalDiv.innerHTML = '';
                                    addCardModalDiv.classList.add('d-none');
                                    const newKanbanItem = await response.json() as KanbanItem;
                                    if (newKanbanItem) {
                                        kanbanItems.push(newKanbanItem);
                                        await updateKanbanItemsInColumn(newKanbanItem.columnId);
                                    }
                                } else {
                                    console.error('Error adding kanban item. Status: ' + response.status);
                                }
                            }).catch(function (error) {
                                console.error('Error adding kanban item: ' + error);
                            });
                            stopFullPageSpinner();
                        }
                        addKanbanItemForm.removeEventListener('submit', addKanbanItemFormFunction);
                        addKanbanItemForm.addEventListener('submit', addKanbanItemFormFunction);
                        initializeAddEditKanbanItem('kanban-item-details-div');
                    }
                }
            }
            return new Promise<void>(function (resolve, reject) {
                resolve();
            });
        }

        button.removeEventListener('click', addCardButtonFunction);
        button.addEventListener('click', addCardButtonFunction);
    });
}

function renderKanbanItemsInColumn(columnId: number): void {
    
    const columnBodyDiv = document.querySelector<HTMLDivElement>('#kanban-column-body-' + columnId);
    if (!columnBodyDiv) {
        return;
    }

    // Clear existing items
    columnBodyDiv.innerHTML = '';

    // Get the list of KanbanItems in the column
    const kanbanItemsInColumn = kanbanItems.filter(k => k.columnId === columnId);

    // sort the kanban items by their rowIndex
    kanbanItemsInColumn.sort((a, b) => a.rowIndex - b.rowIndex);
    let rowIndex = 0;
    addCardDividerElement(columnId, rowIndex);
    for (const item of kanbanItemsInColumn) {
        rowIndex++;
        const columnBodyDiv = document.querySelector<HTMLDivElement>('#kanban-column-body-' + item.columnId);
        if (columnBodyDiv && item.todoItem) {
            // Add the kanban item card.
            const cardDiv = createKanbanItemCardHTML(item);
            columnBodyDiv.appendChild(cardDiv);

            addCardDividerElement(columnId, rowIndex);
        }
    }    

    kanbanItemsInColumn.forEach((item) => {
        addCardEventListeners(item.kanbanItemId, userCanEdit);
    });
        
    updateCardCountersInColumn(columnId);    
}

function addCardDividerElement(columnId: number, rowIndex: number): void {
    const columnBodyDiv = document.querySelector<HTMLDivElement>('#kanban-column-body-' + columnId);
    if (!columnBodyDiv) {
        return;
    }

    const cardDivider = document.createElement('div');
    cardDivider.classList.add('kanban-card-divider');
    cardDivider.dataset.rowIndex = rowIndex.toString();
    cardDivider.dataset.columnId = columnId.toString();
    columnBodyDiv.appendChild(cardDivider);
    addCardDividerEventListeners(columnId, rowIndex);
}

function updateCardCountersInColumn(columnId: number): void {
    const columnDiv = document.querySelector<HTMLDivElement>('.kanban-column[data-column-id="' + columnId + '"]');
    if (columnDiv) {
        const column = kanbanBoard.columnsList.find(c => c.id === columnId);
        if (column) {
            const numberOfKanbanItems = (kanbanItems.filter(k => k.columnId === column.id)).length;
            let limitString = '[ ' + numberOfKanbanItems + '/' + column.wipLimit + ' ]';
            if (column.wipLimit === 0) {
                limitString = '[ ' + numberOfKanbanItems + '/&#8734; ]';
            }
            const wipLimitSpan = columnDiv.querySelector<HTMLSpanElement>('span[data-wip-limit-id="' + column.id + '"]');
            if (wipLimitSpan) {
                wipLimitSpan.innerHTML = limitString;
            }
        }
    }
}


export async function updateKanbanItemsInColumn(columnId: number): Promise<void> {
    // Get the column HTMLDiv element.
    const columnDiv = document.querySelector<HTMLDivElement>('.kanban-column[data-column-id="' + columnId + '"]');
    if (columnDiv) {
        ensureColumnRowIndexesAreSequential(columnId);

        // Get the list of KanbanItems in the column
        const kanbanItemsInColumn = kanbanItems.filter(k => k.columnId === columnId);
        
        for (const kanbanItem of kanbanItemsInColumn) {
            await updateKanbanItem(kanbanItem); 
        }

        renderKanbanItemsInColumn(columnId);
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    }); 
}

function ensureColumnRowIndexesAreSequential(columnId: number): void {
    // Sort the KanbanItems by their rowIndex
    kanbanItems.sort((a, b) => a.rowIndex - b.rowIndex);
    let indexInColumn = 0;
    kanbanItems.forEach((item) => {
        if (item.columnId === columnId) {
            item.rowIndex = indexInColumn;
            indexInColumn++;
        }
    });
}

async function setColumnTranslations(): Promise<void> {
    if (renameString === '') {
        renameString = await getTranslation('Rename', 'Todos', getCurrentLanguageId());
    }
    if (setLimitString === '') {
        setLimitString = await getTranslation('Set limit', 'Todos', getCurrentLanguageId());
    }
    if (changeStatusString === '') {
        changeStatusString = await getTranslation('Set status', 'Todos', getCurrentLanguageId());
    }
    if (moveLeftString === '') {
        moveLeftString = await getTranslation('Move left', 'Todos', getCurrentLanguageId());
    }
    if (moveRightString === '') {
        moveRightString = await getTranslation('Move right', 'Todos', getCurrentLanguageId());
    }
    if (setLimitHeaderString === '') {
        setLimitHeaderString = await getTranslation('Set WIP limit for column (0 = no limit):', 'Todos', getCurrentLanguageId());
    }
    if (noStatusString === '') {
        noStatusString = await getTranslation('No status', 'Todos', getCurrentLanguageId());
    }
    if (statusNotStartedString === '') {
        statusNotStartedString = `<i class="material-icons-small">${getStatusIconForTodoItems(TodoStatusType.NotStarted)}</i>` + await getTranslation('Not started', 'Todos', getCurrentLanguageId());
    }
    if (statusInProgressString === '') {
        statusInProgressString = `<i class="material-icons-small">${getStatusIconForTodoItems(TodoStatusType.InProgress)}</i>` + await getTranslation('In progress', 'Todos', getCurrentLanguageId());
    }
    if (statusDoneString === '') {
        statusDoneString = `<i class="material-icons-small">${getStatusIconForTodoItems(TodoStatusType.Completed)}</i>` + await getTranslation('Done', 'Todos', getCurrentLanguageId());
    }
    if (statusCancelledString === '') {
        statusCancelledString = `<i class="material-icons-small">${getStatusIconForTodoItems(TodoStatusType.Cancelled)}</i>` + await getTranslation('Cancelled', 'Todos', getCurrentLanguageId());
    }    
}

async function createKanbanBoardContainer(kanbanBoard: KanbanBoard): Promise<string> {
    let kanbanBoardHtml = '<div class="kanban-board-container"><div class="kanban-column-divider" data-column-divider-id="0"></div>';
    let dividerIndex = 1;

    await setColumnTranslations();

    kanbanBoard.columnsList.forEach((column) => {
        let numberOfKanbanItems = (kanbanItems.filter(k => k.columnId === column.id)).length;
        let limitString = '[ ' + numberOfKanbanItems + '/' + column.wipLimit + ' ]';
        if (column.wipLimit === 0) {
            limitString = '[ ' + numberOfKanbanItems + '/&#8734; ]';
        }
        let statusIconHtml = '';
        if (column.setStatus > -1) {
            statusIconHtml = `<i class="material-icons-small">${ getStatusIconForTodoItems(column.setStatus)}</i>`;
        }

        kanbanBoardHtml += `
                        <div class="kanban-column" data-column-id="${column.id}">
                            <div class="kanban-column-header">
                                <div class="kanban-column-menu-div d-none float-right" data-column-id="${column.id}">
                                    <button class="kanban-column-menu-button" data-column-id="${column.id}">...</button>
                                    <div class="kanban-column-menu-content d-none" data-column-id="${column.id}">
                                        <button class="kanban-column-menu-item-button" data-column-menu-action="rename" data-column-id="${column.id}" ><span class="material-icons kanban-menu-material-icons">edit</span> ${renameString}</button>
                                        <div class="item-menu-divider"></div>
                                        <button class="kanban-column-menu-item-button" data-column-menu-action="setlimit" data-column-id="${column.id}" ><span class="material-icons kanban-menu-material-icons">compress</span> ${setLimitString}</button>
                                        <button class="kanban-column-menu-item-button" data-column-menu-action="setstatus" data-column-id="${column.id}" ><span class="material-icons kanban-menu-material-icons">label</span> ${changeStatusString}</button>
                                        <div class="item-menu-divider"></div>
                                        <button class="kanban-column-menu-item-button" data-column-menu-action="moveleft" data-column-id="${column.id}" ><span class="material-icons kanban-menu-material-icons">arrow_back</span> ${moveLeftString}</button>
                                        <button class="kanban-column-menu-item-button" data-column-menu-action="moveright" data-column-id="${column.id}" ><span class="material-icons kanban-menu-material-icons">arrow_forward</span>${moveRightString}</button>
                                    </div>
                                </div>
                                <div class="kanban-column-title" data-column-id="${column.id}">${statusIconHtml}<span class="mr-2" data-title-span-id="${column.id}">${column.title}</span><span class="kanban-card-wip-limit text-muted" data-wip-limit-id="${column.id}">${limitString}<span></div>
                                <div class="input-group kanban-column-rename-input-group d-none" id="rename-column-input-group-${column.id}" style="width: auto;" draggable="true" ondragstart="event.preventDefault(); event.stopPropagation();">
                                    <input type="text" class="form-control" id="rename-column-input-${column.id}" value="${column.title}" >
                                    <div class="input-group-append">
                                        <button class="btn btn-sm btn-success mt-0 mb-0" type="button" id="rename-column-save-button-${column.id}"><i class="material-icons">save</i></button>
                                    </div>
                                </div>
                            </div>
                            <div class="kanban-column-body" id="kanban-column-body-${column.id}">
                                <!-- Cards will be dynamically added here -->
                            </div>
                            <div class="kanban-column-footer">`;
        if (userCanEdit) {
            kanbanBoardHtml += `
                                <button class="btn btn-outline-info btn-sm add-card-button ml-auto mr-auto" data-column-id="${column.id}">Add Card</button>
                            </div>
                        </div>`;

        }
        else {
            kanbanBoardHtml += `
                </div>
            </div>`;
        }
        kanbanBoardHtml += `<div class="kanban-column-divider" data-column-divider-id="${dividerIndex}"></div>`;
        
        const limitInputHTML = `
        <div class="settings-modal d-none" tabindex="-1" role="dialog" id="set-wip-limit-modal-${column.id}">
            <div class="form-group modal-settings-panel">
                <div class="h5">${column.title}</div>
                <label for="wip-limit-input-${column.id}">${setLimitHeaderString}</label>
                <div class="input-group">                
                    <input type="number" class="form-control" name="wip-limit-input-${column.id}" id="wip-limit-input-${column.id}" value="${column.wipLimit}" min="0">
                    <div class="input-group-append">
                        <button class="btn btn-sm btn-success mt-0 mb-0" type="button" id="wip-limit-save-button-${column.id}"><i class="material-icons">save</i></button>
                    </div>
                </div>
            </div>
        </div>`;
        kanbanBoardHtml += limitInputHTML;

        const setStatusInputHTML = `
        <div class="settings-modal d-none" tabindex="-1" role="dialog" id="set-column-status-modal-${column.id}">
            <div class="form-group modal-settings-panel">
                <div class="h5">${column.title + ': ' + changeStatusString}</div>
                <div class="kanban-column-set-status-input-group" id="set-column-status-input-group-${column.id}" style="width: auto;" draggable="true" ondragstart="event.preventDefault(); event.stopPropagation();">
                <button class="btn btn-success ${column.setStatus === -1 ? 'disabled' : ''}" type="button" id="set-column-status-none-button-${column.id}">${noStatusString}</button>
                <button class="btn btn-success ${column.setStatus === 0 ? 'disabled' : ''}" type="button" id="set-column-status-not-started-button-${column.id}">${statusNotStartedString}</button>
                <button class="btn btn-success ${column.setStatus === 1 ? 'disabled' : ''}" type="button" id="set-column-status-in-progress-button-${column.id}">${statusInProgressString}</button>
                <button class="btn btn-success ${column.setStatus === 2 ? 'disabled' : ''}" type="button" id="set-column-status-done-button-${column.id}">${statusDoneString}</button>
                <button class="btn btn-success ${column.setStatus === 3 ? 'disabled' : ''}" type="button" id="set-column-status-cancelled-button-${column.id}">${statusCancelledString}</button>
                </div>
            </div>
        </div>`;
        kanbanBoardHtml += setStatusInputHTML;

        const addCardInputHTML = `
        <div class="settings-modal d-none" tabindex="-1" role="dialog" id="add-card-modal-${column.id}"></div>`;
        kanbanBoardHtml += addCardInputHTML;
        
        dividerIndex++;
    });

    let addColumnButtonHtml = `<div class="pr-3">
                                   <button id="add-kanban-column-button" class="btn btn-sm btn-success mr-auto"><i class="material-icons kinauna-icon-small">add</i></button>
                               </div>`;
    if (userCanEdit) {
        kanbanBoardHtml += addColumnButtonHtml;
    }
    kanbanBoardHtml += '</div>';
    
    return kanbanBoardHtml;
}

function addColumnDividerEventListeners(): void {
    const columnDividers = document.querySelectorAll<HTMLDivElement>('.kanban-column-divider');
    columnDividers.forEach((divider) => {
        // If a column is dragged over a divider, show a visual indicator
        divider.removeEventListener('dragover', onColumnDragOverDivider);
        divider.addEventListener('dragover', onColumnDragOverDivider);

        // If a column is dragged into a divider, show a visual indicator
        divider.removeEventListener('dragenter', onColumnDragEnterDivider);
        divider.addEventListener('dragenter', onColumnDragEnterDivider);

        // If a column is dropped on a divider, reorder the columns accordingly.
        divider.removeEventListener('dragleave', onColumnDragLeaveDivider);
        divider.addEventListener('dragleave', onColumnDragLeaveDivider);

        // Handle the drop event.
        divider.removeEventListener('drop', onColumnDropped);
        divider.addEventListener('drop', onColumnDropped);
    });
}

function onColumnDragOverDivider(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    // Handle the dragover event when a column is dragged over a divider.
    // Show a visual indicator that the column can be dropped here.
    const columnDivider = event.currentTarget as HTMLDivElement;
    // Check if element being dragged is a kanban-column.
    if (event.dataTransfer === null || !event.dataTransfer.types.includes('kanban-column')) {
        if (event.dataTransfer === null) {
            return;
        }
        event.dataTransfer.dropEffect = 'none';
        return;
    }
    
    columnDivider.classList.add('kanban-column-divider-drag-over');
}

function onColumnDragEnterDivider(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    // Handle the dragover event when a column is dragged over a divider.
    // Show a visual indicator that the column can be dropped here.
    const columnDivider = event.currentTarget as HTMLDivElement;
    // Check if element being dragged is a kanban-column.
    if (event.dataTransfer === null || !event.dataTransfer.types.includes('kanban-column')) {
        if (event.dataTransfer === null) {
            return;
        }
        event.dataTransfer.dropEffect = 'none';
        return;
    }

    columnDivider.classList.add('kanban-column-divider-drag-over');
}

function onColumnDragLeaveDivider(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    // Handle the dragleave event when a column is dragged away from a divider.
    // Remove the visual indicator.
    const columnDivider = event.currentTarget as HTMLDivElement;
    columnDivider.classList.remove('kanban-column-divider-drag-over');
}

const onColumnDropped = async function (event: DragEvent): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    // Handle the drop event when a column is dropped on a divider.
    const columnDivider = event.currentTarget as HTMLDivElement;
    columnDivider.classList.remove('kanban-column-divider-drag-over');
    const draggedColumnId = event.dataTransfer?.getData('column-id');
    const dividerId = columnDivider.dataset.columnDividerId;
    if (draggedColumnId && dividerId) {
        startLoadingItemsSpinner('kanban-board-main-div');
        const draggedColumn = kanbanBoard.columnsList.find(c => c.id.toString() === draggedColumnId);
        
        // Reorder the columns based on the dragged column and the divider it was dropped on.
        // Update the kanbanBoard.columnsList array accordingly and save to server.
        
        if (draggedColumn) {
            // If the column divider is next to the dragged column, don't reorder.
            const columnIndex = draggedColumn.columnIndex;
            if (columnIndex.toString() === dividerId || (columnIndex + 1).toString() === dividerId) {
                stopLoadingItemsSpinner('kanban-board-main-div');
                return new Promise<void>(function (resolve, reject) {
                    resolve();
                });
            }

            // Remove the dragged column from its current position.
            kanbanBoard.columnsList = kanbanBoard.columnsList.filter(c => c.id.toString() !== draggedColumnId);

            // Insert the dragged column at the new position based on the dividerId.
            const newIndex = parseInt(dividerId);
            // If the column is moved to the right, we need to adjust the newIndex by -1
            if (newIndex > columnIndex) {
                kanbanBoard.columnsList.splice(newIndex - 1, 0, draggedColumn);
            } else {
                kanbanBoard.columnsList.splice(newIndex, 0, draggedColumn);
            }

            // Update columnIndex for all columns.
            kanbanBoard.columnsList.forEach((c, index) => {
                c.columnIndex = index;
            });
            kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);

            // Save the updated KanbanBoard to the server.
            await updateKanbanBoardColumns(kanbanBoard);
            await renderKanbanBoard(false);            
        }
    }

    stopLoadingItemsSpinner('kanban-board-main-div');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function addColumnEventListeners(): void {
    // Set up drag-and-drop event listeners for each column.
    const columns = document.querySelectorAll<HTMLDivElement>('.kanban-column');
    columns.forEach((column) => {
        if (userCanEdit) {
            column.setAttribute('draggable', 'true');
            const columnDragStartFunction = function (event: DragEvent) {
                event.stopPropagation();
                //event.preventDefault();
                if (event.dataTransfer !== null) {
                    event.dataTransfer.setData('column-id', column.dataset.columnId || '');
                    event.dataTransfer.setData('kanban-column', 'kanban-column');
                    event.dataTransfer.effectAllowed = 'move';
                    column.classList.add('kanban-column-dragging');
                }
            }
            column.removeEventListener('dragstart', columnDragStartFunction);
            column.addEventListener('dragstart', columnDragStartFunction);

            const columnDragEndFunction = function (event: DragEvent) {
                column.classList.remove('kanban-column-dragging');
            }
            column.removeEventListener('dragend', columnDragEndFunction);
            column.addEventListener('dragend', columnDragEndFunction);
        }
    });

    addColumnDividerEventListeners();

    // Set up event listener for Add Column button.
    const addColumnButton = document.querySelector<HTMLButtonElement>('#add-kanban-column-button');
    if (addColumnButton) {
        const addColumnFunction = async function () {
            let newColumnIndex = 0;
            if (kanbanBoard.columnsList.length > 0) {
                newColumnIndex = Math.max(...kanbanBoard.columnsList.map(c => c.columnIndex)) + 1;
            }
            let newColumnId = 1;
            if (kanbanBoard.columnsList.length > 0) {
                newColumnId = Math.max(...kanbanBoard.columnsList.map(c => c.id)) + 1;
            }
            let newKanbanBoardColumn: KanbanBoardColumn = new KanbanBoardColumn();
            newKanbanBoardColumn.id = newColumnId;
            newKanbanBoardColumn.title = await getTranslation(defaultColumnTitle, 'Todos', getCurrentLanguageId());
            newKanbanBoardColumn.columnIndex = newColumnIndex;
            newKanbanBoardColumn.wipLimit = 0;
            kanbanBoard.columnsList.push(newKanbanBoardColumn);
            kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
            // Save the updated KanbanBoard to the server.
            await updateKanbanBoardColumns(kanbanBoard);
            // Re-render the KanbanBoard.
            await renderKanbanBoard(false);

            return new Promise<void>(function (resolve, reject) {
                resolve();
            });
        }
        addColumnButton.removeEventListener('click', addColumnFunction);
        addColumnButton.addEventListener('click', addColumnFunction);
    }

    const dragOverColumnFunction = function (event: DragEvent) {
        event.preventDefault();
        event.stopPropagation();
        // Check if dragged element is kanban-card.
        if (event.dataTransfer === null || !event.dataTransfer.types.includes('kanban-item-card')) {
            event.dataTransfer!.dropEffect = 'none';
            return;
        }

        // Check if WIP limit is reached for this column.
        const columnId = (event.currentTarget as HTMLDivElement).dataset.columnId;
        if (columnId) {
            const kanbanBoardColumn: KanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId) as KanbanBoardColumn;
            if (kanbanBoardColumn.wipLimit > 0) {
                const columnBodyDiv = document.querySelector<HTMLDivElement>('#kanban-column-body-' + kanbanBoardColumn.id);
                if (columnBodyDiv) {
                    const kanbanItemsInColumn = kanbanItems.filter(k => k.columnId.toString() === columnId);
                    if (kanbanItemsInColumn.length >= kanbanBoardColumn.wipLimit) {
                        event.dataTransfer!.dropEffect = 'none';
                        return;
                    }
                }
            }
            event.dataTransfer!.dropEffect = 'move';
        }
    }

    const dragEnterColumnFunction = function (event: DragEvent) {
        event.preventDefault();
        event.stopPropagation();
        // Check if dragged element is kanban-card.
        if (event.dataTransfer === null || !event.dataTransfer.types.includes('kanban-item-card')) {
            event.dataTransfer!.dropEffect = 'none';
            return;
        }
        // Check if WIP limit is reached for this column.
        const columnId = (event.currentTarget as HTMLDivElement).dataset.columnId;
        if (columnId) {
            const kanbanBoardColumn: KanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId) as KanbanBoardColumn;
            if (kanbanBoardColumn.wipLimit > 0) {
                const columnBodyDiv = document.querySelector<HTMLDivElement>('#kanban-column-body-' + kanbanBoardColumn.id);
                if (columnBodyDiv) {
                    const kanbanItemsInColumn = kanbanItems.filter(k => k.columnId.toString() === columnId);
                    if (kanbanItemsInColumn.length >= kanbanBoardColumn.wipLimit) {
                        event.dataTransfer!.dropEffect = 'none';
                        return;
                    }
                }
            }
            event.dataTransfer.dropEffect = 'move';
        }
        (event.currentTarget as HTMLDivElement).classList.add('kanban-column-drag-over');
    }
    const dragLeaveColumnFunction = function (event: DragEvent) {
        event.preventDefault();
        event.stopPropagation();
        // Check if dragged element is kanban-card.
        if (event.dataTransfer === null || !event.dataTransfer.types.includes('kanban-item-card')) {
            event.dataTransfer!.dropEffect = 'none';
            return;
        }
        (event.currentTarget as HTMLDivElement).classList.remove('kanban-column-drag-over');
    }

    const dropOnColumnFunction = async function (event: DragEvent): Promise<void> {
        event.preventDefault();
        event.stopPropagation();
        // Check if dragged element is kanban-card.
        if (event.dataTransfer === null || !event.dataTransfer.types.includes('kanban-item-card')) {
            event.dataTransfer!.dropEffect = 'none';
            return new Promise<void>(function (resolve, reject) {
                resolve();
            });
        }
        startLoadingItemsSpinner('kanban-board-main-div');
        (event.currentTarget as HTMLDivElement).classList.remove('kanban-column-drag-over');
        const kanbanItemId = event.dataTransfer?.getData('kanban-item-id');
        const targetColumnId = (event.currentTarget as HTMLDivElement).dataset.columnId;
        if (kanbanItemId && targetColumnId) {
            const kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
            
            if (kanbanItem) {
                const currentKanbanItemColumnId = kanbanItem.columnId;
                const kanbanBoardColumn: KanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === targetColumnId) as KanbanBoardColumn;
                if (kanbanBoardColumn.wipLimit > 0) {
                    const columnBodyDiv = document.querySelector<HTMLDivElement>('#kanban-column-body-' + kanbanBoardColumn.id);
                    if (columnBodyDiv) {
                        const kanbanItemsInColumn = kanbanItems.filter(k => k.columnId.toString() === targetColumnId);
                        if (kanbanItemsInColumn.length >= kanbanBoardColumn.wipLimit) {
                            alert('WIP limit reached for this column. Cannot move item here.'); // Todo: Replace with a nicer alert.
                            return new Promise<void>(function (resolve, reject) {
                                resolve();
                            });
                        }
                    }
                }
                const kanbanItemsInColumn = kanbanItems.filter(k => k.columnId.toString() === targetColumnId);
                kanbanItem.columnId = parseInt(targetColumnId);
                kanbanItem.rowIndex = kanbanItemsInColumn.length; // New items are added at the bottom of the column.

                // Reorder the kanbanItems array to reflect the new order.
                kanbanItems = kanbanItems.filter(k => k.kanbanItemId.toString() !== kanbanItemId);
                kanbanItems.push(kanbanItem);
                // Reassign rowIndex values for all items in the target column.
                const itemsInTargetColumn = kanbanItems.filter(k => k.columnId.toString() === targetColumnId);
                itemsInTargetColumn.forEach((item, index) => {
                    item.rowIndex = index;
                });

                // Reassign rowIndex values for all items in the current column, if different from target column.
                if (currentKanbanItemColumnId && targetColumnId !== currentKanbanItemColumnId.toString()) {
                    const itemsInCurrentColumn = kanbanItems.filter(k => k.columnId === currentKanbanItemColumnId);
                    itemsInCurrentColumn.forEach((item, index) => {
                        item.rowIndex = index;
                    });
                }
                // Save the updated KanbanItems to the server.
                if (currentKanbanItemColumnId && targetColumnId !== currentKanbanItemColumnId.toString()) {
                    await updateKanbanItemsInColumn(currentKanbanItemColumnId);
                }
                await updateKanbanItemsInColumn(parseInt(targetColumnId));
            }
        }
        stopLoadingItemsSpinner('kanban-board-main-div');

        return new Promise<void>(function (resolve, reject) {
            resolve();
        });
    }

    // Set up drag-and-drop event listeners for each column.
    columns.forEach((column) => {
        column.removeEventListener('dragover', dragOverColumnFunction);
        column.addEventListener('dragover', dragOverColumnFunction);
        column.removeEventListener('dragenter', dragEnterColumnFunction);
        column.addEventListener('dragenter', dragEnterColumnFunction);
        column.removeEventListener('dragleave', dragLeaveColumnFunction);
        column.addEventListener('dragleave', dragLeaveColumnFunction);
        column.removeEventListener('drop', dropOnColumnFunction);
        column.addEventListener('drop', dropOnColumnFunction);
    });

    // Set event listeners for column menu buttons.
    if (userCanEdit) {
        const columnMenuWrapperDivs = document.querySelectorAll<HTMLDivElement>('.kanban-column-menu-div');
        columnMenuWrapperDivs.forEach((menuDiv) => {
            menuDiv.classList.remove('d-none');
        });
        const columnMenuButtons = document.querySelectorAll<HTMLButtonElement>('.kanban-column-menu-button');
        columnMenuButtons.forEach((button) => {
            button.removeEventListener('click', showColumnMenu);
            button.addEventListener('click', showColumnMenu);
        });
    }    
}

const showColumnMenu = function (event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const button = event.currentTarget as HTMLButtonElement;
    const columnId = button.dataset.columnId;
    hideColumnMenus(columnId);
    hideCardMenus();
    if (columnId) {
        // Toggle d-none class of .kanban-column-menu-content
        const menuContentDiv = document.querySelector<HTMLDivElement>('.kanban-column-menu-content[data-column-id="' + columnId + '"]');
        if (menuContentDiv) {
            if (menuContentDiv.classList.contains('d-none')) {
                menuContentDiv.classList.remove('d-none');
                // Set up event listeners for menu items.
                const renameButton = menuContentDiv.querySelector<HTMLButtonElement>('button[data-column-menu-action="rename"]');
                if (renameButton) {
                    const renameFunction = function (event: MouseEvent) {
                        event.preventDefault();
                        event.stopPropagation();
                        menuContentDiv.classList.add('d-none');
                        showRenameColumnPrompt(columnId);
                    }
                    renameButton.removeEventListener('click', renameFunction);
                    renameButton.addEventListener('click', renameFunction);
                }

                const setLimitButton = menuContentDiv.querySelector<HTMLButtonElement>('button[data-column-menu-action="setlimit"]');
                if (setLimitButton) {
                    const setLimitFunction = async function (event: MouseEvent) {
                        event.preventDefault();
                        event.stopPropagation();
                        menuContentDiv.classList.add('d-none');
                        await showSetLimitPrompt(columnId);
                        return new Promise<void>(function (resolve, reject) {
                            resolve();
                        });
                    }
                    setLimitButton.removeEventListener('click', setLimitFunction);
                    setLimitButton.addEventListener('click', setLimitFunction);
                }

                const setColumnStatusButton = menuContentDiv.querySelector<HTMLButtonElement>('button[data-column-menu-action="setstatus"]');
                if (setColumnStatusButton) {
                    const setColumnStatusFunction = async function (event: MouseEvent) {
                        event.preventDefault();
                        event.stopPropagation();
                        menuContentDiv.classList.add('d-none');
                        await showSetColumnStatusPrompt(columnId);
                        return new Promise<void>(function (resolve, reject) {
                            resolve();
                        });
                    }
                    setColumnStatusButton.removeEventListener('click', setColumnStatusFunction);
                    setColumnStatusButton.addEventListener('click', setColumnStatusFunction);
                }

                const moveLeftButton = menuContentDiv.querySelector<HTMLButtonElement>('button[data-column-menu-action="moveleft"]');
                if (moveLeftButton) {
                    // If this is the first column, disable the button.
                    if (kanbanBoard.columnsList.find(c => c.id.toString() === columnId)?.columnIndex === 0) {
                        moveLeftButton.disabled = true;
                    } else {
                        moveLeftButton.disabled = false;
                        const moveLeftFunction = async function (event: MouseEvent) {
                            menuContentDiv.classList.add('d-none');
                            await moveColumnLeft(columnId);
                            return new Promise<void>(function (resolve, reject) {
                                resolve();
                            });
                        }
                        moveLeftButton.removeEventListener('click', moveLeftFunction);
                        moveLeftButton.addEventListener('click', moveLeftFunction);
                    }                    
                }

                const moveRightButton = menuContentDiv.querySelector<HTMLButtonElement>('button[data-column-menu-action="moveright"]');
                if (moveRightButton) {
                    // If this is the last column, disable the button.
                    if (kanbanBoard.columnsList.find(c => c.id.toString() === columnId)?.columnIndex === kanbanBoard.columnsList.length - 1) {
                        moveRightButton.disabled = true;
                    } else {
                        moveRightButton.disabled = false;
                        const moveRightFunction = async function (event: MouseEvent) {
                            menuContentDiv.classList.add('d-none');
                            await moveColumnRight(columnId);
                            return new Promise<void>(function (resolve, reject) {
                                resolve();
                            });
                        }
                        moveRightButton.removeEventListener('click', moveRightFunction);
                        moveRightButton.addEventListener('click', moveRightFunction);
                    }                    
                }

            } else {
                menuContentDiv.classList.add('d-none');
            }
        }
    }
}

async function moveColumnLeft(columnId: string): Promise<void> {
    hideColumnMenus();
    startLoadingItemsSpinner('kanban-board-main-div');
    const kanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId);
    if (kanbanBoardColumn) {
        const currentIndex = kanbanBoardColumn.columnIndex;
        if (currentIndex > 0) {
            // Find the column to the left and swap indexes.
            const leftColumn = kanbanBoard.columnsList.find(c => c.columnIndex === currentIndex - 1);
            if (leftColumn) {
                leftColumn.columnIndex = currentIndex;
                kanbanBoardColumn.columnIndex = currentIndex - 1;
                // Sort the columnsList by columnIndex.
                kanbanBoard.columnsList.sort((a, b) => a.columnIndex - b.columnIndex);
                kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                // Save the updated KanbanBoard to the server.
                await updateKanbanBoardColumns(kanbanBoard);
                await renderKanbanBoard(false);
            }
        }
    }
    stopLoadingItemsSpinner('kanban-board-main-div');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function moveColumnRight(columnId: string): Promise<void> {
    hideColumnMenus();
    startLoadingItemsSpinner('kanban-board-main-div');
    const kanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId);
    if (kanbanBoardColumn) {
        const currentIndex = kanbanBoardColumn.columnIndex;
        if (currentIndex < kanbanBoard.columnsList.length - 1) {
            // Find the column to the right and swap indexes.
            const rightColumn = kanbanBoard.columnsList.find(c => c.columnIndex === currentIndex + 1);
            if (rightColumn) {
                rightColumn.columnIndex = currentIndex;
                kanbanBoardColumn.columnIndex = currentIndex + 1;
                // Sort the columnsList by columnIndex.
                kanbanBoard.columnsList.sort((a, b) => a.columnIndex - b.columnIndex);
                kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                // Save the updated KanbanBoard to the server.
                await updateKanbanBoardColumns(kanbanBoard);
                await renderKanbanBoard(false);
            }
        }
    }
    stopLoadingItemsSpinner('kanban-board-main-div');

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function showSetLimitPrompt(columnId: string): Promise<void> {
    hideSettingsModals();

    const kanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId);
    if (kanbanBoardColumn) {
        const setLimitPromptDiv = document.querySelector<HTMLDivElement>('#set-wip-limit-modal-' + columnId);
        if (setLimitPromptDiv === null) {
            return new Promise<void>(function (resolve, reject) {
                resolve();
            });
        }
        setLimitPromptDiv.classList.remove('d-none');

        const wipLimitInput = document.querySelector<HTMLInputElement>('#wip-limit-input-' + columnId);
        const saveButton = document.querySelector<HTMLButtonElement>('#wip-limit-save-button-' + columnId);
        if (wipLimitInput && saveButton) {
            wipLimitInput.focus();
            // Save on enter key press. Close on Esc key press.
            const wipLimitKeyPressedFunction = function (event: KeyboardEvent) {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    saveButton.click();
                }
                if (event.key === 'Escape') {
                    event.preventDefault();
                    // Remove the modal from the DOM.
                    hideSettingsModals();
                }
            }

            wipLimitInput.removeEventListener('keydown', wipLimitKeyPressedFunction);
            wipLimitInput.addEventListener('keydown', wipLimitKeyPressedFunction);

            const saveFunction = async function () {
                hideSettingsModals();
                startLoadingItemsSpinner('kanban-board-main-div');
                const newLimit = parseInt(wipLimitInput.value);
                if (!isNaN(newLimit) && newLimit >= 0) {
                    kanbanBoardColumn.wipLimit = newLimit;
                    kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                    // Save the updated KanbanBoard to the server.
                    await updateKanbanBoardColumns(kanbanBoard);
                    const limitSpan = document.querySelector('.kanban-card-wip-limit[data-wip-limit-id="' + kanbanBoardColumn.id + '"]');
                    if (limitSpan) {
                        // Replace the limitSpan content with the new limit.
                        let updatedLimitContent = '/&#8734; ]'
                        if (kanbanBoardColumn.wipLimit > 0) {
                            updatedLimitContent = '/' + kanbanBoardColumn.wipLimit + ' ]';
                        }
                        const numberOfItemsContent = limitSpan.innerHTML.split('/')[0];
                        updatedLimitContent = numberOfItemsContent + updatedLimitContent;
                        limitSpan.innerHTML = updatedLimitContent;
                    }
                }
                stopLoadingItemsSpinner('kanban-board-main-div');
                return new Promise<void>(function (resolve, reject) {
                    resolve();
                });
            }

            saveButton.removeEventListener('click', saveFunction);
            saveButton.addEventListener('click', saveFunction);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

async function showSetColumnStatusPrompt(columnId: string): Promise<void> {
    hideSettingsModals();

    const kanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId);
    if (kanbanBoardColumn) {
        const setStatusPromptDiv = document.querySelector<HTMLDivElement>('#set-column-status-modal-' + columnId);
        if (setStatusPromptDiv === null) {
            return new Promise<void>(function (resolve, reject) {
                resolve();
            });
        }
        setStatusPromptDiv.classList.remove('d-none');
        const setStatusNoneButton = document.querySelector<HTMLButtonElement>('#set-column-status-none-button-' + columnId);
        const setStatusNotStartedButton = document.querySelector<HTMLButtonElement>('#set-column-status-not-started-button-' + columnId);
        const setStatusInProgressButton = document.querySelector<HTMLButtonElement>('#set-column-status-in-progress-button-' + columnId);
        const setStatusDoneButton = document.querySelector<HTMLButtonElement>('#set-column-status-done-button-' + columnId);
        const setStatusCancelledButton = document.querySelector<HTMLButtonElement>('#set-column-status-cancelled-button-' + columnId);
        if (setStatusNoneButton) {
            const setNoneFunction = async function () {
                hideSettingsModals();
                startLoadingItemsSpinner('kanban-board-main-div');
                kanbanBoardColumn.setStatus = TodoStatusType.None;
                kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                // Save the updated KanbanBoard to the server.
                await updateKanbanBoardColumns(kanbanBoard);
                await renderKanbanBoard(false);
                stopLoadingItemsSpinner('kanban-board-main-div');
                return new Promise<void>(function (resolve, reject) {
                    resolve();
                });
            }
            if (setStatusNoneButton.classList.contains('disabled')) {
                setStatusNoneButton.disabled = true;
            }
            else {
                setStatusNoneButton.removeEventListener('click', setNoneFunction);
                setStatusNoneButton.addEventListener('click', setNoneFunction);
            }
            
        }
        if (setStatusNotStartedButton) {
            const setNotStartedFunction = async function () {
                hideSettingsModals();
                startLoadingItemsSpinner('kanban-board-main-div');
                kanbanBoardColumn.setStatus = TodoStatusType.NotStarted;
                kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                // Save the updated KanbanBoard to the server.
                await updateKanbanBoardColumns(kanbanBoard);
                await renderKanbanBoard(false);
                stopLoadingItemsSpinner('kanban-board-main-div');
                return new Promise<void>(function (resolve, reject) {
                    resolve();
                });
            }
            if (setStatusNotStartedButton.classList.contains('disabled')) {
                setStatusNotStartedButton.disabled = true;
            }
            else {
                setStatusNotStartedButton.removeEventListener('click', setNotStartedFunction);
                setStatusNotStartedButton.addEventListener('click', setNotStartedFunction);
            }
            
        }
        if (setStatusInProgressButton) {
            const setInProgressFunction = async function () {
                hideSettingsModals();
                startLoadingItemsSpinner('kanban-board-main-div');
                kanbanBoardColumn.setStatus = TodoStatusType.InProgress;
                kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                // Save the updated KanbanBoard to the server.
                await updateKanbanBoardColumns(kanbanBoard);
                await renderKanbanBoard(false);
                stopLoadingItemsSpinner('kanban-board-main-div');
                return new Promise<void>(function (resolve, reject) {
                    resolve();
                });
            }
            if (setStatusInProgressButton.classList.contains('disabled')) {
                setStatusInProgressButton.disabled = true;
            }
            else {
                setStatusInProgressButton.removeEventListener('click', setInProgressFunction);
                setStatusInProgressButton.addEventListener('click', setInProgressFunction);
            }
            
        }
        if (setStatusDoneButton) {
            const setDoneFunction = async function () {
                hideSettingsModals();
                startLoadingItemsSpinner('kanban-board-main-div');
                kanbanBoardColumn.setStatus = TodoStatusType.Completed;
                kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                // Save the updated KanbanBoard to the server.
                await updateKanbanBoardColumns(kanbanBoard);
                await renderKanbanBoard(false);
                stopLoadingItemsSpinner('kanban-board-main-div');
                return new Promise<void>(function (resolve, reject) {
                    resolve();
                });
            }
            if (setStatusDoneButton.classList.contains('disabled')) {
                setStatusDoneButton.disabled = true;
            }
            else {
                setStatusDoneButton.removeEventListener('click', setDoneFunction);
                setStatusDoneButton.addEventListener('click', setDoneFunction);
            }            
        }
        if (setStatusCancelledButton) {
            const setCancelledFunction = async function () {
                hideSettingsModals();
                startLoadingItemsSpinner('kanban-board-main-div');
                kanbanBoardColumn.setStatus = TodoStatusType.Cancelled;
                kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                // Save the updated KanbanBoard to the server.
                await updateKanbanBoardColumns(kanbanBoard);
                await renderKanbanBoard(false);
                stopLoadingItemsSpinner('kanban-board-main-div');
                return new Promise<void>(function (resolve, reject) {
                    resolve();
                });
            }
            if (setStatusCancelledButton.classList.contains('disabled')) {
                setStatusCancelledButton.disabled = true;
            }
            else {
                setStatusCancelledButton.removeEventListener('click', setCancelledFunction);
                setStatusCancelledButton.addEventListener('click', setCancelledFunction);
            }
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function showRenameColumnPrompt(columnId: string): void {
    const kanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId);
    if (kanbanBoardColumn) {
        
        const columnTitleDiv = document.querySelector<HTMLDivElement>('.kanban-column-title[data-column-id="' + columnId + '"]');
        const columnMenuDiv = document.querySelector<HTMLDivElement>('.kanban-column-menu-div[data-column-id="' + columnId + '"]');
        const renameColumnInputWrapper = document.querySelector<HTMLDivElement>('#rename-column-input-group-' + columnId);

        if (columnTitleDiv && renameColumnInputWrapper && columnMenuDiv) {
            columnTitleDiv.classList.add('d-none');
            columnMenuDiv.classList.add('d-none');
            renameColumnInputWrapper.classList.remove('d-none');
            
            const renameInput = document.querySelector<HTMLInputElement>('#rename-column-input-' + columnId);
            if (renameInput) {
                renameInput.focus();
                // Save on enter key press. Close on Esc key press.
                const renameKeyPressedFunction = function (event: KeyboardEvent) {

                    if (event.key === 'Enter') {
                        event.preventDefault();
                        const saveButton = document.querySelector<HTMLButtonElement>('#rename-column-save-button-' + columnId);
                        if (saveButton) {
                            saveButton.click();
                        }
                    }

                    if (event.key === 'Escape') {
                        event.preventDefault();
                        hideRenameInputs();
                    }
                }
                renameInput.removeEventListener('keydown', renameKeyPressedFunction);
                renameInput.addEventListener('keydown', renameKeyPressedFunction);
            }
            const saveButton = document.querySelector<HTMLButtonElement>('#rename-column-save-button-' + columnId);
            if (saveButton && renameInput) {
                const saveFunction = async function () {
                    hideRenameInputs();
                    startLoadingItemsSpinner('kanban-board-main-div');
                    const newTitle = renameInput.value;
                    if (newTitle && newTitle.trim() !== '') {

                        kanbanBoardColumn.title = newTitle.trim();
                        kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                        // Save the updated KanbanBoard to the server.
                        await updateKanbanBoardColumns(kanbanBoard);
                        const columnTitleSpan = document.querySelector<HTMLSpanElement>('span[data-title-span-id="' + columnId + '"]');
                        if (columnTitleSpan) {
                            columnTitleSpan.innerHTML = kanbanBoardColumn.title;
                        }                        
                    }
                    stopLoadingItemsSpinner('kanban-board-main-div');
                    return new Promise<void>(function (resolve, reject) {
                        resolve();
                    });
                }
                saveButton.removeEventListener('click', saveFunction);
                saveButton.addEventListener('click', saveFunction);
            }
        }
    }    
}

function hideSettingsModals() {
    const modalDivs = document.querySelectorAll<HTMLDivElement>('.settings-modal');
    if (modalDivs) {
        modalDivs.forEach((modalDiv) => {
            modalDiv.classList.add('d-none');
        });
    }
}

export function hideColumnMenus(columnId: string = '') {
    const allColumnMenus = document.querySelectorAll<HTMLDivElement>('.kanban-column-menu-content');
    allColumnMenus.forEach((menu) => {
        const menuColumnId = menu.dataset.columnId;
        if (columnId === '' || menuColumnId !== columnId) {
            menu.classList.add('d-none');
        }
    });
}

export function hideCardMenus(kanbanItemId: string = '') {
    const allCardMenus = document.querySelectorAll<HTMLDivElement>('.kanban-card-menu-content');
    allCardMenus.forEach((menu) => {
        const menuKanbanItemId = menu.dataset.kanbanItemId;
        if (kanbanItemId === '' || menuKanbanItemId !== kanbanItemId) {
            menu.classList.add('d-none');
        }
    });
}

function hideRenameInputs() {
    const allRenameInputGroups = document.querySelectorAll<HTMLDivElement>('.kanban-column-rename-input-group');
    allRenameInputGroups.forEach((inputGroup) => {
        inputGroup.classList.add('d-none');
    });

    const allColumnTitleDivs = document.querySelectorAll<HTMLDivElement>('.kanban-column-title');
    allColumnTitleDivs.forEach((titleDiv) => {
        titleDiv.classList.remove('d-none');
    });

    const allMenuDivs = document.querySelectorAll<HTMLDivElement>('.kanban-column-menu-div');
    allMenuDivs.forEach((menuDiv) => {
        menuDiv.classList.remove('d-none');
    });
}

export function hideAllMenusAndModals(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.kanban-column-menu-div')) {
        hideColumnMenus();
    }

    if (!target.closest('.kanban-card-menu-div')) {
        hideCardMenus();
    }

    if (!target.closest('.kanban-column-rename-input-group') && !target.closest('.kanban-column-menu-div')) {
        hideRenameInputs();
    }

    if (!target.closest('.modal-settings-panel') && !target.closest('.modal-content') && !target.closest('.kanban-column-menu-div') && !target.closest('.add-edit-kanban-item-modal')) {
        hideSettingsModals();
    }
}

async function updateKanbanBoardColumns(kanbanBoard: KanbanBoard): Promise<void> {
    let url = '/Kanbans/UpdateKanbanBoardColumns';
    await fetch(url, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(kanbanBoard)
    }).then(async function (response) {
        if (response.ok) {
            // KanbanBoard updated successfully.
        } else {
            console.error('Error updating Kanban Board. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error updating Kanban Board. Error: ' + error);
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}


