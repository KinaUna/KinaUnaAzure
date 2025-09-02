import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item.js";
import { getCurrentLanguageId, TimelineChangedEvent } from "../data-tools-v9.js";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v9.js";
import { getTranslation } from "../localization-v9.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9.js";
import { KanbanBoardColumn, TimelineItem } from "../page-models-v9.js";
import { displayKanbanItemDetails, getKanbanItemsForBoard, updateKanbanItem } from "./kanban-items.js";
let kanbanBoardMainDiv = document.querySelector('#kanban-board-main-div');
let kanbanBoard;
let kanbanItems = [];
const defaultColumnTitle = 'Unnamed Column';
let userCanEdit = false;
/**
 * Adds event listeners to all elements with the data-kanban-board-id attribute.
 * When clicked, the DisplayKanbanBoard function is called.
 * @param {string} itemId The id of the KanbanBoard item to add event listeners for.
 */
export function addKanbanBoardListeners(itemId) {
    const kanbanBoardElementsWithDataId = document.querySelectorAll('[data-kanban-board-id="' + itemId + '"]');
    if (kanbanBoardElementsWithDataId) {
        kanbanBoardElementsWithDataId.forEach((element) => {
            element.removeEventListener('click', onKanbanBoardDivClicked);
            element.addEventListener('click', onKanbanBoardDivClicked);
        });
    }
}
async function onKanbanBoardDivClicked(event) {
    const kanbanBoardElement = event.currentTarget;
    if (kanbanBoardElement !== null) {
        const kanbanBoardId = kanbanBoardElement.dataset.kanbanBoardId;
        if (kanbanBoardId) {
            await displayKanbanBoard(kanbanBoardId);
        }
    }
}
/**
* Enable other scripts to call the DisplayKanbanBoard function.
* @param {string} kanbanBoardId The id of the KanbanBoard to display.
*/
export async function popupKanbanBoard(kanbanBoardId) {
    await displayKanbanBoard(kanbanBoardId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Displays a KanbanBoard in a popup.
 * @param {string} todoId The id of the KanbanBoard to display.
 */
async function displayKanbanBoard(kanbanBoardId) {
    startFullPageSpinner();
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
            const kanbanBoardDetailsPopupDiv = document.querySelector('#item-details-div');
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
                await renderKanbanBoard();
            }
        }
        else {
            console.error('Error getting Kanban Board. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting Kanban Board. Error: ' + error);
    });
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets up event listeners for the Kanban Board details popup.
 * @param {string} itemId The id of the Kanban Board item to set event listeners for.
 * @param {HTMLDivElement} kanbanBoardDetailsPopupDiv The div element for the Kanban Board details popup.
 */
async function setKanbanBoardDetailsEventListeners(itemId, kanbanBoardDetailsPopupDiv) {
    let closeButtonsList = document.querySelectorAll('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function () {
                kanbanBoardDetailsPopupDiv.innerHTML = '';
                kanbanBoardDetailsPopupDiv.classList.add('d-none');
                showBodyScrollbars();
            };
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }
}
function dispatchTimelineItemChangedEvent(kanbanBoardId) {
    const timelineItem = new TimelineItem();
    timelineItem.itemType = 16;
    timelineItem.itemId = kanbanBoardId;
    const timelineItemChangedEvent = new TimelineChangedEvent(timelineItem);
    window.dispatchEvent(timelineItemChangedEvent);
}
async function getKanbanBoard(kanbanBoardId) {
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
            kanbanBoard.columnsList = JSON.parse(kanbanBoard.columns);
        }
        else {
            console.error('Error getting Kanban Board. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting Kanban Board. Error: ' + error);
    });
    return kanbanBoard;
}
async function renderKanbanBoard() {
    kanbanBoardMainDiv = document.querySelector('#kanban-board-main-div');
    if (kanbanBoardMainDiv) {
        const kanbanBoardId = kanbanBoardMainDiv.dataset.viewKanbanBoardId;
        const userCanEditData = kanbanBoardMainDiv.dataset.userCanEdit;
        if (userCanEditData && userCanEditData === 'True') {
            userCanEdit = true;
        }
        if (kanbanBoardId) {
            const kanbanBoard = await getKanbanBoard(parseInt(kanbanBoardId));
            if (kanbanBoard) {
                kanbanBoardMainDiv.innerHTML = createKanbanBoardContainer(kanbanBoard);
                // If the KanbanBoard has no columns, add a default "To Do" column.
                if (kanbanBoard.columnsList !== null && kanbanBoard.columnsList.length === 0) {
                    let defaultKanbanBoardColumn = new KanbanBoardColumn();
                    defaultKanbanBoardColumn.id = 1;
                    defaultKanbanBoardColumn.title = await getTranslation(defaultColumnTitle, 'Todos', getCurrentLanguageId());
                    defaultKanbanBoardColumn.columnIndex = 0;
                    defaultKanbanBoardColumn.wipLimit = 0;
                    kanbanBoard.columnsList.push(defaultKanbanBoardColumn);
                    kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                    // Save the updated KanbanBoard to the server.
                    await updateKanbanBoardColumns(kanbanBoard);
                }
                // Load KanbanItems and render them in the appropriate columns.
                kanbanItems = await getKanbanItemsForBoard(kanbanBoard.kanbanBoardId);
                await renderKanbanItems(kanbanBoard.kanbanBoardId);
                addColumnEventListeners();
            }
        }
    }
}
async function renderKanbanItems(kanbanBoardId) {
    kanbanItems.forEach((item) => {
        const columnBodyDiv = document.querySelector('#kanban-column-body-' + item.columnIndex);
        if (columnBodyDiv && item.todoItem) {
            const cardDiv = document.createElement('div');
            cardDiv.classList.add('kanban-card');
            cardDiv.setAttribute('data-card-id', item.kanbanItemId.toString());
            cardDiv.innerHTML = `
                            <div class="kanban-card-header">
                                <i class="material-icons kinauna-icon-medium">${getStatusIconForCard(item.todoItem.status)}</i>
                                <h5>${item.todoItem.title}</h5>
                            </div>
                            <div class="kanban-card-body">
                                <p>Actions go here</p>
                            </div>
                        `; // Todo: Add profile picture, context, tags, etc.
            columnBodyDiv.appendChild(cardDiv);
        }
        // Set up event listeners for adding cards, editing cards, deleting cards, dragging and dropping cards, etc.
        const addCardButtons = document.querySelectorAll('.add-card-button');
        addCardButtons.forEach((button) => {
            const addCardButtonFunction = async function () {
                const columnId = button.dataset.columnId;
                if (columnId) {
                    // Todo: Open a modal or popup to add a new card to the specified column.
                    console.log('Add card to column: ' + columnId);
                }
            };
            button.removeEventListener('click', addCardButtonFunction);
            button.addEventListener('click', addCardButtonFunction);
        });
        const kanbanCards = document.querySelectorAll('.kanban-card');
        kanbanCards.forEach((card) => {
            const cardClickFunction = async function () {
                const kanbanItemId = card.dataset.kanbanItemId;
                if (kanbanItemId) {
                    displayKanbanItemDetails(kanbanItemId);
                }
            };
            card.removeEventListener('click', cardClickFunction);
            card.addEventListener('click', cardClickFunction);
            if (userCanEdit) {
                card.setAttribute('draggable', 'true');
                const cardDragFunction = function (event) {
                    if (event.dataTransfer !== null) {
                        event.dataTransfer.setData('element-id', card.dataset.kanbanItemId || '');
                        event.dataTransfer.setData('element-type', 'kanban-item-card');
                        event.dataTransfer.setData('source-column-id', card.parentElement?.parentElement?.dataset.columnId || '');
                        event.dataTransfer.effectAllowed = 'move';
                        card.classList.add('kanban-item-dragging');
                    }
                };
                card.removeEventListener('dragstart', cardDragFunction);
                card.addEventListener('dragstart', cardDragFunction);
                const cardDragEndFunction = async function (event) {
                    card.classList.remove('kanban-item-dragging');
                    const kanbanItemId = card.dataset.kanbanItemId;
                    if (kanbanItemId) {
                        // Update the KanbanItem's columnIndex based on the new column it was dropped into.
                        const targetColumn = event.target;
                        const targetColumnId = targetColumn.closest('.kanban-column')?.dataset.columnId;
                        const kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
                        const currentKanbanItemColumnIndex = kanbanItem?.columnIndex;
                        if (targetColumnId) {
                            const kanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === targetColumnId);
                            if (kanbanBoardColumn.wipLimit > 0) {
                                const columnBodyDiv = document.querySelector('#kanban-column-body-' + kanbanBoardColumn.id);
                                if (columnBodyDiv) {
                                    const kanbanItemsInColumn = columnBodyDiv.querySelectorAll('.kanban-card');
                                    if (kanbanItemsInColumn.length >= kanbanBoardColumn.wipLimit) {
                                        alert('WIP limit reached for this column. Cannot move item here.'); // Todo: Replace with a nicer alert.
                                        return;
                                    }
                                }
                            }
                            await updateKanbanItemsInColumn(parseInt(targetColumnId));
                            if (currentKanbanItemColumnIndex && targetColumnId !== currentKanbanItemColumnIndex.toString()) {
                                await updateKanbanItemsInColumn(currentKanbanItemColumnIndex);
                            }
                            dispatchTimelineItemChangedEvent(kanbanBoardId.toString());
                        }
                    }
                };
                card.removeEventListener('dragend', cardDragEndFunction);
                card.addEventListener('dragend', cardDragEndFunction);
            }
        });
    });
}
async function updateKanbanItemsInColumn(columnId) {
    // Get the column HTMLDiv element.
    const columnDiv = document.querySelector('.kanban-column[data-column-id="' + columnId + '"]');
    if (columnDiv) {
        // Get the list of KanbanItems in the column
        const kanbanItemsInColumn = columnDiv.querySelectorAll('.kanban-card');
        kanbanItemsInColumn.forEach(async (card) => {
            const kanbanItemId = card.dataset.kanbanItemId;
            if (kanbanItemId) {
                // Update the KanbanItem's columnIndex based on the new column it was dropped into.
                const kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
                if (kanbanItem) {
                    kanbanItem.columnIndex = columnId;
                    kanbanItem.rowIndex = Array.from(kanbanItemsInColumn).indexOf(card);
                    // Save the updated KanbanItem to the server.
                    await updateKanbanItem(kanbanItem);
                }
            }
        });
    }
}
function getStatusIconForCard(status) {
    switch (status) {
        case 0:
            return 'pending_actions'; // Not started
        case 1:
            return 'assignment'; // In progress
        case 2:
            return 'assignment_turned_in'; // Completed
        case 3:
            return 'playlist_remove'; // Cancelled
        default:
            return 'help_outline'; // Unknown status
    }
}
function createKanbanBoardContainer(kanbanBoard) {
    let kanbanBoardHtml = '<div class="kanban-board-container"><div class="kanban-column-divider" data-column-divider-id="0"></div>';
    let dividerIndex = 1;
    kanbanBoard.columnsList.forEach((column) => {
        kanbanBoardHtml += `
                        <div class="kanban-column" data-column-id="${column.id}">
                            <div class="kanban-column-header">
                                <div class="kanban-column-menu-div d-none float-right" data-column-id="${column.id}">
                                    <button class="kanban-column-menu-button" data-column-id="${column.id}">...</button>
                                    <div class="kanban-column-menu-content d-none" data-column-id="${column.id}">
                                        <button class="btn kanban-column-menu-item-button" data-column-menu-action="rename" data-column-id="${column.id}" >Rename</button>
                                    </div>
                                </div>
                                <h5>${column.title}</h5>
                            </div>
                            <div class="kanban-column-body" id="kanban-column-body-${column.id}">
                                <!-- Cards will be dynamically added here -->
                            </div>
                            <div class="kanban-column-footer">`;
        if (userCanEdit) {
            kanbanBoardHtml += `
                                <button class="btn btn-sm btn-primary add-card-button" data-column-id="${column.id}">Add Card</button>
                            </div>
                        </div>`;
        }
        else {
            kanbanBoardHtml += `
            </div>
            </div>`;
        }
        kanbanBoardHtml += `<div class="kanban-column-divider" data-column-divider-id="${dividerIndex}"></div>`;
        dividerIndex++;
    });
    let addColumnButtonHtml = `<div class="canban-column"><button class="btn btn-sm btn-success" id="add-kanban-column-button"><i class="material-icons kinauna-icon-small">add</i></button></div>`;
    if (userCanEdit) {
        kanbanBoardHtml += addColumnButtonHtml;
    }
    kanbanBoardHtml += '</div>';
    return kanbanBoardHtml;
}
function addColumnDividerEventListeners() {
    const columnDividers = document.querySelectorAll('.kanban-column-divider');
    columnDividers.forEach((divider) => {
        // If a column is dragged over a divider, show a visual indicator
        divider.removeEventListener('dragover', onColumnDragOverDidivider);
        divider.addEventListener('dragover', onColumnDragOverDidivider);
        // If a column is dropped on a divider, reorder the columns accordingly.
        divider.removeEventListener('dragleave', onColumnDragLeaveDivider);
        divider.addEventListener('dragleave', onColumnDragLeaveDivider);
        // Handle the drop event.
        divider.removeEventListener('drop', onColumnDropped);
        divider.addEventListener('drop', onColumnDropped);
    });
}
function onColumnDragOverDidivider(event) {
    event.preventDefault();
    // Handle the dragover event when a column is dragged over a divider.
    // Show a visual indicator that the column can be dropped here.
    const columnDivider = event.currentTarget;
    columnDivider.classList.add('kanban-column-divider-drag-over');
}
function onColumnDragLeaveDivider(event) {
    event.preventDefault();
    // Handle the dragleave event when a column is dragged away from a divider.
    // Remove the visual indicator.
    const columnDivider = event.currentTarget;
    columnDivider.classList.remove('kanban-column-divider-drag-over');
}
const onColumnDropped = async function (event) {
    event.preventDefault();
    // Handle the drop event when a column is dropped on a divider.
    const columnDivider = event.currentTarget;
    columnDivider.classList.remove('kanban-column-divider-drag-over');
    const draggedColumnId = event.dataTransfer?.getData('element-id');
    const dividerId = columnDivider.dataset.columnDividerId;
    if (draggedColumnId && dividerId) {
        // If the column divider is next to the dragged column, don't reorder.
        if (draggedColumnId === dividerId || draggedColumnId + 1 === dividerId) {
            return;
        }
        // Reorder the columns based on the dragged column and the divider it was dropped on.
        // Update the kanbanBoard.columnsList array accordingly and save to server.
        const draggedColumn = kanbanBoard.columnsList.find(c => c.id.toString() === draggedColumnId);
        if (draggedColumn) {
            // Remove the dragged column from its current position.
            kanbanBoard.columnsList = kanbanBoard.columnsList.filter(c => c.id.toString() !== draggedColumnId);
            // Insert the dragged column at the new position based on the dividerId.
            const newIndex = parseInt(dividerId);
            kanbanBoard.columnsList.splice(newIndex, 0, draggedColumn);
            // Update columnIndex for all columns.
            kanbanBoard.columnsList.forEach((c, index) => {
                c.columnIndex = index;
            });
            kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
            // Save the updated KanbanBoard to the server.
            await updateKanbanBoardColumns(kanbanBoard);
            await renderKanbanBoard();
        }
    }
};
function addColumnEventListeners() {
    // Set up drag-and-drop event listeners for each column.
    const columns = document.querySelectorAll('.kanban-column');
    columns.forEach((column) => {
        if (userCanEdit) {
            column.setAttribute('draggable', 'true');
            const columnDragFunction = function (event) {
                if (event.dataTransfer !== null) {
                    event.dataTransfer.setData('element-id', column.dataset.columnId || '');
                    event.dataTransfer.setData('element-type', 'kanban-column');
                    event.dataTransfer.effectAllowed = 'move';
                    column.classList.add('kanban-column-dragging');
                }
            };
            column.removeEventListener('dragstart', columnDragFunction);
            column.addEventListener('dragstart', columnDragFunction);
            const columnDragEndFunction = function (event) {
                column.classList.remove('kanban-column-dragging');
            };
            column.removeEventListener('dragend', columnDragEndFunction);
            column.addEventListener('dragend', columnDragEndFunction);
        }
    });
    addColumnDividerEventListeners();
    // Set up event listener for Add Column button.
    const addColumnButton = document.querySelector('#add-kanban-column-button');
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
            let newKanbanBoardColumn = new KanbanBoardColumn();
            newKanbanBoardColumn.id = newColumnId;
            newKanbanBoardColumn.title = await getTranslation(defaultColumnTitle, 'Todos', getCurrentLanguageId());
            newKanbanBoardColumn.columnIndex = newColumnIndex;
            newKanbanBoardColumn.wipLimit = 0;
            kanbanBoard.columnsList.push(newKanbanBoardColumn);
            kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
            // Save the updated KanbanBoard to the server.
            await updateKanbanBoardColumns(kanbanBoard);
            // Re-render the KanbanBoard.
            await renderKanbanBoard();
        };
        addColumnButton.removeEventListener('click', addColumnFunction);
        addColumnButton.addEventListener('click', addColumnFunction);
    }
    const dragOverFunction = function (event) {
        event.preventDefault();
        // Check if dragged element is kanban-card.
        if (event.dataTransfer === null || !event.dataTransfer.types.includes('element-type')) {
            event.dataTransfer.dropEffect = 'none';
            return;
        }
        const elementType = event.dataTransfer.getData('element-type');
        if (elementType !== 'kanban-item-card' && elementType !== 'kanban-column') {
            event.dataTransfer.dropEffect = 'none';
            return;
        }
        // Check if WIP limit is reached for this column.
        const columnId = event.currentTarget.dataset.columnId;
        if (columnId) {
            const kanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId);
            if (kanbanBoardColumn.wipLimit > 0) {
                const columnBodyDiv = document.querySelector('#kanban-column-body-' + kanbanBoardColumn.id);
                if (columnBodyDiv) {
                    const kanbanItemsInColumn = columnBodyDiv.querySelectorAll('.kanban-card');
                    if (kanbanItemsInColumn.length >= kanbanBoardColumn.wipLimit) {
                        event.dataTransfer.dropEffect = 'none';
                        return;
                    }
                }
            }
            event.dataTransfer.dropEffect = 'move';
        }
    };
    const dragEnterFunction = function (event) {
        event.preventDefault();
        event.currentTarget.classList.add('kanban-column-drag-over');
    };
    const dragLeaveFunction = function (event) {
        event.preventDefault();
        event.currentTarget.classList.remove('kanban-column-drag-over');
    };
    // Set up drag-and-drop event listeners for each column.
    const kanbanColumns = document.querySelectorAll('.kanban-column');
    kanbanColumns.forEach((column) => {
        column.removeEventListener('dragover', dragOverFunction);
        column.addEventListener('dragover', dragOverFunction);
        column.removeEventListener('dragenter', dragEnterFunction);
        column.addEventListener('dragenter', dragEnterFunction);
        column.removeEventListener('dragleave', dragLeaveFunction);
        column.addEventListener('dragleave', dragLeaveFunction);
    });
    // Set event listeners for column menu buttons.
    if (userCanEdit) {
        const columnMenuWrapperDivs = document.querySelectorAll('.kanban-column-menu-div');
        columnMenuWrapperDivs.forEach((menuDiv) => {
            menuDiv.classList.remove('d-none');
        });
        const columnMenuButtons = document.querySelectorAll('.kanban-column-menu-button');
        columnMenuButtons.forEach((button) => {
            button.removeEventListener('click', showColumnMenu);
            button.addEventListener('click', showColumnMenu);
        });
    }
}
function showColumnMenu(event) {
    const button = event.currentTarget;
    const columnId = button.dataset.columnId;
    if (columnId) {
        // Toggle d-none class of .kanban-column-menu-content
        const menuContentDiv = document.querySelector('.kanban-column-menu-content[data-column-id="' + columnId + '"]');
        if (menuContentDiv) {
            if (menuContentDiv.classList.contains('d-none')) {
                menuContentDiv.classList.remove('d-none');
                // Set up event listeners for menu items.
                const renameButton = menuContentDiv.querySelector('button[data-column-menu-action="rename"]');
                if (renameButton) {
                    const renameFunction = async function () {
                        const newTitle = prompt('Enter new column title:');
                        if (newTitle && newTitle.trim() !== '') {
                            const kanbanBoardColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId);
                            if (kanbanBoardColumn) {
                                kanbanBoardColumn.title = newTitle.trim();
                                kanbanBoard.columns = JSON.stringify(kanbanBoard.columnsList);
                                // Save the updated KanbanBoard to the server.
                                await updateKanbanBoardColumns(kanbanBoard);
                                // Re-render the KanbanBoard.
                                await renderKanbanBoard();
                            }
                        }
                    };
                    renameButton.removeEventListener('click', renameFunction);
                    renameButton.addEventListener('click', renameFunction);
                }
            }
            else {
                menuContentDiv.classList.add('d-none');
            }
        }
    }
}
async function updateKanbanBoardColumns(kanbanBoard) {
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
        }
        else {
            console.error('Error updating Kanban Board. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error updating Kanban Board. Error: ' + error);
    });
}
//# sourceMappingURL=kanban-board-details.js.map