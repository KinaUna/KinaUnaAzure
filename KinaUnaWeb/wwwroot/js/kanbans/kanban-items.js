import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item.js";
import { TodoStatusType } from "../page-models-v9.js";
import { addSubtask } from "../todos/subtasks.js";
import { getSubtaskList, popupTodoItem } from "../todos/todo-details.js";
import { initializeAddEditKanbanItem } from "./add-edit-kanban-item.js";
import { getKanbanItems, setKanbanItems, updateKanbanItemsInColumn } from "./kanban-board-details.js";
let popupContainerId = '';
let popupKanbanItemObject;
export async function getKanbanItemsForBoard(kanbanBoardId) {
    let kanbanItems = [];
    const url = '/KanbanItems/GetKanbanItemsForBoard?kanbanBoardId=' + kanbanBoardId;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            kanbanItems = await response.json();
        }
        else {
            console.error('Error fetching kanban items for board. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching kanban items for board: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve(kanbanItems);
    });
}
;
async function popupKanbanItem(kanbanItem, containerId) {
    if (!kanbanItem || !kanbanItem.todoItem) {
        console.error('Invalid kanban item or missing todo item.');
        return;
    }
    popupKanbanItemObject = kanbanItem;
    let url = '/KanbanItems/KanbanItemDetails?kanbanItemId=' + kanbanItem.kanbanItemId;
    let kanbanItemHtml = '';
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'text/html',
            'Content-Type': 'text/html'
        },
    }).then(async function (response) {
        if (response.ok) {
            kanbanItemHtml = await response.text();
        }
        else {
            console.error('Error fetching kanban item details. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching kanban item details: ' + error);
    });
    const containerElement = document.getElementById(containerId);
    if (containerElement) {
        popupContainerId = containerId;
        containerElement.innerHTML = kanbanItemHtml;
        setKanbanItemDetailsEventListeners(kanbanItem.todoItemId.toString(), containerId);
        setEditItemButtonEventListeners();
        setDeleteItemButtonEventListeners();
        await getSubtaskList(kanbanItem.todoItemId.toString());
        containerElement.classList.remove('d-none');
    }
    else {
        console.error('Container element not found: ' + containerId);
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
function setKanbanItemDetailsEventListeners(itemId, todoDetailsPopupDivId) {
    let closeButtonsList = document.querySelectorAll('.kanban-item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function (event) {
                event.preventDefault();
                event.stopPropagation();
                const todoDetailsPopupDiv = document.getElementById(todoDetailsPopupDivId);
                if (!todoDetailsPopupDiv)
                    return;
                todoDetailsPopupDiv.innerHTML = '';
                todoDetailsPopupDiv.classList.add('d-none');
            };
            // Clear existing event listeners.
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }
    const todoElementsWithDataCompletedId = document.querySelectorAll('[data-set-todo-item-completed-id="' + itemId + '"]');
    if (todoElementsWithDataCompletedId) {
        todoElementsWithDataCompletedId.forEach((element) => {
            // Clear existing event listeners.
            element.removeEventListener('click', onSetAsCompletedButtonClicked);
            element.addEventListener('click', onSetAsCompletedButtonClicked);
        });
    }
    const todoElementsWithDataCancelledId = document.querySelectorAll('[data-set-todo-item-cancelled-id="' + itemId + '"]');
    if (todoElementsWithDataCancelledId) {
        todoElementsWithDataCancelledId.forEach((element) => {
            // Clear existing event listeners.
            element.removeEventListener('click', onSetAsCancelledButtonClicked);
            element.addEventListener('click', onSetAsCancelledButtonClicked);
        });
    }
    const todoElementsWithDataInProgressId = document.querySelectorAll('[data-set-todo-item-in-progress-id="' + itemId + '"]');
    if (todoElementsWithDataInProgressId) {
        todoElementsWithDataInProgressId.forEach((element) => {
            // Clear existing event listeners.
            element.removeEventListener('click', onSetAsInProgressButtonClicked);
            element.addEventListener('click', onSetAsInProgressButtonClicked);
        });
    }
    const todoElementsWithDataNotStartedId = document.querySelectorAll('[data-set-todo-item-not-started-id="' + itemId + '"]');
    if (todoElementsWithDataNotStartedId) {
        todoElementsWithDataNotStartedId.forEach((element) => {
            // Clear existing event listeners.
            element.removeEventListener('click', onSetAsNotStartedButtonClicked);
            element.addEventListener('click', onSetAsNotStartedButtonClicked);
        });
    }
    const addSubtaskForm = document.querySelector('#add-subtask-inline-form');
    if (addSubtaskForm) {
        const subtaskFormPreventDefaultAction = function (event) {
            event.preventDefault();
        };
        // Prevent form submission from reloading the page.
        // Clear existing event listeners.
        addSubtaskForm.removeEventListener('submit', subtaskFormPreventDefaultAction);
        addSubtaskForm.addEventListener('submit', subtaskFormPreventDefaultAction);
    }
    const addSubtaskInput = document.querySelector('#todo-details-add-subtask-title-input');
    const addSubtaskButton = document.querySelector('#todo-details-add-subtask-button');
    const subtasksListDivId = 'todo-details-sub-tasks-list-div';
    if (addSubtaskButton) {
        const addSubTaskButtonClickedAction = async function (event) {
            if (addSubtaskInput && addSubtaskInput.value.trim() !== '') {
                event.preventDefault();
                const subtaskTitle = addSubtaskInput.value.trim();
                await addSubtask(null, subtasksListDivId);
                addSubtaskInput.value = '';
            }
            ;
        };
        // Clear existing event listeners.
        addSubtaskButton.removeEventListener('click', addSubTaskButtonClickedAction);
        addSubtaskButton.addEventListener('click', addSubTaskButtonClickedAction);
    }
    if (addSubtaskInput) {
        const addSubtaskInputKeydownAction = async function (event) {
            if (event.key === 'Enter' && addSubtaskInput.value.trim() !== '') {
                event.preventDefault();
                await addSubtask(null, subtasksListDivId);
                addSubtaskInput.value = '';
                // Focus back to the input field after adding the subtask
                addSubtaskInput.focus();
            }
            ;
        };
        // Clear existing event listeners.
        addSubtaskInput.removeEventListener('keydown', addSubtaskInputKeydownAction);
        addSubtaskInput.addEventListener('keydown', addSubtaskInputKeydownAction);
    }
}
/**
 * Handles the click event for the "Set as Not Started" button.
 * When clicked, it sends a request to set the todo item as not started.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsNotStartedButtonClicked(event) {
    event.preventDefault();
    event.stopPropagation();
    const buttonElement = event.currentTarget;
    if (buttonElement !== null) {
        const todoId = buttonElement.dataset.setTodoItemNotStartedId;
        if (todoId) {
            let url = '/Todos/SetTodoAsNotStarted?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    await popupKanbanItem(popupKanbanItemObject, popupContainerId);
                    let kanbanItems = getKanbanItems();
                    let kanbanItem = kanbanItems.find(k => k.kanbanItemId === popupKanbanItemObject.kanbanItemId);
                    if (kanbanItem && kanbanItem.todoItem) {
                        kanbanItem.todoItem.status = TodoStatusType.NotStarted;
                    }
                    setKanbanItems(kanbanItems);
                    await updateKanbanItemsInColumn(popupKanbanItemObject.columnId);
                    return new Promise(function (resolve, reject) {
                        resolve();
                    });
                }
                else {
                    console.error('Error setting todo as not started. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as not started. Error: ' + error);
            });
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Handles the click event for the "Set as In Progress" button.
 * When clicked, it sends a request to set the todo item as in progress.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsInProgressButtonClicked(event) {
    event.preventDefault();
    event.stopPropagation();
    const buttonElement = event.currentTarget;
    if (buttonElement !== null) {
        const todoId = buttonElement.dataset.setTodoItemInProgressId;
        if (todoId) {
            let url = '/Todos/SetTodoAsInProgress?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    await popupKanbanItem(popupKanbanItemObject, popupContainerId);
                    let kanbanItems = getKanbanItems();
                    let kanbanItem = kanbanItems.find(k => k.kanbanItemId === popupKanbanItemObject.kanbanItemId);
                    if (kanbanItem && kanbanItem.todoItem) {
                        kanbanItem.todoItem.status = TodoStatusType.InProgress;
                    }
                    setKanbanItems(kanbanItems);
                    await updateKanbanItemsInColumn(popupKanbanItemObject.columnId);
                    return new Promise(function (resolve, reject) {
                        resolve();
                    });
                }
                else {
                    console.error('Error setting todo as in progress. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as in progress. Error: ' + error);
            });
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Handles the click event for the "Set as Done" button.
 * When clicked, it sends a request to set the todo item as done.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsCompletedButtonClicked(event) {
    event.preventDefault();
    event.stopPropagation();
    const buttonElement = event.currentTarget;
    if (buttonElement !== null) {
        const todoId = buttonElement.dataset.setTodoItemCompletedId;
        if (todoId) {
            let url = '/Todos/SetTodoAsCompleted?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    await popupKanbanItem(popupKanbanItemObject, popupContainerId);
                    let kanbanItems = getKanbanItems();
                    let kanbanItem = kanbanItems.find(k => k.kanbanItemId === popupKanbanItemObject.kanbanItemId);
                    if (kanbanItem && kanbanItem.todoItem) {
                        kanbanItem.todoItem.status = TodoStatusType.Completed;
                    }
                    setKanbanItems(kanbanItems);
                    await updateKanbanItemsInColumn(popupKanbanItemObject.columnId);
                    return new Promise(function (resolve, reject) {
                        resolve();
                    });
                }
                else {
                    console.error('Error setting todo as completed. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as completed. Error: ' + error);
            });
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Handles the click event for the "Set as Cancelled" button.
 * When clicked, it sends a request to set the todo item as cancelled.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsCancelledButtonClicked(event) {
    event.preventDefault();
    event.stopPropagation();
    const buttonElement = event.currentTarget;
    if (buttonElement !== null) {
        const todoId = buttonElement.dataset.setTodoItemCancelledId;
        if (todoId) {
            let url = '/Todos/SetTodoAsCancelled?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    await popupKanbanItem(popupKanbanItemObject, popupContainerId);
                    let kanbanItems = getKanbanItems();
                    let kanbanItem = kanbanItems.find(k => k.kanbanItemId === popupKanbanItemObject.kanbanItemId);
                    if (kanbanItem && kanbanItem.todoItem) {
                        kanbanItem.todoItem.status = TodoStatusType.Cancelled;
                    }
                    setKanbanItems(kanbanItems);
                    await updateKanbanItemsInColumn(popupKanbanItemObject.columnId);
                    return new Promise(function (resolve, reject) {
                        resolve();
                    });
                }
                else {
                    console.error('Error setting todo as cancelled. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as cancelled. Error: ' + error);
            });
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function displayKanbanItemDetails(kanbanItemId, container) {
    console.log('displayKanbanItemDetails, container:' + container);
    let kanbanItem;
    const url = '/KanbanItems/GetKanbanItem?kanbanItemId=' + kanbanItemId;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            kanbanItem = await response.json();
            if (kanbanItem !== null && kanbanItem.todoItemId !== 0) {
                if (container === '') {
                    await popupTodoItem(kanbanItem.todoItemId.toString());
                }
                else {
                    await popupKanbanItem(kanbanItem, container);
                }
            }
        }
        else {
            console.error('Error fetching kanban item details. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching kanban item details: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function updateKanbanItem(kanbanItem) {
    let success = false;
    const url = '/KanbanItems/UpdateKanbanItem';
    await fetch(url, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(kanbanItem)
    }).then(async function (response) {
        if (response.ok) {
            success = true;
        }
        else {
            console.error('Error updating kanban item. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error updating kanban item: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve(success);
    });
}
export async function getAddKanbanItemForm(kanbanBoardId, columnId, rowIndex) {
    let formHtml = '';
    const url = '/KanbanItems/AddKanbanItem?kanbanBoardId=' + kanbanBoardId + '&columnId=' + columnId + '&rowIndex=' + rowIndex;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            formHtml = await response.text();
        }
        else {
            console.error('Error fetching add kanban item form. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching add kanban item form: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve(formHtml);
    });
}
export async function getEditKanbanItemForm(kanbanItemId) {
    let formHtml = '';
    const url = '/KanbanItems/EditKanbanItem?kanbanItemId=' + kanbanItemId;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            formHtml = await response.text();
        }
        else {
            console.error('Error fetching add kanban item form. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching add kanban item form: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve(formHtml);
    });
}
export async function getRemoveKanbanItemForm(kanbanItemId) {
    let formHtml = '';
    const url = '/KanbanItems/RemoveKanbanItem?kanbanItemId=' + kanbanItemId;
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            formHtml = await response.text();
        }
        else {
            console.error('Error fetching add kanban item form. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching add kanban item form: ' + error);
    });
    return new Promise(function (resolve, reject) {
        resolve(formHtml);
    });
}
export async function removeKanbanItemFunction(kanbanItemId) {
    const removeKanbanItemModalDiv = document.querySelector('#kanban-item-details-div');
    if (removeKanbanItemModalDiv) {
        removeKanbanItemModalDiv.innerHTML = '';
        const formHtml = await getRemoveKanbanItemForm(kanbanItemId);
        removeKanbanItemModalDiv.innerHTML = formHtml;
        removeKanbanItemModalDiv.classList.remove('d-none');
        const cancelButton = removeKanbanItemModalDiv.querySelector('.remove-kanban-item-cancel-button');
        if (cancelButton) {
            const closeButtonFunction = function () {
                removeKanbanItemModalDiv.innerHTML = '';
                removeKanbanItemModalDiv.classList.add('d-none');
            };
            cancelButton.removeEventListener('click', closeButtonFunction);
            cancelButton.addEventListener('click', closeButtonFunction);
            const closeButton = removeKanbanItemModalDiv.querySelector('.modal-close-button');
            if (closeButton) {
                closeButton.removeEventListener('click', closeButtonFunction);
                closeButton.addEventListener('click', closeButtonFunction);
            }
        }
        const removeKanbanItemForm = removeKanbanItemModalDiv.querySelector('#remove-kanban-card-form');
        if (removeKanbanItemForm) {
            const removeKanbanItemFormFunction = async function (event) {
                event.preventDefault();
                const formData = new FormData(removeKanbanItemForm);
                const url = '/KanbanItems/RemoveKanbanItem';
                await fetch(url, {
                    method: 'POST',
                    body: formData
                }).then(async function (response) {
                    if (response.ok) {
                        // Successfully saved the KanbanItem. Re-render the KanbanBoard.
                        removeKanbanItemModalDiv.innerHTML = '';
                        removeKanbanItemModalDiv.classList.add('d-none');
                        const removedKanbanItem = await response.json();
                        if (removedKanbanItem) {
                            let kanbanItems = getKanbanItems();
                            // remove the item from kanbanItems array
                            kanbanItems = kanbanItems.filter(k => k.kanbanItemId.toString() !== kanbanItemId);
                            setKanbanItems(kanbanItems);
                            await updateKanbanItemsInColumn(removedKanbanItem.columnId);
                        }
                    }
                    else {
                        console.error('Error removing kanban item. Status: ' + response.status);
                    }
                }).catch(function (error) {
                    console.error('Error removing kanban item: ' + error);
                });
            };
            removeKanbanItemForm.removeEventListener('submit', removeKanbanItemFormFunction);
            removeKanbanItemForm.addEventListener('submit', removeKanbanItemFormFunction);
            initializeAddEditKanbanItem('kanban-item-details-div');
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function editKanbanItemFunction(kanbanItemId) {
    const editKanbanItemModalDiv = document.querySelector('#kanban-item-details-div');
    if (editKanbanItemModalDiv) {
        editKanbanItemModalDiv.innerHTML = '';
        const formHtml = await getEditKanbanItemForm(kanbanItemId);
        editKanbanItemModalDiv.innerHTML = formHtml;
        editKanbanItemModalDiv.classList.remove('d-none');
        const cancelButton = editKanbanItemModalDiv.querySelector('.edit-kanban-item-cancel-button');
        if (cancelButton) {
            const closeButtonFunction = function () {
                editKanbanItemModalDiv.innerHTML = '';
                editKanbanItemModalDiv.classList.add('d-none');
            };
            cancelButton.removeEventListener('click', closeButtonFunction);
            cancelButton.addEventListener('click', closeButtonFunction);
            const closeButton = editKanbanItemModalDiv.querySelector('.modal-close-button');
            if (closeButton) {
                closeButton.removeEventListener('click', closeButtonFunction);
                closeButton.addEventListener('click', closeButtonFunction);
            }
        }
        const editKanbanItemForm = editKanbanItemModalDiv.querySelector('#save-kanban-card-form');
        if (editKanbanItemForm) {
            const editKanbanItemFormFunction = async function (event) {
                event.preventDefault();
                const formData = new FormData(editKanbanItemForm);
                const url = '/KanbanItems/EditKanbanItem';
                await fetch(url, {
                    method: 'POST',
                    body: formData
                }).then(async function (response) {
                    if (response.ok) {
                        // Successfully saved the KanbanItem. Re-render the KanbanBoard.
                        editKanbanItemModalDiv.innerHTML = '';
                        editKanbanItemModalDiv.classList.add('d-none');
                        const updatedKanbanItem = await response.json();
                        if (updatedKanbanItem) {
                            let kanbanItems = getKanbanItems();
                            // update the item in kanbanItems array
                            const index = kanbanItems.findIndex(k => k.kanbanItemId === updatedKanbanItem.kanbanItemId);
                            if (index !== -1) {
                                kanbanItems[index] = updatedKanbanItem;
                                setKanbanItems(kanbanItems);
                                await updateKanbanItemsInColumn(updatedKanbanItem.columnId);
                            }
                        }
                    }
                    else {
                        console.error('Error editing kanban item. Status: ' + response.status);
                    }
                }).catch(function (error) {
                    console.error('Error editing kanban item: ' + error);
                });
            };
            editKanbanItemForm.removeEventListener('submit', editKanbanItemFormFunction);
            editKanbanItemForm.addEventListener('submit', editKanbanItemFormFunction);
            initializeAddEditKanbanItem('kanban-item-details-div');
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
//# sourceMappingURL=kanban-items.js.map