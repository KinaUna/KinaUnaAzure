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
    return kanbanItems;
}
;
export async function displayKanbanItemDetails(kanbanItemId) {
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
    return success;
}
//# sourceMappingURL=kanban-items.js.map