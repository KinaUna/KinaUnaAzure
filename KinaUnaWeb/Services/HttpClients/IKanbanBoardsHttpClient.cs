using KinaUna.Data.Models.DTOs;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface IKanbanBoardsHttpClient
    {
        Task<KanbanBoard> AddKanbanBoard(KanbanBoard kanbanBoard);
        Task<KanbanBoard> GetKanbanBoard(int kanbanBoardId);
        Task<KanbanBoardsResponse> GetProgeniesKanbanBoardsList(KanbanBoardsRequest kanbanBoardsRequest);
        Task<KanbanBoard> UpdateKanbanBoard(KanbanBoard kanbanBoard);
    }
}
