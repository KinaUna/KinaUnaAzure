import { getCurrentLanguageId } from "../data-tools-v9.js";
import { getTranslation } from "../localization-v9.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9.js";
import { getStatusIconForTodoItems } from "../todos/todo-details.js";
import { getKanbanBoard, getKanbanItems, hideCardMenus, hideColumnMenus, setKanbanItems, updateKanbanItemsInColumn } from "./kanban-board-details.js";
import { displayKanbanItemDetails, getRemoveKanbanItemForm } from "./kanban-items.js";
let draggedCard = null;
let moveUpString = '';
let moveDownString = '';
let moveLeftString = '';
let moveRightString = '';
let removeCardString = '';
let copyToBoardString = '';
let moveToBoardString = '';
export async function loadKanbanItemsTranslations() {
    if (moveUpString === '') {
        moveUpString = await getTranslation('Move up', 'Todos', getCurrentLanguageId());
    }
    if (moveDownString === '') {
        moveDownString = await getTranslation('Move down', 'Todos', getCurrentLanguageId());
    }
    if (moveLeftString === '') {
        moveLeftString = await getTranslation('Move left', 'Todos', getCurrentLanguageId());
    }
    if (moveRightString === '') {
        moveRightString = await getTranslation('Move right', 'Todos', getCurrentLanguageId());
    }
    if (removeCardString === '') {
        removeCardString = await getTranslation('Remove card', 'Todos', getCurrentLanguageId());
    }
    if (copyToBoardString === '') {
        copyToBoardString = await getTranslation('Copy card to...', 'Todos', getCurrentLanguageId());
    }
    if (moveToBoardString === '') {
        moveToBoardString = await getTranslation('Move card to...', 'Todos', getCurrentLanguageId());
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export function createKanbanItemCardHTML(kanbanItem) {
    const cardDiv = document.createElement('div');
    if (!kanbanItem || !kanbanItem.todoItem) {
        return cardDiv;
    }
    let subtasksCountSpanClass = 'd-none';
    if (kanbanItem.todoItem.subtaskCount > 0) {
        subtasksCountSpanClass = 'text-appendage-light ml-2';
    }
    cardDiv.classList.add('kanban-card');
    cardDiv.setAttribute('data-kanban-item-id', kanbanItem.kanbanItemId.toString());
    cardDiv.setAttribute('data-column-id', kanbanItem.columnId.toString());
    cardDiv.innerHTML = `<div class="kanban-card-header">
                                <div>
                                    <img src="${kanbanItem.todoItem.progeny.pictureLink}" class="kanban-card-profile-picture float-right" />

                                    <i class="material-icons float-left">${getStatusIconForTodoItems(kanbanItem.todoItem.status)}</i>
                                </div>
                                <div class="kanban-card-title">
                                    <div class="kanban-card-menu-div d-none float-right" data-kanban-item-id="${kanbanItem.kanbanItemId}">
                                    <button class="kanban-card-menu-button" data-kanban-item-id="${kanbanItem.kanbanItemId}">...</button>
                                </div>
                                    ${kanbanItem.todoItem.title}
                                    <span class="${subtasksCountSpanClass}">[${kanbanItem.todoItem.completedSubtaskCount}/${kanbanItem.todoItem.subtaskCount}]</span>
                                </div>
                                <div class="w-100">
                                    <div class="w-50 float-right">
                                        <div class="kanban-card-menu-content d-none" data-kanban-item-id="${kanbanItem.kanbanItemId}">
                                            <button class="kanban-card-menu-item-button" data-card-menu-action="moveup" data-kanban-item-id="${kanbanItem.kanbanItemId}" ><span class="material-icons kanban-menu-material-icons">move_up</span><span>${moveUpString}</span></button>
                                            <button class="kanban-card-menu-item-button" data-card-menu-action="movedown" data-kanban-item-id="${kanbanItem.kanbanItemId}" ><span class="material-icons kanban-menu-material-icons">move_down</span> ${moveDownString}</button>
                                            <div class="item-menu-divider"></div>
                                            <button class="kanban-card-menu-item-button" data-card-menu-action="moveleft" data-kanban-item-id="${kanbanItem.kanbanItemId}" ><span class="material-icons kanban-menu-material-icons">arrow_back</span> ${moveLeftString}</button>
                                            <button class="kanban-card-menu-item-button" data-card-menu-action="moveright" data-kanban-item-id="${kanbanItem.kanbanItemId}" ><span class="material-icons kanban-menu-material-icons">arrow_forward</span> ${moveRightString}</button>
                                            <div class="item-menu-divider"></div>
                                            <button class="kanban-card-menu-item-button" data-card-menu-action="copytoboard" data-kanban-item-id="${kanbanItem.kanbanItemId}" ><span class="material-icons kanban-menu-material-icons">folder_copy</span> ${copyToBoardString}</button>
                                            <button class="kanban-card-menu-item-button" data-card-menu-action="movetoboard" data-kanban-item-id="${kanbanItem.kanbanItemId}" ><span class="material-icons kanban-menu-material-icons">drive_file_move</span> ${moveToBoardString}</button>
                                            <div class="item-menu-divider"></div>
                                            <button class="kanban-card-menu-item-button" data-card-menu-action="removecard" data-kanban-item-id="${kanbanItem.kanbanItemId}" ><span class="material-icons kanban-menu-material-icons">delete</span> ${removeCardString}</button>
                                        </div>
                                    </div>
                                </div>
                            </div>`;
    return cardDiv;
}
export function addCardEventListeners(kanbanItemId, userCanEdit) {
    const card = document.querySelector('.kanban-card[data-kanban-item-id="' + kanbanItemId + '"]');
    if (!card) {
        return;
    }
    const cardClickFunction = async function (event) {
        event.preventDefault();
        event.stopPropagation();
        const kanbanItemId = card.dataset.kanbanItemId;
        if (kanbanItemId) {
            displayKanbanItemDetails(kanbanItemId, 'kanban-item-details-div');
        }
    };
    card.removeEventListener('click', cardClickFunction);
    card.addEventListener('click', cardClickFunction);
    if (userCanEdit) {
        card.setAttribute('draggable', 'true');
        const cardDragFunction = function (event) {
            event.stopPropagation();
            if (event.dataTransfer !== null) {
                let draggedKanbanItemId = card.dataset.kanbanItemId;
                draggedCard = event.target;
                if (draggedKanbanItemId) {
                    event.dataTransfer.setData('kanban-item-id', draggedKanbanItemId);
                    event.dataTransfer.setData('kanban-item-card', 'kanban-item-card');
                    event.dataTransfer.setData('source-column-id', card.parentElement?.parentElement?.dataset.columnId || '');
                    event.dataTransfer.effectAllowed = 'move';
                    card.classList.add('kanban-item-dragging');
                }
            }
        };
        card.removeEventListener('dragstart', cardDragFunction);
        card.addEventListener('dragstart', cardDragFunction);
        const cardDragEndFunction = async function (event) {
            draggedCard = null;
            card.classList.remove('kanban-item-dragging');
        };
        card.removeEventListener('dragend', cardDragEndFunction);
        card.addEventListener('dragend', cardDragEndFunction);
        const cardMenuDiv = card.querySelector('.kanban-card-menu-div');
        if (cardMenuDiv) {
            cardMenuDiv.classList.remove('d-none');
            const cardMenuButton = card.querySelector('.kanban-card-menu-button');
            if (cardMenuButton) {
                cardMenuButton.removeEventListener('click', showCardMenu);
                cardMenuButton.addEventListener('click', showCardMenu);
            }
        }
    }
}
export function addCardDividerEventListeners(columnId, rowIndex) {
    const dividerElements = document.querySelectorAll('.kanban-card-divider');
    dividerElements.forEach((divider) => {
        if (!divider) {
            return;
        }
        if (divider.dataset.columnId === columnId.toString() && divider.dataset.rowIndex === rowIndex.toString()) {
            const dividerDragEnterFunction = function (event) {
                event.preventDefault();
                event.stopPropagation();
                // Check if event.dataTransfer contains kanban-item-card
                if (!event.dataTransfer || !event.dataTransfer.types.includes('kanban-item-card')) {
                    if (event.dataTransfer) {
                        event.dataTransfer.dropEffect = 'none';
                    }
                    return;
                }
                // Check column WIP limit if the card is being moved to a different column.
                const kanbanBoard = getKanbanBoard();
                const kanbanItems = getKanbanItems();
                let kanbanItemId = '';
                if (draggedCard) {
                    const draggedCardDiv = draggedCard;
                    if (draggedCardDiv) {
                        if (draggedCardDiv.dataset.kanbanItemId) {
                            kanbanItemId = draggedCardDiv.dataset.kanbanItemId;
                        }
                    }
                }
                console.log('kanbanItemId: ' + kanbanItemId);
                const kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
                if (kanbanItem) {
                    const sourceColumnId = kanbanItem.columnId;
                    console.log('sourceColumnId: ' + sourceColumnId);
                    console.log('targetColumnId: ' + columnId);
                    const targetColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId.toString());
                    console.log('targetColumn: ');
                    console.log(targetColumn);
                    if (targetColumn && sourceColumnId) {
                        if (sourceColumnId !== columnId) {
                            const itemsInTargetColumn = kanbanItems.filter(k => k.columnId === targetColumn.id);
                            if (itemsInTargetColumn.length >= targetColumn.wipLimit && targetColumn.wipLimit > 0) {
                                if (event.dataTransfer) {
                                    event.dataTransfer.dropEffect = 'none';
                                }
                                return;
                            }
                        }
                        divider.classList.add('kanban-card-divider-drag-over');
                        if (event.dataTransfer) {
                            event.dataTransfer.dropEffect = 'move';
                            return;
                        }
                    }
                }
                if (event.dataTransfer) {
                    event.dataTransfer.dropEffect = 'none';
                }
            };
            divider.removeEventListener('dragenter', dividerDragEnterFunction);
            divider.addEventListener('dragenter', dividerDragEnterFunction);
            const dividerDragOverFunction = function (event) {
                event.preventDefault();
                event.stopPropagation();
                // Check if event.dataTransfer contains kanban-item-card
                if (!event.dataTransfer || !event.dataTransfer.types.includes('kanban-item-card')) {
                    if (event.dataTransfer) {
                        event.dataTransfer.dropEffect = 'none';
                    }
                    return;
                }
                // Check column WIP limit if the card is being moved to a different column.
                const kanbanBoard = getKanbanBoard();
                const kanbanItems = getKanbanItems();
                let kanbanItemId = event.dataTransfer.getData('kanban-item-id');
                if (draggedCard) {
                    const draggedCardDiv = draggedCard;
                    if (draggedCardDiv) {
                        if (draggedCardDiv.dataset.kanbanItemId) {
                            kanbanItemId = draggedCardDiv.dataset.kanbanItemId;
                        }
                    }
                }
                const kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
                if (kanbanItem) {
                    const sourceColumnId = kanbanItem.columnId;
                    const targetColumn = kanbanBoard.columnsList.find(c => c.id.toString() === columnId.toString());
                    if (targetColumn && sourceColumnId) {
                        if (sourceColumnId !== columnId) {
                            const itemsInTargetColumn = kanbanItems.filter(k => k.columnId === targetColumn.id);
                            if (itemsInTargetColumn.length >= targetColumn.wipLimit && targetColumn.wipLimit > 0) {
                                if (event.dataTransfer) {
                                    event.dataTransfer.dropEffect = 'none';
                                }
                                return;
                            }
                        }
                        divider.classList.add('kanban-card-divider-drag-over');
                        if (event.dataTransfer) {
                            event.dataTransfer.dropEffect = 'move';
                            return;
                        }
                    }
                }
                if (event.dataTransfer) {
                    event.dataTransfer.dropEffect = 'none';
                }
            };
            divider.removeEventListener('dragover', dividerDragOverFunction);
            divider.addEventListener('dragover', dividerDragOverFunction);
            const dividerDragLeaveFunction = function (event) {
                event.preventDefault();
                event.stopPropagation();
                divider.classList.remove('kanban-card-divider-drag-over');
            };
            divider.removeEventListener('dragleave', dividerDragLeaveFunction);
            divider.addEventListener('dragleave', dividerDragLeaveFunction);
            const dividerDropFunction = async function (event) {
                event.preventDefault();
                event.stopPropagation();
                divider.classList.remove('kanban-card-divider-drag-over');
                if (event.dataTransfer) {
                    const kanbanItemId = event.dataTransfer.getData('kanban-item-id');
                    let kanbanItems = getKanbanItems();
                    let kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
                    const targetColumnId = divider.dataset.columnId;
                    const targetRowIndexString = divider.dataset.rowIndex;
                    if (kanbanItemId && targetColumnId && targetRowIndexString) {
                        const targetRowIndex = parseInt(targetRowIndexString);
                        if (kanbanItem) {
                            const previousColumnId = kanbanItem.columnId;
                            const previousRowIndex = kanbanItem.rowIndex;
                            // Check if WipLimit has been reached.
                            if (previousColumnId.toString() !== targetColumnId) {
                                let kanbanBoard = getKanbanBoard();
                                const targetColumn = kanbanBoard.columnsList.find(k => k.id.toString() === targetColumnId);
                                if (targetColumn) {
                                    const itemsInTargetColumn = kanbanItems.filter(k => k.columnId === targetColumn.id);
                                    if (itemsInTargetColumn.length >= targetColumn.wipLimit && targetColumn.wipLimit > 0) {
                                        // Todo: show message that limit has been reached.
                                        return;
                                    }
                                }
                            }
                            else if (targetRowIndex === kanbanItem.rowIndex || targetRowIndex === kanbanItem.rowIndex + 1) {
                                // The divider is right under or over the dragged item, no need to change the order.
                                return;
                            }
                            // Move the item to the target column and set its rowIndex to the target row index.
                            // Reassign rowIndex values for items with row index equal or higher than divider row index in the target column.
                            kanbanItems.forEach((item) => {
                                if (item.kanbanItemId.toString() === kanbanItemId) {
                                    item.columnId = parseInt(targetColumnId);
                                    item.rowIndex = targetRowIndex;
                                }
                                else if (item.columnId.toString() === targetColumnId) {
                                    if (item.rowIndex >= targetRowIndex) {
                                        item.rowIndex = item.rowIndex + 1;
                                    }
                                }
                            });
                            // If the target column is different from the previous column, update row indexes in the previous column as well.
                            if (previousColumnId.toString() !== targetColumnId) {
                                kanbanItems.forEach((item) => {
                                    if (item.columnId === previousColumnId) {
                                        if (item.rowIndex >= previousRowIndex)
                                            item.rowIndex = item.rowIndex - 1;
                                    }
                                });
                            }
                            setKanbanItems(kanbanItems);
                            // Save the updated KanbanItems to the server.
                            if (previousColumnId.toString() !== targetColumnId) {
                                await updateKanbanItemsInColumn(previousColumnId);
                            }
                            await updateKanbanItemsInColumn(parseInt(targetColumnId));
                        }
                    }
                }
                return new Promise(function (resolve, reject) {
                    resolve();
                });
            };
            divider.removeEventListener('drop', dividerDropFunction);
            divider.addEventListener('drop', dividerDropFunction);
        }
    });
}
const showCardMenu = function (event) {
    event.preventDefault();
    event.stopPropagation();
    const button = event.currentTarget;
    const kanbanItemId = button.dataset.kanbanItemId;
    hideCardMenus(kanbanItemId);
    hideColumnMenus();
    if (kanbanItemId) {
        const menuContentDiv = document.querySelector('.kanban-card-menu-content[data-kanban-item-id="' + kanbanItemId + '"]');
        if (menuContentDiv) {
            if (menuContentDiv.classList.contains('d-none')) {
                menuContentDiv.classList.remove('d-none');
                let kanbanItems = getKanbanItems();
                let kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
                const moveUpButton = menuContentDiv.querySelector('button[data-card-menu-action="moveup"]');
                if (moveUpButton) {
                    // If the card is the first in the column, hide the move up button.
                    if (kanbanItem) {
                        const itemsInColumn = kanbanItems.filter(k => k.columnId === kanbanItem.columnId);
                        // Sort by row index
                        itemsInColumn.sort((a, b) => a.rowIndex - b.rowIndex);
                        if (itemsInColumn.length > 0 && itemsInColumn[0].kanbanItemId.toString() === kanbanItemId) {
                            moveUpButton.disabled = true;
                        }
                        else {
                            moveUpButton.disabled = false;
                            const moveCardUpFunction = async function (event) {
                                event.preventDefault();
                                event.stopPropagation();
                                menuContentDiv.classList.add('d-none');
                                await moveCardUp(kanbanItemId);
                                return new Promise(function (resolve, reject) {
                                    resolve();
                                });
                            };
                            moveUpButton.removeEventListener('click', moveCardUpFunction);
                            moveUpButton.addEventListener('click', moveCardUpFunction);
                        }
                    }
                }
                const moveDownButton = menuContentDiv.querySelector('button[data-card-menu-action="movedown"]');
                if (moveDownButton) {
                    // If the card is the last in the column, hide the move down button.
                    if (kanbanItem) {
                        const itemsInColumn = kanbanItems.filter(k => k.columnId === kanbanItem.columnId);
                        // Sort by row index
                        itemsInColumn.sort((a, b) => a.rowIndex - b.rowIndex);
                        if (itemsInColumn.length > 0 && itemsInColumn[itemsInColumn.length - 1].kanbanItemId.toString() === kanbanItemId) {
                            moveDownButton.disabled = true;
                        }
                        else {
                            moveDownButton.disabled = false;
                            const moveCardDownFunction = async function (event) {
                                event.preventDefault();
                                event.stopPropagation();
                                menuContentDiv.classList.add('d-none');
                                await moveCardDown(kanbanItemId);
                                return new Promise(function (resolve, reject) {
                                    resolve();
                                });
                            };
                            moveDownButton.removeEventListener('click', moveCardDownFunction);
                            moveDownButton.addEventListener('click', moveCardDownFunction);
                        }
                    }
                }
                const moveLeftButton = menuContentDiv.querySelector('button[data-card-menu-action="moveleft"]');
                if (moveLeftButton) {
                    // If the card is in the first column, hide the move left button.
                    if (kanbanItem) {
                        const kanbanBoard = getKanbanBoard();
                        const currentColumnIndex = kanbanBoard.columnsList.find(c => c.id === kanbanItem.columnId)?.columnIndex;
                        if (currentColumnIndex !== undefined && currentColumnIndex === 0) {
                            moveLeftButton.disabled = true;
                        }
                        else {
                            moveLeftButton.disabled = false;
                            const moveCardLeftFunction = async function (event) {
                                event.preventDefault();
                                event.stopPropagation();
                                menuContentDiv.classList.add('d-none');
                                await moveCardLeft(kanbanItemId);
                                return new Promise(function (resolve, reject) {
                                    resolve();
                                });
                            };
                            moveLeftButton.removeEventListener('click', moveCardLeftFunction);
                            moveLeftButton.addEventListener('click', moveCardLeftFunction);
                        }
                    }
                }
                const moveRightButton = menuContentDiv.querySelector('button[data-card-menu-action="moveright"]');
                if (moveRightButton) {
                    // If the card is in the last column, hide the move right button.
                    if (kanbanItem) {
                        const kanbanBoard = getKanbanBoard();
                        const currentColumnIndex = kanbanBoard.columnsList.find(c => c.id === kanbanItem.columnId)?.columnIndex;
                        if (currentColumnIndex !== undefined && currentColumnIndex === kanbanBoard.columnsList.length - 1) {
                            moveRightButton.disabled = true;
                        }
                        else {
                            moveRightButton.disabled = false;
                            const moveCardRightFunction = async function (event) {
                                event.preventDefault();
                                event.stopPropagation();
                                menuContentDiv.classList.add('d-none');
                                await moveCardRight(kanbanItemId);
                                return new Promise(function (resolve, reject) {
                                    resolve();
                                });
                            };
                            moveRightButton.removeEventListener('click', moveCardRightFunction);
                            moveRightButton.addEventListener('click', moveCardRightFunction);
                        }
                    }
                }
                const removeCardButton = menuContentDiv.querySelector('button[data-card-menu-action="removecard"]');
                if (removeCardButton) {
                    const removeCardFunction = async function (event) {
                        event.preventDefault();
                        event.stopPropagation();
                        menuContentDiv.classList.add('d-none');
                        await removeCard(kanbanItemId);
                        return new Promise(function (resolve, reject) {
                            resolve();
                        });
                    };
                    removeCardButton.removeEventListener('click', removeCardFunction);
                    removeCardButton.addEventListener('click', removeCardFunction);
                }
                const copyCardToButton = menuContentDiv.querySelector('button[data-card-menu-action="copytoboard"]');
                if (copyCardToButton) {
                    const copyCardToFunction = async function (event) {
                        event.preventDefault();
                        event.stopPropagation();
                        menuContentDiv.classList.add('d-none');
                        await copyCardToBoard(kanbanItemId);
                        return new Promise(function (resolve, reject) {
                            resolve();
                        });
                    };
                    copyCardToButton.removeEventListener('click', copyCardToFunction);
                    copyCardToButton.addEventListener('click', copyCardToFunction);
                }
                const moveCardToButton = menuContentDiv.querySelector('button[data-card-menu-action="movetoboard"]');
                if (moveCardToButton) {
                    const moveCardToFunction = async function (event) {
                        event.preventDefault();
                        event.stopPropagation();
                        menuContentDiv.classList.add('d-none');
                        await moveCardToBoard(kanbanItemId);
                        return new Promise(function (resolve, reject) {
                            resolve();
                        });
                    };
                    moveCardToButton.removeEventListener('click', moveCardToFunction);
                    moveCardToButton.addEventListener('click', moveCardToFunction);
                }
            }
            else {
                menuContentDiv.classList.add('d-none');
            }
        }
    }
};
async function moveCardUp(kanbanItemId) {
    let kanbanItems = getKanbanItems();
    let kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
    if (kanbanItem) {
        if (kanbanItem.rowIndex > 0) {
            // Find the item above and swap rowIndex values.
            const kanbanItemAbove = kanbanItems.find(k => k.columnId === kanbanItem.columnId && k.rowIndex === kanbanItem.rowIndex - 1);
            if (kanbanItemAbove) {
                kanbanItemAbove.rowIndex = kanbanItem.rowIndex;
                kanbanItem.rowIndex = kanbanItem.rowIndex - 1;
                setKanbanItems(kanbanItems);
                // Save the updated KanbanItems to the server.
                await updateKanbanItemsInColumn(kanbanItem.columnId);
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function moveCardDown(kanbanItemId) {
    let kanbanItems = getKanbanItems();
    let kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
    if (kanbanItem) {
        const kanbanItemsInColumn = kanbanItems.filter(k => k.columnId === kanbanItem.columnId);
        if (kanbanItem.rowIndex < kanbanItemsInColumn.length - 1) {
            // Find the item below and swap rowIndex values.
            const kanbanItemBelow = kanbanItems.find(k => k.columnId === kanbanItem.columnId && k.rowIndex === kanbanItem.rowIndex + 1);
            if (kanbanItemBelow) {
                kanbanItemBelow.rowIndex = kanbanItem.rowIndex;
                kanbanItem.rowIndex = kanbanItem.rowIndex + 1;
                setKanbanItems(kanbanItems);
                // Save the updated KanbanItems to the server.
                await updateKanbanItemsInColumn(kanbanItem.columnId);
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function moveCardRight(kanbanItemId) {
    let kanbanBoard = getKanbanBoard();
    let kanbanItems = getKanbanItems();
    let kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
    if (kanbanItem) {
        const currentColumnIndex = kanbanBoard.columnsList.find(c => c.id === kanbanItem.columnId)?.columnIndex;
        if (currentColumnIndex !== undefined && currentColumnIndex < kanbanBoard.columnsList.length - 1) {
            // Find the column to the right.
            const rightColumn = kanbanBoard.columnsList.find(c => c.columnIndex === currentColumnIndex + 1);
            if (rightColumn) {
                const kanbanItemsInRightColumn = kanbanItems.filter(k => k.columnId === rightColumn.id);
                // Check if limit has been reached.
                if (kanbanItemsInRightColumn.length >= rightColumn.wipLimit && rightColumn.wipLimit > 0) {
                    alert('Cannot move card. The limit for the column "' + rightColumn.title + '" has been reached.');
                    return new Promise(function (resolve, reject) {
                        resolve();
                    });
                }
                const previousColumnId = kanbanItem.columnId;
                // Move the item to the right column and set its rowIndex to the end of the column.
                kanbanItem.columnId = rightColumn.id;
                kanbanItem.rowIndex = kanbanItemsInRightColumn.length;
                // Reassign rowIndex values for all items in the current column.
                const itemsInCurrentColumn = kanbanItems.filter(k => k.columnId === kanbanItem.columnId);
                itemsInCurrentColumn.forEach((item, index) => {
                    item.rowIndex = index;
                });
                setKanbanItems(kanbanItems);
                // Save the updated KanbanItems to the server.
                await updateKanbanItemsInColumn(kanbanItem.columnId);
                await updateKanbanItemsInColumn(previousColumnId);
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function moveCardLeft(kanbanItemId) {
    let kanbanBoard = getKanbanBoard();
    let kanbanItems = getKanbanItems();
    let kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
    if (kanbanItem) {
        const currentColumnIndex = kanbanBoard.columnsList.find(c => c.id === kanbanItem.columnId)?.columnIndex;
        if (currentColumnIndex !== undefined && currentColumnIndex > 0) {
            // Find the column to the left.
            const leftColumn = kanbanBoard.columnsList.find(c => c.columnIndex === currentColumnIndex - 1);
            if (leftColumn) {
                const kanbanItemsInLeftColumn = kanbanItems.filter(k => k.columnId === leftColumn.id);
                // Check if limit has been reached.
                if (kanbanItemsInLeftColumn.length >= leftColumn.wipLimit && leftColumn.wipLimit > 0) {
                    alert('Cannot move card. The limit for the column "' + leftColumn.title + '" has been reached.');
                    return new Promise(function (resolve, reject) {
                        resolve();
                    });
                }
                const previousColumnId = kanbanItem.columnId;
                // Move the item to the left column and set its rowIndex to the end of the column.
                kanbanItem.columnId = leftColumn.id;
                kanbanItem.rowIndex = kanbanItemsInLeftColumn.length;
                // Reassign rowIndex values for all items in the current column.
                const itemsInCurrentColumn = kanbanItems.filter(k => k.columnId === kanbanItem.columnId);
                itemsInCurrentColumn.forEach((item, index) => {
                    item.rowIndex = index;
                });
                setKanbanItems(kanbanItems);
                // Save the updated KanbanItems to the server.
                await updateKanbanItemsInColumn(kanbanItem.columnId);
                await updateKanbanItemsInColumn(previousColumnId);
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function removeCard(kanbanItemId) {
    let kanbanItems = getKanbanItems();
    let kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
    if (kanbanItem) {
        const removeCardHtml = await getRemoveKanbanItemForm(kanbanItemId);
        const removeCardModalDiv = document.querySelector('#kanban-item-details-div');
        if (removeCardModalDiv) {
            removeCardModalDiv.innerHTML = removeCardHtml;
            removeCardModalDiv.classList.remove('d-none');
            const cancelButton = removeCardModalDiv.querySelector('.remove-kanban-item-cancel-button');
            if (cancelButton) {
                const closeButtonFunction = function () {
                    removeCardModalDiv.innerHTML = '';
                    removeCardModalDiv.classList.add('d-none');
                };
                cancelButton.removeEventListener('click', closeButtonFunction);
                cancelButton.addEventListener('click', closeButtonFunction);
                const closeButton = removeCardModalDiv.querySelector('.modal-close-button');
                if (closeButton) {
                    closeButton.removeEventListener('click', closeButtonFunction);
                    closeButton.addEventListener('click', closeButtonFunction);
                }
            }
            const removeKanbanItemForm = removeCardModalDiv.querySelector('#remove-kanban-item-form');
            if (removeKanbanItemForm) {
                const removeKanbanItemFormFunction = async function (event) {
                    event.preventDefault();
                    startFullPageSpinner();
                    const formData = new FormData(removeKanbanItemForm);
                    const url = '/KanbanItems/RemoveKanbanItem';
                    await fetch(url, {
                        method: 'POST',
                        body: formData
                    }).then(async function (response) {
                        if (response.ok) {
                            // Successfully removed the KanbanItem. Re-render the KanbanBoard.
                            removeCardModalDiv.innerHTML = '';
                            removeCardModalDiv.classList.add('d-none');
                            const kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
                            if (kanbanItem) {
                                const columnId = kanbanItem.columnId;
                                // Remove the item from the kanbanItems array.
                                kanbanItems = kanbanItems.filter(k => k.kanbanItemId.toString() !== kanbanItemId);
                                // Reassign rowIndex values for all items in the column.
                                const itemsInColumn = kanbanItems.filter(k => k.columnId === columnId);
                                // Sort by row index
                                itemsInColumn.sort((a, b) => a.rowIndex - b.rowIndex);
                                itemsInColumn.forEach((item, index) => {
                                    item.rowIndex = index;
                                });
                                setKanbanItems(kanbanItems);
                                // Save the updated KanbanItems to the server.
                                await updateKanbanItemsInColumn(columnId);
                            }
                        }
                        else {
                            console.error('Error removing kanban item. Status: ' + response.status);
                        }
                    }).catch(function (error) {
                        console.error('Error removing kanban item: ' + error);
                    });
                    stopFullPageSpinner();
                };
                removeKanbanItemForm.removeEventListener('submit', removeKanbanItemFormFunction);
                removeKanbanItemForm.addEventListener('submit', removeKanbanItemFormFunction);
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function copyCardToBoard(kanbanItemId) {
    // Get form html from server.
    let url = '/KanbanItems/CopyKanbanItemToKanbanBoard?kanbanItemId=' + kanbanItemId;
    const response = await fetch(url);
    if (response.ok) {
        const formHtml = await response.text();
        const modalDiv = document.querySelector('#kanban-item-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = formHtml;
            modalDiv.classList.remove('d-none');
            const cancelButton = modalDiv.querySelector('.copy-kanban-item-cancel-button');
            if (cancelButton) {
                const closeButtonFunction = function () {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                };
                cancelButton.removeEventListener('click', closeButtonFunction);
                cancelButton.addEventListener('click', closeButtonFunction);
                const closeButton = modalDiv.querySelector('.modal-close-button');
                if (closeButton) {
                    closeButton.removeEventListener('click', closeButtonFunction);
                    closeButton.addEventListener('click', closeButtonFunction);
                }
            }
            $(".selectpicker").selectpicker('refresh');
            const copyKanbanItemForm = modalDiv.querySelector('#copy-kanban-item-to-kanban-board-form');
            if (copyKanbanItemForm) {
                const copyKanbanItemFormFunction = async function (event) {
                    event.preventDefault();
                    startFullPageSpinner();
                    const formData = new FormData(copyKanbanItemForm);
                    const url = '/KanbanItems/CopyKanbanItemToKanbanBoard';
                    await fetch(url, {
                        method: 'POST',
                        body: formData
                    }).then(async function (response) {
                        if (response.ok) {
                            // Successfully copied the KanbanItem. Close the modal.
                            modalDiv.innerHTML = '';
                            modalDiv.classList.add('d-none');
                        }
                        else {
                            console.error('Error copying kanban item. Status: ' + response.status);
                        }
                    }).catch(function (error) {
                        console.error('Error copying kanban item: ' + error);
                    });
                    stopFullPageSpinner();
                };
                copyKanbanItemForm.removeEventListener('submit', copyKanbanItemFormFunction);
                copyKanbanItemForm.addEventListener('submit', copyKanbanItemFormFunction);
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function moveCardToBoard(kanbanItemId) {
    // Get form html from server.
    let url = '/KanbanItems/MoveKanbanItemToKanbanBoard?kanbanItemId=' + kanbanItemId;
    const response = await fetch(url);
    if (response.ok) {
        const formHtml = await response.text();
        const modalDiv = document.querySelector('#kanban-item-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = formHtml;
            modalDiv.classList.remove('d-none');
            const cancelButton = modalDiv.querySelector('.move-kanban-item-cancel-button');
            if (cancelButton) {
                const closeButtonFunction = function () {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                };
                cancelButton.removeEventListener('click', closeButtonFunction);
                cancelButton.addEventListener('click', closeButtonFunction);
                const closeButton = modalDiv.querySelector('.modal-close-button');
                if (closeButton) {
                    closeButton.removeEventListener('click', closeButtonFunction);
                    closeButton.addEventListener('click', closeButtonFunction);
                }
            }
            $(".selectpicker").selectpicker('refresh');
            const moveKanbanItemForm = modalDiv.querySelector('#move-kanban-item-to-kanban-board-form');
            if (moveKanbanItemForm) {
                const moveKanbanItemFormFunction = async function (event) {
                    event.preventDefault();
                    startFullPageSpinner();
                    const formData = new FormData(moveKanbanItemForm);
                    const url = '/KanbanItems/MoveKanbanItemToKanbanBoard';
                    await fetch(url, {
                        method: 'POST',
                        body: formData
                    }).then(async function (response) {
                        if (response.ok) {
                            // Successfully moved the KanbanItem. Close the modal.
                            modalDiv.innerHTML = '';
                            modalDiv.classList.add('d-none');
                            // Remove the item from the kanbanItems array.
                            let kanbanItems = getKanbanItems();
                            let kanbanItem = kanbanItems.find(k => k.kanbanItemId.toString() === kanbanItemId);
                            if (kanbanItem) {
                                const columnId = kanbanItem.columnId;
                                kanbanItems = kanbanItems.filter(k => k.kanbanItemId.toString() !== kanbanItemId);
                                // Reassign rowIndex values for all items in the column.
                                const itemsInColumn = kanbanItems.filter(k => k.columnId === columnId);
                                // Sort by row index
                                itemsInColumn.sort((a, b) => a.rowIndex - b.rowIndex);
                                itemsInColumn.forEach((item, index) => {
                                    item.rowIndex = index;
                                });
                                setKanbanItems(kanbanItems);
                                // Save the updated KanbanItems to the server.
                                await updateKanbanItemsInColumn(columnId);
                            }
                        }
                        else {
                            console.error('Error moving kanban item. Status: ' + response.status);
                        }
                    }).catch(function (error) {
                        console.error('Error moving kanban item: ' + error);
                    });
                    stopFullPageSpinner();
                };
                moveKanbanItemForm.removeEventListener('submit', moveKanbanItemFormFunction);
                moveKanbanItemForm.addEventListener('submit', moveKanbanItemFormFunction);
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=kanban-cards.js.map