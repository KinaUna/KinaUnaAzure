import { task } from 'grunt';
import { startFullPageSpinner, stopFullPageSpinner } from '../navigation-tools-v8.js';

let currentScheduledTasks: BackgroundTaskDto[] = [];
let commands: string[] = [];

/**
 * Gets the list of scheduled tasks from the server and populates the tasks-list-table with the data.
 * @returns
 */
async function getScheduledTasks(): Promise<void> {
    startFullPageSpinner();
    
    await fetch('/Admin/LoadScheduledTasks', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    }).then(async function (getScheduledTasksResult) {
        if (getScheduledTasksResult != null) {
            console.log('getScheduledTasksResult: ');
            console.log(getScheduledTasksResult);
            currentScheduledTasks = (await getScheduledTasksResult.json()) as BackgroundTaskDto[];
            setScheduledTasksTableContent(currentScheduledTasks);
            addNewTaskItemToTable();
        }
    }).catch(function (error) {
        console.log('Error loading scheduled tasks. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Populates the tasks-list-table with the data from the scheduledTasks array.
 * Adds a header, then each task item.
 * @param scheduledTasks The object containing the array of tasks to add to the table. 
 */
function setScheduledTasksTableContent(scheduledTasks: BackgroundTaskDto[]): void {
    const scheduledTasksListTable = document.querySelector<HTMLTableElement>('#tasks-list-table');
    if (scheduledTasksListTable !== null) {
        const headerRow = document.querySelector<HTMLTableRowElement>('#tasks-list-table-header');
        scheduledTasksListTable.innerHTML = '';
        if (headerRow !== null) {
            scheduledTasksListTable.appendChild(headerRow);
        }
                
        scheduledTasks.forEach((taskItem) => {
            addTaskItemToTable(taskItem);
        });
    }
}

/**
 * Adds a row to the table for the given task item, with columns for id, name, description, command, parameters, etc., and the add,delete actions.
 * If the taskItem has a taskId of 0, it is for adding a new task, and the save button is replaced with an add button.
 * @param taskItem The BackgroundTaskDto object to add to the table.
 */
function addTaskItemToTable(taskItem: BackgroundTaskDto): void {
    const tasksListTable = document.querySelector<HTMLTableElement>('#tasks-list-table');
    if (tasksListTable === null) {
        return;
    }

    let rowToInsert = document.createElement('tr');

    let taskIdColumn = createIdColumn(taskItem.taskId);

    let nameColumn = createNameColumn(taskItem.taskName, taskItem.taskId);

    let descriptionColumn = createDescriptionColumn(taskItem.taskDescription, taskItem.taskId);

    let commandColumn = createCommandColumn(taskItem.command, taskItem.taskId);

    let parametersColumn = createParametersColumn(taskItem.parameters, taskItem.taskId);

    let isEnabledColumn = createIsEnabledColumn(taskItem.isEnabled, taskItem.taskId);

    let lastRunColumn = createLastRunColumn(taskItem.lastRun, taskItem.taskId);

    let isRunningColumn = createIsRunningColumn(taskItem.isRunning, taskItem.taskId);

    let intervalMinutesColumn = createIntervalMinutesColumn(taskItem.interval, taskItem.taskId);

    let saveColumn = createSaveButtonColumn(taskItem.taskId);

    let deleteColumn = createDeleteColumn(taskItem.taskId);

    rowToInsert.appendChild(taskIdColumn);
    rowToInsert.appendChild(isEnabledColumn);
    rowToInsert.appendChild(nameColumn);    
    
    let rowToInsert2 = document.createElement('tr');
    rowToInsert2.appendChild(descriptionColumn);
    

    let rowToInsert3 = document.createElement('tr');
    rowToInsert3.appendChild(commandColumn);
    
    let rowToInsert4 = document.createElement('tr');
    rowToInsert4.appendChild(parametersColumn);
        
    let rowToInsert5 = document.createElement('tr');
    rowToInsert5.appendChild(intervalMinutesColumn);
        
    let rowToInsert6 = document.createElement('tr');
    rowToInsert6.appendChild(lastRunColumn);
    rowToInsert6.appendChild(isRunningColumn);

    let rowToInsert7 = document.createElement('tr');
    if (taskItem.taskId === 0) {
        let addNewColumn = document.createElement('td');
        let addNewButton = document.createElement('button');
        addNewButton.classList.add('btn');
        addNewButton.classList.add('btn-primary');
        addNewButton.innerHTML = 'Add New Task';
        addNewButton.addEventListener('click', async function () {
            await addNewTask();
        });

        addNewColumn.appendChild(addNewButton);

        rowToInsert7.appendChild(addNewColumn);
    }
    else {
        rowToInsert7.appendChild(saveColumn);
        rowToInsert7.appendChild(deleteColumn);
    }
    

    let rowToInsert8 = document.createElement('tr');
    rowToInsert8.appendChild(createSpaceRow());

    tasksListTable.appendChild(rowToInsert);
    tasksListTable.appendChild(rowToInsert2);
    tasksListTable.appendChild(rowToInsert3);
    tasksListTable.appendChild(rowToInsert4);
    tasksListTable.appendChild(rowToInsert5);
    if (taskItem.taskId !== 0) {
        tasksListTable.appendChild(rowToInsert6);
    }
    tasksListTable.appendChild(rowToInsert7);
    tasksListTable.appendChild(rowToInsert8);
}

/**
 * Adds fields for creating a new task item to the table.
 */
function addNewTaskItemToTable() {
    addTaskItemToTable(new BackgroundTaskDto());
}

/**
 * Data transfer object for a scheduled task.
 */
class BackgroundTaskDto {
    taskId: number = 0;
    taskName: string = "";
    taskDescription: string = "";
    command: string = "";
    parameters: string = "";
    lastRun: string = "";
    interval: number = 0; // In minutes.
    isEnabled: boolean = false;
    isRunning: boolean = false;
    
}

/**
 * Creates a row with a single column containing a non-breaking space.
 * @returns
 */
function createSpaceRow(): HTMLTableRowElement {
    let spaceRow = document.createElement('tr');
    let spaceColumn = document.createElement('td');
    spaceColumn.innerHTML = '&nbsp;';
    spaceRow.appendChild(spaceColumn);

    return spaceRow;

}

/**
 * Creates a column with the taskId.
 * @param taskId
 * @returns HTMLTableCellElement
 */
function createIdColumn(taskId: number): HTMLTableCellElement {
    let idColumn = document.createElement('td');
    idColumn.rowSpan = 8;
    if (taskId !== 0) {
        idColumn.innerHTML = taskId.toString();
    }    

    return idColumn;
}

/**
 * Creates a column with a save button.
 * @param taskId
 * @returns HTMLTableCellElement
 */
function createSaveButtonColumn(taskId: number): HTMLTableCellElement {
    let saveButtonColumn = document.createElement('td');
    saveButtonColumn.colSpan = 3;

    let saveButton = document.createElement('button');
    saveButton.classList.add('btn');
    saveButton.classList.add('btn-success');
    saveButton.id = 'save-button' + taskId;
    saveButton.innerHTML = 'Save';
    saveButton.addEventListener('click', async function () {
        await saveTask(taskId);
    });

    saveButtonColumn.appendChild(saveButton);

    return saveButtonColumn;
}

/**
 * Creates a column with a name input field.
 * @param taskName The name of the task.
 * @param taskId The id of the task.
 * @returns HTMLTableCellElement
 */
function createNameColumn(taskName: string, taskId: number): HTMLTableCellElement {
    let nameColumn = document.createElement('td');
    nameColumn.colSpan = 4;
    let nameLabel = document.createElement('label');
    nameLabel.classList.add('control-label');
    nameLabel.classList.add('pt-2');
    if (taskId === 0) {
        nameLabel.innerHTML = 'Add New Scheduled Task:';   
    }
    else {
        nameLabel.innerHTML = 'Task name:'
    }
    nameLabel.htmlFor = 'task-description-input' + taskId;
    nameColumn.appendChild(nameLabel);

    let nameInput = document.createElement('input');
    nameInput.id = 'task-name-input' + taskId;
    nameInput.value = taskName;
    nameInput.classList.add('w-100');
    nameInput.classList.add('form-control');
    nameColumn.appendChild(nameInput);

    return nameColumn;
}

/**
 * Creates a column with a description textarea input.
 * @param taskDescription The description of the task.
 * @param taskId The id of the task.
 * @returns HTMLTableCellElement
 */
function createDescriptionColumn(taskDescription: string, taskId: number): HTMLTableCellElement {
    let descriptionColumn = document.createElement('td');
    descriptionColumn.colSpan = 4;
    let descriptionLabel = document.createElement('label');
    descriptionLabel.classList.add('control-label');
    descriptionLabel.classList.add('pt-2');
    descriptionLabel.innerHTML = 'Description:';
    descriptionLabel.htmlFor = 'task-description-input' + taskId;
    descriptionColumn.appendChild(descriptionLabel);

    let descriptionInput = document.createElement('textarea');
    descriptionInput.rows = 2;
    descriptionInput.id = 'task-description-input' + taskId;
    descriptionInput.value = taskDescription;
    descriptionInput.classList.add('w-100');
    descriptionInput.classList.add('form-control');
    descriptionColumn.appendChild(descriptionInput);

    return descriptionColumn;
}

/**
 * Creates a column with a command input field.
 * @param apiEndpoint The command of the task.
 * @param taskId The id of the task.
 * @returns HTMLTableCellElement
 */
function createCommandColumn(apiEndpoint: string, taskId: number): HTMLTableCellElement {
    let commandColumn = document.createElement('td');
    commandColumn.colSpan = 4;
    let commandLabel = document.createElement('label');
    commandLabel.classList.add('control-label');
    commandLabel.classList.add('pt-2');
    commandLabel.innerHTML = 'Command:';
    commandLabel.htmlFor = 'api-endpoint-input' + taskId;
    commandColumn.appendChild(commandLabel);

    //let commandInput = document.createElement('input');
    //commandInput.id = 'api-endpoint-input' + taskId;
    //commandInput.value = apiEndpoint;
    //commandInput.classList.add('w-100');
    //commandInput.classList.add('form-control');
    //commandColumn.appendChild(commandInput);

    let commandSelectList = addCommandsSelectList(taskId, apiEndpoint);
    commandColumn.appendChild(commandSelectList);

    return commandColumn;
}

/**
 * Creates a column with a parameters input field.
 * @param parameters The parameters of the task.
 * @param taskId The id of the task.
 * @returns HTMLTableCellElement
 */
function createParametersColumn(parameters: string, taskId: number): HTMLTableCellElement {
    let parametersColumn = document.createElement('td');
    parametersColumn.colSpan = 4;
    let parametersLabel = document.createElement('label');
    parametersLabel.classList.add('control-label');
    parametersLabel.classList.add('pt-2');
    parametersLabel.innerHTML = 'Parameters:';
    parametersLabel.htmlFor = 'parameters-input' + taskId;
    parametersColumn.appendChild(parametersLabel);

    let parametersInput = document.createElement('input');
    parametersInput.id = 'parameters-input' + taskId;
    parametersInput.value = parameters;
    parametersInput.classList.add('w-100');
    parametersInput.classList.add('form-control');
    parametersColumn.appendChild(parametersInput);

    return parametersColumn;
}

/**
 * Creates a column with a isEnabled input field.
 * @param isEnabled The isEnabled of the task.
 * @param taskId The id of the task.
 * @returns HTMLTableCellElement
 */
function createIsEnabledColumn(isEnabled: boolean, taskId: number): HTMLTableCellElement {
    let isEnabledColumn = document.createElement('td');
    isEnabledColumn.rowSpan = 8;
    let isEnabledInput = document.createElement('input');
    isEnabledInput.type = 'checkbox';
    isEnabledInput.id = 'is-enabled-input' + taskId;
    isEnabledInput.checked = isEnabled;
    isEnabledColumn.appendChild(isEnabledInput);

    return isEnabledColumn;
}

/**
 * Creates a column with the last run data.
 * @param lastRun The last run data.
 * @param taskId The id of the task.
 * @returns HTMLTableCellElement
 */
function createLastRunColumn(lastRun: string, taskId: number): HTMLTableCellElement {
    let lastRunColumn = document.createElement('td');
    lastRunColumn.colSpan = 2;

    let lastRunDiv = document.createElement('div');
    lastRunDiv.classList.add('pt-2');
    lastRunDiv.classList.add('pb-2');
    lastRunDiv.innerHTML = 'Last run: ' + lastRun;
    lastRunColumn.appendChild(lastRunDiv);

    return lastRunColumn;
}

/**
 * Creates a column with the isRunning data.
 * @param isRunning The isRunning data.
 * @param taskId The id of the task.
 * @returns HTMLTableCellElement
 */
function createIsRunningColumn(isRunning: boolean, taskId: number): HTMLTableCellElement {
    let isRunningColumn = document.createElement('td');
    isRunningColumn.colSpan = 2;
    let isRunningDiv = document.createElement('div');
    isRunningDiv.classList.add('pt-2');
    isRunningDiv.classList.add('pb-2');

    let isRunningSpan = document.createElement('span');
    isRunningSpan.innerHTML = 'IsRunning: ' + isRunning.toString();
    isRunningSpan.classList.add('float-right');
    isRunningDiv.appendChild(isRunningSpan);
    isRunningColumn.appendChild(isRunningDiv);

    return isRunningColumn;
}

/**
 * Creates a column with an interval input field.
 * @param intervalMinutes The interval, in minutes, of the task.
 * @param taskId The id of the task.
 * @returns HTMLTableCellElement
 */
function createIntervalMinutesColumn(intervalMinutes: number, taskId: number): HTMLTableCellElement {
    let intervalMinutesColumn = document.createElement('td');
    intervalMinutesColumn.colSpan = 1;
    let intervalMinutesLabel = document.createElement('label');
    intervalMinutesLabel.classList.add('control-label');
    intervalMinutesLabel.classList.add('pt-2');
    intervalMinutesLabel.innerHTML = 'Interval (minutes):';
    intervalMinutesLabel.htmlFor = 'interval-minutes-input' + taskId;
    intervalMinutesColumn.appendChild(intervalMinutesLabel);

    let intervalMinutesInput = document.createElement('input');
    intervalMinutesInput.type = 'number';
    intervalMinutesInput.id = 'interval-minutes-input' + taskId;
    intervalMinutesInput.value = intervalMinutes.toString();
    intervalMinutesInput.classList.add('form-control');
    intervalMinutesColumn.appendChild(intervalMinutesInput);

    return intervalMinutesColumn;
}

/**
 * Creates a column with a delete button.
 * @param taskId The id of the task.
 * @returns HTMLTableCellElement
 */
function createDeleteColumn(taskId: number): HTMLTableCellElement {
    let deleteColumn = document.createElement('td');
    let deleteButton = document.createElement('button');
    deleteButton.classList.add('btn');
    deleteButton.classList.add('btn-danger');

    deleteButton.innerHTML = 'Delete';
    deleteButton.addEventListener('click', async function () {
        await deleteTask(taskId);
    });

    deleteColumn.appendChild(deleteButton);

    return deleteColumn;
}

/**
 * Adds a new task to the list of scheduled tasks in the database.
 * Then refreshes the list of tasks.
 * @returns
 */
async function addNewTask(): Promise<void> {
    startFullPageSpinner();
    let taskId = 0;
    let taskNameInput = document.querySelector<HTMLInputElement>('#task-name-input' + taskId);
    let taskDescriptionInput = document.querySelector<HTMLInputElement>('#task-description-input' + taskId);
    let parametersInput = document.querySelector<HTMLInputElement>('#parameters-input' + taskId);
    let intervalMinutesInput = document.querySelector<HTMLInputElement>('#interval-minutes-input' + taskId);
    let isEnabledInput = document.querySelector<HTMLInputElement>('#is-enabled-input' + taskId);
    let commandSelectList = document.querySelector<HTMLSelectElement>('#command-select-list' + taskId);
    // Todo: validate input.

    let taskToUpdate: BackgroundTaskDto = new BackgroundTaskDto();
    taskToUpdate.taskId = taskId;
    taskToUpdate.taskName = taskNameInput?.value ?? '';
    taskToUpdate.taskDescription = taskDescriptionInput?.value ?? '';
    taskToUpdate.command = commandSelectList?.value ?? '';
    taskToUpdate.parameters = parametersInput?.value ?? '';
    taskToUpdate.interval = parseInt(intervalMinutesInput?.value ?? '0');
    taskToUpdate.isEnabled = isEnabledInput?.checked ?? false;

    await fetch('/Admin/AddScheduledTask', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(taskToUpdate)
    }).then(async function (saveTaskResult) {
        if (saveTaskResult != null) {
            await getScheduledTasks();
        }
    }).catch(function (error) {
        console.log('Error saving task. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Saves the task with the given taskId to the database.
 * Then refreshes the list of tasks.
 * @param taskId The id of the task to save.
 * @returns
 */
async function saveTask(taskId: number): Promise<void> {
    startFullPageSpinner();
        
    let taskNameInput = document.querySelector<HTMLInputElement>('#task-name-input' + taskId);
    let taskDescriptionInput = document.querySelector<HTMLInputElement>('#task-description-input' + taskId);
    let parametersInput = document.querySelector<HTMLInputElement>('#parameters-input' + taskId);
    let intervalMinutesInput = document.querySelector<HTMLInputElement>('#interval-minutes-input' + taskId);
    let isEnabledInput = document.querySelector<HTMLInputElement>('#is-enabled-input' + taskId);
    let commandSelectList = document.querySelector<HTMLSelectElement>('#command-select-list' + taskId);

    let taskToUpdate: BackgroundTaskDto = new BackgroundTaskDto();
    taskToUpdate.taskId = taskId;
    taskToUpdate.taskName = taskNameInput?.value ?? '';
    taskToUpdate.taskDescription = taskDescriptionInput?.value ?? '';
    taskToUpdate.command = commandSelectList?.value ?? '';
    taskToUpdate.parameters = parametersInput?.value ?? '';
    taskToUpdate.interval = parseInt(intervalMinutesInput?.value ?? '0');
    taskToUpdate.isEnabled = isEnabledInput?.checked ?? false;

    await fetch('/Admin/UpdateScheduledTask', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(taskToUpdate)
    }).then(async function (saveTaskResult) {
        if (saveTaskResult != null) {
            await getScheduledTasks();
        }
    }).catch(function (error) {
        console.log('Error saving task. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });

}

/**
 * Deletes the task with the given taskId from the database.
 * Then refreshes the list of tasks.
 * @param taskId The id of the task to delete.
 * @returns
 */
async function deleteTask(taskId: number): Promise<void> {
    startFullPageSpinner();

    await fetch('/Admin/DeleteScheduledTask', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: taskId.toString()
    }).then(async function (deleteTaskResult) {
        if (deleteTaskResult != null) {
            await getScheduledTasks();
        }
    }).catch(function (error) {
        console.log('Error deleting task. Error: ' + error);
    });

    stopFullPageSpinner();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Gets the list of commands from the server and updates the commands variable.
 * @returns
 */
async function getCommands(): Promise<void> {
    await fetch('/Admin/GetCommands', {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    }).then(async function (getCommandsResult) {
        if (getCommandsResult != null) {
            commands = (await getCommandsResult.json()) as string[];
        }
    }).catch(function (error) {
        console.log('Error loading commands. Error: ' + error);
    });

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
}

/**
 * Adds a select list with the available commands to the page.
 * @param taskId The id of the task.
 * @param selectedCommand The command that is selected by default.
 * @returns HTMLSelectElement
 */
function addCommandsSelectList(taskId: number, selectedCommand: string): HTMLSelectElement {
    let commandSelectList = document.createElement('select');
    commandSelectList.id = 'command-select-list' + taskId;
    commandSelectList.classList.add('form-control');
    commandSelectList.classList.add('w-100');
    commands.forEach((command) => {
        let commandOption = document.createElement('option');
        commandOption.value = command;
        commandOption.innerHTML = command;
        if (command === selectedCommand) {
            commandOption.selected = true;
        }
        commandSelectList.appendChild(commandOption);
    });

    return commandSelectList;
}

/**
 * Initialization of the page: Set event listeners.
 */
document.addEventListener('DOMContentLoaded', async function (): Promise<void> {
    await getCommands();
    await getScheduledTasks();

    return new Promise<void>(function (resolve, reject) {
        resolve();
    });
});