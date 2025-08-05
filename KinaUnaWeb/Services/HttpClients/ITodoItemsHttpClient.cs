using KinaUna.Data.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface ITodoItemsHttpClient
    {
        Task<TodoItem> GetTodoItem(int itemId);
        Task<List<TodoItem>> GetProgeniesTodoItemsList(TodoItemsRequest request);
    }
}
