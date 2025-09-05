using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface IKanbanItemsHttpClient
    {
        Task<KanbanItem> AddKanbanItem(KanbanItem kanbanItem);
        Task<KanbanItem> DeleteKanbanItem(KanbanItem kanbanItem);
        Task<KanbanItem> GetKanbanItem(int kanbanItemId);
        Task<List<KanbanItem>> GetKanbanItemsForBoard(int kanbanBoardId);
        Task<KanbanItem> UpdateKanbanItem(KanbanItem kanbanItem);
    }
}
