using KinaUna.Data.Models.DTOs;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface ISubtasksHttpClient
    {
        Task<TodoItem> GetSubtask(int itemId);
        Task<SubtasksResponse> GetSubtasksList(SubtasksRequest request);
        Task<TodoItem> AddSubtask(TodoItem subtask);
        Task<TodoItem> UpdateSubtask(TodoItem subtask);
        Task<bool> DeleteSubtask(int subtaskId);
    }
}
