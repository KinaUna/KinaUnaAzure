using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    /// <summary>
    /// Provides functionality for managing subtasks associated with a parent to-do item.
    /// </summary>
    /// <remarks>This service allows for creating, retrieving, updating, and deleting subtasks, as well as
    /// generating filtered and paginated responses for subtasks. It is designed to work with a database context to
    /// persist changes.</remarks>
    /// <param name="progenyDbContext">Database context for accessing the Progeny database.</param>
    public class SubtasksService(ProgenyDbContext progenyDbContext) : ISubtasksService
    {
        /// <summary>
        /// Adds a new subtask to the database and initializes its properties.
        /// </summary>
        /// <remarks>The method initializes the subtask's creation and modification timestamps to the
        /// current UTC time  and sets its <c>IsDeleted</c> property to <see langword="false"/>. The subtask is then
        /// saved to the database.</remarks>
        /// <param name="value">The <see cref="TodoItem"/> containing the details of the subtask to add. The <c>CreatedBy</c> property must
        /// be set.</param>
        /// <returns>A <see cref="TodoItem"/> representing the newly added subtask, including its initialized properties.</returns>
        public async Task<TodoItem> AddSubtask(TodoItem value)
        {
            TodoItem subtaskToAdd = new();
            subtaskToAdd.CopyPropertiesForAdd(value);

            subtaskToAdd.CreatedTime = DateTime.UtcNow;
            subtaskToAdd.CreatedBy = value.CreatedBy;
            subtaskToAdd.ModifiedTime = DateTime.UtcNow;
            subtaskToAdd.ModifiedBy = value.CreatedBy;
            subtaskToAdd.IsDeleted = false;

            _ = progenyDbContext.TodoItemsDb.Add(subtaskToAdd);
            _ = await progenyDbContext.SaveChangesAsync();

            return subtaskToAdd;
        }

        /// <summary>
        /// Creates a response containing a filtered, sorted, and paginated list of subtasks for a specified to-do item.
        /// </summary>
        /// <remarks>The method applies the following operations to the provided list of subtasks based on
        /// the <paramref name="request"/>: <list type="bullet"> <item><description>Filters subtasks by start date, end
        /// date, tags, context, location, and status.</description></item> <item><description>Applies pagination using
        /// the <c>Skip</c> and <c>NumberOfItems</c> properties of the request.</description></item>
        /// <item><description>Calculates metadata such as the total number of items and pages.</description></item>
        /// </list> Sorting logic can be added in the future based on the <c>Sort</c> and <c>SortBy</c> properties of
        /// the request.</remarks>
        /// <param name="subtasks">The list of subtasks to process. This list will be filtered, sorted, and paginated based on the request
        /// parameters.</param>
        /// <param name="request">The request object containing filtering, sorting, and pagination criteria. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <returns>A <see cref="SubtasksResponse"/> object containing the processed list of subtasks, along with metadata such
        /// as pagination details.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="request"/> is <see langword="null"/>.</exception>
        public SubtasksResponse CreateSubtaskResponseForTodoItem(List<TodoItem> subtasks, SubtasksRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("Request is invalid.");
            }

            // Process the request to filter, sort, and paginate the subtasks

            request.SetStartDateAndEndDate();
            if (request.StartDate.HasValue)
            {
                subtasks = [.. subtasks.Where(t => t.StartDate >= request.StartDate.Value)];
            }

            if (request.EndDate.HasValue)
            {
                subtasks = [.. subtasks.Where(t => t.DueDate <= request.EndDate.Value)];
            }

            if (!string.IsNullOrEmpty(request.TagFilter))
            {
                subtasks = [.. subtasks.Where(t => t.Tags != null && t.Tags.Contains(request.TagFilter))];
            }

            if (!string.IsNullOrEmpty(request.ContextFilter))
            {
                subtasks = [.. subtasks.Where(t => t.Context != null && t.Context.Contains(request.ContextFilter))];
            }

            if (!string.IsNullOrEmpty(request.LocationFilter))
            {
                subtasks = [.. subtasks.Where(t => t.Location != null && t.Location.Contains(request.LocationFilter))];
            }

            if (request.StatusFilter != null && request.StatusFilter.Count > 0)
            {
                subtasks = [.. subtasks.Where(t => request.StatusFilter.Contains((KinaUnaTypes.TodoStatusType)t.Status))];
            }


            // Todo: Sorting logic can be added here based on request.Sort and request.SortBy
            if (request.GroupBy == 1)
            {
                // Group by Status
                subtasks =
                [
                    .. subtasks
                        .OrderBy(t => t.Status)
                        .ThenBy(t => t.StartDate)
                        .ThenBy(t => t.CreatedTime)
                ];
            }
            else if (request.GroupBy == 2)
            {
                // Group by Progeny
                subtasks =
                [
                    .. subtasks
                        .OrderBy(t => t.ProgenyId)
                        .ThenBy(t => t.StartDate)
                        .ThenBy(t => t.CreatedTime)
                ];
            }
            else if (request.GroupBy == 3)
            {
                // Group by Location
                subtasks =
                [
                    .. subtasks
                        .OrderBy(t => t.Location)
                        .ThenBy(t => t.StartDate)
                        .ThenBy(t => t.CreatedTime)
                ];
            }
            else
            {
                subtasks =
                [
                    .. subtasks
                        .OrderBy(t => t.StartDate)
                        .ThenBy(t => t.CreatedTime)
                ];
            }

            if (request.Skip > 0)
            {
                subtasks = [.. subtasks.Skip(request.Skip)];
            }

            if (request.NumberOfItems > 0)
            {
                subtasks = [.. subtasks.Take(request.NumberOfItems)];
            }
            // Create the response object
            SubtasksResponse response = new()
            {
                ParentTodoItemId = request.ParentTodoItemId,
                Subtasks = subtasks,
                SubtasksRequest = request,
                PageNumber = request.NumberOfItems > 0 && request.Skip > 0 ? (request.Skip / request.NumberOfItems) + 1 : 1,
                TotalPages = (int)Math.Ceiling((double)subtasks.Count / (request.NumberOfItems > 0 ? request.NumberOfItems : 1)),
                TotalItems = subtasks.Count,
            };

            return response;
        }

        /// <summary>
        /// Deletes the specified subtask from the database, either by marking it as deleted or by permanently removing
        /// it.
        /// </summary>
        /// <remarks>When <paramref name="hardDelete"/> is <see langword="false"/>, the subtask is marked
        /// as deleted by setting its  <c>IsDeleted</c> property to <see langword="true"/> and updating its modification
        /// metadata.</remarks>
        /// <param name="subtask">The subtask to delete. The <see cref="TodoItem.TodoItemId"/> property must match an existing subtask in the
        /// database.</param>
        /// <param name="hardDelete">A boolean value indicating whether the subtask should be permanently removed.  If <see langword="true"/>,
        /// the subtask is permanently deleted; otherwise, it is marked as deleted.</param>
        /// <returns><see langword="true"/> if the subtask was successfully deleted or marked as deleted;  otherwise, <see
        /// langword="false"/> if the subtask does not exist.</returns>
        public async Task<bool> DeleteSubtask(TodoItem subtask, bool hardDelete = false)
        {
            TodoItem subtaskToDelete = await progenyDbContext.TodoItemsDb
                .SingleOrDefaultAsync(t => t.TodoItemId == subtask.TodoItemId);

            if (subtaskToDelete == null)
            {
                return false;
            }

            if (hardDelete)
            {
                progenyDbContext.TodoItemsDb.Remove(subtaskToDelete);
            }
            else
            {
                subtaskToDelete.IsDeleted = true;
                subtaskToDelete.ModifiedTime = DateTime.UtcNow;
                subtaskToDelete.ModifiedBy = subtask.ModifiedBy;
                progenyDbContext.TodoItemsDb.Update(subtaskToDelete);
            }

            _ = await progenyDbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Retrieves a subtask with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the subtask to retrieve.</param>
        /// <returns>A <see cref="TodoItem"/> representing the subtask with the specified identifier,  or <see langword="null"/>
        /// if no subtask with the given identifier exists.</returns>
        public async Task<TodoItem> GetSubtask(int id)
        {
            TodoItem subtask = await progenyDbContext.TodoItemsDb.AsNoTracking().SingleOrDefaultAsync(t => t.TodoItemId == id);

            return subtask;
        }

        /// <summary>
        /// Retrieves a list of subtasks associated with the specified to-do item.
        /// </summary>
        /// <remarks>Subtasks are filtered to exclude any that are marked as deleted. The method performs
        /// a  database query and returns the results without tracking changes to the retrieved entities.</remarks>
        /// <param name="todoItemTodoItemId">The unique identifier of the parent to-do item for which subtasks are to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  TodoItem objects
        /// representing the subtasks of the specified to-do item.  The list will be empty if no subtasks are found.</returns>
        public async Task<List<TodoItem>> GetSubtasksForTodoItem(int todoItemTodoItemId)
        {
            List<TodoItem> subtasks = await progenyDbContext.TodoItemsDb
                .AsNoTracking()
                .Where(t => t.ParentTodoItemId == todoItemTodoItemId && !t.IsDeleted)
                .ToListAsync();

            
            return subtasks;
        }

        /// <summary>
        /// Updates an existing subtask with the specified values and adjusts its status-related dates as needed.
        /// </summary>
        /// <remarks>This method updates the subtask's properties and adjusts its status-related dates
        /// based on the provided status.  - If the status is set to <see
        /// cref="KinaUnaTypes.TodoStatusType.Completed"/>, the <see cref="TodoItem.CompletedDate"/> is set to the
        /// current UTC time. - If the status is set to <see cref="KinaUnaTypes.TodoStatusType.NotStarted"/>, both the
        /// <see cref="TodoItem.CompletedDate"/> and <see cref="TodoItem.StartDate"/> are reset to <see
        /// langword="null"/>. - If the status is set to <see cref="KinaUnaTypes.TodoStatusType.InProgress"/>, the <see
        /// cref="TodoItem.StartDate"/> is set to the current UTC time if not already set. - If the status is set to
        /// <see cref="KinaUnaTypes.TodoStatusType.Cancelled"/>, the <see cref="TodoItem.CompletedDate"/> is reset to
        /// <see langword="null"/>. - For other statuses, the existing <see cref="TodoItem.CompletedDate"/> is
        /// preserved.</remarks>
        /// <param name="value">The <see cref="TodoItem"/> containing the updated values for the subtask. The <see
        /// cref="TodoItem.TodoItemId"/> property must match an existing subtask.</param>
        /// <returns>The updated <see cref="TodoItem"/> if the subtask is found and successfully updated; otherwise, <see
        /// langword="null"/> if no subtask with the specified <see cref="TodoItem.TodoItemId"/> exists.</returns>
        public async Task<TodoItem> UpdateSubtask(TodoItem value)
        {
            TodoItem currentSubtask = await progenyDbContext.TodoItemsDb
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
                }
                else if (value.Status == (int)KinaUnaTypes.TodoStatusType.InProgress)
                {
                    value.StartDate = DateTime.UtcNow; // Set start date if not already set
                    value.CompletedDate = null; // Reset completed date if in progress
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
            progenyDbContext.TodoItemsDb.Update(currentSubtask);
            _ = await progenyDbContext.SaveChangesAsync();
            return currentSubtask;
        }
    }
}
