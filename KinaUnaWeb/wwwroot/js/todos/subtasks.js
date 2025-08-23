import { onEditItemButtonClicked } from "../addItem/add-item.js";
import { getCurrentLanguageId } from "../data-tools-v9.js";
import { startFullPageSpinner, startLoadingItemsSpinner, stopFullPageSpinner, stopLoadingItemsSpinner } from "../navigation-tools-v9.js";
import { TodoItemParameters } from "../page-models-v9.js";
let lastSubTaskPageParemeters;
let containerElementId = '';
export async function getSubtasks(subtasksPageParameters, subtasksListElementId, resetList) {
    startLoadingSpinner();
    if (subtasksPageParameters.currentPageNumber === 0) {
        subtasksPageParameters.currentPageNumber = 1;
    }
    if (subtasksPageParameters.itemsPerPage === 0) {
        subtasksPageParameters.itemsPerPage = 10;
    }
    const subtaskListElement = document.querySelector('#' + subtasksListElementId);
    if (resetList) {
        subtasksPageParameters.currentPageNumber = 1;
        if (subtaskListElement !== null) {
            subtaskListElement.innerHTML = '';
        }
    }
    const moreSubtasksButton = document.getElementById('more-subtasks-button');
    moreSubtasksButton?.classList.add('d-none');
    const getSubtasksResponse = await fetch('/Subtasks/GetSubtasksList', {
        method: 'POST',
        body: JSON.stringify(subtasksPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
        }
    });
    if (getSubtasksResponse.ok && getSubtasksResponse.body !== null) {
        const subtasksPageResponse = await getSubtasksResponse.json();
        if (subtasksPageResponse) {
            subtasksPageParameters.totalPages = subtasksPageResponse.totalPages;
            subtasksPageParameters.totalItems = subtasksPageResponse.totalItems;
            if (subtasksPageResponse.totalItems < 1) {
                getSubtaskElement(0, subtasksListElementId);
                subtaskListElement?.classList.add('d-none');
            }
            else {
                subtaskListElement?.classList.remove('d-none');
                for await (const todoItem of subtasksPageResponse.subtasksList) {
                    await getSubtaskElement(todoItem.todoItemId, subtasksListElementId);
                    addSubtaskListeners(todoItem.todoItemId.toString());
                }
                ;
            }
            subtasksPageParameters.currentPageNumber++;
            if (subtasksPageResponse.totalPages > subtasksPageResponse.pageNumber && moreSubtasksButton !== null) {
                moreSubtasksButton.classList.remove('d-none');
            }
        }
    }
    lastSubTaskPageParemeters = subtasksPageParameters;
    containerElementId = subtasksListElementId;
    stopLoadingSpinner();
    return new Promise(function (resolve, reject) {
        resolve(subtasksPageParameters);
    });
}
async function getSubtaskElement(id, containerElementId) {
    if (id < 1) {
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    const getSubtaskElementParameters = new TodoItemParameters();
    getSubtaskElementParameters.todoItemId = id;
    getSubtaskElementParameters.languageId = getCurrentLanguageId();
    const getSubtaskElementResponse = await fetch('/Subtasks/SubtaskElement', {
        method: 'POST',
        body: JSON.stringify(getSubtaskElementParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    });
    if (getSubtaskElementResponse.ok && getSubtaskElementResponse.text !== null) {
        const subtaskHtml = await getSubtaskElementResponse.text();
        const containerElement = document.getElementById(containerElementId);
        if (containerElement != null) {
            containerElement.insertAdjacentHTML('beforeend', subtaskHtml);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
export async function addSubtask(subtask, containerElementId) {
    if (subtask === null) {
        let addSubtaskForm = document.querySelector('#add-subtask-inline-form');
        if (!addSubtaskForm) {
            return new Promise(function (resolve, reject) {
                resolve();
            });
        }
        let formData = new FormData(addSubtaskForm);
        const addSubtaskElementResponse = await fetch('/Subtasks/AddSubtaskInline', {
            method: 'POST',
            body: formData
        });
        if (addSubtaskElementResponse.ok) {
            const addedSubtask = await addSubtaskElementResponse.json();
            await getSubtasks(lastSubTaskPageParemeters, containerElementId, true);
        }
        else {
            console.log('Error adding subtask. Status: ' + addSubtaskElementResponse.status + ', Message: ' + addSubtaskElementResponse.statusText);
        }
    }
    else {
        const addSubtaskElementResponse = await fetch('/Subtasks/AddSubtask', {
            method: 'POST',
            body: JSON.stringify(subtask),
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
        });
        if (addSubtaskElementResponse.ok) {
            const addedSubtask = await addSubtaskElementResponse.json();
            await getSubtasks(lastSubTaskPageParemeters, containerElementId, true);
        }
        else {
            console.log('Error adding subtask. Status: ' + addSubtaskElementResponse.status + ', Message: ' + addSubtaskElementResponse.statusText);
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Shows the loading spinner in the loading-subtasks-div.
*/
function startLoadingSpinner() {
    startLoadingItemsSpinner('loading-subtasks-div');
}
/** Hides the loading spinner in the loading-subtasks-div.
 */
function stopLoadingSpinner() {
    stopLoadingItemsSpinner('loading-subtasks-div');
}
function addSubtaskListeners(itemId) {
    const editButtonElement = document.querySelector('[data-edit-item-item-id="' + itemId + '"]');
    if (editButtonElement) {
        // Clear existing event listeners to avoid duplicates.
        editButtonElement.removeEventListener('click', onEditItemButtonClicked);
        editButtonElement.addEventListener('click', onEditItemButtonClicked);
    }
    const subtaskElementsWithDataCompletedId = document.querySelectorAll('[data-set-subtask-completed-id="' + itemId + '"]');
    if (subtaskElementsWithDataCompletedId) {
        subtaskElementsWithDataCompletedId.forEach((element) => {
            // Clear existing event listeners to avoid duplicates.
            element.removeEventListener('click', onSetAsCompletedButtonClicked);
            element.addEventListener('click', onSetAsCompletedButtonClicked);
        });
    }
    const subtaskElementsWithDataCancelledId = document.querySelectorAll('[data-set-subtask-cancelled-id="' + itemId + '"]');
    if (subtaskElementsWithDataCancelledId) {
        subtaskElementsWithDataCancelledId.forEach((element) => {
            // Clear existing event listeners to avoid duplicates.
            element.removeEventListener('click', onSetAsCancelledButtonClicked);
            element.addEventListener('click', onSetAsCancelledButtonClicked);
        });
    }
    const subtaskElementsWithDataInProgressId = document.querySelectorAll('[data-set-subtask-in-progress-id="' + itemId + '"]');
    if (subtaskElementsWithDataInProgressId) {
        subtaskElementsWithDataInProgressId.forEach((element) => {
            // Clear existing event listeners to avoid duplicates.
            element.removeEventListener('click', onSetAsInProgressButtonClicked);
            element.addEventListener('click', onSetAsInProgressButtonClicked);
        });
    }
    const subtaskElementsWithDataNotStartedId = document.querySelectorAll('[data-set-subtask-not-started-id="' + itemId + '"]');
    if (subtaskElementsWithDataNotStartedId) {
        subtaskElementsWithDataNotStartedId.forEach((element) => {
            // Clear existing event listeners to avoid duplicates.
            element.removeEventListener('click', onSetAsNotStartedButtonClicked);
            element.addEventListener('click', onSetAsNotStartedButtonClicked);
        });
    }
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
        const todoId = buttonElement.dataset.setSubtaskNotStartedId;
        if (todoId) {
            startFullPageSpinner();
            let url = '/Subtasks/SetSubtaskAsNotStarted?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    stopFullPageSpinner();
                    await getSubtasks(lastSubTaskPageParemeters, containerElementId, true);
                    return;
                }
                else {
                    console.error('Error setting subtask as not started. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting subtask as not started. Error: ' + error);
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
        const todoId = buttonElement.dataset.setSubtaskInProgressId;
        if (todoId) {
            startFullPageSpinner();
            let url = '/Subtasks/SetSubtaskAsInProgress?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    stopFullPageSpinner();
                    await getSubtasks(lastSubTaskPageParemeters, containerElementId, true);
                    return;
                }
                else {
                    console.error('Error setting subtask as in progress. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting subtask as in progress. Error: ' + error);
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
        const todoId = buttonElement.dataset.setSubtaskCompletedId;
        if (todoId) {
            startFullPageSpinner();
            let url = '/Subtasks/SetSubtaskAsCompleted?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    stopFullPageSpinner();
                    await getSubtasks(lastSubTaskPageParemeters, containerElementId, true);
                    return;
                }
                else {
                    console.error('Error setting subtask as completed. Status: ' + response.status + ', Message: ' + response.statusText);
                }
            }).catch(function (error) {
                console.error('Error setting subtask as completed. Error: ' + error);
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
        const todoId = buttonElement.dataset.setSubtaskCancelledId;
        if (todoId) {
            startFullPageSpinner();
            let url = '/Subtasks/SetSubtaskAsCancelled?todoId=' + todoId;
            await fetch(url, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
            }).then(async function (response) {
                if (response.ok) {
                    stopFullPageSpinner();
                    await getSubtasks(lastSubTaskPageParemeters, containerElementId, true);
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
//# sourceMappingURL=subtasks.js.map