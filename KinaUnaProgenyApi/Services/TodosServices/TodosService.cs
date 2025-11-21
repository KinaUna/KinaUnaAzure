using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.AccessManagementService;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    /// <summary>
    /// Provides functionality for managing and retrieving to-do items associated with a progeny.
    /// </summary>
    /// <remarks>This service allows for the creation, retrieval, updating, and deletion of to-do items. It
    /// also supports filtering and pagination for retrieving lists of to-do items based on various criteria such as
    /// access level, date range, tags, context, and status.</remarks>
    /// <param name="progenyDbContext"></param>
    public class TodosService(ProgenyDbContext progenyDbContext, IAccessManagementService accessManagementService): ITodosService
    {
        public async Task<TodoItem> AddTodoItem(TodoItem value, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null)
            {
                return null;
            }

            if (value.ProgenyId > 0 && value.FamilyId > 0)
            {
                return null;
            }

            if (value.ProgenyId == 0 && value.FamilyId == 0)
            {
                return null;
            }

            bool hasAccess = false;
            if (value.ProgenyId > 0)
            {
                if (await accessManagementService.HasProgenyPermission(value.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }

            if (value.FamilyId > 0)
            {
                if (await accessManagementService.HasFamilyPermission(value.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }
            if (!hasAccess)
            {
                return null;
            }

            TodoItem todoItemToAdd = new();
            todoItemToAdd.CopyPropertiesForAdd(value);

            todoItemToAdd.CreatedTime = DateTime.UtcNow;
            todoItemToAdd.CreatedBy = value.CreatedBy;
            todoItemToAdd.ModifiedTime = DateTime.UtcNow;
            todoItemToAdd.ModifiedBy = value.CreatedBy;
            todoItemToAdd.IsDeleted = false;

            _ = progenyDbContext.TodoItemsDb.Add(todoItemToAdd);
            _ = await progenyDbContext.SaveChangesAsync();

            if (value.ItemPermissionsListToCopy != null && value.ItemPermissionsListToCopy.Count > 0)
            {
                await accessManagementService.CopyItemPermissions(KinaUnaTypes.TimeLineType.TodoItem, todoItemToAdd.TodoItemId,
                    todoItemToAdd.ProgenyId, todoItemToAdd.FamilyId, value.ItemPermissionsListToCopy, currentUserInfo);
            }
            else
            {
                await accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.TodoItem, todoItemToAdd.TodoItemId,
                    todoItemToAdd.ProgenyId, todoItemToAdd.FamilyId, todoItemToAdd.ItemPermissionsDtoList, currentUserInfo);
            }
            
            return todoItemToAdd;
        }

        /// <summary>
        /// Updates an existing to-do item with new values.
        /// </summary>
        /// <remarks>This method retrieves the existing to-do item from the database, updates its
        /// properties with the values from the provided <paramref name="todoItem"/>,  and saves the changes. If no
        /// matching item is found, the method returns <see langword="null"/> without making any changes.</remarks>
        /// <param name="todoItem">The to-do item containing the updated values. The <see cref="TodoItem.TodoItemId"/> property must match an
        ///     existing item.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated <see cref="TodoItem"/> if the operation is successful; otherwise, <see langword="null"/> if no
        /// matching item is found.</returns>
        public async Task<TodoItem> UpdateTodoItem(TodoItem todoItem, UserInfo currentUserInfo)
        {
            if (currentUserInfo == null)
            {
                return null;
            }

            if (todoItem.ProgenyId > 0 && todoItem.FamilyId > 0)
            {
                return null;
            }

            if (todoItem.ProgenyId == 0 && todoItem.FamilyId == 0)
            {
                return null;
            }
            
            TodoItem currentTodoItem = await progenyDbContext.TodoItemsDb
                .SingleOrDefaultAsync(t => t.TodoItemId == todoItem.TodoItemId);
            if (currentTodoItem == null)
            {
                return null; // Item not found
            }
            
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoItem.TodoItemId, currentUserInfo, PermissionLevel.Edit))
            {
                // Allow updates if the current user is assigned to the TodoItem.
                currentTodoItem.Progeny = await progenyDbContext.ProgenyDb.SingleOrDefaultAsync(p => p.Id == currentTodoItem.ProgenyId);
                if (currentTodoItem.Progeny.UserId != currentUserInfo.UserId)
                {
                    return null;
                }

            }

            // Check if the status has changed and update the completed date accordingly
            if (todoItem.Status != currentTodoItem.Status)
            {
                if (todoItem.Status == (int)KinaUnaTypes.TodoStatusType.Completed)
                {
                    todoItem.CompletedDate = DateTime.UtcNow;
                }
                else if (todoItem.Status == (int)KinaUnaTypes.TodoStatusType.NotStarted)
                {
                    todoItem.CompletedDate = null; // Reset completed date if not started
                }
                else if (todoItem.Status == (int)KinaUnaTypes.TodoStatusType.InProgress)
                {
                    todoItem.StartDate = DateTime.UtcNow; // Set start date if not already set
                    todoItem.CompletedDate = null; // Reset completed date if in progress
                }
                else if (todoItem.Status == (int)KinaUnaTypes.TodoStatusType.Cancelled)
                {
                    todoItem.CompletedDate = null; // Reset completed date if cancelled
                }
                else
                {
                    todoItem.CompletedDate = currentTodoItem.CompletedDate; // Keep the existing completed date for other statuses
                }
            }

            // Update properties.
            currentTodoItem.CopyPropertiesForUpdate(todoItem);
            progenyDbContext.TodoItemsDb.Update(currentTodoItem);
            _ = await progenyDbContext.SaveChangesAsync();

            // Update permissions.
            await accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.TodoItem, currentTodoItem.TodoItemId,
                currentTodoItem.ProgenyId, currentTodoItem.FamilyId, currentTodoItem.ItemPermissionsDtoList, currentUserInfo);

            // Update permissions for subtasks.
            List<TodoItem> subTasks = await progenyDbContext.TodoItemsDb.AsNoTracking().Where(t => t.ParentTodoItemId == currentTodoItem.TodoItemId).ToListAsync();
            foreach (TodoItem subTask in subTasks)
            {
                await accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.TodoItem, subTask.TodoItemId,
                    subTask.ProgenyId, subTask.FamilyId, currentTodoItem.ItemPermissionsDtoList, currentUserInfo);
            }

            TodoItem result = await GetTodoItem(currentTodoItem.TodoItemId, currentUserInfo);

            return result;
        }

        /// <summary>
        /// Deletes the specified to-do item from the database, either by marking it as deleted or by performing a hard
        /// delete.
        /// </summary>
        /// <remarks>When performing a soft delete (i.e., <paramref name="hardDelete"/> is <see
        /// langword="false"/>),  the method updates the <c>IsDeleted</c> property of the to-do item to <see
        /// langword="true"/>  and sets its <c>ModifiedTime</c> and <c>ModifiedBy</c> properties.  Callers should ensure
        /// that the <paramref name="todoItem"/> parameter contains the necessary metadata for these updates.</remarks>
        /// <param name="todoItem">The to-do item to delete. Must not be <see langword="null"/> and must have a valid <c>TodoItemId</c>.</param>
        /// <param name="currentUserInfo"></param>
        /// <param name="hardDelete">A value indicating whether to perform a hard delete.  If <see langword="true"/>, the item is permanently
        ///     removed from the database;  otherwise, the item is marked as deleted and its metadata is updated.</param>
        /// <returns><see langword="true"/> if the to-do item was successfully deleted or marked as deleted;  otherwise, <see
        /// langword="false"/> if the item was not found in the database.</returns>
        public async Task<bool> DeleteTodoItem(TodoItem todoItem, UserInfo currentUserInfo, bool hardDelete = false)
        {
            if (currentUserInfo == null)
            {
                return false;
            }

            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoItem.TodoItemId, currentUserInfo, PermissionLevel.Admin))
            {
                return false;
            }

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

            // Revoke all permissions.
            List<TimelineItemPermission> timelineItemPermissionsList = await accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.TodoItem, todoItemToDelete.TodoItemId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            {
                await accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            // Delete subtasks recursively.
            List<TodoItem> subtasks = await progenyDbContext.TodoItemsDb.AsNoTracking().Where(s => s.ParentTodoItemId == todoItemToDelete.TodoItemId).ToListAsync();
            foreach (TodoItem subtask in subtasks)
            {
                await DeleteTodoItem(subtask, currentUserInfo, hardDelete);
            }

            return true;
        }

        /// <summary>
        /// Retrieves a to-do item by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the to-do item to retrieve.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>A <see cref="TodoItem"/> representing the to-do item with the specified identifier,  or <see
        /// langword="null"/> if no matching item is found.</returns>
        public async Task<TodoItem> GetTodoItem(int id, UserInfo currentUserInfo)
        {
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            TodoItem todoItem = await progenyDbContext.TodoItemsDb.AsNoTracking().SingleOrDefaultAsync(t => t.TodoItemId == id);
            if (todoItem == null)
            {
                return null;
            }

            List<TodoItem> subtasks = [.. progenyDbContext.TodoItemsDb.AsNoTracking().Where(t => t.ParentTodoItemId == id && !t.IsDeleted)];
            List<TodoItem> accessibleSubtasks = [];
            foreach (TodoItem subtaskItem in subtasks)
            {
                if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, subtaskItem.TodoItemId, currentUserInfo, PermissionLevel.View))
                {
                    accessibleSubtasks.Add(subtaskItem);
                }
            }
            todoItem.SubtaskCount = accessibleSubtasks.Count;
            todoItem.CompletedSubtaskCount = accessibleSubtasks.FindAll(s => s.Status >= (int)KinaUnaTypes.TodoStatusType.Completed).Count;

            todoItem.ItemPerMission = await accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, todoItem.TodoItemId, todoItem.ProgenyId, todoItem.FamilyId, currentUserInfo);
            
            return todoItem;
        }

        /// <summary>
        /// Retrieves a filtered and paginated list of to-do items for a specific progeny or family.
        /// </summary>
        /// <remarks>The method applies the following filters and operations in sequence: <list
        /// type="bullet"> <item><description>Filters by the progeny ID or family ID and permissions.</description></item>
        /// <item><description>Filters by the specified date range, if provided.</description></item>
        /// <item><description>Filters by tags, context, and status, if specified in the request.</description></item>
        /// <item><description>Sorts the results by due date (newest first) and then by creation
        /// time.</description></item> <item><description>Applies pagination based on the skip and take values in the
        /// request.</description></item> </list></remarks>
        /// <param name="progenyId">The unique identifier of the progeny for which to retrieve to-do items.</param>
        /// <param name="familyId">The unique identifier of the family for which to retrieve to-do items.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <param name="request">An object containing filtering, sorting, and pagination options for the to-do items.  This includes date
        /// ranges, tags, context, status, and pagination parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="TodoItem"/>
        /// objects  that match the specified filters and pagination settings.</returns>
        public async Task<List<TodoItem>> GetTodosForProgenyOrFamily(int progenyId, int familyId, UserInfo currentUserInfo, TodoItemsRequest request)
        {
            List<TodoItem> todoItemsForProgenyOrFamily = await progenyDbContext.TodoItemsDb
                .AsNoTracking()
                .Where(t => t.ProgenyId == progenyId && t.FamilyId == familyId && t.ParentTodoItemId == 0 && !t.IsDeleted)
                .ToListAsync();

            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                todoItemsForProgenyOrFamily = [.. todoItemsForProgenyOrFamily.Where(t => t.DueDate == null || (t.DueDate >= request.StartDate.Value && t.DueDate <= request.EndDate.Value))];
            }
            else if (request.StartDate.HasValue)
            {
                todoItemsForProgenyOrFamily = [.. todoItemsForProgenyOrFamily.Where(t => t.DueDate == null || t.DueDate >= request.StartDate.Value)];
            }
            else if (request.EndDate.HasValue)
            {
                todoItemsForProgenyOrFamily = [.. todoItemsForProgenyOrFamily.Where(t => t.DueDate == null || t.DueDate <= request.EndDate.Value)];
            }

            // Filter by locations if provided
            if (!string.IsNullOrWhiteSpace(request.LocationFilter))
            {
                List<string> locations = [.. request.LocationFilter.Split(',').Select(location => location.Trim())];
                todoItemsForProgenyOrFamily =
                [
                    .. todoItemsForProgenyOrFamily.Where(t =>
                        t.Location != null &&
                        t.Location.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(location => location.Trim())
                            .Any(itemLocation => locations.Any(filterLocation => string.Equals(itemLocation, filterLocation, StringComparison.OrdinalIgnoreCase)))
                    )
                ];
            }

            // Filter by tags if provided
            if (!string.IsNullOrWhiteSpace(request.TagFilter))
            {
                List<string> tags = [.. request.TagFilter.Split(',').Select(tag => tag.Trim())];
                todoItemsForProgenyOrFamily = [.. todoItemsForProgenyOrFamily.Where(t =>
                    t.Tags != null &&
                    t.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(tag => tag.Trim())
                        .Any(itemTag => tags.Any(filterTag => string.Equals(itemTag, filterTag, StringComparison.OrdinalIgnoreCase)))
                )];
            }

            // Filter by context if provided
            if (!string.IsNullOrWhiteSpace(request.ContextFilter))
            {
                List<string> contexts = [.. request.ContextFilter.Split(',').Select(context => context.Trim())];
                todoItemsForProgenyOrFamily = [.. todoItemsForProgenyOrFamily.Where(t =>
                    t.Context != null &&
                    t.Context.Split(',')
                        .Select(c => c.Trim())
                        .Any(itemContext => contexts.Any(filterContext => string.Equals(itemContext, filterContext, StringComparison.OrdinalIgnoreCase)))
                )];
            }

            // Filter by status if provided
            if (request.StatusFilter.Count > 0)
            {
                todoItemsForProgenyOrFamily = [.. todoItemsForProgenyOrFamily.Where( t =>
                    request.StatusFilter.Contains((KinaUnaTypes.TodoStatusType)t.Status))];
            }

            // Filter by access level
            List<TodoItem> accessibleTodoItemsForProgeny = [];
            foreach (TodoItem todoItem in todoItemsForProgenyOrFamily)
            {
                if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoItem.TodoItemId, currentUserInfo, PermissionLevel.View))
                {
                    todoItem.ItemPerMission = await accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.TodoItem, todoItem.TodoItemId, todoItem.ProgenyId, todoItem.FamilyId, currentUserInfo);
                    accessibleTodoItemsForProgeny.Add(todoItem);
                }
            }

            return accessibleTodoItemsForProgeny;
        }

        /// <summary>
        /// Creates a <see cref="TodoItemsResponse"/> object for a paginated and sorted list of TodoItems.
        /// </summary>
        /// <remarks>The method applies sorting based on the <paramref name="request"/> parameter's
        /// <c>Sort</c> property, where <c>1</c> indicates sorting by due date in descending order, and other values
        /// indicate sorting by due date in ascending order. Pagination is applied if the <c>NumberOfItems</c> property
        /// in the <paramref name="request"/> is greater than zero. Additionally, the response includes distinct tags
        /// and contexts extracted from the TodoItems.</remarks>
        /// <param name="todoItemsForProgenies">The list of TodoItems to be included in the response. This list may be filtered, sorted, and paginated
        /// based on the request parameters.</param>
        /// <param name="request">The request object containing pagination, sorting, and other parameters used to generate the response.</param>
        /// <returns>A <see cref="TodoItemsResponse"/> object containing the paginated and sorted TodoItems, along with metadata
        /// such as total items, total pages, and extracted tags and contexts.</returns>
        public TodoItemsResponse CreateTodoItemsResponseForTodoPage(List<TodoItem> todoItemsForProgenies, TodoItemsRequest request)
        {
            if(request.NumberOfItems == 0)
            {
                request.NumberOfItems = 1000000;
            }

            TodoItemsResponse response = new()
            {
                TotalItems = todoItemsForProgenies.Count,
                TotalPages = (int)Math.Ceiling((double)todoItemsForProgenies.Count / request.NumberOfItems),
            };

            if (request.SortBy == 0)
            {
                // Sort by DueDate, then by CreatedTime
                if (request.Sort == 0)
                {
                    if (request.GroupBy == 1)
                    {
                        // Group by Status
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Status)
                                .ThenBy(t => t.DueDate)
                                .ThenBy(t => t.CreatedTime)
                        ];
                    }
                    else if (request.GroupBy == 2)
                    {
                        // Group by Progeny
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.ProgenyId)
                                .ThenBy(t => t.FamilyId)
                                .ThenBy(t => t.DueDate)
                                .ThenBy(t => t.CreatedTime)
                        ];
                    }
                    else if (request.GroupBy == 3)
                    {
                        // Group by Location
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Location)
                                .ThenBy(t => t.DueDate)
                                .ThenBy(t => t.CreatedTime)
                        ];
                    }
                    else
                        todoItemsForProgenies =
                    [
                        .. todoItemsForProgenies
                            .OrderBy(t => t.DueDate)
                            .ThenBy(t => t.CreatedTime)
                    ];
                }
                else
                {
                    if (request.GroupBy == 1)
                    {
                        // Group by Status
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Status)
                                .ThenByDescending(t => t.DueDate)
                                .ThenByDescending(t => t.CreatedTime)
                        ];
                    }
                    else if (request.GroupBy == 2)
                    {
                        // Group by Progeny
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.ProgenyId)
                                .ThenBy(t => t.FamilyId)
                                .ThenByDescending(t => t.DueDate)
                                .ThenByDescending(t => t.CreatedTime)
                        ];
                    }
                    else if (request.GroupBy == 3)
                    {
                        // Group by Location
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Location)
                                .ThenByDescending(t => t.DueDate)
                                .ThenByDescending(t => t.CreatedTime)
                        ];
                    }
                    else
                    {
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderByDescending(t => t.DueDate)
                                .ThenByDescending(t => t.CreatedTime)
                        ];
                    }
                }
            }

            if (request.SortBy == 1)
            {
                // Sort by CreatedTime, then by DueDate
                if (request.Sort == 0)
                {
                    if (request.GroupBy == 1)
                    {
                        // Group by Status
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Status)
                                .ThenBy(t => t.CreatedTime)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 2)
                    {
                        // Group by Progeny
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.ProgenyId)
                                .ThenBy(t => t.FamilyId)
                                .ThenBy(t => t.CreatedTime)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 3)
                    {
                        // Group by Location
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Location)
                                .ThenBy(t => t.CreatedTime)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                    else
                    {
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.CreatedTime)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                }
                else
                {
                    if (request.GroupBy == 1)
                    {
                        // Group by Status
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Status)
                                .ThenByDescending(t => t.CreatedTime)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 2)
                    {
                        // Group by Progeny
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.ProgenyId)
                                .ThenBy(t => t.FamilyId)
                                .ThenByDescending(t => t.CreatedTime)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 3)
                    {
                        // Group by Location
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Location)
                                .ThenByDescending(t => t.CreatedTime)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                    else
                    {
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderByDescending(t => t.CreatedTime)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                }
                
            }

            if (request.SortBy == 2)
            {
                // Sort by StartDate, then by DueDate
                if (request.Sort == 0)
                {
                    if (request.GroupBy == 1)
                    {
                        // Group by Status
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Status)
                                .ThenBy(t => t.StartDate)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 2)
                    {
                        // Group by Progeny
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.ProgenyId)
                                .ThenBy(t => t.FamilyId)
                                .ThenBy(t => t.StartDate)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 3)
                    {
                        // Group by Location
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Location)
                                .ThenBy(t => t.StartDate)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                    else
                    {
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.StartDate)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                }
                else
                {
                    if (request.GroupBy == 1)
                    {
                        // Group by Status
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Status)
                                .ThenByDescending(t => t.StartDate)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 2)
                    {
                        // Group by Progeny
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.ProgenyId)
                                .ThenBy(t => t.FamilyId)
                                .ThenByDescending(t => t.StartDate)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 3)
                    {
                        // Group by Location
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Location)
                                .ThenByDescending(t => t.StartDate)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                    else
                    {
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderByDescending(t => t.StartDate)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                }
                
                
            }

            if (request.SortBy == 3)
            {
                // Sort by CompletedDate, then by DueDate
                if (request.Sort == 0)
                {
                    if (request.GroupBy == 1)
                    {
                        // Group by Status
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Status)
                                .ThenBy(t => t.CompletedDate)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 2)
                    {
                        // Group by Progeny
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.ProgenyId)
                                .ThenBy(t => t.FamilyId)
                                .ThenBy(t => t.CompletedDate)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 3)
                    {
                        // Group by Location
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Location)
                                .ThenBy(t => t.CompletedDate)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                    else
                    {
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.CompletedDate)
                                .ThenBy(t => t.DueDate)
                        ];
                    }
                }
                else
                {
                    if (request.GroupBy == 1)
                    {
                        // Group by Status
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Status)
                                .ThenByDescending(t => t.CompletedDate)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 2)
                    {
                        // Group by Progeny
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.ProgenyId)
                                .ThenBy(t => t.FamilyId)
                                .ThenByDescending(t => t.CompletedDate)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                    else if (request.GroupBy == 3)
                    {
                        // Group by Location
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderBy(t => t.Location)
                                .ThenByDescending(t => t.CompletedDate)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                    else
                    {
                        todoItemsForProgenies =
                        [
                            .. todoItemsForProgenies
                                .OrderByDescending(t => t.CompletedDate)
                                .ThenByDescending(t => t.DueDate)
                        ];
                    }
                }
            }
            
            // Apply pagination, if number of items is less than 1, we do not apply pagination
            if (request.NumberOfItems > 0)
            {
                todoItemsForProgenies =
                [
                    .. todoItemsForProgenies
                        .Skip(request.Skip)
                        .Take(request.NumberOfItems)
                ];
            }

            response.TodoItems = todoItemsForProgenies;
            response.PageNumber = request.NumberOfItems > 0 && request.Skip > 0 ? request.Skip / request.NumberOfItems + 1 : 1;
            response.TodoItemsRequest = request;
            response.TagsList = [.. todoItemsForProgenies
                .SelectMany(t => t.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [])
                .Distinct()
                .Select(tag => tag.Trim())];
            response.ContextsList = [.. todoItemsForProgenies
                .SelectMany(t => t.Context?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [])
                .Distinct()
                .Select(context => context.Trim())];
            
            return response;
        }

        /// <summary>
        /// Retrieves a list of to-do items for a specified progeny, filtered by access level.
        /// </summary>
        /// <remarks>To-do items marked as deleted are excluded from the results. The method uses a
        /// no-tracking query to improve performance  when the returned data does not need to be tracked by the database
        /// context.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny for which to retrieve to-do items.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="TodoItem"/>
        /// objects  that match the specified criteria. The list will be empty if no matching items are found.</returns>
        public async Task<List<TodoItem>> GetTodosList(int progenyId, UserInfo currentUserInfo)
        {
            List<TodoItem> todoItems = await progenyDbContext.TodoItemsDb
                .AsNoTracking()
                .Where(t => t.ProgenyId == progenyId && !t.IsDeleted)
                .ToListAsync();
            List<TodoItem> accessibleTodoItems = [];
            foreach (TodoItem todoItem in todoItems)
            {
                if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, todoItem.TodoItemId, currentUserInfo, PermissionLevel.View))
                {
                    accessibleTodoItems.Add(todoItem);
                }
            }

            return accessibleTodoItems;
        }
    }
}
