using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KinaUna.Data;

namespace KinaUnaProgenyApi.Services.KanbanServices
{
    /// <summary>
    /// Provides functionality for managing Kanban boards, including retrieving, adding, updating, and deleting boards.
    /// </summary>
    /// <remarks>This service interacts with the underlying database to perform CRUD operations on Kanban
    /// boards. It ensures that each Kanban board has a unique identifier and handles associated data, such as Kanban
    /// items, when deleting a board.</remarks>
    /// <param name="progenyDbContext">Service for accessing the Progeny database context.</param>
    public class KanbanBoardsService(ProgenyDbContext progenyDbContext, IAccessManagementService accessManagementService, IKinaUnaCacheService kinaUnaCacheService, IDistributedCache cache) : IKanbanBoardsService
    {
        /// <summary>
        /// Retrieves a Kanban board by its unique identifier.
        /// </summary>
        /// <remarks>This method performs a database query and returns the Kanban board without tracking
        /// changes  to the entity. Use this method when you need a read-only representation of the Kanban
        /// board.</remarks>
        /// <param name="kanbanBoardId">The unique identifier of the Kanban board to retrieve.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>A <see cref="KanbanBoard"/> object representing the Kanban board with the specified identifier,  or <see
        /// langword="null"/> if no matching board is found.</returns>
        public async Task<KanbanBoard> GetKanbanBoardById(int kanbanBoardId, UserInfo currentUserInfo)
        {
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoardId, currentUserInfo, PermissionLevel.View))
            {
                return new KanbanBoard();
            }

            KanbanBoard kanbanBoard = await GetKanbanBoardFromCache(kanbanBoardId);
            if (kanbanBoard == null)
            {
                kanbanBoard = await SetKanbanBoardInCache(kanbanBoardId);
                if (kanbanBoard == null)
                {
                    return new KanbanBoard();
                }
            }

            kanbanBoard.ItemPerMission = await accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId, kanbanBoard.ProgenyId, kanbanBoard.FamilyId, currentUserInfo);

            return kanbanBoard;
        }

        /// <summary>
        /// Gets the Kanban Board entity with the specified KanbanBoardId from the cache.
        /// </summary>
        /// <param name="id">The KanbanBoardId of the KanbanBoard to get.</param>
        /// <returns>The KanbanBoard with the given KanbanBoardId. Null if the KanbanBoard isn't in the cache.</returns>
        private async Task<KanbanBoard> GetKanbanBoardFromCache(int id)
        {
            string cachedKanbanBoard = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "kanbanBoard" + id);
            if (string.IsNullOrEmpty(cachedKanbanBoard))
            {
                return null;
            }

            KanbanBoard kanbanBoard = JsonSerializer.Deserialize<KanbanBoard>(cachedKanbanBoard, JsonSerializerOptions.Web);
            return kanbanBoard;
        }

        /// <summary>
        /// Gets a Kanban Board entity with the specified KanbanBoardId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The KanbanBoardId of the KanbanBoard to get and set.</param>
        /// <returns>The KanbanBoard object with the given KanbanBoardId. Null if the KanbanBoard doesn't exist.</returns>
        private async Task<KanbanBoard> SetKanbanBoardInCache(int id)
        {
            KanbanBoard kanbanBoard = await progenyDbContext.KanbanBoardsDb.AsNoTracking().SingleOrDefaultAsync(k => k.KanbanBoardId == id);
            if (kanbanBoard == null)
            {
                return null;
            }
            DistributedCacheEntryOptions cacheOptionsSliding = new();
            cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(96, 0, 0));
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "kanbanBoard" + id, JsonSerializer.Serialize(kanbanBoard, JsonSerializerOptions.Web), cacheOptionsSliding);

            return kanbanBoard;
        }

        private async Task RemoveKanbanBoardFromCache(int id)
        {
            await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "kanbanBoard" + id);
        }

        /// <summary>
        /// Adds a new Kanban board to the database and assigns it a unique identifier.
        /// </summary>
        /// <remarks>This method generates a new unique identifier for the Kanban board before saving it
        /// to the database. Changes are persisted to the database asynchronously.</remarks>
        /// <param name="kanbanBoard">The <see cref="KanbanBoard"/> object to be added. The object must not be null.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>A <see cref="KanbanBoard"/> object representing the newly added Kanban board, including its assigned unique
        /// identifier.</returns>
        public async Task<KanbanBoard> AddKanbanBoard(KanbanBoard kanbanBoard, UserInfo currentUserInfo)
        {
            bool hasAccess = false;
            if (kanbanBoard.ProgenyId > 0)
            {
                if (await accessManagementService.HasProgenyPermission(kanbanBoard.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }

            if (kanbanBoard.FamilyId > 0)
            {
                if (await accessManagementService.HasFamilyPermission(kanbanBoard.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    hasAccess = true;
                }
            }
            
            if (!hasAccess)
            {
                return null;
            }

            kanbanBoard.EnsureColumnsAreValid();
            kanbanBoard.UId = Guid.NewGuid().ToString();
            kanbanBoard.CreatedTime = DateTime.UtcNow;
            kanbanBoard.ModifiedTime = DateTime.UtcNow;
            _ = await progenyDbContext.KanbanBoardsDb.AddAsync(kanbanBoard);
            _ = await progenyDbContext.SaveChangesAsync();

            await accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId, kanbanBoard.ProgenyId, kanbanBoard.FamilyId, kanbanBoard.ItemPermissionsDtoList, currentUserInfo);

            await kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(kanbanBoard.ProgenyId, kanbanBoard.FamilyId, KinaUnaTypes.TimeLineType.KanbanBoard);
            _ = await SetKanbanBoardInCache(kanbanBoard.KanbanBoardId);

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
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>The updated <see cref="KanbanBoard"/> object if the update is successful; otherwise, <see langword="null"/>
        /// if no Kanban board with the specified ID exists.</returns>
        public async Task<KanbanBoard> UpdateKanbanBoard(KanbanBoard kanbanBoard, UserInfo currentUserInfo)
        {
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId, currentUserInfo, PermissionLevel.Edit))
            {
                return new KanbanBoard();
            }

            KanbanBoard existingKanbanBoard = await progenyDbContext.KanbanBoardsDb.SingleOrDefaultAsync(k => k.KanbanBoardId == kanbanBoard.KanbanBoardId);
            if (existingKanbanBoard == null || existingKanbanBoard.ProgenyId != kanbanBoard.ProgenyId || existingKanbanBoard.FamilyId != kanbanBoard.FamilyId)
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
            existingKanbanBoard.Tags = kanbanBoard.Tags;
            existingKanbanBoard.Context = kanbanBoard.Context;
            existingKanbanBoard.ItemPermissionsDtoList = kanbanBoard.ItemPermissionsDtoList;
            existingKanbanBoard.EnsureColumnsAreValid();
            
            progenyDbContext.KanbanBoardsDb.Update(existingKanbanBoard);
            _ = await progenyDbContext.SaveChangesAsync();
            
            _ = await accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.KanbanBoard, existingKanbanBoard.KanbanBoardId, existingKanbanBoard.ProgenyId, existingKanbanBoard.FamilyId,
                existingKanbanBoard.ItemPermissionsDtoList, currentUserInfo);

            await kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(existingKanbanBoard.ProgenyId, kanbanBoard.FamilyId, KinaUnaTypes.TimeLineType.KanbanBoard);
            _ = await SetKanbanBoardInCache(existingKanbanBoard.KanbanBoardId);

            return existingKanbanBoard;
        }

        /// <summary>
        /// Deletes the specified Kanban board and its associated Kanban items from the database.
        /// </summary>
        /// <remarks>This method removes the specified Kanban board and all its associated Kanban items
        /// from the database. Ensure that the provided <paramref name="existingKanbanBoard"/> represents a valid and
        /// existing Kanban board.</remarks>
        /// <param name="existingKanbanBoard">The Kanban board to delete. The board must already exist in the database.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <param name="hardDelete">If set to <see langword="true"/>, the Kanban board and its items are permanently removed from the database.</param>
        /// <returns>The deleted <see cref="KanbanBoard"/> if the operation is successful; otherwise, <see langword="null"/> if
        /// the specified board does not exist.</returns>
        public async Task<KanbanBoard> DeleteKanbanBoard(KanbanBoard existingKanbanBoard, UserInfo currentUserInfo, bool hardDelete = false)
        {
            if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, existingKanbanBoard.KanbanBoardId, currentUserInfo, PermissionLevel.Admin))
            {
                return new KanbanBoard();
            }

            KanbanBoard kanbanBoardToDelete = await progenyDbContext.KanbanBoardsDb.SingleOrDefaultAsync(k => k.KanbanBoardId == existingKanbanBoard.KanbanBoardId);
            if (kanbanBoardToDelete == null)
            {
                return null;
            }

            // Delete the associated KanbanItems too.
            List<KanbanItem> kanbanItemsToDelete = await progenyDbContext.KanbanItemsDb.Where(ki => ki.KanbanBoardId == kanbanBoardToDelete.KanbanBoardId).ToListAsync();
            if (kanbanItemsToDelete.Count != 0)
            {
                if (hardDelete)
                {
                    progenyDbContext.KanbanItemsDb.RemoveRange(kanbanItemsToDelete);
                }
                else
                {
                    foreach (KanbanItem kanbanItem in kanbanItemsToDelete)
                    {
                        kanbanItem.IsDeleted = true;
                    }
                    progenyDbContext.KanbanItemsDb.UpdateRange(kanbanItemsToDelete);
                }
            }

            if (hardDelete)
            {
                progenyDbContext.KanbanBoardsDb.Remove(kanbanBoardToDelete);
            }
            else
            {
                kanbanBoardToDelete.IsDeleted = true;
                progenyDbContext.KanbanBoardsDb.Update(kanbanBoardToDelete);
            }

            _ = await progenyDbContext.SaveChangesAsync();

            // Revoke all permissions associated with the Kanban board.
            List<TimelineItemPermission> timelineItemPermissionsList = await accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoardToDelete.KanbanBoardId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            {
                await accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            await kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(kanbanBoardToDelete.ProgenyId, kanbanBoardToDelete.FamilyId, KinaUnaTypes.TimeLineType.KanbanBoard);
            await RemoveKanbanBoardFromCache(kanbanBoardToDelete.KanbanBoardId);

            return kanbanBoardToDelete;
        }

        /// <summary>
        /// Retrieves a list of Kanban boards associated with a specific progeny, filtered by the user's access level.
        /// </summary>
        /// <remarks>This method performs a database query to retrieve Kanban boards that belong to the
        /// specified progeny and meet the access level requirements. The results are returned as a read-only list and
        /// are not tracked by the database context.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny for which Kanban boards are requested.</param>
        /// <param name="familyId">The unique identifier of the family for which Kanban boards are requested.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <param name="request">An object containing additional parameters for the request. This may include filtering or pagination
        /// options.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
        /// cref="KanbanBoard"/> objects that meet the specified criteria. The list will be empty if no matching boards
        /// are found.</returns>
        public async Task<List<KanbanBoard>> GetKanbanBoardsForProgenyOrFamily(int progenyId, int familyId, UserInfo currentUserInfo, KanbanBoardsRequest request)
        {
            bool hasCachedData = false;
            KanbanBoard[] allKanbanBoardsForProgenyOrFamily = [];
            KanbanBoardsListCacheEntry cacheEntry = await kinaUnaCacheService.GetKanbanBoardsListCache(currentUserInfo.UserId, progenyId, familyId);
            TimelineUpdatedCacheEntry timelineUpdatedCacheEntry = await kinaUnaCacheService.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, familyId, KinaUnaTypes.TimeLineType.KanbanBoard);
            if (cacheEntry != null && timelineUpdatedCacheEntry != null)
            {
                if (cacheEntry.UpdateTime >= timelineUpdatedCacheEntry.UpdateTime)
                {
                    allKanbanBoardsForProgenyOrFamily = cacheEntry.KanbanBoardsList;
                    hasCachedData = true;
                }
            }
            
            if (request.IncludeDeleted)
            {
                if (progenyId > 0)
                {
                    allKanbanBoardsForProgenyOrFamily = await progenyDbContext.KanbanBoardsDb.AsNoTracking()
                        .Where(k => k.ProgenyId == progenyId).ToArrayAsync();
                }

                if (familyId > 0)
                {
                    allKanbanBoardsForProgenyOrFamily = await progenyDbContext.KanbanBoardsDb.AsNoTracking()
                        .Where(k => k.FamilyId == familyId).ToArrayAsync();
                }
            }
            else
            {
                if (progenyId > 0 && !hasCachedData)
                {
                    allKanbanBoardsForProgenyOrFamily = await progenyDbContext.KanbanBoardsDb.AsNoTracking()
                        .Where(k => k.ProgenyId == progenyId && !k.IsDeleted).ToArrayAsync();
                }

                if (familyId > 0 && !hasCachedData)
                {
                    allKanbanBoardsForProgenyOrFamily = await progenyDbContext.KanbanBoardsDb.AsNoTracking()
                        .Where(k => k.FamilyId == familyId && !k.IsDeleted).ToArrayAsync();
                }
            }
            if (allKanbanBoardsForProgenyOrFamily.Length == 0)
            {
                return allKanbanBoardsForProgenyOrFamily.ToList();
            }

            List<KanbanBoard> accessibleKanbanBoards = [];
            if (hasCachedData)
            {
                accessibleKanbanBoards = allKanbanBoardsForProgenyOrFamily.ToList();
            }
            else
            {
                foreach (KanbanBoard kanbanBoard in allKanbanBoardsForProgenyOrFamily)
                {
                    if (!await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId, currentUserInfo, PermissionLevel.View)) continue;
                    kanbanBoard.ItemPerMission = await accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId, kanbanBoard.ProgenyId, kanbanBoard.FamilyId, currentUserInfo);
                    accessibleKanbanBoards.Add(kanbanBoard);
                }

                await kinaUnaCacheService.SetKanbanBoardsListCache(currentUserInfo.UserId, progenyId, familyId, accessibleKanbanBoards.ToArray());
            }
            

            // Filter by tags if provided
            if (!string.IsNullOrWhiteSpace(request.TagFilter))
            {
                List<string> tags = [.. request.TagFilter.Split(',').Select(tag => tag.Trim())];
                accessibleKanbanBoards =
                [
                    .. accessibleKanbanBoards.Where(t =>
                        t.Tags != null &&
                        t.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(tag => tag.Trim())
                            .Any(itemTag => tags.Any(filterTag => string.Equals(itemTag, filterTag, StringComparison.OrdinalIgnoreCase)))
                    )
                ];
            }

            // Filter by context if provided
            if (!string.IsNullOrWhiteSpace(request.ContextFilter))
            {
                List<string> contexts = [.. request.ContextFilter.Split(',').Select(context => context.Trim())];
                accessibleKanbanBoards =
                [
                    .. accessibleKanbanBoards.Where(t =>
                        t.Context != null &&
                        t.Context.Split(',')
                            .Select(c => c.Trim())
                            .Any(itemContext => contexts.Any(filterContext => string.Equals(itemContext, filterContext, StringComparison.OrdinalIgnoreCase)))
                    )
                ];
            }

            return accessibleKanbanBoards;
        }

        /// <summary>
        /// Creates a response object containing a paginated list of Kanban boards and associated metadata.
        /// </summary>
        /// <remarks>The Kanban boards in the response are sorted in descending order by their last
        /// modified time, and then by their creation time if the modified times are equal.  If the <paramref
        /// name="request"/> specifies a positive number of items per page, the response will include only the specified
        /// subset of Kanban boards. Otherwise, all Kanban boards are included in the response.</remarks>
        /// <param name="kanbanBoards">The complete list of Kanban boards to include in the response.</param>
        /// <param name="request">The request object specifying pagination parameters such as the number of items per page and the number of
        /// items to skip.</param>
        /// <returns>A <see cref="KanbanBoardsResponse"/> object containing the paginated list of Kanban boards, the total number
        /// of items, the total number of pages, and the current page number.</returns>
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

        /// <summary>
        /// Retrieves a list of Kanban boards for the specified progeny, that the current user has permission to view.
        /// </summary>
        /// <remarks>Only Kanban boards that are not marked as deleted (<c>IsDeleted</c> is <see
        /// langword="false"/>)  and meet the specified access level are included in the results.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny for which to retrieve Kanban boards.</param>
        /// <param name="familyId">The unique identifier of the family for which to retrieve Kanban boards.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>A list of <see cref="KanbanBoard"/> objects that match the specified criteria. The list will be
        /// empty if no boards are found.</returns>
        public async Task<List<KanbanBoard>> GetKanbanBoardsListForProgenyOrFamily(int progenyId, int familyId, UserInfo currentUserInfo)
        {
            KanbanBoard[] kanbanBoards = [];
            KanbanBoardsListCacheEntry cacheEntry = await kinaUnaCacheService.GetKanbanBoardsListCache(currentUserInfo.UserId, progenyId, familyId);
            TimelineUpdatedCacheEntry timelineUpdatedCacheEntry = await kinaUnaCacheService.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, familyId, KinaUnaTypes.TimeLineType.KanbanBoard);
            if (cacheEntry != null && timelineUpdatedCacheEntry != null)
            {
                if (cacheEntry.UpdateTime >= timelineUpdatedCacheEntry.UpdateTime)
                {
                    return cacheEntry.KanbanBoardsList.ToList();
                }
            }

            if (progenyId > 0)
            {
                kanbanBoards = await progenyDbContext.KanbanBoardsDb.AsNoTracking()
                    .Where(k => k.ProgenyId == progenyId && !k.IsDeleted).ToArrayAsync();
            }
            if (familyId > 0)
            {
                kanbanBoards = await progenyDbContext.KanbanBoardsDb.AsNoTracking()
                    .Where(k => k.FamilyId == familyId && !k.IsDeleted).ToArrayAsync();
            }
            
            List<KanbanBoard> accessibleKanbanBoards = [];
            foreach (KanbanBoard kanbanBoard in kanbanBoards)
            {
                if (await accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId, currentUserInfo, PermissionLevel.View))
                {
                    kanbanBoard.ItemPerMission = await accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.KanbanBoard, kanbanBoard.KanbanBoardId, kanbanBoard.ProgenyId, kanbanBoard.FamilyId, currentUserInfo);
                    accessibleKanbanBoards.Add(kanbanBoard);
                }
            }
            await kinaUnaCacheService.SetKanbanBoardsListCache(currentUserInfo.UserId, progenyId, familyId, accessibleKanbanBoards.ToArray());
            
            return accessibleKanbanBoards;
        }
    }
}
