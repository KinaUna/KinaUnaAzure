import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from "../addItem/add-item.js";
import { getCurrentLanguageId } from "../data-tools-v9.js";
import { getTranslation } from "../localization-v9.js";
import { startFullPageSpinner, stopFullPageSpinner } from "../navigation-tools-v9.js";
import { KanbanItem, TodoStatusType } from "../page-models-v9.js";
import { addSubtask } from "../todos/subtasks.js";
import { getStatusIconForTodoItems, getSubtaskList, popupTodoItem } from "../todos/todo-details.js";
import { initializeAddEditKanbanItem } from "./add-edit-kanban-item.js";
import { dispatchKanbanBoardChangedEvent, getKanbanItems, setKanbanItems, updateKanbanItemsInColumn } from "./kanban-board-details.js";

let popupContainerId = '';
let popupKanbanItemObject: KanbanItem;

export async function getKanbanItemsForBoard(kanbanBoardId: number): Promise<KanbanItem[]> {
    let kanbanItems: KanbanItem[] = [];
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
        } else {
            console.error('Error fetching kanban items for board. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching kanban items for board: ' + error);
    });

    return new Promise<KanbanItem[]>(function (resolve, reject) {
        resolve(kanbanItems);
    });
};

async function popupKanbanItem(kanbanItem: KanbanItem, containerId: string): Promise<void> {
    if (!kanbanItem || !kanbanItem.todoItem) {
        console.error('Invalid kanban item or missing todo item.');
        return;
    }

    startFullPageSpinner();
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
        } else {
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
        await getSubtaskList(kanbanItem.todoItemId.toString(), true);
        containerElement.classList.remove('d-none');
    } else {
        console.error('Container element not found: ' + containerId);
    }
    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function setKanbanItemDetailsEventListeners(itemId: string, todoDetailsPopupDivId: string): void {
    let closeButtonsList = document.querySelectorAll<HTMLButtonElement>('.kanban-item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function (event: MouseEvent) {
                event.preventDefault();
                event.stopPropagation();
                const todoDetailsPopupDiv = document.getElementById(todoDetailsPopupDivId);
                if (!todoDetailsPopupDiv) return;
                todoDetailsPopupDiv.innerHTML = '';
                todoDetailsPopupDiv.classList.add('d-none');
            }
            // Clear existing event listeners.
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }

    const todoElementsWithDataCompletedId = document.querySelectorAll<HTMLButtonElement>('[data-set-todo-item-completed-id="' + itemId + '"]');
    if (todoElementsWithDataCompletedId) {
        todoElementsWithDataCompletedId.forEach((element) => {
            // Clear existing event listeners.
            element.removeEventListener('click', onSetAsCompletedButtonClicked);
            element.addEventListener('click', onSetAsCompletedButtonClicked);
        });
    }

    const todoElementsWithDataCancelledId = document.querySelectorAll<HTMLButtonElement>('[data-set-todo-item-cancelled-id="' + itemId + '"]');
    if (todoElementsWithDataCancelledId) {
        todoElementsWithDataCancelledId.forEach((element) => {
            // Clear existing event listeners.
            element.removeEventListener('click', onSetAsCancelledButtonClicked);
            element.addEventListener('click', onSetAsCancelledButtonClicked);
        });
    }

    const todoElementsWithDataInProgressId = document.querySelectorAll<HTMLButtonElement>('[data-set-todo-item-in-progress-id="' + itemId + '"]');
    if (todoElementsWithDataInProgressId) {
        todoElementsWithDataInProgressId.forEach((element) => {
            // Clear existing event listeners.
            element.removeEventListener('click', onSetAsInProgressButtonClicked);
            element.addEventListener('click', onSetAsInProgressButtonClicked);
        });
    }

    const todoElementsWithDataNotStartedId = document.querySelectorAll<HTMLButtonElement>('[data-set-todo-item-not-started-id="' + itemId + '"]');
    if (todoElementsWithDataNotStartedId) {
        todoElementsWithDataNotStartedId.forEach((element) => {
            // Clear existing event listeners.
            element.removeEventListener('click', onSetAsNotStartedButtonClicked);
            element.addEventListener('click', onSetAsNotStartedButtonClicked);
        });
    }

    const addSubtaskForm = document.querySelector<HTMLFormElement>('#add-subtask-inline-form');
    if (addSubtaskForm) {
        const subtaskFormPreventDefaultAction = function (event: Event) {
            event.preventDefault();
        }
        // Prevent form submission from reloading the page.
        // Clear existing event listeners.
        addSubtaskForm.removeEventListener('submit', subtaskFormPreventDefaultAction);
        addSubtaskForm.addEventListener('submit', subtaskFormPreventDefaultAction);
    }

    const addSubtaskInput = document.querySelector<HTMLInputElement>('#todo-details-add-subtask-title-input');
    const addSubtaskButton = document.querySelector<HTMLButtonElement>('#todo-details-add-subtask-button');
    const subtasksListDivId = 'todo-details-sub-tasks-list-div';
    if (addSubtaskButton) {
        const addSubTaskButtonClickedAction = async function (event: MouseEvent) {
            if (addSubtaskInput && addSubtaskInput.value.trim() !== '') {
                event.preventDefault();
                const subtaskTitle = addSubtaskInput.value.trim();
                await addSubtask(null, subtasksListDivId);
                addSubtaskInput.value = '';
            };
        };
        // Clear existing event listeners.
        addSubtaskButton.removeEventListener('click', addSubTaskButtonClickedAction);
        addSubtaskButton.addEventListener('click', addSubTaskButtonClickedAction);
    }

    if (addSubtaskInput) {
        const addSubtaskInputKeydownAction = async function (event: KeyboardEvent) {
            if (event.key === 'Enter' && addSubtaskInput.value.trim() !== '') {
                event.preventDefault();
                await addSubtask(null, subtasksListDivId);
                addSubtaskInput.value = '';
                // Focus back to the input field after adding the subtask
                addSubtaskInput.focus();
            };
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
async function onSetAsNotStartedButtonClicked(event: MouseEvent): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const buttonElement: HTMLButtonElement = event.currentTarget as HTMLButtonElement;
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
                    dispatchKanbanBoardChangedEvent(popupKanbanItemObject.kanbanBoardId.toString());

                    return new Promise<void>(function (resolve, reject) {
                        resolve();
                    });
                } else {
                    console.error('Error setting todo as not started. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as not started. Error: ' + error);
            });
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Handles the click event for the "Set as In Progress" button.
 * When clicked, it sends a request to set the todo item as in progress.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsInProgressButtonClicked(event: MouseEvent): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const buttonElement: HTMLButtonElement = event.currentTarget as HTMLButtonElement;
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
                    dispatchKanbanBoardChangedEvent(popupKanbanItemObject.kanbanBoardId.toString());

                    return new Promise<void>(function (resolve, reject) {
                        resolve();
                    });

                } else {
                    console.error('Error setting todo as in progress. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as in progress. Error: ' + error);
            });
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Handles the click event for the "Set as Done" button.
 * When clicked, it sends a request to set the todo item as done.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsCompletedButtonClicked(event: MouseEvent): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const buttonElement: HTMLButtonElement = event.currentTarget as HTMLButtonElement;
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
                    dispatchKanbanBoardChangedEvent(popupKanbanItemObject.kanbanBoardId.toString());

                    return new Promise<void>(function (resolve, reject) {
                        resolve();
                    });

                } else {
                    console.error('Error setting todo as completed. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as completed. Error: ' + error);
            });
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Handles the click event for the "Set as Cancelled" button.
 * When clicked, it sends a request to set the todo item as cancelled.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsCancelledButtonClicked(event: MouseEvent): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    const buttonElement: HTMLButtonElement = event.currentTarget as HTMLButtonElement;
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
                    dispatchKanbanBoardChangedEvent(popupKanbanItemObject.kanbanBoardId.toString());

                    return new Promise<void>(function (resolve, reject) {
                        resolve();
                    });

                } else {
                    console.error('Error setting todo as cancelled. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as cancelled. Error: ' + error);
            });
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

export async function displayKanbanItemDetails(kanbanItemId: string, container: string): Promise<void> {
    let kanbanItem: KanbanItem;
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
                } else {
                    await popupKanbanItem(kanbanItem, container);
                }
            }
        } else {
            console.error('Error fetching kanban item details. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching kanban item details: ' + error);
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

export async function updateKanbanItem(kanbanItem: KanbanItem): Promise<boolean> {
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
            // Update the KanbanItems array and re-render the board.
            const updatedKanbanItem = await response.json() as KanbanItem;
            if (updatedKanbanItem) {
                let kanbanItems = getKanbanItems();
                const index = kanbanItems.findIndex(k => k.kanbanItemId === updatedKanbanItem.kanbanItemId);
                if (index !== -1) {
                    kanbanItems[index] = updatedKanbanItem;
                    setKanbanItems(kanbanItems);
                }
            }
        } else {
            console.error('Error updating kanban item. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error updating kanban item: ' + error);
    });

    return new Promise<boolean>(function (resolve, reject) {
        resolve(success);
    });
}

export async function getAddKanbanItemForm(kanbanBoardId: number, columnId: number, rowIndex: number): Promise<string> {
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
        } else {
            console.error('Error fetching add kanban item form. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching add kanban item form: ' + error);
    });

    return new Promise<string>(function (resolve, reject) {
        resolve(formHtml);
    });
}

export async function getEditKanbanItemForm(kanbanItemId: string): Promise<string> {
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
        } else {
            console.error('Error fetching add kanban item form. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching add kanban item form: ' + error);
    });

    return new Promise<string>(function (resolve, reject) {
        resolve(formHtml);
    });
}

export async function getRemoveKanbanItemForm(kanbanItemId: string): Promise<string> {
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
        } else {
            console.error('Error fetching add kanban item form. Status: ' + response.status);
        }
    }).catch(function (error) {
        console.error('Error fetching add kanban item form: ' + error);
    });

    return new Promise<string>(function (resolve, reject) {
        resolve(formHtml);
    });
}

export async function removeKanbanItemFunction(kanbanItemId: string): Promise<void> {
    const removeKanbanItemModalDiv = document.querySelector<HTMLDivElement>('#kanban-item-details-div');
    if (removeKanbanItemModalDiv) {
        removeKanbanItemModalDiv.innerHTML = '';

        const formHtml = await getRemoveKanbanItemForm(kanbanItemId);
        removeKanbanItemModalDiv.innerHTML = formHtml;
        removeKanbanItemModalDiv.classList.remove('d-none');
        
        const cancelButton = removeKanbanItemModalDiv.querySelector<HTMLButtonElement>('.remove-kanban-item-cancel-button');
        if (cancelButton) {
            const closeButtonFunction = function () {
                removeKanbanItemModalDiv.innerHTML = '';
                removeKanbanItemModalDiv.classList.add('d-none');
            }
            cancelButton.removeEventListener('click', closeButtonFunction);
            cancelButton.addEventListener('click', closeButtonFunction);

            const closeButton = removeKanbanItemModalDiv.querySelector<HTMLButtonElement>('.modal-close-button');
            if (closeButton) {
                closeButton.removeEventListener('click', closeButtonFunction);
                closeButton.addEventListener('click', closeButtonFunction);
            }
        }

        const removeKanbanItemForm = removeKanbanItemModalDiv.querySelector<HTMLFormElement>('#remove-kanban-card-form');
        if (removeKanbanItemForm) {
            const removeKanbanItemFormFunction = async function (event: Event) {
                event.preventDefault();
                startFullPageSpinner();
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
                        const removedKanbanItem = await response.json() as KanbanItem;
                        if (removedKanbanItem) {
                            let kanbanItems = getKanbanItems();
                            // remove the item from kanbanItems array
                            kanbanItems = kanbanItems.filter(k => k.kanbanItemId.toString() !== kanbanItemId);

                            setKanbanItems(kanbanItems);

                            await updateKanbanItemsInColumn(removedKanbanItem.columnId);
                        }
                    } else {
                        console.error('Error removing kanban item. Status: ' + response.status);
                    }
                }).catch(function (error) {
                    console.error('Error removing kanban item: ' + error);
                });
                stopFullPageSpinner();
            }
            removeKanbanItemForm.removeEventListener('submit', removeKanbanItemFormFunction);
            removeKanbanItemForm.addEventListener('submit', removeKanbanItemFormFunction);
            initializeAddEditKanbanItem('kanban-item-details-div');
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

export async function editKanbanItemFunction(kanbanItemId: string): Promise<void> {
    const editKanbanItemModalDiv = document.querySelector<HTMLDivElement>('#kanban-item-details-div');
    if (editKanbanItemModalDiv) {
        editKanbanItemModalDiv.innerHTML = '';

        const formHtml = await getEditKanbanItemForm(kanbanItemId);
        editKanbanItemModalDiv.innerHTML = formHtml;
        editKanbanItemModalDiv.classList.remove('d-none');
        
        const cancelButton = editKanbanItemModalDiv.querySelector<HTMLButtonElement>('.edit-kanban-item-cancel-button');
        if (cancelButton) {
            const closeButtonFunction = function () {
                editKanbanItemModalDiv.innerHTML = '';
                editKanbanItemModalDiv.classList.add('d-none');
            }
            cancelButton.removeEventListener('click', closeButtonFunction);
            cancelButton.addEventListener('click', closeButtonFunction);

            const closeButton = editKanbanItemModalDiv.querySelector<HTMLButtonElement>('.modal-close-button');
            if (closeButton) {
                closeButton.removeEventListener('click', closeButtonFunction);
                closeButton.addEventListener('click', closeButtonFunction);
            }
        }

        const editKanbanItemForm = editKanbanItemModalDiv.querySelector<HTMLFormElement>('#save-kanban-card-form');
        if (editKanbanItemForm) {
            const editKanbanItemFormFunction = async function (event: Event) {
                event.preventDefault();
                startFullPageSpinner();
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
                        const updatedKanbanItem = await response.json() as KanbanItem;
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
                    } else {
                        console.error('Error editing kanban item. Status: ' + response.status);
                    }
                }).catch(function (error) {
                    console.error('Error editing kanban item: ' + error);
                });
                stopFullPageSpinner();
            }
            editKanbanItemForm.removeEventListener('submit', editKanbanItemFormFunction);
            editKanbanItemForm.addEventListener('submit', editKanbanItemFormFunction);
            initializeAddEditKanbanItem('kanban-item-details-div');
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}