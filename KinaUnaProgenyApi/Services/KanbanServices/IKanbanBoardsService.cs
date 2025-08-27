using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    public interface IKanbanBoardsService
    {
        Task<KanbanBoard> GetKanbanBoardById(int kanbanBoardId);
        Task<KanbanBoard> AddKanbanBoard(KanbanBoard kanbanBoard);
        Task<KanbanBoard> UpdateKanbanBoard(KanbanBoard kanbanBoard);
        Task<KanbanBoard> DeleteKanbanBoard(KanbanBoard existingKanbanBoard);
    }
}
