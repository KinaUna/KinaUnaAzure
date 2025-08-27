using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    /// <summary>
    /// Provides functionality for managing Kanban boards, including retrieving, adding, updating, and deleting boards.
    /// </summary>
    /// <remarks>This service interacts with the underlying database to perform CRUD operations on Kanban
    /// boards. It ensures that each Kanban board has a unique identifier and handles associated data, such as Kanban
    /// items, when deleting a board.</remarks>
    /// <param name="progenyDbContext">Service for accessing the Progeny database context.</param>
    public class KanbanBoardsService(ProgenyDbContext progenyDbContext): IKanbanBoardsService
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
        public async Task<KanbanBoard> GetKanbanBoardById(int kanbanBoardId)
        {
            KanbanBoard kanbanBoard = await progenyDbContext.KanbanBoardsDb.AsNoTracking().SingleOrDefaultAsync(k => k.KanbanBoardId == kanbanBoardId);
            
            return kanbanBoard;
        }

        /// <summary>
        /// Adds a new Kanban board to the database and assigns it a unique identifier.
        /// </summary>
        /// <remarks>This method generates a new unique identifier for the Kanban board before saving it
        /// to the database. Changes are persisted to the database asynchronously.</remarks>
        /// <param name="kanbanBoard">The <see cref="KanbanBoard"/> object to be added. The object must not be null.</param>
        /// <returns>A <see cref="KanbanBoard"/> object representing the newly added Kanban board, including its assigned unique
        /// identifier.</returns>
        public async Task<KanbanBoard> AddKanbanBoard(KanbanBoard kanbanBoard)
        {
            kanbanBoard.UId = Guid.NewGuid().ToString();
            await progenyDbContext.KanbanBoardsDb.AddAsync(kanbanBoard);
            await progenyDbContext.SaveChangesAsync();
            return kanbanBoard;
        }

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
        public async Task<KanbanBoard> UpdateKanbanBoard(KanbanBoard kanbanBoard)
        {
            KanbanBoard existingKanbanBoard = await progenyDbContext.KanbanBoardsDb.SingleOrDefaultAsync(k => k.KanbanBoardId == kanbanBoard.KanbanBoardId);
            if (existingKanbanBoard == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(existingKanbanBoard.UId))
            {
                existingKanbanBoard.UId = Guid.NewGuid().ToString();
            }

            existingKanbanBoard.Title = kanbanBoard.Title;
            existingKanbanBoard.Description = kanbanBoard.Description;
            existingKanbanBoard.Columns = kanbanBoard.Columns;
            existingKanbanBoard.ModifiedBy = kanbanBoard.ModifiedBy;
            existingKanbanBoard.ModifiedTime = kanbanBoard.ModifiedTime;
            existingKanbanBoard.AccessLevel = kanbanBoard.AccessLevel;
            
            progenyDbContext.KanbanBoardsDb.Update(existingKanbanBoard);
            await progenyDbContext.SaveChangesAsync();

            return existingKanbanBoard;
        }

        /// <summary>
        /// Deletes the specified Kanban board and its associated Kanban items from the database.
        /// </summary>
        /// <remarks>This method removes the specified Kanban board and all its associated Kanban items
        /// from the database. Ensure that the provided <paramref name="existingKanbanBoard"/> represents a valid and
        /// existing Kanban board.</remarks>
        /// <param name="existingKanbanBoard">The Kanban board to delete. The board must already exist in the database.</param>
        /// <returns>The deleted <see cref="KanbanBoard"/> if the operation is successful; otherwise, <see langword="null"/> if
        /// the specified board does not exist.</returns>
        public async Task<KanbanBoard> DeleteKanbanBoard(KanbanBoard existingKanbanBoard)
        {
            KanbanBoard kanbanBoardToDelete = await progenyDbContext.KanbanBoardsDb.SingleOrDefaultAsync(k => k.KanbanBoardId == existingKanbanBoard.KanbanBoardId);
            if (kanbanBoardToDelete == null)
            {
                return null;
            }

            // Delete the associated KanbanItems too.
            List<KanbanItem> kanbanItemsToDelete = await progenyDbContext.KanbanItemsDb.Where(ki => ki.KanbanBoardId == kanbanBoardToDelete.KanbanBoardId).ToListAsync();
            if (kanbanItemsToDelete.Count != 0)
            {
                progenyDbContext.KanbanItemsDb.RemoveRange(kanbanItemsToDelete);
                await progenyDbContext.SaveChangesAsync();
            }

            progenyDbContext.KanbanBoardsDb.Remove(kanbanBoardToDelete);
            await progenyDbContext.SaveChangesAsync();

            return kanbanBoardToDelete;
        }

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
        public async Task<List<KanbanBoard>> GetKanbanBoardsForProgeny(int progenyId, int userAccessAccessLevel, KanbanBoardsRequest request)
        {
            List<KanbanBoard> kanbanBoards = await progenyDbContext.KanbanBoardsDb.AsNoTracking().Where(k => k.ProgenyId == progenyId && k.AccessLevel >= userAccessAccessLevel).ToListAsync();

            return kanbanBoards;
        }

        public KanbanBoardsResponse CreateKanbanBoardsResponse(List<KanbanBoard> kanbanBoards, KanbanBoardsRequest request)
        {
            KanbanBoardsResponse kanbanBoardsResponse = new()
            {
                KanbanBoardsRequest = request,
                TotalItems = kanbanBoards.Count,
                TotalPages = (int)Math.Ceiling((double)kanbanBoards.Count / request.NumberOfItems)
            };

            kanbanBoards = [.. kanbanBoards.OrderByDescending(k => k.ModifiedTime).ThenByDescending(k => k.CreatedTime)];
            if (request.NumberOfItems > 0)
            {
                kanbanBoardsResponse.KanbanBoards = [.. kanbanBoards.Skip(request.Skip).Take(request.NumberOfItems)];
                kanbanBoardsResponse.PageNumber = (int)Math.Ceiling((double)request.Skip / request.NumberOfItems) + 1;
            }
            else
            {
                kanbanBoardsResponse.KanbanBoards = kanbanBoards;
                kanbanBoardsResponse.PageNumber = 1;
            }
            
            return kanbanBoardsResponse;
        }
    }
}
