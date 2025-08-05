using KinaUna.Data.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    public class TodoItemsHttpClient : ITodoItemsHttpClient
    {
        public Task<TodoItem> GetCalendarItem(int popUpTodoItemId)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<TodoItem>> GetProgeniesTodoItemsList(TodoItemsRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
