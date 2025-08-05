import { addTimelineItemEventListener, showPopupAtLoad } from '../item-details/items-display-v8.js';
import * as pageModels from '../page-models-v8.js';
import { getSelectedProgenies } from '../settings-tools-v8.js';
let todosPageParameters = new pageModels.TodosPageParameters();
const todosIndexPageParametersDiv = document.querySelector('#todos-index-page-parameters');
const todosListDiv = document.querySelector('#todos-list-div');
const todosPageMainDiv = document.querySelector('#kinauna-main-div');
function setTodosPageParametersFromPageData() {
    if (todosIndexPageParametersDiv !== null) {
        const pageParameters = todosIndexPageParametersDiv.dataset.todosIndexPageParameters;
        if (pageParameters) {
            todosPageParameters = JSON.parse(pageParameters);
        }
    }
}
async function getTodos() {
    const getMoreTodosResponse = await fetch('/Todos/GetTodoItemsList', {
        method: 'POST',
        body: JSON.stringify(todosPageParameters),
        headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
        }
    });
    if (getMoreTodosResponse.ok && getMoreTodosResponse.body !== null) {
        const todosPageResponse = await getMoreTodosResponse.json();
        if (todosPageResponse) {
            todosPageParameters.currentPageNumber = todosPageResponse.pageNumber;
            todosPageParameters.totalPages = todosPageResponse.totalPages;
            todosPageParameters.totalItems = todosPageResponse.totalItems;
            if (todosPageResponse.totalItems < 1) {
                getTodoElement(0);
            }
            else {
                // updateTodosPageNavigation();
                if (todosListDiv != null) {
                    todosListDiv.innerHTML = '';
                }
                for await (const todoItemId of todosPageResponse.todosList) {
                    await getTodoElement(todoItemId);
                    const timelineItem = new pageModels.TimelineItem();
                    timelineItem.itemId = todoItemId.toString();
                    timelineItem.itemType = 15;
                    addTimelineItemEventListener(timelineItem);
                }
                ;
            }
        }
    }
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
async function getTodoElement(id) {
    const getTodoElementParameters = new pageModels.TodoItemParameters();
    getTodoElementParameters.todoItemId = id;
    getTodoElementParameters.languageId = todosPageParameters.languageId;
    const getTodoElementResponse = await fetch('/Todo/TodoElement', {
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
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
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
document.addEventListener('DOMContentLoaded', async function () {
    await showPopupAtLoad(pageModels.TimeLineType.TodoItem);
    setTodosPageParametersFromPageData();
    addSelectedProgeniesChangedEventListener();
    todosPageParameters.progenies = getSelectedProgenies();
    getTodos();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=todos-index.js.map