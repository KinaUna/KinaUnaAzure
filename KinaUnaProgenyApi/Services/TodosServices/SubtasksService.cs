using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    public class SubtasksService: ISubtasksService
    {
        public async Task<TodoItem> AddSubtask(TodoItem value)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> DeleteSubtask(TodoItem subtask)
        {
            throw new System.NotImplementedException();
        }

        public async Task<TodoItem> GetSubtask(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<TodoItem>> GetSubtasksForTodoItem(int todoItemTodoItemId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<TodoItem> UpdateSubtask(TodoItem value)
        {
            throw new System.NotImplementedException();
        }
    }
}
