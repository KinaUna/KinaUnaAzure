using KinaUna.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    public interface ITodosService
    {
        Task<TodoItem> GetTodoItem(int id);
        Task<List<TodoItem>> GetTodosForProgeny(int id, int accessLevel, DateTime? startDate, DateTime? endDate);
    }
}
