using KinaUna.Data.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface ITodoItemsHttpClient
    {
        Task<TodoItem> GetTodoItem(int itemId);
        Task<List<TodoItem>> GetProgeniesTodoItemsList(TodoItemsRequest request);
        Task<TodoItem> AddTodoItem(TodoItem todoItem);
        Task<TodoItem> UpdateTodoItem(TodoItem todoItem);
        Task<bool> DeleteTodoItem(int todoItemId);
    }
}
