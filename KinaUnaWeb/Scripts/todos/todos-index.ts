import { getCurrentProgenyId } from '../data-tools-v8.js';
import { addTimelineItemEventListener, showPopupAtLoad } from '../item-details/items-display-v8.js';
import * as pageModels from '../page-models-v8.js';
import { getSelectedProgenies } from '../settings-tools-v8.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v8.js';
import * as SettingsHelper from '../settings-tools-v8.js';

let todosPageParameters = new pageModels.TodosPageParameters();
const todosPageSettingsStorageKey = 'todos_page_parameters';
const todosIndexPageParametersDiv = document.querySelector<HTMLDivElement>('#todos-index-page-parameters');
const todosListDiv = document.querySelector<HTMLDivElement>('#todos-list-div');
const todosPageMainDiv = document.querySelector<HTMLDivElement>('#kinauna-main-div');
let moreTodoItemsButton: HTMLButtonElement | null;
const sortAscendingSettingsButton = document.querySelector<HTMLButtonElement>('#todo-settings-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector<HTMLButtonElement>('#todo-settings-sort-descending-button');
const itemsPerPageInput = document.querySelector<HTMLInputElement>('#todo-items-per-page-input');

function setTodosPageParametersFromPageData(): void {
    if (todosIndexPageParametersDiv !== null) {
        const pageParameters = todosIndexPageParametersDiv.dataset.todosIndexPageParameters;
        if (pageParameters) {
            todosPageParameters = JSON.parse(pageParameters);
        }
    }
}

async function getTodos(): Promise<void> {
    const getMoreTodosResponse = await fetch('/Todos/GetTodoItemsList', {
        method: 'POST',
        body: JSON.stringify(todosPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
        }
    });

    if (getMoreTodosResponse.ok && getMoreTodosResponse.body !== null) {
        const todosPageResponse = await getMoreTodosResponse.json() as pageModels.TodosPageResponse;
        if (todosPageResponse) {
            todosPageParameters.currentPageNumber = todosPageResponse.pageNumber;
            todosPageParameters.totalPages = todosPageResponse.totalPages;
            todosPageParameters.totalItems = todosPageResponse.totalItems;

            if (todosPageResponse.totalItems < 1) {
                getTodoElement(0);
            }
            else {
                if (todosListDiv != null) {
                    todosListDiv.innerHTML = '';
                }
                for await (const todoItem of todosPageResponse.todosList) {
                    await getTodoElement(todoItem.todoItemId);
                    const timelineItem = new pageModels.TimelineItem();
                    timelineItem.itemId = todoItem.todoItemId.toString();
                    timelineItem.itemType = 15;
                    addTimelineItemEventListener(timelineItem);
                };
            }
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Gets a todo element by its ID and appends it to the todos list div.
 * Renders the todo item HTML and appends it to the todos list.
 * @param id The ID of the todo item to fetch.
 * @returns A promise that resolves when the todo element is fetched and appended.
 */
async function getTodoElement(id: number): Promise<void> {
    const getTodoElementParameters = new pageModels.TodoItemParameters();
    getTodoElementParameters.todoItemId = id;
    getTodoElementParameters.languageId = todosPageParameters.languageId;

    const getTodoElementResponse = await fetch('/Todos/TodoElement', {
        method: 'POST',
        body: JSON.stringify(getTodoElementParameters),
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
    });

    if (getTodoElementResponse.ok && getTodoElementResponse.text !== null) {
        const todoHtml = await getTodoElementResponse.text();
        if (todosListDiv != null) {
            todosListDiv.insertAdjacentHTML('beforeend', todoHtml);
        }
    }

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Adds an event listener for progeniesChanged events.
 * This event is triggered when the selected progenies change.
 */
function addSelectedProgeniesChangedEventListener() {
    window.addEventListener('progeniesChanged', async () => {
        let selectedProgenies = localStorage.getItem('selectedProgenies');
        if (selectedProgenies !== null) {
            todosPageParameters.progenies = getSelectedProgenies();
            todosPageParameters.currentPageNumber = 1;
            await getTodos();
        }

    });
}

/** Shows the loading spinner in the loading-todo-items-div.
 */
function startLoadingSpinner(): void {
    startLoadingItemsSpinner('loading-todo-items-div');
}

/** Hides the loading spinner in the loading-todo-items-div.
 */
function stopLoadingSpinner(): void {
    stopLoadingItemsSpinner('loading-todo-items-div');
}

/** Clears the list of todo elements in the todos-list-div and scrolls to above the todos-list-div.
*/
function clearTodoItemsElements(): void {
    const pageTitleDiv = document.querySelector<HTMLDivElement>('#page-title-div');
    if (pageTitleDiv !== null) {
        pageTitleDiv.scrollIntoView();
    }
    
    const todoItemsDiv = document.querySelector<HTMLDivElement>('#todos-list-div');
    if (todoItemsDiv !== null) {
        todoItemsDiv.innerHTML = '';
    }
}

/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortTodosAscending(): Promise<void> {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    todosPageParameters.sort = 0;
    
    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Updates parameters sort value, sets the sort buttons to show the descending button as active, and the ascending button as inactive.
 */
async function sortTodosDescending(): Promise<void> {
    sortDescendingSettingsButton?.classList.add('active');
    sortAscendingSettingsButton?.classList.remove('active');
    todosPageParameters.sort = 1;

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

function decreaseTodoItemsPerPage(): void {
    if (itemsPerPageInput !== null) {
        let itemsPerPageInputValue: number = itemsPerPageInput.valueAsNumber;
        itemsPerPageInputValue--;
        if (itemsPerPageInputValue > 0) {
            itemsPerPageInput.value = itemsPerPageInputValue.toString();
        }

        todosPageParameters.itemsPerPage = itemsPerPageInput.valueAsNumber;
    }
}

function increaseTodoItemsPerPage(): void {
    if (itemsPerPageInput !== null) {
        let itemsPerPageInputValue: number = itemsPerPageInput.valueAsNumber;
        itemsPerPageInputValue++;
        if (itemsPerPageInputValue < 101) {
            itemsPerPageInput.value = itemsPerPageInputValue.toString();
        }

        todosPageParameters.itemsPerPage = itemsPerPageInput.valueAsNumber;
    }
}

function setEventListenersForItemsPerPage(): void {
    const decreaseItemsPerPageButton = document.querySelector<HTMLButtonElement>('#decrease-todo-items-per-page-button');
    const increaseItemsPerPageButton = document.querySelector<HTMLButtonElement>('#increase-todo-items-per-page-button');
    if (decreaseItemsPerPageButton !== null) {
        decreaseItemsPerPageButton.addEventListener('click', decreaseTodoItemsPerPage);
    }
    if (increaseItemsPerPageButton !== null) {
        increaseItemsPerPageButton.addEventListener('click', increaseTodoItemsPerPage);
    }
}

function loadTodosPageSettings(): void {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings<pageModels.TodosPageParameters>(todosPageSettingsStorageKey);
    if (pageSettingsFromStorage !== null) {
        todosPageParameters.sort = pageSettingsFromStorage.sort ?? 1;

        if (todosPageParameters.sort === 0) {
            sortTodosAscending();
        }
        else {
            sortTodosDescending();
        }

        todosPageParameters.itemsPerPage = pageSettingsFromStorage.itemsPerPage ?? 10;
        const itemsPerPageInput = document.querySelector<HTMLInputElement>('#todo-items-per-page-input');
        if (itemsPerPageInput !== null) {
            itemsPerPageInput.value = todosPageParameters.itemsPerPage.toString();
        }        
    }
}

/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup(): Promise<void> {
    const todosPageSaveSettingsButton = document.querySelector<HTMLButtonElement>('#todos-page-save-settings-button');
    if (todosPageSaveSettingsButton !== null) {
        todosPageSaveSettingsButton.addEventListener('click', saveTodosPageSettings);
    }

    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortTodosAscending);
        sortDescendingSettingsButton.addEventListener('click', sortTodosDescending);
    }

    setEventListenersForItemsPerPage();
}

/**
 * Saves the current page parameters to local storage and reloads the todo items list.
 */
async function saveTodosPageSettings(): Promise<void> {
    const numberOfItemsToGetInput = document.querySelector<HTMLInputElement>('#todo-items-per-page-input');
    if (numberOfItemsToGetInput !== null) {
        todosPageParameters.itemsPerPage = parseInt(numberOfItemsToGetInput.value);
    }
    else {
        todosPageParameters.itemsPerPage = 10;
    }

    todosPageParameters.sort = sortAscendingSettingsButton?.classList.contains('active') ? 0 : 1;   

    // If the 'set as default' checkbox is checked, save the page settings to local storage.
    const setAsDefaultCheckbox = document.querySelector<HTMLInputElement>('#todo-settings-save-default-checkbox');
    if (setAsDefaultCheckbox !== null && setAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings<pageModels.TodosPageParameters>(todosPageSettingsStorageKey, todosPageParameters);
    }    
    
    SettingsHelper.toggleShowPageSettings();
    clearTodoItemsElements();
    await getTodos();
}
/** Initializes the Todos page by setting up event listeners and fetching initial data.
 * This function is called when the DOM content is fully loaded.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    await showPopupAtLoad(pageModels.TimeLineType.TodoItem);

    setTodosPageParametersFromPageData();
    loadTodosPageSettings();
    addSelectedProgeniesChangedEventListener();
    todosPageParameters.progenies = getSelectedProgenies();

    
    SettingsHelper.initPageSettings();
    initialSettingsPanelSetup();
    

    getTodos();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});