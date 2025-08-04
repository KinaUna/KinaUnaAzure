using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    public interface ITodosService
    {
        Task<TodoItem> GetTodoItem(int id);
        Task<List<TodoItem>> GetTodosForProgeny(int id, int accessLevel, TodoItemsRequest request);
    }
}
