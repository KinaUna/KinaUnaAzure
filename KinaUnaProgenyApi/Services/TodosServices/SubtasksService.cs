using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    public class SubtasksService(ProgenyDbContext progenyDbContext) : ISubtasksService
    {
        public async Task<TodoItem> AddSubtask(TodoItem value)
        {
            TodoItem subtaskToAdd = new();
            subtaskToAdd.CopyPropertiesForAdd(value);

            subtaskToAdd.CreatedTime = DateTime.UtcNow;
            subtaskToAdd.CreatedBy = value.CreatedBy;
            subtaskToAdd.ModifiedTime = DateTime.UtcNow;
            subtaskToAdd.ModifiedBy = value.CreatedBy;
            subtaskToAdd.IsDeleted = false;

            _ = progenyDbContext.SubtasksDb.Add(subtaskToAdd);
            _ = await progenyDbContext.SaveChangesAsync();

            return subtaskToAdd;
        }

        public async Task<bool> DeleteSubtask(TodoItem subtask, bool hardDelete = false)
        {
            TodoItem subtaskToDelete = await progenyDbContext.SubtasksDb
                .SingleOrDefaultAsync(t => t.TodoItemId == subtask.TodoItemId);

            if (subtaskToDelete == null)
            {
                return false;
            }

            if (hardDelete)
            {
                progenyDbContext.SubtasksDb.Remove(subtaskToDelete);
            }
            else
            {
                subtaskToDelete.IsDeleted = true;
                subtaskToDelete.ModifiedTime = DateTime.UtcNow;
                subtaskToDelete.ModifiedBy = subtask.ModifiedBy;
                progenyDbContext.SubtasksDb.Update(subtaskToDelete);
            }

            _ = await progenyDbContext.SaveChangesAsync();
            return true;
        }

        public async Task<TodoItem> GetSubtask(int id)
        {
            TodoItem subtask = await progenyDbContext.SubtasksDb.AsNoTracking().SingleOrDefaultAsync(t => t.TodoItemId == id);

            return subtask;
        }

        public async Task<List<TodoItem>> GetSubtasksForTodoItem(int todoItemTodoItemId)
        {
            List<TodoItem> subtasks = await progenyDbContext.SubtasksDb
                .AsNoTracking()
                .Where(t => t.ParentTodoItemId == todoItemTodoItemId && !t.IsDeleted)
                .ToListAsync();

            return subtasks;
        }

        public async Task<TodoItem> UpdateSubtask(TodoItem value)
        {
            TodoItem currentSubtask = await progenyDbContext.SubtasksDb
                .SingleOrDefaultAsync(t => t.TodoItemId == value.TodoItemId);
            if (currentSubtask == null)
            {
                return null; // Item not found
            }

            // Check if the status has changed and update the completed date accordingly
            if (value.Status != currentSubtask.Status)
            {
                if (value.Status == (int)KinaUnaTypes.TodoStatusType.Completed)
                {
                    value.CompletedDate = DateTime.UtcNow;
                }
                else if (value.Status == (int)KinaUnaTypes.TodoStatusType.NotStarted)
                {
                    value.CompletedDate = null; // Reset completed date if not started
                    value.StartDate = null; // Reset start date if not started
                }
                else if (value.Status == (int)KinaUnaTypes.TodoStatusType.InProgress)
                {
                    value.StartDate = DateTime.UtcNow; // Set start date if not already set
                }
                else if (value.Status == (int)KinaUnaTypes.TodoStatusType.Cancelled)
                {
                    value.CompletedDate = null; // Reset completed date if cancelled
                }
                else
                {
                    value.CompletedDate = currentSubtask.CompletedDate; // Keep the existing completed date for other statuses
                }
            }

            // Update properties
            currentSubtask.CopyPropertiesForUpdate(value);
            progenyDbContext.SubtasksDb.Update(currentSubtask);
            _ = await progenyDbContext.SaveChangesAsync();
            return currentSubtask;
        }
    }
}
