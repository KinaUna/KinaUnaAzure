import { setDeleteItemButtonEventListeners, setEditItemButtonEventListeners } from '../addItem/add-item-v12.js';
import { TimelineChangedEvent } from '../data-tools-v12.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v12.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v12.js';
import { KanbanItem, SubtasksPageParameters, TimelineItem } from '../page-models-v12.js';
import { addSubtask, getSubtasks, hideSubtaskMenus, hideSubtaskMoreInfoDivs, refreshSubtasks } from './subtasks-v12.js';
let subtaskPageParameters = new SubtasksPageParameters();
const subtasksListDivId = 'todo-details-sub-tasks-list-div';
/**
 * Adds event listeners to all elements with the data-todo-id attribute.
 * When clicked, the DisplayTodoItem function is called.
 * @param {string} itemId The id of the todo item to add event listeners for.
 */
export function addTodoItemListeners(itemId) {
    const todoElementsWithDataId = document.querySelectorAll('[data-todo-id="' + itemId + '"]');
    if (todoElementsWithDataId) {
        todoElementsWithDataId.forEach((element) => {
            element.removeEventListener('click', onTodoItemDivClicked);
            element.addEventListener('click', onTodoItemDivClicked);
        });
    }
}
/**
 * The click event handler for todo item divs.
 * When a todo item div is clicked, it retrieves the todoId from the data attribute
 * and calls the displayTodoItem function to show the todo details.
 * @param {MouseEvent} event The click event.
 * @returns {Promise<void>} A promise that resolves when the function completes.
 * */
async function onTodoItemDivClicked(event) {
    const todoElement = event.currentTarget;
    if (todoElement !== null) {
        const todoId = todoElement.dataset.todoId;
        if (todoId) {
            await displayTodoItem(todoId);
        }
    }
}
export function getStatusIconForTodoItems(status) {
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
/**
 * Enable other scripts to call the DisplayTodoItem function.
 * @param {string} todoId The id of the todo to display.
 */
export async function popupTodoItem(todoId) {
    await displayTodoItem(todoId);
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Handles the click event for the "Set as Not Started" button.
 * When clicked, it sends a request to set the todo item as not started.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsNotStartedButtonClicked(event) {
    const buttonElement = event.currentTarget;
    if (buttonElement !== null) {
        const todoId = buttonElement.dataset.setTodoItemNotStartedId;
        if (todoId) {
            startFullPageSpinner();
            let url = '/Todos/SetTodoAsNotStarted?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    stopFullPageSpinner();
                    if (!buttonElement.classList.contains('no-pop-up')) {
                        await displayTodoItem(todoId);
                    }
                    dispatchTimelineItemChangedEvent(todoId);
                    return;
                }
                else {
                    console.error('Error setting todo as not started. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as not started. Error: ' + error);
            });
            stopFullPageSpinner();
        }
    }
}
/**
 * Handles the click event for the "Set as In Progress" button.
 * When clicked, it sends a request to set the todo item as in progress.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsInProgressButtonClicked(event) {
    const buttonElement = event.currentTarget;
    if (buttonElement !== null) {
        const todoId = buttonElement.dataset.setTodoItemInProgressId;
        if (todoId) {
            startFullPageSpinner();
            let url = '/Todos/SetTodoAsInProgress?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    stopFullPageSpinner();
                    if (!buttonElement.classList.contains('no-pop-up')) {
                        await displayTodoItem(todoId);
                    }
                    dispatchTimelineItemChangedEvent(todoId);
                    return;
                }
                else {
                    console.error('Error setting todo as in progress. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as in progress. Error: ' + error);
            });
            stopFullPageSpinner();
        }
    }
}
/**
 * Handles the click event for the "Set as Done" button.
 * When clicked, it sends a request to set the todo item as done.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsCompletedButtonClicked(event) {
    const buttonElement = event.currentTarget;
    if (buttonElement !== null) {
        const todoId = buttonElement.dataset.setTodoItemCompletedId;
        if (todoId) {
            startFullPageSpinner();
            let url = '/Todos/SetTodoAsCompleted?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    stopFullPageSpinner();
                    if (!buttonElement.classList.contains('no-pop-up')) {
                        await displayTodoItem(todoId);
                    }
                    dispatchTimelineItemChangedEvent(todoId);
                    return;
                }
                else {
                    console.error('Error setting todo as completed. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as completed. Error: ' + error);
            });
            stopFullPageSpinner();
        }
    }
}
/**
 * Handles the click event for the "Set as Cancelled" button.
 * When clicked, it sends a request to set the todo item as cancelled.
 * @param {MouseEvent} event The click event.
 * @return {Promise<void>} A promise that resolves when the function completes.
 * */
async function onSetAsCancelledButtonClicked(event) {
    const buttonElement = event.currentTarget;
    if (buttonElement !== null) {
        const todoId = buttonElement.dataset.setTodoItemCancelledId;
        if (todoId) {
            startFullPageSpinner();
            let url = '/Todos/SetTodoAsCancelled?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    stopFullPageSpinner();
                    if (!buttonElement.classList.contains('no-pop-up')) {
                        await displayTodoItem(todoId);
                    }
                    dispatchTimelineItemChangedEvent(todoId);
                    return;
                }
                else {
                    console.error('Error setting todo as cancelled. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting todo as cancelled. Error: ' + error);
            });
            stopFullPageSpinner();
        }
    }
}
function dispatchTimelineItemChangedEvent(todoId) {
    const timelineItem = new TimelineItem();
    timelineItem.itemType = 15;
    timelineItem.itemId = todoId;
    const timelineItemChangedEvent = new TimelineChangedEvent(timelineItem);
    window.dispatchEvent(timelineItemChangedEvent);
}
/**
 * Sets up event listeners for the todo details popup.
 * @param {string} itemId The id of the todo item to set event listeners for.
 * @param {HTMLDivElement} todoDetailsPopupDiv The div element for the todo details popup.
 */
async function setTodoDetailsEventListeners(itemId, todoDetailsPopupDiv) {
    let closeButtonsList = document.querySelectorAll('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            const closeButtonActions = function () {
                todoDetailsPopupDiv.innerHTML = '';
                todoDetailsPopupDiv.classList.add('d-none');
                showBodyScrollbars();
            };
            // Clear existing event listeners.
            button.removeEventListener('click', closeButtonActions);
            button.addEventListener('click', closeButtonActions);
        });
    }
    setAssignStatusButtonsEventListeners(itemId);
    setAssignToButtonEventListener(itemId, true);
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
    if (addSubtaskButton) {
        const addSubTaskButtonClickedAction = async function (event) {
            if (addSubtaskInput && addSubtaskInput.value.trim() !== '') {
                event.preventDefault();
                const subtaskTitle = addSubtaskInput.value.trim();
                await addSubtask(null, subtasksListDivId);
                addSubtaskInput.value = '';
                await refreshSubtasks();
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
    const addTodoItemToKanbanBoardButton = document.querySelector('#add-todo-item-to-kanban-board-button');
    if (addTodoItemToKanbanBoardButton) {
        const addTodoItemToKanbanBoardAction = async function (event) {
            event.preventDefault();
            event.stopPropagation();
            let kanbanBoardSelectList = document.querySelector('#add-todo-item-to-kanban-board-select');
            let kanbanBoardId = kanbanBoardSelectList ? parseInt(kanbanBoardSelectList.value) : 0;
            if (kanbanBoardId !== 0) {
                let kanbanItem = new KanbanItem();
                kanbanItem.todoItemId = parseInt(itemId);
                kanbanItem.columnId = 0; // Default to first column.
                kanbanItem.rowIndex = 0; // Default to top position.
                kanbanItem.kanbanBoardId = kanbanBoardId;
                startFullPageSpinner();
                let url = '/KanbanItems/AddKanbanItemFromTodoItem';
                await fetch(url, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(kanbanItem)
                }).then(async function (response) {
                    if (response.ok) {
                        stopFullPageSpinner();
                        await displayTodoItem(itemId);
                        return;
                    }
                    else {
                        console.error('Error adding todo item to kanban board. Status: ' + response.status + ', Message: ' + response.statusText);
                    }
                }).catch(function (error) {
                    console.error('Error adding todo item to kanban board. Error: ' + error);
                });
                stopFullPageSpinner();
            }
            return new Promise(function (resolve, reject) {
                resolve();
            });
        };
        addTodoItemToKanbanBoardButton.removeEventListener('click', addTodoItemToKanbanBoardAction);
        addTodoItemToKanbanBoardButton.addEventListener('click', addTodoItemToKanbanBoardAction);
    }
}
export function setAssignStatusButtonsEventListeners(itemId) {
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
}
function setAssignToButtonEventListener(itemId, detailsShown) {
    const assignToButton = document.getElementById('assign-todo-item-to-button-' + itemId);
    if (assignToButton) {
        const assignToButtonClickedAction = async function (event) {
            event.preventDefault();
            assignTo(itemId, detailsShown);
        };
        assignToButton.removeEventListener('click', assignToButtonClickedAction);
        assignToButton.addEventListener('click', assignToButtonClickedAction);
    }
}
async function assignTo(todoItemId, detailsShown) {
    // Get form html from server.
    let url = '/Todos/AssignTodoItemTo?todoItemId=' + todoItemId;
    const response = await fetch(url);
    if (response.ok) {
        const formHtml = await response.text();
        const modalDiv = document.querySelector('#item-details-div');
        if (modalDiv) {
            modalDiv.innerHTML = '';
            const fullScreenOverlay = document.createElement('div');
            fullScreenOverlay.classList.add('full-screen-bg');
            fullScreenOverlay.innerHTML = formHtml;
            modalDiv.appendChild(fullScreenOverlay);
            modalDiv.classList.remove('d-none');
            const cancelButton = modalDiv.querySelector('.assign-todo-item-to-cancel-button');
            if (cancelButton) {
                const closeButtonFunction = async function () {
                    modalDiv.innerHTML = '';
                    modalDiv.classList.add('d-none');
                    if (detailsShown) {
                        await displayTodoItem(todoItemId);
                    }
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
            const assignForm = modalDiv.querySelector('#assign-todo-item-to-form');
            if (assignForm) {
                const assignTodoItemToFormFunction = async function (event) {
                    event.preventDefault();
                    startFullPageSpinner();
                    const formData = new FormData(assignForm);
                    const url = '/Todos/AssignTodoItemTo';
                    await fetch(url, {
                        method: 'POST',
                        body: formData
                    }).then(async function (response) {
                        if (response.ok) {
                            // Successfully assigned the KanbanItem. Close the modal.
                            modalDiv.innerHTML = '';
                            modalDiv.classList.add('d-none');
                            // Dispatch event to update Kanban board
                            dispatchTimelineItemChangedEvent(todoItemId);
                            if (detailsShown) {
                                await displayTodoItem(todoItemId);
                            }
                        }
                        else {
                            console.error('Error assigning person to kanban item. Status: ' + response.status);
                        }
                    }).catch(function (error) {
                        console.error('Error assigning person to kanban item: ' + error);
                    });
                    stopFullPageSpinner();
                };
                assignForm.removeEventListener('submit', assignTodoItemToFormFunction);
                assignForm.addEventListener('submit', assignTodoItemToFormFunction);
            }
        }
    }
}
/**
 * Displays a todo item in a popup.
 * @param {string} todoId The id of the todo item to display.
 */
async function displayTodoItem(todoId) {
    startFullPageSpinner();
    subtaskPageParameters = new SubtasksPageParameters();
    let url = '/Todos/ViewTodo?todoId=' + todoId + "&partialView=true";
    await fetch(url, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    }).then(async function (response) {
        if (response.ok) {
            const todoElementHtml = await response.text();
            const todoDetailsPopupDiv = document.querySelector('#item-details-div');
            if (todoDetailsPopupDiv) {
                todoDetailsPopupDiv.innerHTML = '';
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = todoElementHtml;
                todoDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                todoDetailsPopupDiv.classList.remove('d-none');
                setTodoDetailsEventListeners(todoId, todoDetailsPopupDiv);
                setEditItemButtonEventListeners();
                setDeleteItemButtonEventListeners();
                await getSubtaskList(todoId, true);
                $(".selectpicker").selectpicker('refresh');
                document.removeEventListener('click', hideAllTodoDetailsMenusAndModals);
                document.addEventListener('click', hideAllTodoDetailsMenusAndModals);
            }
        }
        else {
            console.error('Error getting todo item. Status: ' + response.status + ', Message: ' + response.statusText);
        }
    }).catch(function (error) {
        console.error('Error getting todo item. Error: ' + error);
    });
    stopFullPageSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function getSubtaskList(todoId, resetList = false) {
    subtaskPageParameters.parentTodoItemId = parseInt(todoId);
    subtaskPageParameters.groupBy = 1;
    subtaskPageParameters = await getSubtasks(subtaskPageParameters, subtasksListDivId, resetList);
    const todoDetailsDiv = document.getElementById('todo-details-sub-tasks-div');
    if (todoDetailsDiv) {
        if (subtaskPageParameters.totalItems > 0) {
            todoDetailsDiv.classList.remove('d-none');
        }
        else {
            todoDetailsDiv.classList.add('d-none');
        }
    }
}
function hideAllTodoDetailsMenusAndModals(event) {
    const target = event.target;
    if (!target.closest('.modal-settings-panel') && !target.closest('.modal-content')) {
        const modalDivs = document.querySelectorAll('.settings-modal');
        if (modalDivs) {
            modalDivs.forEach((modalDiv) => {
                modalDiv.classList.add('d-none');
            });
        }
    }
    if (!target.closest('.subtask-menu-content')) {
        hideSubtaskMenus('');
    }
    if (!target.closest('.subtask-more-info')) {
        hideSubtaskMoreInfoDivs('');
    }
}
//# sourceMappingURL=todo-details-v12.js.map