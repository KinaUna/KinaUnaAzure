using KinaUna.Data.Models.DTOs;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface ITodoItemsHttpClient
    {
        Task<TodoItem> GetTodoItem(int itemId);
        Task<TodoItemsResponse> GetProgeniesTodoItemsList(TodoItemsRequest request);
        Task<TodoItem> AddTodoItem(TodoItem todoItem);
        Task<TodoItem> UpdateTodoItem(TodoItem todoItem);
        Task<bool> DeleteTodoItem(int todoItemId);
    }
}
