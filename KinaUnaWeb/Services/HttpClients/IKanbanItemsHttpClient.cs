using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface IKanbanItemsHttpClient
    {
        Task<KanbanItem> AddKanbanItem(KanbanItem kanbanItem);
        Task<KanbanItem> DeleteKanbanItem(KanbanItem kanbanItem);
        Task<KanbanItem> GetKanbanItem(int kanbanItemId);
        /// <summary>
        /// Retrieves a list of Kanban items associated with the specified Kanban board.
        /// </summary>
        /// <param name="kanbanBoardId">The Id of the Kanban board to get items for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see
        /// cref="KanbanItem"/> objects associated with the specified Kanban board. Each item includes  its
        /// corresponding to-do item details.</returns>
        Task<List<KanbanItem>> GetKanbanItemsForBoard(int kanbanBoardId);
        Task<List<KanbanItem>> GetKanbanItemsForTodoItem(int todoItemId);
        Task<KanbanItem> UpdateKanbanItem(KanbanItem kanbanItem);
    }
}
