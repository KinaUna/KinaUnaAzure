using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="TodoItem"/> class to facilitate property copying and conversion
    /// operations.
    /// </summary>
    /// <remarks>This class includes methods for copying properties between <see cref="TodoItem"/> instances
    /// and converting a <see cref="TodoItem"/> to a <see cref="TimeLineItem"/>. These methods are designed to simplify
    /// common operations when working with <see cref="TodoItem"/> objects in the application.</remarks>
    public static class TodoItemExtensions
    {
        /// <summary>
        /// Returns a user-friendly status text based on the <see cref="TodoItem.Status"/> value.
        /// </summary>
        /// <param name="todoItem">The <see cref="TodoItem"/> instance for which to retrieve the status text.</param>
        /// <returns>The status text corresponding to the <see cref="TodoItem.Status"/> value. Possible values are:
        /// Not started, In progress, Completed, Cancelled, Overdue, or Unknown status.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string StatusText(this TodoItem todoItem)
        {
            if (todoItem == null)
            {
                throw new ArgumentNullException(nameof(todoItem), "TodoItem cannot be null.");
            }

            if (todoItem.Status == (int)TodoStatusTypes.TodoStatusType.NotStarted)
            {
                return "Not started";
            }

            if (todoItem.Status == (int)TodoStatusTypes.TodoStatusType.InProgress)
            {
                return "In progress";
            }

            if (todoItem.Status == (int)TodoStatusTypes.TodoStatusType.Completed)
            {
                return "Completed";
            }

            if (todoItem.Status == (int)TodoStatusTypes.TodoStatusType.Cancelled)
            {
                return "Cancelled";
            }

            if (todoItem.Status == (int)TodoStatusTypes.TodoStatusType.Overdue)
            {
                return "Overdue";
            }

            return "Unknown status";
        }

        /// <summary>
        /// Copies the properties of the specified <see cref="TodoItem"/> to the current <see cref="TodoItem"/>
        /// instance.
        /// </summary>
        /// <remarks>This method performs a shallow copy of all properties from <paramref
        /// name="otherTodoItem"/> to <paramref name="currentTodoItem"/>.  It is intended for use when adding a new <see
        /// cref="TodoItem"/> based on an existing one.</remarks>
        /// <param name="currentTodoItem">The <see cref="TodoItem"/> instance to which the properties will be copied.</param>
        /// <param name="otherTodoItem">The <see cref="TodoItem"/> instance from which the properties will be copied.</param>
        public static void CopyPropertiesForAdd(this TodoItem currentTodoItem, TodoItem otherTodoItem)
        {
            currentTodoItem.ProgenyId = otherTodoItem.ProgenyId;
            currentTodoItem.UId = otherTodoItem.UId;
            currentTodoItem.Title = otherTodoItem.Title;
            currentTodoItem.Description = otherTodoItem.Description;
            currentTodoItem.Status = otherTodoItem.Status;
            currentTodoItem.DueDate = otherTodoItem.DueDate;
            currentTodoItem.CompletedDate = otherTodoItem.CompletedDate;
            currentTodoItem.Notes = otherTodoItem.Notes;
            currentTodoItem.AccessLevel = otherTodoItem.AccessLevel;
            currentTodoItem.Tags = otherTodoItem.Tags;
            currentTodoItem.Context = otherTodoItem.Context;
            currentTodoItem.CreatedBy = null; // Set to null or assign current user if available
            currentTodoItem.CreatedTime = DateTime.UtcNow;
            currentTodoItem.ModifiedTime = DateTime.UtcNow;
            currentTodoItem.ModifiedBy = null; // Set to null or assign current user if available
            currentTodoItem.IsDeleted = otherTodoItem.IsDeleted;
        }

        /// <summary>
        /// Copies the properties of the specified <see cref="TodoItem"/> to the current <see cref="TodoItem"/>
        /// instance,  updating its state for modification.
        /// </summary>
        /// <remarks>This method updates all properties of the current <see cref="TodoItem"/> to match
        /// those of the specified  <paramref name="otherTodoItem"/>, except for the <c>ModifiedTime</c>, which is set
        /// to the current UTC time,  and the <c>ModifiedBy</c> and <c>IsDeleted</c> properties, which retain their
        /// values from <paramref name="otherTodoItem"/>.</remarks>
        /// <param name="currentTodoItem">The <see cref="TodoItem"/> instance to update with the properties of the other item.</param>
        /// <param name="otherTodoItem">The <see cref="TodoItem"/> instance whose properties will be copied to the current item.</param>
        public static void CopyPropertiesForUpdate(this TodoItem currentTodoItem, TodoItem otherTodoItem)
        {
            currentTodoItem.ProgenyId = otherTodoItem.ProgenyId;
            currentTodoItem.UId = otherTodoItem.UId;
            currentTodoItem.Title = otherTodoItem.Title;
            currentTodoItem.Description = otherTodoItem.Description;
            currentTodoItem.Status = otherTodoItem.Status;
            currentTodoItem.DueDate = otherTodoItem.DueDate;
            currentTodoItem.CompletedDate = otherTodoItem.CompletedDate;
            currentTodoItem.Notes = otherTodoItem.Notes;
            currentTodoItem.AccessLevel = otherTodoItem.AccessLevel;
            currentTodoItem.Tags = otherTodoItem.Tags;
            currentTodoItem.Context = otherTodoItem.Context;
            currentTodoItem.ModifiedTime = DateTime.UtcNow; // Update modified time to now
            currentTodoItem.ModifiedBy = otherTodoItem.ModifiedBy; // Keep the same modifier
            currentTodoItem.IsDeleted = otherTodoItem.IsDeleted; // Keep the same deleted status
        }

        /// <summary>
        /// Converts a <see cref="TodoItem"/> instance to a new <see cref="TimeLineItem"/> instance.
        /// </summary>
        /// <remarks>The method initializes a <see cref="TimeLineItem"/> with properties derived from the
        /// provided <see cref="TodoItem"/>. The <see cref="TimeLineItem.ProgenyTime"/> is set to the <see
        /// cref="TodoItem.StartDate"/> if it has a value;  otherwise, it defaults to the <see
        /// cref="TodoItem.CreatedTime"/>.</remarks>
        /// <param name="todoItem">The <see cref="TodoItem"/> to convert. This parameter cannot be <see langword="null"/>.</param>
        /// <returns>A new <see cref="TimeLineItem"/> instance populated with data from the specified <see cref="TodoItem"/>.</returns>
        public static TimeLineItem ToNewTimeLineItem(this TodoItem todoItem)
        {
            DateTime progenyTime = todoItem.CreatedTime;
            if (todoItem.StartDate.HasValue)
            {
                progenyTime = todoItem.StartDate.Value;
            }
            TimeLineItem timeLineItem = new()
            {
                ItemId = todoItem.TodoItemId.ToString(),
                ProgenyId = todoItem.ProgenyId,
                AccessLevel = todoItem.AccessLevel,
                ItemType = (int)KinaUnaTypes.TimeLineType.TodoItem,
                CreatedBy = todoItem.CreatedBy,
                CreatedTime = DateTime.UtcNow,
                ProgenyTime = progenyTime
            };

            return timeLineItem;
        }
    }
}
