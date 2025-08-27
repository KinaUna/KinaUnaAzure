using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    /// <summary>
    /// Provides functionality for managing Kanban boards, including retrieving, adding, updating, and deleting boards.
    /// </summary>
    /// <remarks>This service interacts with the underlying database to perform CRUD operations on Kanban
    /// boards. It ensures that each Kanban board has a unique identifier and handles associated data, such as Kanban
    /// items, when deleting a board.</remarks>
    public interface IKanbanBoardsService
    {
        /// <summary>
        /// Retrieves a Kanban board by its unique identifier.
        /// </summary>
        /// <remarks>This method performs a database query and returns the Kanban board without tracking
        /// changes  to the entity. Use this method when you need a read-only representation of the Kanban
        /// board.</remarks>
        /// <param name="kanbanBoardId">The unique identifier of the Kanban board to retrieve.</param>
        /// <returns>A <see cref="KanbanBoard"/> object representing the Kanban board with the specified identifier,  or <see
        /// langword="null"/> if no matching board is found.</returns>
        Task<KanbanBoard> GetKanbanBoardById(int kanbanBoardId);

        /// <summary>
        /// Adds a new Kanban board to the database and assigns it a unique identifier.
        /// </summary>
        /// <remarks>This method generates a new unique identifier for the Kanban board before saving it
        /// to the database. Changes are persisted to the database asynchronously.</remarks>
        /// <param name="kanbanBoard">The <see cref="KanbanBoard"/> object to be added. The object must not be null.</param>
        /// <returns>A <see cref="KanbanBoard"/> object representing the newly added Kanban board, including its assigned unique
        /// identifier.</returns>
        Task<KanbanBoard> AddKanbanBoard(KanbanBoard kanbanBoard);

        /// <summary>
        /// Updates an existing Kanban board with the provided details.
        /// </summary>
        /// <remarks>This method updates the properties of an existing Kanban board in the database with
        /// the values provided in the <paramref name="kanbanBoard"/> parameter. If the Kanban board does not exist, the
        /// method returns <see langword="null"/>. The method also ensures that the Kanban board has a unique identifier
        /// (<see cref="KanbanBoard.UId"/>) if it is not already set.</remarks>
        /// <param name="kanbanBoard">The <see cref="KanbanBoard"/> object containing the updated details. The <see
        /// cref="KanbanBoard.KanbanBoardId"/> property must match the ID of an existing Kanban board in the database.</param>
        /// <returns>The updated <see cref="KanbanBoard"/> object if the update is successful; otherwise, <see langword="null"/>
        /// if no Kanban board with the specified ID exists.</returns>
        Task<KanbanBoard> UpdateKanbanBoard(KanbanBoard kanbanBoard);

        /// <summary>
        /// Deletes the specified Kanban board and its associated Kanban items from the database.
        /// </summary>
        /// <remarks>This method removes the specified Kanban board and all its associated Kanban items
        /// from the database. Ensure that the provided <paramref name="existingKanbanBoard"/> represents a valid and
        /// existing Kanban board.</remarks>
        /// <param name="existingKanbanBoard">The Kanban board to delete. The board must already exist in the database.</param>
        /// <returns>The deleted <see cref="KanbanBoard"/> if the operation is successful; otherwise, <see langword="null"/> if
        /// the specified board does not exist.</returns>
        Task<KanbanBoard> DeleteKanbanBoard(KanbanBoard existingKanbanBoard);

        /// <summary>
        /// Retrieves a list of Kanban boards associated with a specific progeny, filtered by the user's access level.
        /// </summary>
        /// <remarks>This method performs a database query to retrieve Kanban boards that belong to the
        /// specified progeny and meet the access level requirements. The results are returned as a read-only list and
        /// are not tracked by the database context.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny for which Kanban boards are requested.</param>
        /// <param name="userAccessAccessLevel">The minimum access level required to include a Kanban board in the result. Boards with an access level lower
        /// than this value will be excluded.</param>
        /// <param name="request">An object containing additional parameters for the request. This may include filtering or pagination
        /// options.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="KanbanBoard"/> objects that meet the specified criteria. The list will be empty if no matching boards
        /// are found.</returns>
        Task<List<KanbanBoard>> GetKanbanBoardsForProgeny(int progenyId, int userAccessAccessLevel, KanbanBoardsRequest request);

        KanbanBoardsResponse CreateKanbanBoardsResponse(List<KanbanBoard> kanbanBoards, KanbanBoardsRequest request);
    }
}
