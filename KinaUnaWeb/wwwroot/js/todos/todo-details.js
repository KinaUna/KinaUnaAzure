import { setEditItemButtonEventListeners } from '../addItem/add-item.js';
import { hideBodyScrollbars, showBodyScrollbars } from '../item-details/items-display-v8.js';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';
/**
 * Adds event listeners to all elements with the data-todo-id attribute.
 * When clicked, the DisplayTodoItem function is called.
 * @param {string} itemId The id of the todo item to add event listeners for.
 */
export function addTodoEventListeners(itemId) {
    const todoElementsWithDataId = document.querySelectorAll('[data-todo-id="' + itemId + '"]');
    if (todoElementsWithDataId) {
        todoElementsWithDataId.forEach((element) => {
            element.addEventListener('click', async function () {
                await displayTodoItem(itemId);
            });
        });
    }
}
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