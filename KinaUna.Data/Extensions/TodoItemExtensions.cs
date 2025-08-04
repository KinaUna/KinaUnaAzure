using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class TodoItemExtensions
    {
        public static void CopyPropertiesForAdd(this TodoItem currentTodoItem, TodoItem otherTodoItem)
        {
            currentTodoItem.ProgenyId = otherTodoItem.ProgenyId;
            currentTodoItem.Title = otherTodoItem.Title;
            currentTodoItem.Description = otherTodoItem.Description;
            currentTodoItem.Status = otherTodoItem.Status;
            currentTodoItem.DueDate = otherTodoItem.DueDate;
            currentTodoItem.CompletedDate = otherTodoItem.CompletedDate;
            currentTodoItem.Notes = otherTodoItem.Notes;
            currentTodoItem.AccessLevel = otherTodoItem.AccessLevel;
            currentTodoItem.Tags = otherTodoItem.Tags;
            currentTodoItem.Context = otherTodoItem.Context;
            currentTodoItem.CreatedBy = otherTodoItem.CreatedBy;
            currentTodoItem.CreatedTime = otherTodoItem.CreatedTime;
            currentTodoItem.ModifiedTime = otherTodoItem.ModifiedTime;
            currentTodoItem.ModifiedBy = otherTodoItem.ModifiedBy;
            currentTodoItem.IsDeleted = otherTodoItem.IsDeleted;
        }
    }
}
