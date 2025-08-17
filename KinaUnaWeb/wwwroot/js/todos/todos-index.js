import { getFormattedDateString, getLongDateTimeFormatMoment, getZebraDateTimeFormat, setContextAutoSuggestList, setMomentLocale, setTagsAutoSuggestList, validateDateValue } from '../data-tools-v9.js';
import { addTimelineItemEventListener, showPopupAtLoad } from '../item-details/items-display-v9.js';
import * as pageModels from '../page-models-v9.js';
import { getSelectedProgenies } from '../settings-tools-v9.js';
import { startLoadingItemsSpinner, stopLoadingItemsSpinner } from '../navigation-tools-v9.js';
import * as SettingsHelper from '../settings-tools-v9.js';
import * as LocaleHelper from '../localization-v9.js';
let todosPageParameters = new pageModels.TodosPageParameters();
const todosPageSettingsStorageKey = 'todos_page_parameters';
const todosIndexPageParametersDiv = document.querySelector('#todos-index-page-parameters');
const todosListDiv = document.querySelector('#todos-list-div');
const todosPageMainDiv = document.querySelector('#kinauna-main-div');
let moreTodoItemsButton;
const sortAscendingSettingsButton = document.querySelector('#todo-settings-sort-ascending-button');
const sortDescendingSettingsButton = document.querySelector('#todo-settings-sort-descending-button');
const itemsPerPageInput = document.querySelector('#todo-items-per-page-input');
const sortByDueDateSettingsButton = document.querySelector('#settings-sort-by-due-date-button');
const sortByCreatedDateSettingsButton = document.querySelector('#settings-sort-by-created-date-button');
const sortByStartDateSettingsButton = document.querySelector('#settings-sort-by-start-date-button');
const sortByCompletedDateSettingsButton = document.querySelector('#settings-sort-by-completed-date-button');
const groupByNoneSettingsButton = document.querySelector('#settings-group-by-none-button');
const groupByStatusSettingsButton = document.querySelector('#settings-group-by-status-button');
const groupByAssignedToSettingsButton = document.querySelector('#settings-group-by-assigned-to-button');
const todosStartDateTimePicker = $('#settings-start-date-datetimepicker');
const todosEndDateTimePicker = $('#settings-end-date-datetimepicker');
/**
 * Sets the todos page parameters from the data attributes of the todosIndexPageParametersDiv.
 * If the sort is 0, it sorts the todos in ascending order, otherwise in descending order.
 */
function setTodosPageParametersFromPageData() {
    if (todosIndexPageParametersDiv !== null) {
        const pageParameters = todosIndexPageParametersDiv.dataset.todosIndexPageParameters;
        if (pageParameters) {
            todosPageParameters = JSON.parse(pageParameters);
            if (todosPageParameters.sort === 0) {
                sortTodosAscending();
            }
            else {
                sortTodosDescending();
            }
        }
    }
}
/**
 * Fetches the todo items from the server and appends them to the todos list div.
 * If there are more todo items to fetch, it shows the "more" button.
 * If there are no todo items, it calls getTodoElement with 0 to show an empty state.
 * @returns A promise that resolves when the todo items are fetched and appended.
 */
async function getTodos() {
    startLoadingSpinner();
    moreTodoItemsButton?.classList.add('d-none');
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
                for await (const todoItem of todosPageResponse.todosList) {
                    await getTodoElement(todoItem.todoItemId);
                    const timelineItem = new pageModels.TimelineItem();
                    timelineItem.itemId = todoItem.todoItemId.toString();
                    timelineItem.itemType = 15;
                    addTimelineItemEventListener(timelineItem);
                }
                ;
            }
            todosPageParameters.currentPageNumber++;
            if (todosPageResponse.totalPages > todosPageResponse.pageNumber && moreTodoItemsButton !== null) {
                moreTodoItemsButton.classList.remove('d-none');
            }
        }
    }
    stopLoadingSpinner();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Gets a todo element by its ID and appends it to the todos list div.
 * Renders the todo item HTML and appends it to the todos list.
 * @param id The ID of the todo item to fetch.
 * @returns A promise that resolves when the todo element is fetched and appended.
 */
async function getTodoElement(id) {
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
    return new Promise(function (resolve, reject) {
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
function startLoadingSpinner() {
    startLoadingItemsSpinner('loading-todo-items-div');
}
/** Hides the loading spinner in the loading-todo-items-div.
 */
function stopLoadingSpinner() {
    stopLoadingItemsSpinner('loading-todo-items-div');
}
/** Clears the list of todo elements in the todos-list-div and scrolls to above the todos-list-div.
*/
function clearTodoItemsElements() {
    const pageTitleDiv = document.querySelector('#page-title-div');
    if (pageTitleDiv !== null) {
        pageTitleDiv.scrollIntoView();
    }
    const todoItemsDiv = document.querySelector('#todos-list-div');
    if (todoItemsDiv !== null) {
        todoItemsDiv.innerHTML = '';
    }
    todosPageParameters.currentPageNumber = 1;
}
/**
 * Updates parameters sort value, sets the sort buttons to show the ascending button as active, and the descending button as inactive.
 */
async function sortTodosAscending() {
    sortAscendingSettingsButton?.classList.add('active');
    sortDescendingSettingsButton?.classList.remove('active');
    todosPageParameters.sort = 0;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates parameters sort value, sets the sort buttons to show the descending button as active, and the ascending button as inactive.
 */
async function sortTodosDescending() {
    sortDescendingSettingsButton?.classList.add('active');
    sortAscendingSettingsButton?.classList.remove('active');
    todosPageParameters.sort = 1;
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Decreases the number of todo items displayed per page.
 * Updates the itemsPerPage property in todosPageParameters.
 */
function decreaseTodoItemsPerPage() {
    if (itemsPerPageInput !== null) {
        let itemsPerPageInputValue = itemsPerPageInput.valueAsNumber;
        itemsPerPageInputValue--;
        if (itemsPerPageInputValue > 0) {
            itemsPerPageInput.value = itemsPerPageInputValue.toString();
        }
        todosPageParameters.itemsPerPage = itemsPerPageInput.valueAsNumber;
    }
}
/**
 * Increases the number of todo items displayed per page.
 * Updates the itemsPerPage property in todosPageParameters.
 */
function increaseTodoItemsPerPage() {
    if (itemsPerPageInput !== null) {
        let itemsPerPageInputValue = itemsPerPageInput.valueAsNumber;
        itemsPerPageInputValue++;
        if (itemsPerPageInputValue < 101) {
            itemsPerPageInput.value = itemsPerPageInputValue.toString();
        }
        todosPageParameters.itemsPerPage = itemsPerPageInput.valueAsNumber;
    }
}
/**
 * Sorts the todo items by due date.
 * Updates the sortBy property in todosPageParameters and sets the active class on the appropriate button.
 */
function sortByDueDate() {
    todosPageParameters.sortBy = 0;
    sortByDueDateSettingsButton?.classList.add('active');
    sortByCreatedDateSettingsButton?.classList.remove('active');
    sortByStartDateSettingsButton?.classList.remove('active');
    sortByCompletedDateSettingsButton?.classList.remove('active');
}
/**
 * Sorts the todo items by created date.
 * Updates the sortBy property in todosPageParameters and sets the active class on the appropriate button.
 */
function sortByCreatedDate() {
    todosPageParameters.sortBy = 1;
    sortByDueDateSettingsButton?.classList.remove('active');
    sortByCreatedDateSettingsButton?.classList.add('active');
    sortByStartDateSettingsButton?.classList.remove('active');
    sortByCompletedDateSettingsButton?.classList.remove('active');
}
/**
 * Sorts the todo items by start date.
 * Updates the sortBy property in todosPageParameters and sets the active class on the appropriate button.
 */
function sortByStartDate() {
    todosPageParameters.sortBy = 2;
    sortByDueDateSettingsButton?.classList.remove('active');
    sortByCreatedDateSettingsButton?.classList.remove('active');
    sortByStartDateSettingsButton?.classList.add('active');
    sortByCompletedDateSettingsButton?.classList.remove('active');
}
/**
 * Sorts the todo items by completed date.
 * Updates the sortBy property in todosPageParameters and sets the active class on the appropriate button.
 */
function sortByCompletedDate() {
    todosPageParameters.sortBy = 3;
    sortByDueDateSettingsButton?.classList.remove('active');
    sortByCreatedDateSettingsButton?.classList.remove('active');
    sortByStartDateSettingsButton?.classList.remove('active');
    sortByCompletedDateSettingsButton?.classList.add('active');
}
/**
 * Disables grouping of the todo items.
 * Updates the groupBy property in todosPageParameters and sets the active class on the appropriate button.
 */
function groupByNone() {
    todosPageParameters.groupBy = 0;
    groupByNoneSettingsButton?.classList.add('active');
    groupByStatusSettingsButton?.classList.remove('active');
    groupByAssignedToSettingsButton?.classList.remove('active');
}
/**
 * Groups the todo items by status.
 * Updates the groupBy property in todosPageParameters and sets the active class on the appropriate button.
 */
function groupByStatus() {
    todosPageParameters.groupBy = 1;
    groupByNoneSettingsButton?.classList.remove('active');
    groupByStatusSettingsButton?.classList.add('active');
    groupByAssignedToSettingsButton?.classList.remove('active');
}
/**
 * Groups the todo items by the user they are assigned to.
 * Updates the groupBy property in todosPageParameters and sets the active class on the appropriate button.
 */
function groupByAssignedTo() {
    todosPageParameters.groupBy = 2;
    groupByNoneSettingsButton?.classList.remove('active');
    groupByStatusSettingsButton?.classList.remove('active');
    groupByAssignedToSettingsButton?.classList.add('active');
}
/**
 * Sets event listeners for the items per page buttons.
 * Decreases or increases the number of todo items displayed per page.
 */
function setEventListenersForItemsPerPage() {
    const decreaseItemsPerPageButton = document.querySelector('#decrease-todo-items-per-page-button');
    const increaseItemsPerPageButton = document.querySelector('#increase-todo-items-per-page-button');
    if (decreaseItemsPerPageButton !== null) {
        decreaseItemsPerPageButton.addEventListener('click', decreaseTodoItemsPerPage);
    }
    if (increaseItemsPerPageButton !== null) {
        increaseItemsPerPageButton.addEventListener('click', increaseTodoItemsPerPage);
    }
}
/**
 * Toggles the active state of the tag filter button and updates the tag filter in the parameters.
 */
function toggleShowFilters() {
    const filtersElements = document.querySelectorAll('.todos-filter-options');
    const toggleShowFiltersChevron = document.getElementById('show-filters-chevron');
    filtersElements.forEach(function (element) {
        if (element.classList.contains('d-none')) {
            element.classList.remove('d-none');
        }
        else {
            element.classList.add('d-none');
        }
    });
    if (toggleShowFiltersChevron !== null) {
        if (toggleShowFiltersChevron.classList.contains('chevron-right-rotate-down')) {
            toggleShowFiltersChevron.classList.remove('chevron-right-rotate-down');
        }
        else {
            toggleShowFiltersChevron.classList.add('chevron-right-rotate-down');
        }
    }
}
/**
 * Adds or removes the TimeLineType in the onThisDayParameters.timeLineTypeFilter.
 * @param type The TimeLineType to toggle.
 */
function toggleTodoFilterStatusType(type) {
    const index = todosPageParameters.statusFilter.indexOf(type);
    if (index > -1) {
        todosPageParameters.statusFilter.splice(index, 1);
    }
    else {
        todosPageParameters.statusFilter.push(type);
    }
    updateTodosFilterStatusButtons();
}
/**
 * Updates the status filter buttons to reflect the current status filter in todosPageParameters.
 */
function updateTodosFilterStatusButtons() {
    // If status filter is null or empty, set default status filters.
    if (todosPageParameters.statusFilter === null || todosPageParameters.statusFilter.length === 0) {
        todosPageParameters.statusFilter.push(pageModels.TodoStatusType.NotStarted);
        todosPageParameters.statusFilter.push(pageModels.TodoStatusType.InProgress);
        todosPageParameters.statusFilter.push(pageModels.TodoStatusType.Completed);
    }
    const typeButtons = document.querySelectorAll('.filter-status-type-button');
    typeButtons.forEach(function (button) {
        button.classList.remove('active');
        if (todosPageParameters.statusFilter.includes(parseInt(button.dataset.filterStatusType ?? '-1'))) {
            button.classList.add('active');
        }
    });
}
/**
 * Toggles the assigned to filter for a specific progeny ID.
 * If the progeny ID is already in the assignedToFilter, it removes it.
 * Otherwise, it adds the progeny ID to the assignedToFilter.
 * @param progenyId The ID of the progeny to toggle in the assigned to filter.
 */
function toggleTodoFilterAssignedTo(progenyId) {
    if (todosPageParameters.progenies.includes(progenyId)) {
        const index = todosPageParameters.progenies.indexOf(progenyId);
        if (index > -1) {
            todosPageParameters.progenies.splice(index, 1);
        }
    }
    else {
        todosPageParameters.progenies.push(progenyId);
    }
    updateTodosFilterAssignedToButtons();
}
/**
 * Updates the assigned to filter buttons to reflect the current assigned to filter in todosPageParameters.
 */
function updateTodosFilterAssignedToButtons() {
    const assignedToButtons = document.querySelectorAll('.filter-assigned-to-button');
    assignedToButtons.forEach(function (button) {
        const checkedSpan = button.querySelector('.filter-progeny-check-span');
        if (checkedSpan !== null && todosPageParameters.progenies.includes(parseInt(button.dataset.filterAssignedTo ?? '-1'))) {
            checkedSpan.classList.remove('d-none');
        }
        else if (checkedSpan !== null) {
            checkedSpan.classList.add('d-none');
        }
    });
}
/** Gets the formatted date value from the start date picker and sets the start date in the parameters.
* @param dateTimeFormatMoment The Moment format of the date, which is used by the date picker.
*/
async function setStartDate(dateTimeFormatMoment) {
    let settingsStartValue = SettingsHelper.getPageSettingsStartDate(dateTimeFormatMoment);
    todosPageParameters.startYear = settingsStartValue.year();
    todosPageParameters.startMonth = settingsStartValue.month() + 1;
    todosPageParameters.startDay = settingsStartValue.date();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Gets the formatted date value from the end date picker and sets the end date in the parameters.
* @param dateTimeFormatMoment The Moment format of the date, which is used by the date picker.
*/
async function setEndDate(dateTimeFormatMoment) {
    let settingsEndValue = SettingsHelper.getPageSettingsEndDate(dateTimeFormatMoment);
    todosPageParameters.endYear = settingsEndValue.year();
    todosPageParameters.endMonth = settingsEndValue.month() + 1;
    todosPageParameters.endDay = settingsEndValue.date();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Sets the value of the startDateTimePicker to the given date in the format defined by startDateTimeFormatMoment.
 * @param date The date to assign to the DateTimePicker.
 */
function updateStartDatePicker(date) {
    if (todosStartDateTimePicker !== null) {
        const todosDateTimeFormatMoment = getLongDateTimeFormatMoment();
        const dateString = getFormattedDateString(date, todosDateTimeFormatMoment);
        todosStartDateTimePicker.val(dateString);
    }
}
/**
 * Sets the value of the endDateTimePicker to the given date in the format defined by startDateTimeFormatMoment.
 * @param date The date to assign to the DateTimePicker.
 */
function updateEndDatePicker(date) {
    if (todosEndDateTimePicker !== null) {
        const todosDateTimeFormatMoment = getLongDateTimeFormatMoment();
        const dateString = getFormattedDateString(date, todosDateTimeFormatMoment);
        todosEndDateTimePicker.val(dateString);
    }
}
/**
 * Loads the todos page settings from local storage and applies them to the page.
 * If no settings are found, it uses default values.
 */
function loadTodosPageSettings() {
    const pageSettingsFromStorage = SettingsHelper.getPageSettings(todosPageSettingsStorageKey);
    if (pageSettingsFromStorage !== null) {
        todosPageParameters.sort = pageSettingsFromStorage.sort ?? 1;
        if (todosPageParameters.sort === 0) {
            sortTodosAscending();
        }
        else {
            sortTodosDescending();
        }
        todosPageParameters.itemsPerPage = pageSettingsFromStorage.itemsPerPage ?? 10;
        todosPageParameters.sortBy = pageSettingsFromStorage.sortBy ?? 0;
        todosPageParameters.groupBy = pageSettingsFromStorage.groupBy ?? 0;
        todosPageParameters.statusFilter = pageSettingsFromStorage.statusFilter;
    }
    if (itemsPerPageInput !== null) {
        itemsPerPageInput.value = todosPageParameters.itemsPerPage.toString();
    }
    if (todosPageParameters.sortBy === 0) {
        sortByDueDate();
    }
    else if (todosPageParameters.sortBy === 1) {
        sortByCreatedDate();
    }
    else if (todosPageParameters.sortBy === 2) {
        sortByStartDate();
    }
    else if (todosPageParameters.sortBy === 3) {
        sortByCompletedDate();
    }
    if (todosPageParameters.groupBy === 0) {
        groupByNone();
    }
    else if (todosPageParameters.groupBy === 1) {
        groupByStatus();
    }
    else if (todosPageParameters.groupBy === 2) {
        groupByAssignedTo();
    }
    updateTodosFilterStatusButtons();
}
/**
 * Configures the elements in the settings panel.
 */
async function initialSettingsPanelSetup() {
    const todosPageSaveSettingsButton = document.querySelector('#todos-page-save-settings-button');
    if (todosPageSaveSettingsButton !== null) {
        todosPageSaveSettingsButton.addEventListener('click', saveTodosPageSettings);
    }
    if (sortAscendingSettingsButton !== null && sortDescendingSettingsButton !== null) {
        sortAscendingSettingsButton.addEventListener('click', sortTodosAscending);
        sortDescendingSettingsButton.addEventListener('click', sortTodosDescending);
    }
    setEventListenersForItemsPerPage();
    if (sortByDueDateSettingsButton !== null) {
        sortByDueDateSettingsButton.addEventListener('click', sortByDueDate);
    }
    if (sortByCreatedDateSettingsButton !== null) {
        sortByCreatedDateSettingsButton.addEventListener('click', sortByCreatedDate);
    }
    if (sortByStartDateSettingsButton !== null) {
        sortByStartDateSettingsButton.addEventListener('click', sortByStartDate);
    }
    if (sortByCompletedDateSettingsButton !== null) {
        sortByCompletedDateSettingsButton.addEventListener('click', sortByCompletedDate);
    }
    if (groupByNoneSettingsButton !== null) {
        groupByNoneSettingsButton.addEventListener('click', groupByNone);
    }
    if (groupByStatusSettingsButton !== null) {
        groupByStatusSettingsButton.addEventListener('click', groupByStatus);
    }
    if (groupByAssignedToSettingsButton !== null) {
        groupByAssignedToSettingsButton.addEventListener('click', groupByAssignedTo);
    }
    const toggleShowFiltersButton = document.querySelector('#todos-toggle-filters-button');
    if (toggleShowFiltersButton !== null) {
        toggleShowFiltersButton.addEventListener('click', function (event) {
            event.preventDefault();
            toggleShowFilters();
        });
    }
    const statusTypeButtons = document.querySelectorAll('.filter-status-type-button');
    statusTypeButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            toggleTodoFilterStatusType(parseInt(button.dataset.filterStatusType ?? '-1'));
        });
        if (todosPageParameters.statusFilter.includes(parseInt(button.dataset.filterStatusType ?? '-1'))) {
            button.classList.add('active');
        }
        else {
            button.classList.remove('active');
        }
    });
    updateTodosFilterAssignedToButtons();
    const assignedToButtons = document.querySelectorAll('.filter-assigned-to-button');
    assignedToButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            toggleTodoFilterAssignedTo(parseInt(button.dataset.filterAssignedTo ?? '-1'));
        });
        if (todosPageParameters.progenies.includes(parseInt(button.dataset.filterAssignedTo ?? '-1'))) {
            button.classList.add('active');
        }
        else {
            button.classList.remove('active');
        }
    });
    setMomentLocale();
    const zebraDateTimeFormat = getZebraDateTimeFormat();
    const zebraDatePickerTranslations = await LocaleHelper.getZebraDatePickerTranslations(todosPageParameters.languageId);
    const todosDateTimeFormatMoment = getLongDateTimeFormatMoment();
    todosStartDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { setStartDate(todosDateTimeFormatMoment); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    todosEndDateTimePicker.Zebra_DatePicker({
        format: zebraDateTimeFormat,
        open_icon_only: true,
        onSelect: function (a, b, c) { setEndDate(todosDateTimeFormatMoment); },
        days: zebraDatePickerTranslations.daysArray,
        months: zebraDatePickerTranslations.monthsArray,
        lang_clear_date: zebraDatePickerTranslations.clearString,
        show_select_today: zebraDatePickerTranslations.todayString,
        select_other_months: true
    });
    const startPickerInput = document.getElementById('settings-start-date-datetimepicker');
    if (startPickerInput) {
        startPickerInput.addEventListener('blur', function () {
            if (!validateDateValue(startPickerInput.value, getLongDateTimeFormatMoment())) {
                startPickerInput.value = '';
                todosPageParameters.startYear = 0;
                todosPageParameters.startMonth = 0;
                todosPageParameters.startDay = 0;
                // Todo: show error and focus on the input.
            }
        });
    }
    const endPickerInput = document.getElementById('settings-end-date-datetimepicker');
    if (endPickerInput) {
        endPickerInput.addEventListener('blur', function () {
            if (!validateDateValue(endPickerInput.value, getLongDateTimeFormatMoment())) {
                endPickerInput.value = '';
                todosPageParameters.endYear = 0;
                todosPageParameters.endMonth = 0;
                todosPageParameters.endDay = 0;
                // Todo: show error and focus on the input.
            }
        });
    }
    await setTagsAutoSuggestList(todosPageParameters.progenies, 'tag-filter-input', true);
    await setContextAutoSuggestList(todosPageParameters.progenies, 'context-filter-input', true);
    updateSettingsNotificationDiv();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/**
 * Updates the settings notification div with the current settings.
 * This function is called when the settings are changed or saved.
 */
function updateSettingsNotificationDiv() {
    let todoSettingsNotificationText = '';
    const settingsSortLabelDiv = document.querySelector('settings-sort-label');
    if (settingsSortLabelDiv !== null) {
        todoSettingsNotificationText = settingsSortLabelDiv.innerHTML;
    }
    if (todosPageParameters.sort === 0) {
        todoSettingsNotificationText += ' ' + sortAscendingSettingsButton?.innerHTML;
    }
    else {
        todoSettingsNotificationText += ' ' + sortDescendingSettingsButton?.innerHTML;
    }
    const itemsPerPageSpan = document.querySelector('#settings-items-per-page-label');
    if (itemsPerPageSpan !== null) {
        todoSettingsNotificationText += '<br/>' + itemsPerPageSpan.innerHTML + ' ' + todosPageParameters.itemsPerPage;
    }
    const settingsSortByLabelDiv = document.querySelector('#settings-sort-by-label');
    if (settingsSortByLabelDiv !== null) {
        todoSettingsNotificationText += '<br/>' + settingsSortByLabelDiv.innerHTML;
    }
    if (todosPageParameters.sortBy === 0) {
        todoSettingsNotificationText += ' ' + sortByDueDateSettingsButton?.innerHTML;
    }
    else if (todosPageParameters.sortBy === 1) {
        todoSettingsNotificationText += ' ' + sortByCreatedDateSettingsButton?.innerHTML;
    }
    else if (todosPageParameters.sortBy === 2) {
        todoSettingsNotificationText += ' ' + sortByStartDateSettingsButton?.innerHTML;
    }
    else if (todosPageParameters.sortBy === 3) {
        todoSettingsNotificationText += ' ' + sortByCompletedDateSettingsButton?.innerHTML;
    }
    const settingsGroupByLabelDiv = document.querySelector('#settings-group-by-label');
    if (settingsGroupByLabelDiv !== null) {
        todoSettingsNotificationText += '<br/>' + settingsGroupByLabelDiv.innerHTML;
    }
    if (todosPageParameters.groupBy === 0) {
        todoSettingsNotificationText += ' ' + groupByNoneSettingsButton?.innerHTML;
    }
    else if (todosPageParameters.groupBy === 1) {
        todoSettingsNotificationText += ' ' + groupByStatusSettingsButton?.innerHTML;
    }
    else if (todosPageParameters.groupBy === 2) {
        todoSettingsNotificationText += ' ' + groupByAssignedToSettingsButton?.innerHTML;
    }
    const settingsStatusFilterLabelSpan = document.querySelector('#settings-status-filter-label');
    if (settingsStatusFilterLabelSpan !== null) {
        todoSettingsNotificationText += '<br/>' + settingsStatusFilterLabelSpan.innerHTML;
    }
    const statusTypeButtons = document.querySelectorAll('.filter-status-type-button');
    statusTypeButtons.forEach(function (button) {
        if (button.classList.contains('active')) {
            todoSettingsNotificationText += ' [' + button.innerHTML + ']';
        }
    });
    if (todosPageParameters.startYear > 0 && todosPageParameters.startMonth > 0 && todosPageParameters.startDay > 0) {
        const settingsStartLabel = document.querySelector('#start-filter-span');
        if (settingsStartLabel !== null) {
            todoSettingsNotificationText += '<br/>' + settingsStartLabel.innerHTML + ': ';
        }
        const settingsStartDateInput = document.querySelector('#settings-start-date-datetimepicker');
        todoSettingsNotificationText += settingsStartDateInput?.value ?? '';
    }
    if (todosPageParameters.endYear > 0 && todosPageParameters.endMonth > 0 && todosPageParameters.endDay > 0) {
        const settingsEndLabel = document.querySelector('#end-filter-span');
        if (settingsEndLabel !== null) {
            todoSettingsNotificationText += '<br/>' + settingsEndLabel.innerHTML + ': ';
        }
        const settingsEndDateInput = document.querySelector('#settings-end-date-datetimepicker');
        todoSettingsNotificationText += settingsEndDateInput?.value ?? '';
    }
    const contextFilterInput = document.querySelector('#context-filter-input');
    if (contextFilterInput !== null && contextFilterInput.value.length > 0) {
        const settingsContextFilterLabel = document.querySelector('#context-filter-span');
        if (settingsContextFilterLabel !== null) {
            todoSettingsNotificationText += '<br/>' + settingsContextFilterLabel.innerHTML;
        }
        todoSettingsNotificationText += ' [' + contextFilterInput.value + ']';
    }
    const tagFilterInput = document.querySelector('#tag-filter-input');
    if (tagFilterInput !== null && tagFilterInput.value.length > 0) {
        const settingsTagFilterLabel = document.querySelector('#tag-filter-span');
        if (settingsTagFilterLabel !== null) {
            todoSettingsNotificationText += '<br/>' + settingsTagFilterLabel.innerHTML;
        }
        todoSettingsNotificationText += ' [' + tagFilterInput.value + ']';
    }
    const settingsNotificationsDiv = document.querySelector('#settings-notification-div');
    if (settingsNotificationsDiv !== null && todoSettingsNotificationText !== undefined) {
        settingsNotificationsDiv.innerHTML = todoSettingsNotificationText;
    }
}
/**
 * Saves the current page parameters to local storage and reloads the todo items list.
 */
async function saveTodosPageSettings() {
    const numberOfItemsToGetInput = document.querySelector('#todo-items-per-page-input');
    if (numberOfItemsToGetInput !== null) {
        todosPageParameters.itemsPerPage = parseInt(numberOfItemsToGetInput.value);
    }
    else {
        todosPageParameters.itemsPerPage = 10;
    }
    todosPageParameters.sort = sortAscendingSettingsButton?.classList.contains('active') ? 0 : 1;
    const tagFilterInput = document.querySelector('#tag-filter-input');
    if (tagFilterInput !== null) {
        todosPageParameters.tagFilter = tagFilterInput.value;
    }
    const contextFilterInput = document.querySelector('#context-filter-input');
    if (contextFilterInput !== null) {
        todosPageParameters.contextFilter = contextFilterInput.value;
    }
    // If the 'set as default' checkbox is checked, save the page settings to local storage.
    const setAsDefaultCheckbox = document.querySelector('#todo-settings-save-default-checkbox');
    if (setAsDefaultCheckbox !== null && setAsDefaultCheckbox.checked) {
        SettingsHelper.savePageSettings(todosPageSettingsStorageKey, todosPageParameters);
    }
    SettingsHelper.toggleShowPageSettings();
    clearTodoItemsElements();
    updateSettingsNotificationDiv();
    await getTodos();
    return new Promise(function (resolve, reject) {
        resolve();
    });
}
/** Initializes the Todos page by setting up event listeners and fetching initial data.
 * This function is called when the DOM content is fully loaded.
 */
document.addEventListener('DOMContentLoaded', async function () {
    await showPopupAtLoad(pageModels.TimeLineType.TodoItem);
    setTodosPageParametersFromPageData();
    loadTodosPageSettings();
    addSelectedProgeniesChangedEventListener();
    todosPageParameters.progenies = getSelectedProgenies();
    moreTodoItemsButton = document.querySelector('#more-todo-items-button');
    if (moreTodoItemsButton !== null) {
        moreTodoItemsButton.addEventListener('click', async () => {
            getTodos();
        });
    }
    SettingsHelper.initPageSettings();
    initialSettingsPanelSetup();
    getTodos();
    return new Promise(function (resolve, reject) {
        resolve();
    });
});
//# sourceMappingURL=todos-index.js.map