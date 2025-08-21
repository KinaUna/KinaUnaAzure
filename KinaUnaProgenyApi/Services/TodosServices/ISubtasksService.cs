using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    public interface ISubtasksService
    {
        Task<TodoItem> AddSubtask(TodoItem value);
        Task<bool> DeleteSubtask(TodoItem subtask, bool hardDelete = false);
        Task<TodoItem> GetSubtask(int id);
        Task<List<TodoItem>> GetSubtasksForTodoItem(int todoItemTodoItemId);
        Task<TodoItem> UpdateSubtask(TodoItem value);
    }
}
