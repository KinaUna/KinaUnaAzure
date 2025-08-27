using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    public interface IKanbanItemsService
    {
        public Task<KanbanItem> GetKanbanItemById(int kanbanItemId);
        Task<KanbanItem> AddKanbanItem(KanbanItem kanbanItem);
        Task<KanbanItem> UpdateKanbanItem(KanbanItem kanbanItem);
        Task<KanbanItem> DeleteKanbanItem(KanbanItem kanbanItem);
        Task<List<KanbanItem>> GetKanbanItemsForBoard(int kanbanBoardId);
    }
}
