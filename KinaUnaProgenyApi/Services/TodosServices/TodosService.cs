using KinaUna.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    public class TodosService(ProgenyDbContext progenyDbContext): ITodosService
    {
        public async Task<TodoItem> GetTodoItem(int id)
        {
            TodoItem todoItem = await progenyDbContext.TodoItemsDb.AsNoTracking().SingleOrDefaultAsync(t => t.TodoItemId == id);
            
            return todoItem;
        }

        public async Task<List<TodoItem>> GetTodosForProgeny(int id, int accessLevel, DateTime? startDate, DateTime? endDate)
        {
            List<TodoItem> todoItemsForProgeny = await progenyDbContext.TodoItemsDb
                .AsNoTracking()
                .Where(t => t.ProgenyId == id && t.AccessLevel <= accessLevel)
                .ToListAsync();

            if (startDate.HasValue && endDate.HasValue)
            {
                todoItemsForProgeny = todoItemsForProgeny
                    .Where(t => t.DueDate >= startDate.Value && t.DueDate <= endDate.Value)
                    .ToList();
            }
            else if (startDate.HasValue)
            {
                todoItemsForProgeny = todoItemsForProgeny
                    .Where(t => t.DueDate >= startDate.Value)
                    .ToList();
            }
            else if (endDate.HasValue)
            {
                todoItemsForProgeny = todoItemsForProgeny
                    .Where(t => t.DueDate <= endDate.Value)
                    .ToList();
            }

            return todoItemsForProgeny;
        }
    }
}
