using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    public class TodosService(ProgenyDbContext progenyDbContext): ITodosService
    {
        public async Task<TodoItem> AddTodoItem(TodoItem value)
        {
            TodoItem todoItemToAdd = new TodoItem();
            todoItemToAdd.CopyPropertiesForAdd(value);

            todoItemToAdd.CreatedTime = DateTime.UtcNow;
            todoItemToAdd.ModifiedTime = DateTime.UtcNow;
            todoItemToAdd.ModifiedBy = value.CreatedBy;
            todoItemToAdd.IsDeleted = false;

            _ = progenyDbContext.TodoItemsDb.Add(todoItemToAdd);
            _ = await progenyDbContext.SaveChangesAsync();
            
            return todoItemToAdd;
        }

        public async Task<TodoItem> GetTodoItem(int id)
        {
            TodoItem todoItem = await progenyDbContext.TodoItemsDb.AsNoTracking().SingleOrDefaultAsync(t => t.TodoItemId == id);
            
            return todoItem;
        }

        public async Task<List<TodoItem>> GetTodosForProgeny(int id, int accessLevel, TodoItemsRequest request)
        {
            List<TodoItem> todoItemsForProgeny = await progenyDbContext.TodoItemsDb
                .AsNoTracking()
                .Where(t => t.ProgenyId == id && t.AccessLevel <= accessLevel)
                .ToListAsync();

            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                todoItemsForProgeny = todoItemsForProgeny
                    .Where(t => t.DueDate >= request.StartDate.Value && t.DueDate <= request.EndDate.Value)
                    .ToList();
            }
            else if (request.StartDate.HasValue)
            {
                todoItemsForProgeny = todoItemsForProgeny
                    .Where(t => t.DueDate >= request.StartDate.Value)
                    .ToList();
            }
            else if (request.EndDate.HasValue)
            {
                todoItemsForProgeny = todoItemsForProgeny
                    .Where(t => t.DueDate <= request.EndDate.Value)
                    .ToList();
            }

            // Filter by tags if provided
            if (!string.IsNullOrWhiteSpace(request.TagFilter))
            {
                List<string> tags = request.TagFilter.Split(',').Select(tag => tag.Trim()).ToList();
                todoItemsForProgeny = todoItemsForProgeny
                    .Where(t => tags.Any(tag => t.Tags.Contains(tag, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Filter by context if provided
            if (!string.IsNullOrWhiteSpace(request.ContextFilter))
            {
                List<string> contexts = request.ContextFilter.Split(',').Select(context => context.Trim()).ToList();
                todoItemsForProgeny = todoItemsForProgeny
                    .Where(t => contexts.Any(context => t.Context.Contains(context, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(request.StatusFilter))
            {
                List<int> statusCodes = request.StatusFilter.Split(',')
                    .Select(status => int.TryParse(status.Trim(), out int code) ? code : -1)
                    .Where(code => code >= 0).ToList();
                
                todoItemsForProgeny = todoItemsForProgeny
                    .Where(t => statusCodes.Contains(t.Status))
                    .ToList();
            }

            // Apply pagination
            todoItemsForProgeny = todoItemsForProgeny
                .Skip(request.Skip)
                .Take(request.NumberOfItems)
                .ToList();

            return todoItemsForProgeny;
        }
    }
}
