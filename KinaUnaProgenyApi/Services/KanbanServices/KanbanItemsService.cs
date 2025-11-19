using System;
using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    /// <summary>
    /// Service for managing Kanban items.
    /// </summary>
    /// <param name="progenyDbContext"></param>
    /// <param name="todosService"></param>
    public class KanbanItemsService(ProgenyDbContext progenyDbContext, ITodosService todosService, IAccessManagementService accessManagementService): IKanbanItemsService
    {
        /// <summary>
        /// Retrieves a Kanban item by its unique identifier.
        /// </summary>
        /// <param name="kanbanItemId">The unique identifier of the Kanban item.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The Kanban item if found, with the associated TodoItem; otherwise, a new KanbanItem object with id = 0.</returns>
        public async Task<KanbanItem> GetKanbanItemById(int kanbanItemId, UserInfo currentUserInfo)
        {
            KanbanItem kanbanItem = await progenyDbContext.KanbanItemsDb.AsNoTracking().SingleOrDefaultAsync(k => k.KanbanItemId == kanbanItemId);
            if (kanbanItem == null)
            {
                return new KanbanItem();
            }

            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, kanbanItem.TodoItemId, currentUserInfo, PermissionLevel.View))
            {
                return new KanbanItem();
            }

            kanbanItem.TodoItem = await todosService.GetTodoItem(kanbanItem.TodoItemId, currentUserInfo);

            return kanbanItem;
        }

        /// <summary>
        /// Adds a new Kanban item to the database and assigns it a unique identifier.
        /// </summary>
        /// <remarks>This method generates a new unique identifier for the Kanban item before saving it to
        /// the database. Ensure that the provided <paramref name="kanbanItem"/> is valid and contains all required
        /// data.</remarks>
        /// <param name="kanbanItem">The <see cref="KanbanItem"/> to be added. The item's properties should be populated before calling this
        ///     method.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added <see cref="KanbanItem"/> with its unique identifier assigned. Does not include the associated TodoItem.</returns>
        public async Task<KanbanItem> AddKanbanItem(KanbanItem kanbanItem, UserInfo currentUserInfo)
        {
            
            kanbanItem.TodoItem = await todosService.GetTodoItem(kanbanItem.TodoItemId, currentUserInfo);

            bool hasAccess = false;
            if (kanbanItem.TodoItem.ProgenyId > 0)
            {
                if (await accessManagementService.HasProgenyPermission(kanbanItem.TodoItem.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }

            if (kanbanItem.TodoItem.FamilyId > 0)
            {
                if (await accessManagementService.HasFamilyPermission(kanbanItem.TodoItem.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }

            if (!hasAccess)
            {
                return null;
            }

            KanbanBoard kanbanBoard = await progenyDbContext.KanbanBoardsDb.SingleOrDefaultAsync(k => k.KanbanBoardId == kanbanItem.KanbanBoardId);
            if (kanbanBoard == null)
            {
                return new KanbanItem();
            }

            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId, currentUserInfo, PermissionLevel.Add))
            {
                return null;
            }

            kanbanBoard.SetColumnsListFromColumns();
            if (!kanbanBoard.ColumnsList.Exists(k => k.Id == kanbanItem.ColumnId))
            {
                kanbanItem.ColumnId = kanbanBoard.ColumnsList[0].Id;
            }
            int kanbanItemsCount = await progenyDbContext.KanbanItemsDb.CountAsync(k => k.KanbanBoardId == kanbanItem.KanbanBoardId && k.ColumnId == kanbanItem.ColumnId);
            kanbanItem.RowIndex = kanbanItemsCount;

            kanbanItem.UId = Guid.NewGuid().ToString();
            kanbanItem.CreatedTime = DateTime.UtcNow;
            kanbanItem.ModifiedTime = DateTime.UtcNow;
            await progenyDbContext.KanbanItemsDb.AddAsync(kanbanItem);
            await progenyDbContext.SaveChangesAsync();

            // No permissions are set for Kanban items, as they are protected through the linked TodoItem.
            return kanbanItem;
        }

        /// <summary>
        /// Updates an existing Kanban item with new values and saves the changes to the database.
        /// </summary>
        /// <remarks>If the specified Kanban item does not exist in the database, the method returns <see
        /// langword="null"/> without making any changes. If the existing Kanban item does not have a unique identifier
        /// (<c>UId</c>), a new GUID is generated and assigned to it.</remarks>
        /// <param name="kanbanItem">The <see cref="KanbanItem"/> containing the updated values. The item's <c>KanbanItemId</c> must match an
        ///     existing item in the database.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated <see cref="KanbanItem"/> if the item exists in the database; otherwise, <see langword="null"/>.
        /// Does not include the associated TodoItem.</returns>
        public async Task<KanbanItem> UpdateKanbanItem(KanbanItem kanbanItem, UserInfo currentUserInfo)
        {
            
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, kanbanItem.TodoItemId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            KanbanItem existingKanbanItem = await progenyDbContext.KanbanItemsDb.SingleOrDefaultAsync(k => k.KanbanItemId == kanbanItem.KanbanItemId);
            if (existingKanbanItem == null)
            {
                return null;
            }

            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, existingKanbanItem.KanbanBoardId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            if (string.IsNullOrEmpty(existingKanbanItem.UId))
            {
                existingKanbanItem.UId = Guid.NewGuid().ToString();
            }

            if (kanbanItem.RowIndex < 0)
            {
                int kanbanItemsCount = await progenyDbContext.KanbanItemsDb.CountAsync(k => k.KanbanBoardId == kanbanItem.KanbanBoardId && k.ColumnId == kanbanItem.ColumnId);
                kanbanItem.RowIndex = kanbanItemsCount;
            }

            existingKanbanItem.ColumnId = kanbanItem.ColumnId;
            existingKanbanItem.RowIndex = kanbanItem.RowIndex;
            existingKanbanItem.ModifiedBy = kanbanItem.ModifiedBy;
            existingKanbanItem.ModifiedTime = DateTime.UtcNow;
            existingKanbanItem.KanbanBoardId = kanbanItem.KanbanBoardId;
            if (string.IsNullOrEmpty(existingKanbanItem.CreatedBy))
            {
                existingKanbanItem.CreatedBy = kanbanItem.ModifiedBy;
            }

            progenyDbContext.Update(existingKanbanItem);
            await progenyDbContext.SaveChangesAsync();
            // No permissions are set for Kanban items, as they are protected through the linked TodoItem.
            return existingKanbanItem;
        }

        /// <summary>
        /// Deletes the specified Kanban item from the database.
        /// </summary>
        /// <remarks>This method performs a lookup to ensure the specified Kanban item exists in the
        /// database before attempting to delete it. If the item is not found, no changes are made to the
        /// database.</remarks>
        /// <param name="kanbanItem">The Kanban item to delete. The item must have a valid <see cref="KanbanItem.KanbanItemId"/>.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <param name="hardDelete">If set to <see langword="true"/>, the Kanban item is permanently removed from the database.</param>
        /// <returns>The deleted Kanban item if it was successfully removed; otherwise, <see langword="null"/> if the item does
        /// not exist in the database. Does not include the associated TodoItem.</returns>
        public async Task<KanbanItem> DeleteKanbanItem(KanbanItem kanbanItem, UserInfo currentUserInfo, bool hardDelete = false)
        {
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, kanbanItem.TodoItemId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }
            KanbanItem existingKanbanItem = await progenyDbContext.KanbanItemsDb.SingleOrDefaultAsync(k => k.KanbanItemId == kanbanItem.KanbanItemId);
            if (existingKanbanItem == null)
            {
                return null;
            }

            if (hardDelete)
            {
                progenyDbContext.KanbanItemsDb.Remove(existingKanbanItem);
            }
            else
            {
                existingKanbanItem.ModifiedTime = DateTime.UtcNow;
                existingKanbanItem.IsDeleted = true;
                progenyDbContext.KanbanItemsDb.Update(existingKanbanItem);
            }

            await progenyDbContext.SaveChangesAsync();
            // No permissions are set for Kanban items, as they are protected through the linked TodoItem.
            return existingKanbanItem;
        }

        /// <summary>
        /// Retrieves a list of Kanban items associated with the specified Kanban board.
        /// </summary>
        /// <remarks>This method fetches all Kanban items for the specified board from the database and
        /// populates  their associated to-do item details by retrieving them individually. The returned list will be 
        /// empty if no items are associated with the specified board. This method does not validate if a user has access to the data.</remarks>
        /// <param name="kanbanBoardId">The unique identifier of the Kanban board for which to retrieve the items.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <param name="includeDeleted">If set to <see langword="true"/>, items marked as deleted will be included in the results.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see
        /// cref="KanbanItem"/> objects associated with the specified Kanban board. Each item includes  its
        /// corresponding to-do item details.</returns>
        public async Task<List<KanbanItem>> GetKanbanItemsForBoard(int kanbanBoardId, UserInfo currentUserInfo, bool includeDeleted = false)
        {
            List<KanbanItem> kanbanItems = await progenyDbContext.KanbanItemsDb.AsNoTracking().Where(ki => ki.KanbanBoardId == kanbanBoardId).ToListAsync();
            List<KanbanItem> resultItems = [];
            foreach (KanbanItem kanbanItem in kanbanItems)
            {
                if (!includeDeleted && kanbanItem.IsDeleted)
                {
                    continue;
                }
                
                kanbanItem.TodoItem = await todosService.GetTodoItem(kanbanItem.TodoItemId, currentUserInfo);
                if (kanbanItem.TodoItem == null || kanbanItem.TodoItem.TodoItemId == 0)
                {
                    continue;
                }
                resultItems.Add(kanbanItem);
            }

            return resultItems;
        }

        /// <summary>
        /// Gets all Kanban items for a specific to-do item, with optional filtering for deleted items and access control.
        /// </summary>
        /// <param name="todoItemId">The unique identifier of the to-do item for which to retrieve Kanban items.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <param name="includeDeleted">Determines whether to include items marked as deleted in the results. Default is false.</param>
        /// <returns>List of Kanban items associated with the specified to-do item, filtered by deletion status and user access permissions.</returns>
        public async Task<List<KanbanItem>> GetKanbanItemsForTodoItem(int todoItemId, UserInfo currentUserInfo, bool includeDeleted = false)
        {
            List<KanbanItem> kanbanItems = await progenyDbContext.KanbanItemsDb.AsNoTracking().Where(ki => ki.TodoItemId == todoItemId).ToListAsync();
            List<KanbanItem> resultItems = [];
            foreach (KanbanItem kanbanItem in kanbanItems)
            {
                if (!includeDeleted && kanbanItem.IsDeleted)
                {
                    continue;
                }
                if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.TodoItem, kanbanItem.TodoItemId, currentUserInfo, PermissionLevel.View))
                {
                    continue;
                }

                resultItems.Add(kanbanItem);
            }

            return resultItems;
        }
    }
}
