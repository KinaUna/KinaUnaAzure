import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item";
import { TimelineChangedEvent } from "../data-tools-v9";
import { hideBodyScrollbars, showBodyScrollbars } from "../item-details/items-display-v9";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9";
import { KanbanBoard, KanbanBoardColumn, KanbanItem, TimelineItem } from "../page-models-v9";
import { displayKanbanItemDetails, getKanbanItemsForBoard, updateKanbanItem } from "./kanban-items";

const kanbanBoardMainDiv = document.querySelector<HTMLDivElement>('#kanban-board-main-div');
let kanbanItems: KanbanItem[] = [];

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
}

/**
* Enable other scripts to call the DisplayKanbanBoard function.
* @param {string} kanbanBoardId The id of the KanbanBoard to display.
*/
export async function popupTodoItem(kanbanBoardId: string): Promise<void> {
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

            }
        } else {
            console.error('Error getting Kanban Board. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting Kanban Board. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Sets up event listeners for the Kanban Board details popup.
 * @param {string} itemId The id of the Kanban Board item to set event listeners for.
 * @param {HTMLDivElement} kanbanBoardDetailsPopupDiv The div element for the Kanban Board details popup.
 */
async function setKanbanBoardDetailsEventListeners(itemId: string, kanbanBoardDetailsPopupDiv: HTMLDivElement) {
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

function dispatchTimelineItemChangedEvent(kanbanBoardId: string): void {

    const timelineItem = new TimelineItem();
    timelineItem.itemType = 16;
    timelineItem.itemId = kanbanBoardId;
    const timelineItemChangedEvent = new TimelineChangedEvent(timelineItem);
    window.dispatchEvent(timelineItemChangedEvent);
}

async function getKanbanBoard(kanbanBoardId: number): Promise<KanbanBoard> {
    let kanbanBoard = new KanbanBoard();
    let url = '/api/KanbanBoards/GetKanbanBoard?kanbanBoardId=' + kanbanBoardId;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            kanbanBoard = await response.json();
        } else {
            console.error('Error getting Kanban Board. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting Kanban Board. Error: ' + error);
    });
    return kanbanBoard;
}

async function renderKanbanBoard() {
    if (kanbanBoardMainDiv) {
        const kanbanBoardId = kanbanBoardMainDiv.dataset.viewKanbanBoardId;
        if (kanbanBoardId) {
            const kanbanBoard: KanbanBoard = await getKanbanBoard(parseInt(kanbanBoardId));
            if (kanbanBoard) {
                if (kanbanBoard.columns !== null && kanbanBoard.columns.length === 0) {
                    kanbanBoardMainDiv.innerHTML = '<div class="alert alert-info" role="alert">No columns defined for this Kanban Board.</div>';
                }
                else {
                    kanbanBoardMainDiv.innerHTML = createKanbanBoardContainer(kanbanBoard);

                    // Load KanbanItems and render them in the appropriate columns.
                    kanbanItems = await getKanbanItemsForBoard(kanbanBoard.kanbanBoardId);
                    await renderKanbanItems(kanbanBoard.kanbanBoardId);
                    
                }
            }
        }
    }
}

async function renderKanbanItems(kanbanBoardId: number) {
    kanbanItems.forEach((item) => {
        const columnBodyDiv = document.querySelector<HTMLDivElement>('#kanban-column-body-' + item.columnIndex);
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
        const addCardButtons = document.querySelectorAll<HTMLButtonElement>('.add-card-button');
        addCardButtons.forEach((button) => {
            const addCardButtonFunction = async function () {
                const columnId = button.dataset.columnId;
                if (columnId) {
                    // Todo: Open a modal or popup to add a new card to the specified column.
                    console.log('Add card to column: ' + columnId);
                }
            }

            button.removeEventListener('click', addCardButtonFunction);
            button.addEventListener('click', addCardButtonFunction);
        });

        const kanbanCards = document.querySelectorAll<HTMLDivElement>('.kanban-card');
        kanbanCards.forEach((card) => {
            card.setAttribute('draggable', 'true');
            const cardClickFunction = async function () {
                const kanbanItemId = card.dataset.kanbanItemId;
                if (kanbanItemId) {
                    displayKanbanItemDetails(kanbanItemId);
                }
            }
            card.removeEventListener('click', cardClickFunction);
            card.addEventListener('click', cardClickFunction);

            const cardDragFunction = function (event: DragEvent) {
                if (event.dataTransfer !== null) {
                    event.dataTransfer.setData('text/plain', card.dataset.kanbanItemId || '');
                    event.dataTransfer.setData('source-column-id', card.parentElement?.parentElement?.dataset.columnId || '');
                    event.dataTransfer.effectAllowed = 'move';
                    card.classList.add('kanban-item-dragging');
                }
            }
            card.removeEventListener('dragstart', cardDragFunction);
            card.addEventListener('dragstart', cardDragFunction);

            const cardDragEndFunction = async function (event: DragEvent) {
                card.classList.remove('kanban-item-dragging');
                const kanbanItemId = card.dataset.kanbanItemId;
                if (kanbanItemId) {
                    // Update the KanbanItem's columnIndex based on the new column it was dropped into.
                    const targetColumn = event.target as HTMLDivElement;
                    const targetColumnId = (targetColumn.closest('.kanban-column') as HTMLDivElement)?.dataset.columnId;
                    const kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
                    const currentKanbanItemColumnIndex = kanbanItem?.columnIndex;
                    if (targetColumnId) {
                        await updateKanbanItemsInColumn(parseInt(targetColumnId));
                        if (currentKanbanItemColumnIndex && targetColumnId !== currentKanbanItemColumnIndex.toString()) {
                            await updateKanbanItemsInColumn(currentKanbanItemColumnIndex);
                        }
                        dispatchTimelineItemChangedEvent(kanbanBoardId.toString());
                    }
                }
            }
            card.removeEventListener('dragend', cardDragEndFunction);
            card.addEventListener('dragend', cardDragEndFunction);
        });
    });
}

async function updateKanbanItemsInColumn(columnId: number): Promise<void> {
    // Get the column HTMLDiv element.
    const columnDiv = document.querySelector<HTMLDivElement>('.kanban-column[data-column-id="' + columnId + '"]');
    if (columnDiv) {
        // Get the list of KanbanItems in the column
        const kanbanItemsInColumn = columnDiv.querySelectorAll<HTMLDivElement>('.kanban-card');
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

function getStatusIconForCard(status: number): string {
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

function createKanbanBoardContainer(kanbanBoard: KanbanBoard) {
    const kanbanColumns: KanbanBoardColumn[] = JSON.parse(kanbanBoard.columns);
    let kanbanBoardHtml = '<div class="kanban-board-container">';
    kanbanColumns.forEach((column) => {
        kanbanBoardHtml += `
                        <div class="kanban-column" data-column-id="${column.id}">
                            <div class="kanban-column-header">
                                <h3>${column.title}</h3>
                            </div>
                            <div class="kanban-column-body" id="kanban-column-body-${column.id}">
                                <!-- Cards will be dynamically added here -->
                            </div>
                            <div class="kanban-column-footer">
                                <button class="btn btn-sm btn-primary add-card-button" data-column-id="${column.id}">Add Card</button>
                            </div>
                        </div>`;
    });
    kanbanBoardHtml += '</div>';

    return kanbanBoardHtml;
}