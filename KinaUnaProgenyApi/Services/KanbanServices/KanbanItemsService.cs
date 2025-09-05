using System.Collections.Generic;
using System.Linq;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    /// <summary>
    /// Service for managing Kanban items.
    /// </summary>
    /// <param name="progenyDbContext"></param>
    /// <param name="todosService"></param>
    public class KanbanItemsService(ProgenyDbContext progenyDbContext, ITodosService todosService): IKanbanItemsService
    {
        /// <summary>
        /// Retrieves a Kanban item by its unique identifier.
        /// </summary>
        /// <param name="kanbanItemId">The unique identifier of the Kanban item.</param>
        /// <returns>The Kanban item if found, with the associated TodoItem; otherwise, null.</returns>
        public async Task<KanbanItem> GetKanbanItemById(int kanbanItemId)
        {
            KanbanItem kanbanItem = await progenyDbContext.KanbanItemsDb.AsNoTracking().SingleOrDefaultAsync(k => k.KanbanItemId == kanbanItemId);
            kanbanItem.TodoItem = await todosService.GetTodoItem(kanbanItem.TodoItemId);

            return kanbanItem;
        }

        /// <summary>
        /// Adds a new Kanban item to the database and assigns it a unique identifier.
        /// </summary>
        /// <remarks>This method generates a new unique identifier for the Kanban item before saving it to
        /// the database. Ensure that the provided <paramref name="kanbanItem"/> is valid and contains all required
        /// data.</remarks>
        /// <param name="kanbanItem">The <see cref="KanbanItem"/> to be added. The item's properties should be populated before calling this
        /// method.</param>
        /// <returns>The added <see cref="KanbanItem"/> with its unique identifier assigned. Does not include the associated TodoItem.</returns>
        public async Task<KanbanItem> AddKanbanItem(KanbanItem kanbanItem)
        {
            int kanbanItemsCount = await progenyDbContext.KanbanItemsDb.CountAsync(k => k.KanbanBoardId == kanbanItem.KanbanBoardId && k.ColumnId == kanbanItem.ColumnId);
            kanbanItem.RowIndex = kanbanItemsCount;

            kanbanItem.UId = System.Guid.NewGuid().ToString();
            await progenyDbContext.KanbanItemsDb.AddAsync(kanbanItem);
            await progenyDbContext.SaveChangesAsync();

            return kanbanItem;
        }

        /// <summary>
        /// Updates an existing Kanban item with new values and saves the changes to the database.
        /// </summary>
        /// <remarks>If the specified Kanban item does not exist in the database, the method returns <see
        /// langword="null"/> without making any changes. If the existing Kanban item does not have a unique identifier
        /// (<c>UId</c>), a new GUID is generated and assigned to it.</remarks>
        /// <param name="kanbanItem">The <see cref="KanbanItem"/> containing the updated values. The item's <c>KanbanItemId</c> must match an
        /// existing item in the database.</param>
        /// <returns>The updated <see cref="KanbanItem"/> if the item exists in the database; otherwise, <see langword="null"/>.
        /// Does not include the associated TodoItem.</returns>
        public async Task<KanbanItem> UpdateKanbanItem(KanbanItem kanbanItem)
        {
            KanbanItem existingKanbanItem = await progenyDbContext.KanbanItemsDb.SingleOrDefaultAsync(k => k.KanbanItemId == kanbanItem.KanbanItemId);
            if (existingKanbanItem == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(existingKanbanItem.UId))
            {
                existingKanbanItem.UId = System.Guid.NewGuid().ToString();
            }
            existingKanbanItem.ColumnId = kanbanItem.ColumnId;
            existingKanbanItem.RowIndex = kanbanItem.RowIndex;
            existingKanbanItem.ModifiedBy = kanbanItem.ModifiedBy;
            existingKanbanItem.ModifiedTime = kanbanItem.ModifiedTime;
            
            progenyDbContext.Update(existingKanbanItem);
            await progenyDbContext.SaveChangesAsync();

            return existingKanbanItem;
        }

        /// <summary>
        /// Deletes the specified Kanban item from the database.
        /// </summary>
        /// <remarks>This method performs a lookup to ensure the specified Kanban item exists in the
        /// database before attempting to delete it. If the item is not found, no changes are made to the
        /// database.</remarks>
        /// <param name="kanbanItem">The Kanban item to delete. The item must have a valid <see cref="KanbanItem.KanbanItemId"/>.</param>
        /// <param name="hardDelete">If set to <see langword="true"/>, the Kanban item is permanently removed from the database.</param>
        /// <returns>The deleted Kanban item if it was successfully removed; otherwise, <see langword="null"/> if the item does
        /// not exist in the database. Does not include the associated TodoItem.</returns>
        public async Task<KanbanItem> DeleteKanbanItem(KanbanItem kanbanItem, bool hardDelete = false)
        {
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
                existingKanbanItem.IsDeleted = true;
                progenyDbContext.KanbanItemsDb.Update(existingKanbanItem);
            }

            await progenyDbContext.SaveChangesAsync();

            return existingKanbanItem;
        }

        /// <summary>
        /// Retrieves a list of Kanban items associated with the specified Kanban board.
        /// </summary>
        /// <remarks>This method fetches all Kanban items for the specified board from the database and
        /// populates  their associated to-do item details by retrieving them individually. The returned list will be 
        /// empty if no items are associated with the specified board. This method does not validate if a user has access to the data.</remarks>
        /// <param name="kanbanBoardId">The unique identifier of the Kanban board for which to retrieve the items.</param>
        /// <param name="includeDeleted">If set to <see langword="true"/>, items marked as deleted will be included in the results.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see
        /// cref="KanbanItem"/> objects associated with the specified Kanban board. Each item includes  its
        /// corresponding to-do item details.</returns>
        public async Task<List<KanbanItem>> GetKanbanItemsForBoard(int kanbanBoardId, bool includeDeleted = false)
        {
            List<KanbanItem> kanbanItems = await progenyDbContext.KanbanItemsDb.AsNoTracking().Where(ki => ki.KanbanBoardId == kanbanBoardId).ToListAsync();
            List<KanbanItem> resultItems = [];
            foreach (KanbanItem kanbanItem in kanbanItems)
            {
                if (!includeDeleted && kanbanItem.IsDeleted)
                {
                    continue;
                }

                kanbanItem.TodoItem = await todosService.GetTodoItem(kanbanItem.TodoItemId);
                resultItems.Add(kanbanItem);
            }

            return resultItems;
        }
    }
}
