import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
/**
 * Adds event listeners to all elements with the data-todo-id attribute.
 * When clicked, the DisplayTodoItem function is called.
 * @param {string} itemId The id of the todo item to add event listeners for.
 */
export function addTodoItemListeners(itemId) {
    const todoElementsWithDataId = document.querySelectorAll('[data-todo-id="' + itemId + '"]');
    if (todoElementsWithDataId) {
        todoElementsWithDataId.forEach((element) => {
            element.addEventListener('click', async function () {
                await displayTodoItem(itemId);
            });
        });
    }
}
/**
 * Adds a click event listener to the todo item div.
 * When clicked, it calls the onTodoItemDivClicked function.
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
                    await displayTodoItem(todoId);
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
                    await displayTodoItem(todoId);
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
                    await displayTodoItem(todoId);
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
                    await displayTodoItem(todoId);
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
async function setTodoDetailsEventListeners(itemId, todoDetailsPopupDiv) {
    let closeButtonsList = document.querySelectorAll('.item-details-close-button');
    if (closeButtonsList) {
        closeButtonsList.forEach((button) => {
            button.addEventListener('click', function () {
                todoDetailsPopupDiv.innerHTML = '';
                todoDetailsPopupDiv.classList.add('d-none');
                showBodyScrollbars();
            });
        });
    }
    const todoElementsWithDataCompletedId = document.querySelectorAll('[data-set-todo-item-completed-id="' + itemId + '"]');
    if (todoElementsWithDataCompletedId) {
        todoElementsWithDataCompletedId.forEach((element) => {
            element.addEventListener('click', async function (event) {
                event.stopPropagation(); // Prevent the click from bubbling up to the todo item div
                await onSetAsCompletedButtonClicked(event);
            });
        });
    }
    const todoElementsWithDataCancelledId = document.querySelectorAll('[data-set-todo-item-cancelled-id="' + itemId + '"]');
    if (todoElementsWithDataCancelledId) {
        todoElementsWithDataCancelledId.forEach((element) => {
            element.addEventListener('click', async function (event) {
                event.stopPropagation(); // Prevent the click from bubbling up to the todo item div
                await onSetAsCancelledButtonClicked(event);
            });
        });
    }
    const todoElementsWithDataInProgressId = document.querySelectorAll('[data-set-todo-item-in-progress-id="' + itemId + '"]');
    if (todoElementsWithDataInProgressId) {
        todoElementsWithDataInProgressId.forEach((element) => {
            element.addEventListener('click', async function (event) {
                event.stopPropagation(); // Prevent the click from bubbling up to the todo item div
                await onSetAsInProgressButtonClicked(event);
            });
        });
    }
    const todoElementsWithDataNotStartedId = document.querySelectorAll('[data-set-todo-item-not-started-id="' + itemId + '"]');
    if (todoElementsWithDataNotStartedId) {
        todoElementsWithDataNotStartedId.forEach((element) => {
            element.addEventListener('click', async function (event) {
                event.stopPropagation(); // Prevent the click from bubbling up to the todo item div
                await onSetAsNotStartedButtonClicked(event);
            });
        });
    }
}
/**
 * Displays a todo item in a popup.
 * @param {string} todoId The id of the todo item to display.
 */
async function displayTodoItem(todoId) {
    startFullPageSpinner();
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
                const fullScreenOverlay = document.createElement('div');
                fullScreenOverlay.classList.add('full-screen-bg');
                fullScreenOverlay.innerHTML = todoElementHtml;
                todoDetailsPopupDiv.appendChild(fullScreenOverlay);
                hideBodyScrollbars();
                todoDetailsPopupDiv.classList.remove('d-none');
                setTodoDetailsEventListeners(todoId, todoDetailsPopupDiv);
                setEditItemButtonEventListeners();
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
//# sourceMappingURL=todo-details.js.map