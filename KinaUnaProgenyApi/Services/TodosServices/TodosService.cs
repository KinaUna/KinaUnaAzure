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
            TodoItem todoItemToAdd = new();
            todoItemToAdd.CopyPropertiesForAdd(value);

            todoItemToAdd.CreatedTime = DateTime.UtcNow;
            todoItemToAdd.ModifiedTime = DateTime.UtcNow;
            todoItemToAdd.ModifiedBy = value.CreatedBy;
            todoItemToAdd.IsDeleted = false;

            _ = progenyDbContext.TodoItemsDb.Add(todoItemToAdd);
            _ = await progenyDbContext.SaveChangesAsync();
            
            return todoItemToAdd;
        }

        public async Task<bool> DeleteTodoItem(TodoItem todoItem, bool hardDelete = false)
        {
            TodoItem todoItemToDelete = await progenyDbContext.TodoItemsDb
                .SingleOrDefaultAsync(t => t.TodoItemId == todoItem.TodoItemId);

            if (todoItemToDelete == null)
            {
                return false;
            }
            
            if (hardDelete)
            {
                progenyDbContext.TodoItemsDb.Remove(todoItemToDelete);
            }
            else
            {
                todoItemToDelete.IsDeleted = true;
                todoItemToDelete.ModifiedTime = DateTime.UtcNow;
                todoItemToDelete.ModifiedBy = todoItem.ModifiedBy;
                progenyDbContext.TodoItemsDb.Update(todoItemToDelete);
            }

            _ = await progenyDbContext.SaveChangesAsync();
            return true;
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
                todoItemsForProgeny = [.. todoItemsForProgeny.Where(t => t.DueDate >= request.StartDate.Value && t.DueDate <= request.EndDate.Value)];
            }
            else if (request.StartDate.HasValue)
            {
                todoItemsForProgeny = [.. todoItemsForProgeny.Where(t => t.DueDate >= request.StartDate.Value)];
            }
            else if (request.EndDate.HasValue)
            {
                todoItemsForProgeny = [.. todoItemsForProgeny.Where(t => t.DueDate <= request.EndDate.Value)];
            }

            // Filter by tags if provided
            if (!string.IsNullOrWhiteSpace(request.TagFilter))
            {
                List<string> tags = [.. request.TagFilter.Split(',').Select(tag => tag.Trim())];
                todoItemsForProgeny = [.. todoItemsForProgeny.Where(t => tags.Any(tag => t.Tags.Contains(tag, StringComparison.OrdinalIgnoreCase)))];
            }

            // Filter by context if provided
            if (!string.IsNullOrWhiteSpace(request.ContextFilter))
            {
                List<string> contexts = [.. request.ContextFilter.Split(',').Select(context => context.Trim())];
                todoItemsForProgeny = [.. todoItemsForProgeny.Where(t => contexts.Any(context => t.Context.Contains(context, StringComparison.OrdinalIgnoreCase)))];
            }

            // Filter by status if provided
            if (!string.IsNullOrWhiteSpace(request.StatusFilter))
            {
                List<int> statusCodes = [.. request.StatusFilter.Split(',')
                    .Select(status => int.TryParse(status.Trim(), out int code) ? code : -1)
                    .Where(code => code >= 0)];
                
                todoItemsForProgeny = [.. todoItemsForProgeny.Where(t => statusCodes.Contains(t.Status))];
            }

            // Sort by DueDate, newest first, then by CreatedTime
            todoItemsForProgeny = [.. todoItemsForProgeny
                .OrderByDescending(t => t.DueDate)
                .ThenByDescending(t => t.CreatedTime)];

            // Apply pagination
            todoItemsForProgeny = [.. todoItemsForProgeny
                .Skip(request.Skip)
                .Take(request.NumberOfItems)];

            return todoItemsForProgeny;
        }

        public async Task<TodoItem> UpdateTodoItem(TodoItem todoItem)
        {
            TodoItem currentTodoItem = await progenyDbContext.TodoItemsDb
                .SingleOrDefaultAsync(t => t.TodoItemId == todoItem.TodoItemId);
            if (currentTodoItem == null)
            {
                return null; // Item not found
            }
            // Update properties
            currentTodoItem.CopyPropertiesForUpdate(todoItem);
            progenyDbContext.TodoItemsDb.Update(currentTodoItem);
            _ = await progenyDbContext.SaveChangesAsync();
            return currentTodoItem;
        }
    }
}
