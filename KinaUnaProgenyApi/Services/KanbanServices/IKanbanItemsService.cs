using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    /// <summary>
    /// Defines the contract for managing Kanban items, including retrieving, adding, updating, and deleting items, as
    /// well as retrieving items associated with a specific Kanban board.
    /// </summary>
    /// <remarks>This interface provides asynchronous methods for interacting with Kanban items in a Kanban
    /// board system. Implementations of this interface should ensure thread safety and proper handling of data
    /// persistence.</remarks>
    public interface IKanbanItemsService
    {
        /// <summary>
        /// Retrieves a Kanban item by its unique identifier.
        /// </summary>
        /// <param name="kanbanItemId">The unique identifier of the Kanban item.</param>
        /// <returns>The Kanban item if found, with the associated TodoItem; otherwise, null.</returns>
        public Task<KanbanItem> GetKanbanItemById(int kanbanItemId);

        /// <summary>
        /// Adds a new Kanban item to the database and assigns it a unique identifier.
        /// </summary>
        /// <remarks>This method generates a new unique identifier for the Kanban item before saving it to
        /// the database. Ensure that the provided <paramref name="kanbanItem"/> is valid and contains all required
        /// data.</remarks>
        /// <param name="kanbanItem">The <see cref="KanbanItem"/> to be added. The item's properties should be populated before calling this
        /// method.</param>
        /// <returns>The added <see cref="KanbanItem"/> with its unique identifier assigned. Does not include the associated TodoItem.</returns>
        Task<KanbanItem> AddKanbanItem(KanbanItem kanbanItem);

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
        Task<KanbanItem> UpdateKanbanItem(KanbanItem kanbanItem);

        /// <summary>
        /// Deletes the specified Kanban item from the database.
        /// </summary>
        /// <remarks>This method performs a lookup to ensure the specified Kanban item exists in the
        /// database before attempting to delete it. If the item is not found, no changes are made to the
        /// database.</remarks>
        /// <param name="kanbanItem">The Kanban item to delete. The item must have a valid <see cref="KanbanItem.KanbanItemId"/>.</param>
        /// <returns>The deleted Kanban item if it was successfully removed; otherwise, <see langword="null"/> if the item does
        /// not exist in the database. Does not include the associated TodoItem.</returns>
        Task<KanbanItem> DeleteKanbanItem(KanbanItem kanbanItem);

        /// <summary>
        /// Retrieves a list of Kanban items associated with the specified Kanban board.
        /// </summary>
        /// <remarks>This method fetches all Kanban items for the specified board from the database and
        /// populates  their associated to-do item details by retrieving them individually. The returned list will be 
        /// empty if no items are associated with the specified board. This method does not validate if a user has access to the data.</remarks>
        /// <param name="kanbanBoardId">The unique identifier of the Kanban board for which to retrieve the items.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see
        /// cref="KanbanItem"/> objects associated with the specified Kanban board. Each item includes  its
        /// corresponding to-do item details.</returns>
        Task<List<KanbanItem>> GetKanbanItemsForBoard(int kanbanBoardId);
    }
}
