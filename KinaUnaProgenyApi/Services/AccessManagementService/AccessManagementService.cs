using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.AccessManagementService
{
    /// <summary>
    /// Service for managing access permissions for users and groups to various resources such as progeny, families, and timeline items.
    /// </summary>
    /// <param name="progenyDbContext"></param>
    public class AccessManagementService(ProgenyDbContext progenyDbContext, MediaDbContext mediaDbContext, IPermissionAuditLogsService permissionAuditLogService, IDistributedCache cache) : IAccessManagementService
    {
        /// <summary>
        /// Determines whether a user has the specified access level for a given timeline item (e.g., Note, TodoItem, Sleep, etc.).
        /// </summary>
        /// <param name="itemType">KinaUnaTypes.TimeLineType of the item whose access to is being checked.</param>
        /// <param name="itemId">The unique identifier of the item whose access to is being checked.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <param name="requiredLevel">The minimum permission level required for access.</param>
        /// <returns>Boolean value indicating whether the user has the required access level for the timeline item.</returns>
        public async Task<bool> HasItemPermission(KinaUnaTypes.TimeLineType itemType, int itemId, UserInfo userInfo, PermissionLevel requiredLevel)
        {
            if (userInfo == null || itemId == 0)
            {
                return false;
            }
            
            // Todo: Allow access to default progeny items, for users who are not logged in.
            
            TimelineItemPermission itemPermission = new()
            {
                PermissionLevel = PermissionLevel.None
            };

            string cachedItemPermission = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion +
                                                                     "hasItemPermissionPermission" + (int)itemType + "_itemId_" + itemId + "_userId_" + userInfo.UserId + "_level_" + (int)requiredLevel);
            if (!string.IsNullOrEmpty(cachedItemPermission))
            {
                HasItemPermissionCacheEntry cacheEntry = JsonSerializer.Deserialize<HasItemPermissionCacheEntry>(cachedItemPermission, JsonSerializerOptions.Web);
                // Check if user data has been modified since the cache was created.
                string userCacheEntryString = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userCacheEntry_" + userInfo.UserId);
                if (!string.IsNullOrEmpty(userCacheEntryString))
                {
                    UserUpdatedCacheEntry userCacheEntry = JsonSerializer.Deserialize<UserUpdatedCacheEntry>(userCacheEntryString, JsonSerializerOptions.Web);
                    if (userCacheEntry != null)
                    {
                        if (userCacheEntry.UpdateTime < cacheEntry.UpdateTime)
                        {
                            return cacheEntry.TimelineItemPermission.PermissionLevel >= requiredLevel;
                        }
                    }
                }
                else
                {
                    return cacheEntry.TimelineItemPermission.PermissionLevel >= requiredLevel;
                }
            }

            // Special cases, Creator only and Private.
            if (requiredLevel == PermissionLevel.CreatorOnly)
            {
                return await HasCreatorOnlyPermission(itemType, itemId, userInfo);
            }

            if (requiredLevel == PermissionLevel.Private)
            {
                return await HasPrivatePermission(itemType, itemId, userInfo);
            }

            Dictionary<int, PermissionLevel> allUsersPermissionsForType = await AllUsersItemPermissionsDictionary(userInfo, itemType);
            if (allUsersPermissionsForType.TryGetValue(itemId, out PermissionLevel value))
            {
                itemPermission.PermissionLevel = value;
            }
            HasItemPermissionCacheEntry newCacheEntry = new()
            {
                TimelineItemPermission = itemPermission,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSlidingView = new();
            cacheOptionsSlidingView.SetSlidingExpiration(new TimeSpan(0, 1, 0, 0));
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "hasItemPermissionPermission" + (int)itemType + "_itemId_" + itemId + "_userId_" + userInfo.UserId + "_level_" + (int)requiredLevel
                , JsonSerializer.Serialize(newCacheEntry, JsonSerializerOptions.Web), cacheOptionsSlidingView);

            return itemPermission.PermissionLevel >= requiredLevel;
        }

        /// <summary>
        /// Retrieves the permission level for a specific timeline item and user.
        /// </summary>
        /// <remarks>This method checks both direct user permissions and group-based permissions to
        /// determine the highest applicable permission level. Group permissions are considered only if the user is a
        /// member of the respective group.</remarks>
        /// <param name="itemType">The type of the timeline item, represented as a <see cref="KinaUnaTypes.TimeLineType"/>.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <param name="progenyId">The unique identifier of the progeny associated with the timeline item.</param>
        /// <param name="familyId">The unique identifier of the family associated with the timeline item.</param>
        /// <param name="userInfo">The user information, represented as a <see cref="UserInfo"/> object, for whom the permission is being
        /// retrieved.</param>
        /// <param name="requiredLevel">An optional parameter specifying a minimum required permission level.
        /// If provided, the method may optimize its search by stopping early if this level is met or exceeded.</param>
        /// <returns>A <see cref="TimelineItemPermission"/> object representing the highest permission level the specified user
        /// has for the given timeline item. If no permissions are found, the returned object will have a <see
        /// cref="PermissionLevel"/> of <see cref="PermissionLevel.None"/>.</returns>
        public async Task<TimelineItemPermission> GetItemPermissionForUser(KinaUnaTypes.TimeLineType itemType, int itemId, int progenyId, int familyId, UserInfo userInfo, PermissionLevel? requiredLevel = null)
        {
            TimelineItemPermission resultPermission = new()
            {
                PermissionLevel = PermissionLevel.None
            };

            if (progenyId == Constants.DefaultChildId)
            {
                // Default progeny, allow view access to everyone.
                resultPermission.PermissionLevel = PermissionLevel.View;
            }
            
            PermissionLevel highestPermission = PermissionLevel.None;

            List<TimelineItemPermission> usersItemPermissions = await AllUsersTimelineItemPermissions(userInfo, itemType);
            // Check for inherited permissions.
            TimelineItemPermission inheritedPermission = usersItemPermissions.Find(tp => tp.InheritPermissions && tp.ItemId == itemId);
            if (inheritedPermission != null)
            {
                if (inheritedPermission.ProgenyId > 0)
                {
                    ProgenyPermission progenyPermission = await GetProgenyPermissionForUser(inheritedPermission.ProgenyId, userInfo);
                    if (progenyPermission.PermissionLevel > highestPermission)
                    {
                        highestPermission = progenyPermission.PermissionLevel;
                        inheritedPermission.PermissionLevel = progenyPermission.PermissionLevel;
                        resultPermission = inheritedPermission;
                        if (requiredLevel <= highestPermission)
                        {
                            return resultPermission;
                        }
                    }
                }

                if (inheritedPermission.FamilyId > 0)
                {
                    FamilyPermission familyPermission = await GetFamilyPermissionForUser(inheritedPermission.FamilyId, userInfo);
                    if (familyPermission.PermissionLevel > highestPermission)
                    {
                        highestPermission = familyPermission.PermissionLevel;
                        inheritedPermission.PermissionLevel = familyPermission.PermissionLevel;
                        resultPermission = inheritedPermission;
                        if (requiredLevel != null)
                        {
                            if (requiredLevel <= highestPermission)
                            {
                                return resultPermission;
                            }
                        }
                    }
                }
            }
            
            // Check direct user permissions.
            TimelineItemPermission timelineItemPermission = usersItemPermissions.Find(tp => tp.UserId == userInfo.UserId && tp.TimelineType == itemType && tp.ItemId == itemId);
            if (timelineItemPermission != null)
            {
                if (timelineItemPermission.PermissionLevel == PermissionLevel.CreatorOnly)
                {
                    if (await HasCreatorOnlyPermission(itemType, itemId, userInfo))
                    {
                        // User is the creator, return CreatorOnly permission.
                        return timelineItemPermission;
                    }
                }

                if (timelineItemPermission.PermissionLevel == PermissionLevel.Private)
                {
                    if (await HasPrivatePermission(itemType, itemId, userInfo))
                    {
                        // User is the owner of the progeny, return Private permission.
                        return timelineItemPermission;
                    }
                }

                if (timelineItemPermission.PermissionLevel > highestPermission)
                {
                    resultPermission = timelineItemPermission;
                    highestPermission = timelineItemPermission.PermissionLevel;
                    if (requiredLevel != null)
                    {
                        if (requiredLevel <= highestPermission)
                        {
                            return resultPermission;
                        }
                    }
                }
            }

            // Check group permissions.
            List<TimelineItemPermission> groupPermissions = usersItemPermissions.Where(tp => tp.GroupId > 0 && tp.ItemId == itemId && tp.PermissionLevel < PermissionLevel.CreatorOnly).ToList();
            
            foreach (TimelineItemPermission permission in groupPermissions)
            {
                bool isMember = await progenyDbContext.UserGroupMembersDb.AnyAsync(ug => ug.UserId == userInfo.UserId && ug.UserGroupId == permission.GroupId);
                if (!isMember || permission.PermissionLevel <= highestPermission) continue;

                highestPermission = permission.PermissionLevel;
                resultPermission = permission;
                if (requiredLevel != null)
                {
                    if (requiredLevel <= highestPermission)
                    {
                        return resultPermission;
                    }
                }
            }

            if (highestPermission == PermissionLevel.View)
            {
                // Check if the user has permission to add for the progeny or family, as it affects the availability of some actions, like adding subtasks for a TodoItem.
                if (progenyId > 0 && await HasProgenyPermission(progenyId, userInfo, PermissionLevel.Add))
                {
                    resultPermission.PermissionLevel = PermissionLevel.Add;
                }

                if (familyId > 0 && await HasFamilyPermission(familyId, userInfo, PermissionLevel.Add))
                {
                    resultPermission.PermissionLevel = PermissionLevel.Add;
                }
            }
            
            return resultPermission;
        }
        
        /// <summary>
        /// Determines whether a user has permission to access an item that is restricted to its creator only.
        /// </summary>
        /// <param name="itemType">The type of the timeline item (e.g., Note, TodoItem, Sleep, etc.).</param>
        /// <param name="itemId">The unique identifier of the item.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <returns></returns>
        private async Task<bool> HasCreatorOnlyPermission(KinaUnaTypes.TimeLineType itemType, int itemId, UserInfo userInfo)
        {
            TimeLineItem item = await progenyDbContext.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(ti => ti.ItemId == itemId.ToString() && ti.ItemType == (int)itemType);
            if (item != null )
            {
                if (item.CreatedBy != userInfo.UserId)
                {
                    return false;
                }
            }
            else
            {
                // Get the item from the specific table.
                switch (itemType)
                {
                    case KinaUnaTypes.TimeLineType.Calendar:
                        CalendarItem calendarItem = await progenyDbContext.CalendarDb.AsNoTracking().SingleOrDefaultAsync(c => c.EventId == itemId);
                        if (calendarItem == null || calendarItem.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.Contact:
                        Contact contact = await progenyDbContext.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == itemId);
                        if (contact == null || contact.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.Friend:
                        Friend friend = await progenyDbContext.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == itemId);
                        if (friend == null || friend.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.KanbanBoard:
                        KanbanBoard kanbanBoard = await progenyDbContext.KanbanBoardsDb.AsNoTracking().SingleOrDefaultAsync(k => k.KanbanBoardId == itemId);
                        if (kanbanBoard == null || kanbanBoard.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.KanbanItem:
                        KanbanItem kanbanItem = await progenyDbContext.KanbanItemsDb.AsNoTracking().SingleOrDefaultAsync(ki => ki.KanbanItemId == itemId);
                        if (kanbanItem == null || kanbanItem.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.Location:
                        Location location = await progenyDbContext.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == itemId);
                        if (location == null || location.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Measurement:
                        Measurement measurement = await progenyDbContext.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == itemId);
                        if (measurement == null || measurement.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Note:
                        Note note = await progenyDbContext.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == itemId);
                        if (note == null || note.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Photo:
                        Picture picture = await mediaDbContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == itemId);
                        if (picture == null || picture.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Sleep:
                        Sleep sleep = await progenyDbContext.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == itemId);
                        if (sleep == null || sleep.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Skill:
                        Skill skill = await progenyDbContext.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == itemId);
                        if (skill == null || skill.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.TodoItem:
                        TodoItem todoItem = await progenyDbContext.TodoItemsDb.AsNoTracking().SingleOrDefaultAsync(ti => ti.TodoItemId == itemId);
                        if (todoItem == null || todoItem.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    
                    case KinaUnaTypes.TimeLineType.Video:
                        Video video = await mediaDbContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == itemId);
                        if (video == null || video.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Vaccination:
                        Vaccination vaccination = await progenyDbContext.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == itemId);
                        if (vaccination == null || vaccination.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Vocabulary:
                        VocabularyItem vocabulary = await progenyDbContext.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(v => v.WordId == itemId);
                        if (vocabulary == null || vocabulary.CreatedBy != userInfo.UserId)
                        {
                            return false;
                        }

                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified user has permission to access a private item of the given type and ID.
        /// </summary>
        /// <remarks>This method verifies whether the specified user has access to a private item by
        /// checking the item's association with the user's progeny. The method supports various item types, such as
        /// calendar events, contacts, photos, and more, as defined in the <see cref="KinaUnaTypes.TimeLineType"/>
        /// enumeration.</remarks>
        /// <param name="itemType">The type of the item to check, represented as a <see cref="KinaUnaTypes.TimeLineType"/> enumeration value.</param>
        /// <param name="itemId">The unique identifier of the item to check.</param>
        /// <param name="userInfo">The user information used to validate permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the user has
        /// permission to access the item; otherwise, <see langword="false"/>.</returns>
        private async Task<bool> HasPrivatePermission(KinaUnaTypes.TimeLineType itemType, int itemId, UserInfo userInfo)
        {
            Progeny progeny = await progenyDbContext.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userInfo.UserId);
            if (progeny == null)
            {
                return false;
            }

            TimeLineItem item = await progenyDbContext.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(ti => ti.ItemId == itemId.ToString() && ti.ItemType == (int)itemType);
            if (item != null)
            {
                if (item.ProgenyId != progeny.Id)
                {
                    return false;
                }
            }
            else
            {
                // Get the item from the specific table.
                switch (itemType)
                {
                    case KinaUnaTypes.TimeLineType.Calendar:
                        CalendarItem calendarItem = await progenyDbContext.CalendarDb.AsNoTracking().SingleOrDefaultAsync(c => c.EventId == itemId);
                        if (calendarItem == null || calendarItem.ProgenyId != progeny.Id)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.Contact:
                        Contact contact = await progenyDbContext.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == itemId);
                        if (contact == null || contact.ProgenyId != progeny.Id)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.Friend:
                        Friend friend = await progenyDbContext.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == itemId);
                        if (friend == null || friend.ProgenyId != progeny.Id)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.KanbanBoard:
                        KanbanBoard kanbanBoard = await progenyDbContext.KanbanBoardsDb.AsNoTracking().SingleOrDefaultAsync(k => k.KanbanBoardId == itemId);
                        if (kanbanBoard == null || kanbanBoard.ProgenyId != progeny.Id)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.KanbanItem:
                        KanbanItem kanbanItem = await progenyDbContext.KanbanItemsDb.AsNoTracking().SingleOrDefaultAsync(ki => ki.KanbanItemId == itemId);
                        kanbanItem.TodoItem = await progenyDbContext.TodoItemsDb.AsNoTracking().SingleOrDefaultAsync(ti => ti.TodoItemId == kanbanItem.TodoItemId);
                        if (kanbanItem.TodoItem?.ProgenyId != progeny.Id)
                        {
                            return false;
                        }
                        break;
                    case KinaUnaTypes.TimeLineType.Location:
                        Location location = await progenyDbContext.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == itemId);
                        if (location == null || location.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Measurement:
                        Measurement measurement = await progenyDbContext.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == itemId);
                        if (measurement == null || measurement.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Note:
                        Note note = await progenyDbContext.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == itemId);
                        if (note == null || note.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Photo:
                        Picture picture = await mediaDbContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == itemId);
                        if (picture == null || picture.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Sleep:
                        Sleep sleep = await progenyDbContext.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == itemId);
                        if (sleep == null || sleep.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Skill:
                        Skill skill = await progenyDbContext.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == itemId);
                        if (skill == null || skill.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.TodoItem:
                        TodoItem todoItem = await progenyDbContext.TodoItemsDb.AsNoTracking().SingleOrDefaultAsync(ti => ti.TodoItemId == itemId);
                        if (todoItem == null || todoItem.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;

                    case KinaUnaTypes.TimeLineType.Video:
                        Video video = await mediaDbContext.VideoDb.AsNoTracking().SingleOrDefaultAsync(v => v.VideoId == itemId);
                        if (video == null || video.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Vaccination:
                        Vaccination vaccination = await progenyDbContext.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == itemId);
                        if (vaccination == null || vaccination.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;
                    case KinaUnaTypes.TimeLineType.Vocabulary:
                        VocabularyItem vocabulary = await progenyDbContext.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(v => v.WordId == itemId);
                        if (vocabulary == null || vocabulary.ProgenyId != progeny.Id)
                        {
                            return false;
                        }

                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Adds permissions to a timeline item for specified users or groups.
        /// </summary>
        /// <remarks>This method processes each permission in the <paramref
        /// name="itemPermissionsDtoList"/> and associates it with the specified timeline item. Permissions can be
        /// granted to individual users or groups based on the provided identifiers in the <see
        /// cref="ItemPermissionDto"/> objects. If a permission references a non-existent progeny or family permission,
        /// it will be skipped.</remarks>
        /// <param name="itemType">The type of the timeline item to which permissions will be added.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <param name="progenyId">The unique identifier of the progeny associated with the timeline item.</param>
        /// <param name="familyId">The unique identifier of the family associated with the timeline item.</param>
        /// <param name="itemPermissionsDtoList">A list of <see cref="ItemPermissionDto"/> objects representing the permissions to be added.</param>
        /// <param name="currentUserInfo">The information about the current user performing the operation.</param>
        /// <returns></returns>
        public async Task AddItemPermissions(KinaUnaTypes.TimeLineType itemType, int itemId, int progenyId, int familyId,
            List<ItemPermissionDto> itemPermissionsDtoList, UserInfo currentUserInfo)
        {
            if(itemPermissionsDtoList == null || itemPermissionsDtoList.Count == 0)
            {
                // No permissions to add.
                return;
            }

            foreach (ItemPermissionDto permissionDto in itemPermissionsDtoList)
            {
                string userId = string.Empty;
                string email = string.Empty;
                int groupId = 0;
                if (permissionDto.ProgenyPermissionId > 0)
                {
                    ProgenyPermission progenyPermission = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking().SingleOrDefaultAsync(pp => pp.ProgenyPermissionId == permissionDto.ProgenyPermissionId);
                    if (progenyPermission == null)
                    {
                        continue;
                    }
                    groupId = progenyPermission.GroupId;
                }
                if (permissionDto.FamilyPermissionId > 0)
                {
                    FamilyPermission familyPermission = await progenyDbContext.FamilyPermissionsDb.AsNoTracking().SingleOrDefaultAsync(fp => fp.FamilyPermissionId == permissionDto.FamilyPermissionId);
                    if (familyPermission == null)
                    {
                        continue;
                    }
                    groupId = familyPermission.GroupId;
                }
                TimelineItemPermission timelineItemPermission = new()
                {
                    ItemId = itemId,
                    TimelineType = itemType,
                    ProgenyId = progenyId,
                    FamilyId = familyId,
                    UserId = userId,
                    Email = email,
                    GroupId = groupId,
                    PermissionLevel = permissionDto.PermissionLevel,
                    InheritPermissions = permissionDto.InheritPermissions
                };
                _ = await GrantItemPermission(timelineItemPermission, currentUserInfo);
            }
        }

        /// <summary>
        /// Copies the specified permissions to a timeline item for a given progeny and family.
        /// </summary>
        /// <remarks>If the permissions list is null or empty, no permissions are copied and the method
        /// returns immediately.</remarks>
        /// <param name="itemType">The type of timeline item to which permissions will be applied.</param>
        /// <param name="itemId">The unique identifier of the timeline item to receive the permissions.</param>
        /// <param name="progenyId">The identifier of the progeny associated with the timeline item.</param>
        /// <param name="familyId">The identifier of the family associated with the timeline item.</param>
        /// <param name="itemPermissionsList">A list of permissions to copy to the specified timeline item. The list must not be null or empty.</param>
        /// <param name="currentUserInfo">Information about the user performing the permission copy operation.</param>
        /// <returns>A task that represents the asynchronous copy operation. The task completes when all permissions have been
        /// processed.</returns>
        public async Task CopyItemPermissions(KinaUnaTypes.TimeLineType itemType, int itemId, int progenyId, int familyId,
            List<TimelineItemPermission> itemPermissionsList, UserInfo currentUserInfo)
        {
            if (itemPermissionsList == null || itemPermissionsList.Count == 0)
            {
                return;
            }

            foreach (TimelineItemPermission permission in itemPermissionsList)
            {
                TimelineItemPermission newPermission = new()
                {
                    ItemId = itemId,
                    TimelineType = itemType,
                    ProgenyId = progenyId,
                    FamilyId = familyId,
                    UserId = permission.UserId,
                    Email = permission.Email,
                    GroupId = permission.GroupId,
                    PermissionLevel = permission.PermissionLevel,
                    InheritPermissions = permission.InheritPermissions
                };
                _ = await GrantItemPermission(newPermission, currentUserInfo);
            }
        }

        /// <summary>
        /// Updates the permissions for a specific timeline item based on the provided parameters.
        /// </summary>
        /// <remarks>This method updates the permissions for a timeline item based on the provided list of
        /// desired permissions. It ensures that certain permission levels, such as <see
        /// cref="PermissionLevel.CreatorOnly"/> and <see cref="PermissionLevel.Private"/>, cannot be directly modified.
        /// If the item is set to inherit permissions, the method will handle the inheritance logic accordingly.  The
        /// method also validates the current user's permissions and updates them as necessary, taking into account any
        /// family or progeny-level permissions that may apply. If the permissions are changed from inheritance or
        /// restricted levels to custom permissions, the method ensures that all necessary permissions are updated or
        /// added.  This method is asynchronous and performs database operations to retrieve and update
        /// permissions.</remarks>
        /// <param name="itemType">The type of the timeline item to update.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <param name="progenyId">The unique identifier of the progeny associated with the timeline item.</param>
        /// <param name="familyId">The unique identifier of the family associated with the timeline item.</param>
        /// <param name="itemPermissionsDtoList">A list of <see cref="ItemPermissionDto"/> objects representing the desired permissions for the timeline
        /// item.</param>
        /// <param name="currentUserInfo">The user information of the current user making the update request.</param>
        /// <returns>A list of <see cref="TimelineItemPermission"/> objects representing the permissions that were changed as a
        /// result of the update. If no changes were made, the list will be empty.</returns>
        public async Task<List<TimelineItemPermission>> UpdateItemPermissions(KinaUnaTypes.TimeLineType itemType, int itemId, int progenyId, int familyId,
            List<ItemPermissionDto> itemPermissionsDtoList, UserInfo currentUserInfo)
        {
            if (itemPermissionsDtoList == null || itemPermissionsDtoList.Count == 0 || itemId == 0)
            {
                // No permissions to update.
                return [];
            }
            List<TimelineItemPermission> changedItemPermissions = [];
            bool wasPreviouslyCreatorOnly = false;
            bool wasPreviouslyPrivate = false;
            bool wasPreviouslyInheriting = false;
            bool isNowInheriting = false;
            TimelineItemPermission inheritedPermission = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking().SingleOrDefaultAsync(tp => tp.InheritPermissions && tp.TimelineType == itemType && tp.ItemId == itemId);
            TimelineItemPermission usersPermission = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking()
                .SingleOrDefaultAsync(tp => tp.UserId == currentUserInfo.UserId && tp.TimelineType == itemType && tp.ItemId == itemId);
            List<TimelineItemPermission> existingItemPermissions = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking().Where(tp => tp.TimelineType == itemType && tp.ItemId == itemId).ToListAsync();
            if (itemPermissionsDtoList.Count == 1)
            {
                ItemPermissionDto itemPermissionDto = itemPermissionsDtoList.First();
                if (itemPermissionDto.InheritPermissions)
                {
                    if (progenyId > 0)
                    {
                        if (!await IsUserAccessManager(currentUserInfo, PermissionType.Progeny, progenyId))
                        {
                            return changedItemPermissions;
                        }
                    }

                    if (familyId > 0)
                    {
                        if (!await IsUserAccessManager(currentUserInfo, PermissionType.Family, familyId))
                        {
                            return changedItemPermissions;
                        }
                    }

                    isNowInheriting = true;
                    if (inheritedPermission != null)
                    {
                        // Nothing has changed, return.

                        // Remove all other permissions.
                        foreach (TimelineItemPermission existingPermission in existingItemPermissions)
                        {
                            if (!existingPermission.InheritPermissions)
                            {
                                await RevokeItemPermission(existingPermission, currentUserInfo);
                            }
                        }

                        return changedItemPermissions;
                    }
                }

                if (itemPermissionDto.PermissionLevel == PermissionLevel.CreatorOnly)
                {
                    // Not allowed to change this: If a user wants to change an existing item to Only Me they will have to copy it, set the copy to Only Me and delete the original.
                    return changedItemPermissions;
                }

                if (itemPermissionDto.PermissionLevel == PermissionLevel.Private)
                {
                    // Not allowed to change this: If a user wants to change an existing item to private they will have to copy it, set the copy to private and delete the original.
                    return changedItemPermissions;
                }
            }
            
            // Check if the Timeline item's existing permission is Private or CreatorOnly.
            if (usersPermission != null)
            {
                if (usersPermission.PermissionLevel == PermissionLevel.CreatorOnly)
                {
                    wasPreviouslyCreatorOnly = true;
                    ItemPermissionDto usersItemPermissionDto = itemPermissionsDtoList.SingleOrDefault(i => i.ItemPermissionId == usersPermission.TimelineItemPermissionId);
                    if (usersItemPermissionDto != null)
                    {
                        if (usersItemPermissionDto.PermissionLevel == usersPermission.PermissionLevel)
                        {
                            // Nothing changed.
                            return changedItemPermissions;
                        }
                    }
                }

                if (usersPermission.PermissionLevel == PermissionLevel.Private)
                {
                    wasPreviouslyPrivate = true;
                    ItemPermissionDto usersItemPermissionDto = itemPermissionsDtoList.SingleOrDefault(i => i.ItemPermissionId == usersPermission.TimelineItemPermissionId);
                    if (usersItemPermissionDto != null)
                    {
                        if (usersItemPermissionDto.PermissionLevel == usersPermission.PermissionLevel)
                        {
                            // Nothing changed.
                            return changedItemPermissions;
                        }
                    }
                }
            }

            if (!wasPreviouslyCreatorOnly && !wasPreviouslyPrivate)
            {
                if (inheritedPermission != null)
                {
                    wasPreviouslyInheriting = true;
                }
            }
            
            if (!wasPreviouslyInheriting && !wasPreviouslyCreatorOnly && !wasPreviouslyPrivate && !isNowInheriting)
            {
                // Check if user is allow to make the changes.
                if (progenyId > 0)
                {
                    if (!await IsUserAccessManager(currentUserInfo, PermissionType.Progeny, progenyId))
                    {
                        // Not an access manager for the progeny, cannot make changes.
                        return changedItemPermissions;
                    }
                }
                if (familyId > 0)
                {
                    if (!await IsUserAccessManager(currentUserInfo, PermissionType.Family, familyId))
                    {
                        // Not an access manager for the family, cannot make changes.
                        return changedItemPermissions;
                    }
                }

                // Simplest case, update the permission levels for existing items if needed.
                foreach (TimelineItemPermission existingPermission in existingItemPermissions)
                {
                    ItemPermissionDto itemPermissionDto = itemPermissionsDtoList.SingleOrDefault(i => i.ItemPermissionId == existingPermission.TimelineItemPermissionId);
                    if (itemPermissionDto != null && itemPermissionDto.PermissionLevel != existingPermission.PermissionLevel)
                    {
                        existingPermission.PermissionLevel = itemPermissionDto.PermissionLevel;
                        await UpdateItemPermission(existingPermission, currentUserInfo);
                        changedItemPermissions.Add(existingPermission);
                    }
                }

                return changedItemPermissions;
            }

            if (usersPermission != null)
            {
                // We have the current users permission, update that one first.
                usersPermission.PermissionLevel = PermissionLevel.View;
                if (progenyId > 0)
                {
                    ProgenyPermission progenyPermission = await GetProgenyPermissionForUser(progenyId, currentUserInfo);
                    if (progenyPermission != null)
                    {
                        if (progenyPermission.PermissionLevel > PermissionLevel.Add)
                        {
                            usersPermission.PermissionLevel = progenyPermission.PermissionLevel;
                        }
                    }
                }

                if (familyId > 0)
                {
                    FamilyPermission familyPermission = await GetFamilyPermissionForUser(familyId, currentUserInfo);
                    if (familyPermission != null)
                    {
                        if (familyPermission.PermissionLevel > PermissionLevel.Add)
                        {
                            usersPermission.PermissionLevel = familyPermission.PermissionLevel;
                        }
                    }
                }

                usersPermission = await UpdateItemPermission(usersPermission, currentUserInfo);
                changedItemPermissions.Add(usersPermission);
            }
            
            if (isNowInheriting)
            {
                // Add new inherit permission.
                TimelineItemPermission inheritPermission = new TimelineItemPermission
                {
                    InheritPermissions = true,
                    FamilyId = familyId,
                    ProgenyId = progenyId,
                    ItemId = itemId,
                    TimelineType = itemType
                };

                inheritPermission = await GrantItemPermission(inheritPermission, currentUserInfo);
                changedItemPermissions.Add(inheritPermission);
                // Remove all other permissions.
                foreach (TimelineItemPermission existingPermission in existingItemPermissions)
                {
                    if (!existingPermission.InheritPermissions)
                    {
                        await RevokeItemPermission(existingPermission, currentUserInfo);
                    }
                }
            }
            else
            {
                if (wasPreviouslyInheriting)
                {
                    // Remove the inherit permission.
                    await RevokeItemPermission(inheritedPermission, currentUserInfo);
                }

                // Add new permission for all permissions. If we are here, the user has changed from CreatorOnly or Private or Inherit to Custom.
                // The current users permission will fail in the GrantPermission method, but we already set that one.
                await AddItemPermissions(itemType, itemId, progenyId, familyId, itemPermissionsDtoList, currentUserInfo);
            }

            return changedItemPermissions;
        }

        /// <summary>
        /// Grants a specified permission to a user or group for a timeline item, if the current user has the necessary
        /// access rights.
        /// </summary>
        /// <remarks>This method ensures that the current user has the necessary access rights to grant
        /// the specified permission. If the permission already exists,  or if the current user does not have sufficient
        /// privileges, the method returns <see langword="null"/>. The method also sets the creation and modification
        /// timestamps  for the new permission before saving it to the database.</remarks>
        /// <param name="timelineItemPermission">The permission to be granted, including details such as the user or group, familyId, and permission type.</param>
        /// <param name="currentUserInfo">Information about the current user attempting to grant the permission.</param>
        /// <returns>The granted <see cref="ProgenyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the current user lacks the required access rights  or if the specified permission
        /// already exists.</returns>
        public async Task<TimelineItemPermission> GrantItemPermission(TimelineItemPermission timelineItemPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            bool canGrantAccess = false;
            if (timelineItemPermission.ProgenyId > 0)
            {
                if (!await IsUserAccessManager(currentUserInfo, PermissionType.Progeny, timelineItemPermission.ProgenyId))
                {
                    // Current user is not admin for the progeny, check further.
                    if (timelineItemPermission.PermissionLevel == PermissionLevel.CreatorOnly){
                        canGrantAccess = true;
                    }
                    else if(timelineItemPermission.InheritPermissions)
                    {
                        ProgenyPermission progenyPermission = await GetProgenyPermissionForUser(timelineItemPermission.ProgenyId, currentUserInfo);
                        if (progenyPermission.PermissionLevel >= PermissionLevel.Add)
                        {
                            canGrantAccess = true;
                        }
                    }
                    else if (!string.IsNullOrEmpty(timelineItemPermission.UserId) && timelineItemPermission.UserId == currentUserInfo.UserId)
                    {
                        // If the user is not an access manager, they can only grant access for admins and view access for themselves.
                        canGrantAccess = true;
                        timelineItemPermission.PermissionLevel = PermissionLevel.View;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(timelineItemPermission.UserId))
                        {
                            // If the user being assigned to is admin, add the permission.
                            UserInfo assigneeUserInfo = await progenyDbContext.UserInfoDb.SingleOrDefaultAsync(ui => ui.UserId == timelineItemPermission.UserId);
                            if (assigneeUserInfo != null && await IsUserAccessManager(assigneeUserInfo, PermissionType.Progeny, timelineItemPermission.ProgenyId))
                            {
                                canGrantAccess = true;
                                timelineItemPermission.PermissionLevel = PermissionLevel.Admin;
                            }
                        }

                        if (timelineItemPermission.GroupId > 0)
                        {
                            // If the group being assigned to is admin, add the permission.
                            UserGroup assigneeGroup = await progenyDbContext.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.UserGroupId == timelineItemPermission.GroupId);
                            if (assigneeGroup != null)
                            {
                                ProgenyPermission groupProgenyPermission = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking().SingleOrDefaultAsync(pp => pp.GroupId == assigneeGroup.UserGroupId && pp.ProgenyId == timelineItemPermission.ProgenyId);
                                if (groupProgenyPermission != null && groupProgenyPermission.PermissionLevel == PermissionLevel.Admin)
                                {
                                    timelineItemPermission.PermissionLevel = PermissionLevel.Admin;
                                    canGrantAccess = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    canGrantAccess = true;
                }
            }
            else
            {
                if (!await IsUserAccessManager(currentUserInfo, PermissionType.Family, timelineItemPermission.FamilyId))
                {
                    if (timelineItemPermission.PermissionLevel == PermissionLevel.CreatorOnly)
                    {
                        canGrantAccess = true;
                    }
                    else if (timelineItemPermission.InheritPermissions)
                    {
                        FamilyPermission familyPermission = await GetFamilyPermissionForUser(timelineItemPermission.FamilyId, currentUserInfo);
                        if (familyPermission.PermissionLevel >= PermissionLevel.Add)
                        {
                            canGrantAccess = true;
                        }
                    }
                    else if (!string.IsNullOrEmpty(timelineItemPermission.UserId) &&timelineItemPermission.UserId == currentUserInfo.UserId)
                    {
                        // If the user is not an access manager, they can only grant access for admins and view access for themselves.
                        canGrantAccess = true;
                        timelineItemPermission.PermissionLevel = PermissionLevel.View;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(timelineItemPermission.UserId))
                        {
                            // If the user being assigned to is admin, add the permission.
                            UserInfo assigneeUserInfo = await progenyDbContext.UserInfoDb.SingleOrDefaultAsync(ui => ui.UserId == timelineItemPermission.UserId);
                            if (assigneeUserInfo != null && await IsUserAccessManager(assigneeUserInfo, PermissionType.Family, timelineItemPermission.FamilyId))
                            {
                                canGrantAccess = true;
                                timelineItemPermission.PermissionLevel = PermissionLevel.Admin;
                            }
                        }

                        if (timelineItemPermission.GroupId > 0)
                        {
                            // If the group being assigned to is admin, add the permission.
                            UserGroup assigneeGroup = await progenyDbContext.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.UserGroupId == timelineItemPermission.GroupId);
                            if (assigneeGroup != null)
                            {
                                FamilyPermission groupFamilyPermission = await progenyDbContext.FamilyPermissionsDb.AsNoTracking().SingleOrDefaultAsync(fp => fp.GroupId == assigneeGroup.UserGroupId && fp.FamilyId == timelineItemPermission.FamilyId);
                                if (groupFamilyPermission != null && groupFamilyPermission.PermissionLevel == PermissionLevel.Admin)
                                {
                                    canGrantAccess = true;
                                    timelineItemPermission.PermissionLevel = PermissionLevel.Admin;
                                }
                            }
                        }
                    }
                }
                else
                {
                    canGrantAccess = true;
                }
            }

            if (!canGrantAccess)
            {
                return null; // Todo: Use result object instead.
            }

            // Check if the permission exists.
            TimelineItemPermission existingPermission = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking().SingleOrDefaultAsync(tp =>
                tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.Email.Length > 4 && tp.Email == timelineItemPermission.Email);

            // If not found by email, try userId and groupId.
            if (existingPermission == null && !string.IsNullOrWhiteSpace(timelineItemPermission.UserId))
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(tp =>
                        tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.UserId == timelineItemPermission.UserId);
            }

            if (existingPermission == null && timelineItemPermission.GroupId > 0)
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking().SingleOrDefaultAsync(tp =>
                    tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.GroupId == timelineItemPermission.GroupId);
            }

            if (existingPermission == null && timelineItemPermission.InheritPermissions)
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking().SingleOrDefaultAsync(tp =>
                    tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.InheritPermissions == timelineItemPermission.InheritPermissions);
            }

            if (existingPermission != null)
            {
                // Permission for user or group already exists, returning here ensures that CreatorOnly and Private permissions cannot be granted to items that have other permissions.
                // Use UpdateItemPermission to change existing permissions.
                return null; // Todo: Use result object instead.
            }


            timelineItemPermission.CreatedBy = currentUserInfo.UserId;
            timelineItemPermission.CreatedTime = DateTime.UtcNow;
            timelineItemPermission.ModifiedBy = currentUserInfo.UserId;
            timelineItemPermission.ModifiedTime = DateTime.UtcNow;

            progenyDbContext.TimelineItemPermissionsDb.Add(timelineItemPermission);
            await progenyDbContext.SaveChangesAsync();
            
            await permissionAuditLogService.AddTimelineItemPermissionAuditLogEntry(PermissionAction.Add, timelineItemPermission, currentUserInfo);

            // Invalidate caches
            await RemoveCachedAllUserPermissions(timelineItemPermission);

            // If inherited, remove all other permissions for this item?

            return timelineItemPermission; // Todo: Use result object instead.
        }

        private async Task RemoveCachedHasAccesses(TimelineItemPermission timelineItemPermission)
        {
            if (!string.IsNullOrEmpty(timelineItemPermission.UserId))
            {
                foreach (PermissionLevel level in Enum.GetValues(typeof(PermissionLevel)))
                {
                    await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                            "hasItemPermissionPermission" + (int)timelineItemPermission.TimelineType + "_itemId_" + timelineItemPermission.ItemId + "_userId_" + timelineItemPermission.UserId + "_level_" +
                                            (int)level);
                }
            }

            if (timelineItemPermission.GroupId > 0)
            {
                List<UserGroupMember> groupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking()
                    .Where(ugm => ugm.UserGroupId == timelineItemPermission.GroupId).ToListAsync();
                foreach (UserGroupMember member in groupMembers)
                {
                    foreach (PermissionLevel level in Enum.GetValues(typeof(PermissionLevel)))
                    {
                        await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                                "hasItemPermissionPermission" + (int)timelineItemPermission.TimelineType + "_itemId_" + timelineItemPermission.ItemId + "_userId_" + member.UserId +
                                                "_level_" +
                                                (int)level);
                    }
                }
            }

            if (timelineItemPermission.InheritPermissions)
            {
                if (timelineItemPermission.ProgenyId > 0)
                {
                    List<ProgenyPermission> progenyPermissions = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                        .Where(pp => pp.ProgenyId == timelineItemPermission.ProgenyId).ToListAsync();
                    foreach (ProgenyPermission progenyPermission in progenyPermissions)
                    {
                        List<UserGroupMember> groupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking()
                            .Where(ugm => ugm.UserGroupId == progenyPermission.GroupId).ToListAsync();
                        foreach (UserGroupMember groupMember in groupMembers)
                        {
                            if (!string.IsNullOrEmpty(groupMember.UserId))
                            {
                                foreach (PermissionLevel level in Enum.GetValues(typeof(PermissionLevel)))
                                {
                                    await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                                            "hasItemPermissionPermission" + (int)timelineItemPermission.TimelineType + "_itemId_" + timelineItemPermission.ItemId + "_userId_" + groupMember.UserId +
                                                            "_level_" +
                                                            (int)level);
                                }
                            }
                        }
                    }
                }

                if (timelineItemPermission.FamilyId > 0)
                {
                    List<FamilyPermission> familyPermissions = await progenyDbContext.FamilyPermissionsDb.AsNoTracking()
                        .Where(pp => pp.FamilyId == timelineItemPermission.FamilyId).ToListAsync();
                    foreach (FamilyPermission familyPermission in familyPermissions)
                    {
                        List<UserGroupMember> groupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking()
                            .Where(ugm => ugm.UserGroupId == familyPermission.GroupId).ToListAsync();
                        foreach (UserGroupMember groupMember in groupMembers)
                        {
                            foreach (PermissionLevel level in Enum.GetValues(typeof(PermissionLevel)))
                            {
                                if (!string.IsNullOrEmpty(groupMember.UserId))
                                {
                                    await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                                            "hasItemPermissionPermission" + (int)timelineItemPermission.TimelineType + "_itemId_" + timelineItemPermission.ItemId + "_userId_" + groupMember.UserId +
                                                            "_level_" +
                                                            (int)level);
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task RemoveCachedAllUserPermissions(TimelineItemPermission timelineItemPermission)
        {
            if (!string.IsNullOrEmpty(timelineItemPermission.UserId))
            {
                await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                        "allUsersTimelineItemPermissions" + "_userId_" + timelineItemPermission.UserId + "_type_" + (int)timelineItemPermission.TimelineType);
                await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                           "allUsersItemPermissions" + "_userId_" + timelineItemPermission.UserId + "_type_" + (int)timelineItemPermission.TimelineType);
            }

            if (timelineItemPermission.GroupId > 0)
            {
                List<UserGroupMember> groupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking()
                    .Where(ugm => ugm.UserGroupId == timelineItemPermission.GroupId).ToListAsync();
                foreach (UserGroupMember member in groupMembers)
                {
                    if (!string.IsNullOrEmpty(member.UserId))
                    {
                        await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                                "allUsersTimelineItemPermissions" + "_userId_" + member.UserId + "_type_" + (int)timelineItemPermission.TimelineType);
                        await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                                "allUsersItemPermissions" + "_userId_" + member.UserId + "_type_" + (int)timelineItemPermission.TimelineType);
                    }
                }
            }

            if (timelineItemPermission.InheritPermissions)
            {
                if (timelineItemPermission.ProgenyId > 0)
                {
                    List<ProgenyPermission> progenyPermissions = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                        .Where(pp => pp.ProgenyId == timelineItemPermission.ProgenyId).ToListAsync();
                    foreach (ProgenyPermission progenyPermission in progenyPermissions)
                    {
                        List<UserGroupMember> groupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking()
                            .Where(ugm => ugm.UserGroupId == progenyPermission.GroupId).ToListAsync();
                        foreach (UserGroupMember groupMember in groupMembers)
                        {
                            if (!string.IsNullOrEmpty(groupMember.UserId))
                            {
                                await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                                        "allUsersTimelineItemPermissions" + "_userId_" + groupMember.UserId + "_type_" + (int)timelineItemPermission.TimelineType);
                                await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                                        "allUsersItemPermissions" + "_userId_" + groupMember.UserId + "_type_" + (int)timelineItemPermission.TimelineType);
                            }
                        }
                    }
                }

                if (timelineItemPermission.FamilyId > 0)
                {
                    List<FamilyPermission> familyPermissions = await progenyDbContext.FamilyPermissionsDb.AsNoTracking()
                        .Where(pp => pp.FamilyId == timelineItemPermission.FamilyId).ToListAsync();
                    foreach (FamilyPermission familyPermission in familyPermissions)
                    {
                        List<UserGroupMember> groupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking()
                            .Where(ugm => ugm.UserGroupId == familyPermission.GroupId).ToListAsync();
                        foreach (UserGroupMember groupMember in groupMembers)
                        {
                            if (!string.IsNullOrEmpty(groupMember.UserId))
                            {
                                await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                                        "allUsersTimelineItemPermissions" + "_userId_" + groupMember.UserId + "_type_" + (int)timelineItemPermission.TimelineType);
                                await cache.RemoveAsync(Constants.AppName + Constants.ApiVersion +
                                                        "allUsersItemPermissions" + "_userId_" + groupMember.UserId + "_type_" + (int)timelineItemPermission.TimelineType);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Revokes a specific permission for a timeline item from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="timelineItemPermission">The permission to be revoked, including details such as the user or group, permission type, and progenyId.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> RevokeItemPermission(TimelineItemPermission timelineItemPermission, UserInfo currentUserInfo)
        {
            // Check if the permission exists.
            TimelineItemPermission existingPermission = await progenyDbContext.TimelineItemPermissionsDb
                .SingleOrDefaultAsync(tp => tp.TimelineItemPermissionId == timelineItemPermission.TimelineItemPermissionId);

            // If not found by email, try userId and groupId.
            if (existingPermission == null && !string.IsNullOrWhiteSpace(timelineItemPermission.UserId))
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp =>
                        tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.UserId == timelineItemPermission.UserId);

            }

            if (existingPermission == null && timelineItemPermission.GroupId > 0)
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp =>
                    tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.GroupId == timelineItemPermission.GroupId);
            }

            if (existingPermission == null && timelineItemPermission.InheritPermissions)
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp =>
                    tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.InheritPermissions == timelineItemPermission.InheritPermissions);
            }

            if (existingPermission == null)
            {
                return false; // Todo: Use result object instead.
            }

            if (existingPermission.PermissionLevel == PermissionLevel.CreatorOnly)
            {
                // CreatorOnly permissions can be revoked by the creator of the item.
                if (!await HasCreatorOnlyPermission(timelineItemPermission.TimelineType, timelineItemPermission.ItemId, currentUserInfo))
                {
                    return false; // Todo: Use result object instead.
                }
            }

            if (existingPermission.PermissionLevel == PermissionLevel.Private)
            {
                // Private permissions can be revoked by the owner of the progeny.
                if (!await HasPrivatePermission(timelineItemPermission.TimelineType, timelineItemPermission.ItemId, currentUserInfo))
                {
                    return false; // Todo: Use result object instead.
                }
            }
            
            // Check if the current user can grant the specified permission level.
            if (existingPermission.ProgenyId > 0)
            {
                if (existingPermission.PermissionLevel < PermissionLevel.CreatorOnly && !await IsUserAccessManager(currentUserInfo, PermissionType.TimelineItem, timelineItemPermission.ProgenyId))
                {
                    return false;
                }
            }
            else
            {
                if (existingPermission.PermissionLevel < PermissionLevel.CreatorOnly && !await IsUserAccessManager(currentUserInfo, PermissionType.Family, timelineItemPermission.FamilyId))
                {
                    return false; // Todo: Use result object instead.
                }
            }
            
            PermissionAuditLog logEntry = await permissionAuditLogService.AddTimelineItemPermissionAuditLogEntry(PermissionAction.Delete, existingPermission, currentUserInfo);

            progenyDbContext.TimelineItemPermissionsDb.Remove(existingPermission);
            await progenyDbContext.SaveChangesAsync();

            logEntry.ItemAfter = JsonSerializer.Serialize(existingPermission);
            await permissionAuditLogService.UpdatePermissionAuditLogEntry(logEntry);

            await RemoveCachedAllUserPermissions(existingPermission);
            await RemoveCachedHasAccesses(timelineItemPermission);

            return true; // Todo: Use result object instead.
        }

        /// <summary>
        /// Updates the permission level of an existing timelineItem permission.
        /// </summary>
        /// <remarks>This method validates whether the current user has the necessary access rights to
        /// update the specified permission level. If the user does not have sufficient rights or the specified
        /// permission does not exist, the method returns <see langword="null"/>.</remarks>
        /// <param name="timelineItemPermission">The updated timelineItem permission details, including the new permission level.</param>
        /// <param name="currentUserInfo">Information about the current user performing the update, used to validate access rights.</param>
        /// <returns>The updated <see cref="TimelineItemPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the user lacks sufficient access rights or the specified permission does not exist.</returns>
        public async Task<TimelineItemPermission> UpdateItemPermission(TimelineItemPermission timelineItemPermission, UserInfo currentUserInfo)
        {
            // Check if the permission exists.
            TimelineItemPermission existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp => tp.TimelineItemPermissionId == timelineItemPermission.TimelineItemPermissionId);

            if (existingPermission == null && !string.IsNullOrWhiteSpace(timelineItemPermission.Email))
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp =>
                    tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.Email.Length > 4 && tp.Email == timelineItemPermission.Email);
                
            }

            if (existingPermission == null && !string.IsNullOrWhiteSpace(timelineItemPermission.UserId))
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb
                    .SingleOrDefaultAsync(tp =>
                        tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.UserId == timelineItemPermission.UserId);

            }
            if (existingPermission == null && timelineItemPermission.GroupId > 0)
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp =>
                    tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.GroupId == timelineItemPermission.GroupId);
            }
            if (existingPermission == null && timelineItemPermission.InheritPermissions)
            {
                existingPermission = await progenyDbContext.TimelineItemPermissionsDb.SingleOrDefaultAsync(tp =>
                    tp.TimelineType == timelineItemPermission.TimelineType && tp.ItemId == timelineItemPermission.ItemId && tp.InheritPermissions == timelineItemPermission.InheritPermissions);
            }

            if (existingPermission == null)
            {
                return null; // Todo: Use result object instead.
            }

            if (existingPermission.PermissionLevel == PermissionLevel.CreatorOnly)
            {
                // CreatorOnly permissions can be updated by the creator of the item.
                if (!await HasCreatorOnlyPermission(timelineItemPermission.TimelineType, timelineItemPermission.ItemId, currentUserInfo))
                {
                    return null; // Todo: Use result object instead.
                }
            }

            if (existingPermission.PermissionLevel == PermissionLevel.Private)
            {
                // Private permissions can be updated by the owner of the progeny.
                if (!await HasPrivatePermission(timelineItemPermission.TimelineType, timelineItemPermission.ItemId, currentUserInfo))
                {
                    return null; // Todo: Use result object instead.
                }
            }
            // Check if the current user can grant the specified permission level.
            if (timelineItemPermission.ProgenyId > 0)
            {
                if (existingPermission.PermissionLevel < PermissionLevel.CreatorOnly && !await IsUserAccessManager(currentUserInfo, PermissionType.TimelineItem, timelineItemPermission.ProgenyId))
                {
                    return null; // Todo: Use result object instead.
                }
            }
            else
            {
                if (existingPermission.PermissionLevel < PermissionLevel.CreatorOnly && !await IsUserAccessManager(currentUserInfo, PermissionType.Family, timelineItemPermission.FamilyId))
                {
                    return null; // Todo: Use result object instead.
                }
            }
            
            PermissionAuditLog logEntry = await permissionAuditLogService.AddTimelineItemPermissionAuditLogEntry(PermissionAction.Update, existingPermission, currentUserInfo);

            existingPermission.PermissionLevel = timelineItemPermission.PermissionLevel;
            existingPermission.ModifiedTime = DateTime.UtcNow;
            existingPermission.ModifiedBy = currentUserInfo.UserId;

            progenyDbContext.TimelineItemPermissionsDb.Update(existingPermission);
            await progenyDbContext.SaveChangesAsync();

            logEntry.ItemAfter = JsonSerializer.Serialize(existingPermission);
            await permissionAuditLogService.UpdatePermissionAuditLogEntry(logEntry);

            await RemoveCachedAllUserPermissions(existingPermission);
            await RemoveCachedAllUserPermissions(timelineItemPermission);
            await RemoveCachedHasAccesses(existingPermission);
            await RemoveCachedHasAccesses(timelineItemPermission);

            return existingPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Retrieves a list of permissions for a specific timeline item that the current user has access to view.
        /// </summary>
        /// <param name="itemType">The type of the timeline item, represented as a <see cref="KinaUnaTypes.TimeLineType"/> enumeration.</param>
        /// <param name="itemId">The unique identifier of the timeline item.</param>
        /// <param name="currentUserInfo">The information of the current user requesting the permissions.</param>
        /// <returns>List of <see cref="TimelineItemPermission"/> objects representing the permissions for the specified timeline item that the current user has access to view.</returns>
        public async Task<List<TimelineItemPermission>> GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType itemType, int itemId, UserInfo currentUserInfo)
        {
            List<TimelineItemPermission> allPermissionsForItem = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking()
                .Where(tp => tp.TimelineType == itemType && tp.ItemId == itemId).ToListAsync();

            List<TimelineItemPermission> accessibleItemPermissions = [];

            foreach (TimelineItemPermission permission in allPermissionsForItem)
            {
                if (permission.PermissionLevel == PermissionLevel.CreatorOnly)
                {
                    // CreatorOnly permissions can be viewed by the creator of the item.
                    if (await HasCreatorOnlyPermission(itemType, itemId, currentUserInfo))
                    {
                        accessibleItemPermissions.Add(permission);
                    }
                }
                if (permission.PermissionLevel == PermissionLevel.Private)
                {
                    // Private permissions can be viewed by the owner of the progeny.
                    if (await HasPrivatePermission(itemType, itemId, currentUserInfo))
                    {
                        accessibleItemPermissions.Add(permission);
                    }
                }
                
                if (permission.PermissionLevel < PermissionLevel.CreatorOnly)
                {
                    // Check if the current user can view the specified permission level.
                    if (permission.ProgenyId > 0)
                    {
                        if (await IsUserAccessManager(currentUserInfo, PermissionType.Progeny, permission.ProgenyId))
                        {
                            accessibleItemPermissions.Add(permission);
                        }
                        else
                        {
                            // Special case for TodoItems, when adding a subtask users are allowed to copy the original's permissions.
                            if (itemType == KinaUnaTypes.TimeLineType.TodoItem)
                            {
                                TodoItem todoItem = await progenyDbContext.TodoItemsDb.AsNoTracking().SingleOrDefaultAsync(t => t.TodoItemId == itemId);
                                if (todoItem != null)
                                {
                                    TimelineItemPermission timelineItemPermission = await GetItemPermissionForUser(itemType, itemId, permission.ProgenyId, 0, currentUserInfo);
                                    if (timelineItemPermission != null && timelineItemPermission.PermissionLevel > PermissionLevel.View)
                                    {
                                        accessibleItemPermissions.Add(permission);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (await IsUserAccessManager(currentUserInfo, PermissionType.Family, permission.FamilyId))
                        {
                            accessibleItemPermissions.Add(permission);
                        }
                        else
                        {
                            // Special case for TodoItems, when adding a subtask users are allowed to copy the original's permissions.
                            if (itemType == KinaUnaTypes.TimeLineType.TodoItem)
                            {
                                TodoItem todoItem = await progenyDbContext.TodoItemsDb.AsNoTracking().SingleOrDefaultAsync(t => t.TodoItemId == itemId);
                                if (todoItem != null)
                                {
                                    TimelineItemPermission timelineItemPermission = await GetItemPermissionForUser(itemType, itemId, 0, permission.FamilyId, currentUserInfo);
                                    if (timelineItemPermission != null && timelineItemPermission.PermissionLevel > PermissionLevel.View)
                                    {
                                        accessibleItemPermissions.Add(permission);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return accessibleItemPermissions;
        }

        /// <summary>
        /// Determines whether a user has the specified access level for a given progeny.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny whose access to is being checked.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <param name="requiredLevel">The minimum permission level required for access.</param>
        /// <returns>Boolean value indicating whether the user has the required access level for the progeny.</returns>
        public async Task<bool> HasProgenyPermission(int progenyId, UserInfo userInfo, PermissionLevel requiredLevel)
        {
            if (userInfo == null || progenyId == 0)
            {
                return false;
            }

            if (progenyId == Constants.DefaultChildId && requiredLevel == PermissionLevel.View)
            {
                return true;
            }
            
            IEnumerable<ProgenyPermission> groupPermissions = progenyDbContext.ProgenyPermissionsDb
                .AsNoTracking()
                .Where(pp => pp.GroupId > 0 && pp.ProgenyId == progenyId);

            foreach (ProgenyPermission permission in groupPermissions)
            {
                bool isMember = await progenyDbContext.UserGroupMembersDb.AnyAsync(ug => ug.UserId == userInfo.UserId && ug.UserGroupId == permission.GroupId);
                if (isMember && permission.PermissionLevel >= requiredLevel)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves a progeny permission record by its identifier, if the current user has the necessary permissions.
        /// </summary>
        /// <remarks>This method ensures that only users with administrative permissions for the progeny or
        /// the subject of the  progeny permission can access the requested record. If the user lacks the required
        /// permissions, the method  returns <see langword="null"/>.</remarks>
        /// <param name="progenyPermissionId">The unique identifier of the progeny permission to retrieve.</param>
        /// <param name="currentUserInfo">The information about the current user, used to validate access permissions.</param>
        /// <returns>The <see cref="ProgenyPermission"/> object if the current user has administrative access to the progeny  or is
        /// the subject of the permission; otherwise, <see langword="null"/>.</returns>
        public async Task<ProgenyPermission> GetProgenyPermission(int progenyPermissionId, UserInfo currentUserInfo)
        {
            ProgenyPermission progenyPermission = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                .SingleOrDefaultAsync(pp => pp.ProgenyPermissionId == progenyPermissionId);

            if (await HasProgenyPermission(progenyPermission.ProgenyId, currentUserInfo, PermissionLevel.Admin))
            {
                return progenyPermission;
            }

            return null;
        }

        /// <summary>
        /// Grants a specified permission to a user or group for a progeny, if the current user has the necessary
        /// access rights.
        /// </summary>
        /// <remarks>This method ensures that the current user has the necessary access rights to grant
        /// the specified permission. If the permission already exists,  or if the current user does not have sufficient
        /// privileges, the method returns <see langword="null"/>. The method also sets the creation and modification
        /// timestamps  for the new permission before saving it to the database.</remarks>
        /// <param name="progenyPermission">The permission to be granted, including details such as the user or group, familyId, and permission type.</param>
        /// <param name="currentUserInfo">Information about the current user attempting to grant the permission.</param>
        /// <returns>The granted <see cref="ProgenyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the current user lacks the required access rights  or if the specified permission
        /// already exists.</returns>
        public async Task<ProgenyPermission> GrantProgenyPermission(ProgenyPermission progenyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo, PermissionType.Progeny, progenyPermission.ProgenyId))
            {
                return null; // Todo: Use result object instead.
            }

            // Check if a permission with the same group id already exists for this progeny.
            ProgenyPermission existingPermission = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                .SingleOrDefaultAsync(pp => pp.GroupId == progenyPermission.GroupId
                                             && pp.ProgenyId == progenyPermission.ProgenyId);
            
            if (existingPermission != null)
            {
                // Permission for user or group already exists for this progeny. Use UpdateProgenyPermission to change existing permissions.
                return null; // Todo: Use result object instead.
            }

            if (progenyPermission.PermissionLevel >= PermissionLevel.CreatorOnly)
            {
                // CreatorOnly and Private permissions are not allowed to be set on progeny level.
                return null; // Todo: Use result object instead.
            }

            // If the new permission is admin, ensure only one admin group exists.
            if (progenyPermission.PermissionLevel == PermissionLevel.Admin)
            {
                ProgenyPermission adminPermission = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(pp => pp.ProgenyId == progenyPermission.ProgenyId && pp.PermissionLevel == PermissionLevel.Admin);
                if (adminPermission != null)
                {
                    return null;
                }
            }
            
            progenyPermission.CreatedBy = currentUserInfo.UserId;
            progenyPermission.CreatedTime = DateTime.UtcNow;
            progenyPermission.ModifiedBy = currentUserInfo.UserId;
            progenyPermission.ModifiedTime = DateTime.UtcNow;

            progenyDbContext.ProgenyPermissionsDb.Add(progenyPermission);
            await progenyDbContext.SaveChangesAsync();

            await permissionAuditLogService.AddProgenyPermissionAuditLogEntry(PermissionAction.Add, progenyPermission, currentUserInfo);

            return progenyPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Revokes a specific permission for a progeny from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="progenyPermission">The permission to be revoked, including details such as the user or group, permission type, and progenyId.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> RevokeProgenyPermission(ProgenyPermission progenyPermission, UserInfo currentUserInfo)
        {
            Progeny progeny = await progenyDbContext.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progenyPermission.ProgenyId);
            // Check if the current user can grant the specified permission level.
            if (progeny!= null && !await IsUserAccessManager(currentUserInfo, PermissionType.Progeny, progenyPermission.ProgenyId))
            {
                return false; // Todo: Use result object instead.
            }

            // Check if the permission exists.
            ProgenyPermission existingPermission = await progenyDbContext.ProgenyPermissionsDb
                .SingleOrDefaultAsync(pp => pp.ProgenyPermissionId == progenyPermission.ProgenyPermissionId && pp.ProgenyId == progenyPermission.ProgenyId);
            if (existingPermission == null)
            {
                return false; // Todo: Use result object instead
            }

            // Don't allow removing admin groups permission, unless the progeny has been deleted.
            if (progenyPermission.PermissionLevel == PermissionLevel.Admin)
            {
                if (progeny != null)
                {
                    return false; // Todo: Use result object instead.
                }
            }

            PermissionAuditLog logEntry = await permissionAuditLogService.AddProgenyPermissionAuditLogEntry(PermissionAction.Delete, progenyPermission, currentUserInfo);
            
            progenyDbContext.ProgenyPermissionsDb.Remove(existingPermission);
            await progenyDbContext.SaveChangesAsync();
            
            logEntry.ItemAfter = JsonSerializer.Serialize(existingPermission);
            await permissionAuditLogService.UpdatePermissionAuditLogEntry(logEntry);
            
            return true; // Todo: Use result object instead.
        }

        /// <summary>
        /// Updates the permission level of an existing progeny permission.
        /// </summary>
        /// <remarks>This method validates whether the current user has the necessary access rights to
        /// update the specified permission level. If the user does not have sufficient rights or the specified
        /// permission does not exist, the method returns <see langword="null"/>.</remarks>
        /// <param name="progenyPermission">The updated progeny permission details, including the new permission level.</param>
        /// <param name="currentUserInfo">Information about the current user performing the update, used to validate access rights.</param>
        /// <returns>The updated <see cref="ProgenyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the user lacks sufficient access rights or the specified permission does not exist.</returns>
        public async Task<ProgenyPermission> UpdateProgenyPermission(ProgenyPermission progenyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo, PermissionType.Progeny, progenyPermission.ProgenyId))
            {
                return null; // Todo: Use result object instead.
            }

            if (progenyPermission.PermissionLevel >= PermissionLevel.CreatorOnly)
            {
                // CreatorOnly and Private permissions are not allowed to be set on progeny level.
                return null; // Todo: Use result object instead.
            }

            // Check if the permission exists.
            ProgenyPermission existingPermission = await progenyDbContext.ProgenyPermissionsDb
                .SingleOrDefaultAsync(pp => pp.ProgenyPermissionId == progenyPermission.ProgenyPermissionId
                                            && pp.ProgenyId == progenyPermission.ProgenyId);
            if (existingPermission == null)
            {
                return null; // Todo: Use result object instead
            }

            PermissionAuditLog logEntry = await permissionAuditLogService.AddProgenyPermissionAuditLogEntry(PermissionAction.Update, progenyPermission, currentUserInfo);

            // If the existing permission is admin and the new permission isn't, return. One, and only one admin group allowed.
            if (progenyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                return null;
            }

            // If the existing permission is not admin and the new permission is, return. One, and only one admin group allowed.
            if (progenyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                return null;
            }

            existingPermission.PermissionLevel = progenyPermission.PermissionLevel;
            existingPermission.ModifiedTime = DateTime.UtcNow;
            existingPermission.ModifiedBy = currentUserInfo.UserId;

            progenyDbContext.ProgenyPermissionsDb.Update(existingPermission);
            await progenyDbContext.SaveChangesAsync();

            logEntry.ItemAfter = JsonSerializer.Serialize(existingPermission);
            await permissionAuditLogService.UpdatePermissionAuditLogEntry(logEntry);

            return existingPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Retrieves a list of permissions associated with a specified progeny.
        /// </summary>
        /// <remarks>This method checks whether the current user has the necessary access rights to manage
        /// permissions for the specified progeny. If the user does not have sufficient permissions, an empty list is
        /// returned.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose permissions are to be retrieved.</param>
        /// <param name="currentUserInfo">The user information of the current user making the request. This is used to verify access permissions.</param>
        /// <returns>A list of <see cref="ProgenyPermission"/> objects representing the permissions for the specified progeny. 
        /// Returns an empty list if the current user does not have the required access rights.</returns>
        public async Task<List<ProgenyPermission>> GetProgenyPermissionsList(int progenyId, UserInfo currentUserInfo)
        {
            if (!await IsUserAccessManager(currentUserInfo, PermissionType.Progeny, progenyId))
            {
                return [];
            }

            return await progenyDbContext.ProgenyPermissionsDb.AsNoTracking().Where(pp => pp.ProgenyId == progenyId).ToListAsync();
        }

        /// <summary>
        /// Updates the administrative permissions for a specified progeny based on the current list of administrators.
        /// </summary>
        /// <remarks>This method synchronizes the administrative permissions for the specified progeny by
        /// performing the following actions: <list type="bullet"> <item><description>Downgrades permissions for users
        /// who are no longer in the current list of administrators.</description></item> <item><description>Adds or
        /// updates permissions for new administrators in the list.</description></item> </list> The method ensures that
        /// the database reflects the current state of the progeny's administrative list.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose administrative permissions are to be updated.</param>
        /// <returns><see langword="true"/> if the administrative permissions were successfully updated;  otherwise, <see
        /// langword="false"/> if the specified progeny does not exist.</returns>
        public async Task<bool> ProgenyAdminsUpdated(int progenyId)
        {
            Progeny progeny = await progenyDbContext.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == progenyId);
            if (progeny == null)
            {
                return false;
            }

            // Get all existing admin permissions for the progeny.
            ProgenyPermission adminPermission = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                .SingleOrDefaultAsync(pp => pp.ProgenyId == progenyId && pp.PermissionLevel == PermissionLevel.Admin);
            List<UserGroupMember> adminGroupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking().Where(ugm => ugm.UserGroupId == adminPermission.GroupId).ToListAsync();
            List<string> adminEmails = progeny.GetAdminsList();

            // Downgrade permissions for users no longer in the admin list.
            foreach (UserGroupMember member in adminGroupMembers)
            {
                if (!adminEmails.Contains(member.Email, StringComparer.InvariantCultureIgnoreCase))
                {
                    // User is no longer an admin, downgrade their permission.
                    UserGroupMember userGroupMember = await progenyDbContext.UserGroupMembersDb
                        .SingleOrDefaultAsync(ugm => ugm.UserGroupMemberId == member.UserGroupMemberId);
                    if (userGroupMember != null)
                    {
                        // Remove from admin group.
                        progenyDbContext.UserGroupMembersDb.Remove(userGroupMember);
                        // Add or update to regular permission.
                        List<ProgenyPermission> groupPermissions = await progenyDbContext.ProgenyPermissionsDb
                            .Where(pp => pp.ProgenyId == progenyId && pp.PermissionLevel < PermissionLevel.Admin).OrderBy(pp => pp.PermissionLevel).ToListAsync();
                        ProgenyPermission highestPermission = groupPermissions.LastOrDefault();
                        if (highestPermission != null)
                        {
                            UserGroupMember newUserGroupMember = new UserGroupMember()
                            {
                                UserId = member.UserId,
                                Email = member.Email,
                                UserGroupId = highestPermission.GroupId,
                                CreatedBy = "System",
                                CreatedTime = DateTime.UtcNow,
                                ModifiedBy = "System",
                                ModifiedTime = DateTime.UtcNow
                            };
                            progenyDbContext.UserGroupMembersDb.Add(newUserGroupMember);
                        }
                    }
                }
            }
            

            // Add admin permissions for new admins.
            foreach (string email in adminEmails)
            {
                if (adminGroupMembers.All(ap => ap.Email.ToUpper() != email.ToUpper()))
                {
                    
                    UserInfo adminsUserInfo = await progenyDbContext.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == email.ToUpper());
                    UserGroupMember newAdminMember = new UserGroupMember()
                    {
                        UserId = adminsUserInfo != null ? adminsUserInfo.UserId : "",
                        Email = email,
                        UserGroupId = adminPermission.GroupId,
                        CreatedBy = "System",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "System",
                        ModifiedTime = DateTime.UtcNow
                    };
                    progenyDbContext.UserGroupMembersDb.Add(newAdminMember);
                }
            }

            await progenyDbContext.SaveChangesAsync();
            return true;

        }

        /// <summary>
        /// Determines whether a user has the specified access level for a given family.
        /// </summary>
        /// <param name="familyId">The unique identifier of the family whose access to is being checked.</param>
        /// <param name="userInfo">The information of the user whose access is being verified.</param>
        /// <param name="requiredLevel">The minimum permission level required for access.</param>
        /// <returns>Boolean value indicating whether the user has the required access level for the family.</returns>
        public async Task<bool> HasFamilyPermission(int familyId, UserInfo userInfo, PermissionLevel requiredLevel)
        {
            if (userInfo == null || familyId == 0)
            {
                return false;
            }

            Family family = await progenyDbContext.FamiliesDb.AsNoTracking().SingleOrDefaultAsync(f => f.FamilyId == familyId);
            if (family == null)
            {
                return false;
            }

            if (family.IsInAdminList(userInfo.UserEmail))
            {
                return true;
            }

            List<FamilyPermission> groupPermissions = await progenyDbContext.FamilyPermissionsDb
                .AsNoTracking()
                .Where(fp => fp.GroupId > 0 && fp.FamilyId== familyId)
                .ToListAsync();
            PermissionLevel highestGroupPermission = PermissionLevel.None;
            foreach (FamilyPermission permission in groupPermissions)
            {
                bool isMember = await progenyDbContext.UserGroupMembersDb.AnyAsync(ug => ug.UserId == userInfo.UserId && ug.UserGroupId == permission.GroupId);
                if (isMember && permission.PermissionLevel > highestGroupPermission)
                {
                    highestGroupPermission = permission.PermissionLevel;
                }
            }

            return highestGroupPermission >= requiredLevel;
        }

        /// <summary>
        /// Retrieves a family permission record by its identifier, if the current user has the necessary permissions.
        /// </summary>
        /// <remarks>This method ensures that only users with administrative permissions for the family or
        /// the owner of the  family permission can access the requested record. If the user lacks the required
        /// permissions, the method  returns <see langword="null"/>.</remarks>
        /// <param name="familyPermissionId">The unique identifier of the family permission to retrieve.</param>
        /// <param name="currentUserInfo">The information about the current user, used to validate access permissions.</param>
        /// <returns>The <see cref="FamilyPermission"/> object if the current user has administrative access to the family  or is
        /// the owner of the permission; otherwise, <see langword="null"/>.</returns>
        public async Task<FamilyPermission> GetFamilyPermission(int familyPermissionId, UserInfo currentUserInfo)
        {
            FamilyPermission familyPermission = await progenyDbContext.FamilyPermissionsDb.AsNoTracking()
                .SingleOrDefaultAsync(fp => fp.FamilyPermissionId == familyPermissionId);

            if (await HasFamilyPermission(familyPermission.FamilyId, currentUserInfo, PermissionLevel.Admin))
            {
                return familyPermission;
            }

            return null;
        }

        /// <summary>
        /// Grants a specified permission to a user or group for a family, if the current user has the necessary
        /// access rights.
        /// </summary>
        /// <remarks>This method ensures that the current user has the necessary access rights to grant
        /// the specified permission. If the permission already exists,  or if the current user does not have sufficient
        /// privileges, the method returns <see langword="null"/>. The method also sets the creation and modification
        /// timestamps  for the new permission before saving it to the database.</remarks>
        /// <param name="familyPermission">The permission to be granted, including details such as the user or group, familyId, and permission type.</param>
        /// <param name="currentUserInfo">Information about the current user attempting to grant the permission.</param>
        /// <returns>The granted <see cref="FamilyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the current user lacks the required access rights  or if the specified permission
        /// already exists.</returns>
        public async Task<FamilyPermission> GrantFamilyPermission(FamilyPermission familyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo, PermissionType.Family, familyPermission.FamilyId))
            {
                return null; // Todo: Use result object instead.
            }

            if (familyPermission.PermissionLevel >= PermissionLevel.CreatorOnly)
            {
                // CreatorOnly and Private permissions are not allowed to be set on family level.
                return null; // Todo: Use result object instead.
            }

            // Check if the permission already exists.
            FamilyPermission existingPermission = await progenyDbContext.FamilyPermissionsDb.AsNoTracking()
                .SingleOrDefaultAsync(fp => fp.GroupId == familyPermission.GroupId
                                           && fp.FamilyId == familyPermission.FamilyId);

            if (existingPermission != null)
            {
                // Permission for group already exists
                return null; // Todo: Use result object instead.
            }

            // If the new permission is admin, check if another admin permission exists. Only one admin group is allowed.
            if (familyPermission.PermissionLevel == PermissionLevel.Admin)
            {
                FamilyPermission adminPermission = await progenyDbContext.FamilyPermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(fp => fp.FamilyId == familyPermission.FamilyId && fp.PermissionLevel == PermissionLevel.Admin);
                if (adminPermission != null)
                {
                    return null;
                }
            }

            familyPermission.CreatedBy = currentUserInfo.UserId;
            familyPermission.CreatedTime = DateTime.UtcNow;
            familyPermission.ModifiedBy = currentUserInfo.UserId;
            familyPermission.ModifiedTime = DateTime.UtcNow;
            
            progenyDbContext.FamilyPermissionsDb.Add(familyPermission);
            await progenyDbContext.SaveChangesAsync();

            await permissionAuditLogService.AddFamilyPermissionAuditLogEntry(PermissionAction.Add, familyPermission, currentUserInfo);

            return familyPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Revokes a specific permission for a family from a user or group.
        /// </summary>
        /// <remarks>The method checks whether the current user has sufficient access rights to revoke the
        /// specified permission. If the permission does not exist or the user lacks the necessary access rights, the
        /// method returns <see langword="false"/>.</remarks>
        /// <param name="familyPermission">The permission to be revoked, including details such as the user or group, permission type, and familyId.</param>
        /// <param name="currentUserInfo">The information of the user attempting to revoke the permission.</param>
        /// <returns><see langword="true"/> if the permission was successfully revoked; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> RevokeFamilyPermission(FamilyPermission familyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo, PermissionType.Family, familyPermission.FamilyId))
            {
                return false; // Todo: Use result object instead.
            }
            
            // Check if the permission exists.
            FamilyPermission existingPermission = await progenyDbContext.FamilyPermissionsDb
                .SingleOrDefaultAsync(fp => fp.FamilyPermissionId == familyPermission.FamilyPermissionId && fp.FamilyId == familyPermission.FamilyId);
            if (existingPermission == null)
            {
                return false; // Todo: Use result object instead
            }

            // If the existing permission is admin, only allow removing admin rights if the family has been deleted.
            if (existingPermission.PermissionLevel == PermissionLevel.Admin)
            {
                Family family = await progenyDbContext.FamiliesDb.SingleOrDefaultAsync(f => f.FamilyId == familyPermission.FamilyId);
                if (family != null)
                {
                    return false;
                }
            }

            PermissionAuditLog logEntry = await permissionAuditLogService.AddFamilyPermissionAuditLogEntry(PermissionAction.Delete, familyPermission, currentUserInfo);
            
            progenyDbContext.FamilyPermissionsDb.Remove(existingPermission);
            await progenyDbContext.SaveChangesAsync();

            logEntry.ItemAfter = JsonSerializer.Serialize(existingPermission);
            await permissionAuditLogService.UpdatePermissionAuditLogEntry(logEntry);

            return true; // Todo: Use result object instead.
        }

        /// <summary>
        /// Updates the permission level of an existing family permission.
        /// </summary>
        /// <remarks>This method validates whether the current user has the necessary access rights to
        /// update the specified permission level. If the user does not have sufficient rights or the specified
        /// permission does not exist, the method returns <see langword="null"/>.</remarks>
        /// <param name="familyPermission">The updated family permission details, including the new permission level.</param>
        /// <param name="currentUserInfo">Information about the current user performing the update, used to validate access rights.</param>
        /// <returns>The updated <see cref="FamilyPermission"/> object if the operation is successful; otherwise, <see
        /// langword="null"/> if the user lacks sufficient access rights or the specified permission does not exist.</returns>
        public async Task<FamilyPermission> UpdateFamilyPermission(FamilyPermission familyPermission, UserInfo currentUserInfo)
        {
            // Check if the current user can grant the specified permission level.
            if (!await IsUserAccessManager(currentUserInfo, PermissionType.Family, familyPermission.FamilyId))
            {
                return null; // Todo: Use result object instead.
            }

            if (familyPermission.PermissionLevel >= PermissionLevel.CreatorOnly)
            {
                // CreatorOnly and Private permissions are not allowed to be set on family level.
                return null; // Todo: Use result object instead.
            }

            // Check if the permission exists.
            FamilyPermission existingPermission = await progenyDbContext.FamilyPermissionsDb
                .SingleOrDefaultAsync(fp => fp.FamilyPermissionId == familyPermission.FamilyPermissionId && fp.FamilyId == familyPermission.FamilyId);
            if (existingPermission == null)
            {
                return null; // Todo: Use result object instead
            }

            PermissionAuditLog logEntry = await permissionAuditLogService.AddFamilyPermissionAuditLogEntry(PermissionAction.Update, familyPermission, currentUserInfo);

            // If the existing permission is admin and the new permission isn't return. Only one admin group is allowed.
            if (familyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                return null;
            }

            // If the existing permission is not admin and the new permission is, return. Only one admin group is allowed.
            if (familyPermission.PermissionLevel == PermissionLevel.Admin && existingPermission.PermissionLevel != PermissionLevel.Admin)
            {
                return null;
            }

            existingPermission.PermissionLevel = familyPermission.PermissionLevel;
            existingPermission.ModifiedTime = DateTime.UtcNow;
            existingPermission.ModifiedBy = currentUserInfo.UserId;
            
            progenyDbContext.FamilyPermissionsDb.Update(existingPermission);
            await progenyDbContext.SaveChangesAsync();
            
            logEntry.ItemAfter = JsonSerializer.Serialize(existingPermission);
            await permissionAuditLogService.UpdatePermissionAuditLogEntry(logEntry);

            return existingPermission; // Todo: Use result object instead.
        }

        /// <summary>
        /// Retrieves a list of permissions associated with a specific family.
        /// </summary>
        /// <remarks>The method checks whether the current user has the necessary access rights to manage
        /// the specified family before retrieving the permissions. If the user lacks the required permissions, an empty
        /// list is returned.</remarks>
        /// <param name="familyId">The unique identifier of the family whose permissions are to be retrieved.</param>
        /// <param name="currentUserInfo">The information of the user making the request, used to verify access permissions.</param>
        /// <returns>A list of <see cref="FamilyPermission"/> objects representing the permissions for the specified family.
        /// Returns an empty list if the user does not have access to manage the specified family.</returns>
        public async Task<List<FamilyPermission>> GetFamilyPermissionsList(int familyId, UserInfo currentUserInfo)
        {
            if (!await IsUserAccessManager(currentUserInfo, PermissionType.Family, familyId))
            {
                return new List<FamilyPermission>();
            }

            return await progenyDbContext.FamilyPermissionsDb.AsNoTracking().Where(fp => fp.FamilyId == familyId).ToListAsync();
        }

        /// <summary>
        /// Retrieves the permission settings for a specific progeny and user group.
        /// </summary>
        /// <remarks>This method checks whether the current user has the necessary access rights to
        /// retrieve the permission settings. If the user does not have access, the method returns <see
        /// langword="null"/>.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny. Must be greater than 0.</param>
        /// <param name="userGroupId">The unique identifier of the user group. Must be greater than 0.</param>
        /// <param name="currentUserInfo">The information of the current user making the request. Used to verify access permissions.</param>
        /// <returns>A <see cref="ProgenyPermission"/> object representing the permission settings for the specified progeny and
        /// user group,  or <see langword="null"/> if the progeny or user group does not exist, the user lacks access,
        /// or the identifiers are invalid.</returns>
        public async Task<ProgenyPermission> GetProgenyPermissionForGroup(int progenyId, int userGroupId, UserInfo currentUserInfo)
        {
            if (progenyId == 0 || userGroupId == 0)
            {
                return null;
            }

            bool hasAccess = await IsUserAccessManager(currentUserInfo, PermissionType.Progeny, progenyId);
            if (!hasAccess)
            {
                return null;
            }
            
            ProgenyPermission progenyPermission = await progenyDbContext.ProgenyPermissionsDb
                .AsNoTracking()
                .SingleOrDefaultAsync(pp => pp.ProgenyId == progenyId && pp.GroupId == userGroupId);

            return progenyPermission;
        }

        /// <summary>
        /// Retrieves the family permission associated with a specific family and user group.
        /// </summary>
        /// <remarks>The method checks whether the current user has access to manage permissions for the
        /// specified family  before attempting to retrieve the associated family permission. If the user does not have
        /// the required  access, the method returns <c>null</c>.</remarks>
        /// <param name="familyId">The unique identifier of the family. Must be greater than zero.</param>
        /// <param name="userGroupId">The unique identifier of the user group. Must be greater than zero.</param>
        /// <param name="currentUserInfo">The information of the current user making the request. Cannot be <c>null</c>.</param>
        /// <returns>A <see cref="FamilyPermission"/> object representing the permission for the specified family and user group,
        /// or <c>null</c> if the identifiers are invalid, the user lacks access, or no matching permission is found.</returns>
        public async Task<FamilyPermission> GetFamilyPermissionForGroup(int familyId, int userGroupId, UserInfo currentUserInfo)
        {
            if (familyId == 0 || userGroupId == 0)
            {
                return null;
            }

            bool hasAccess = await IsUserAccessManager(currentUserInfo, PermissionType.Family, familyId);
            if (!hasAccess)
            {
                return null;
            }

            FamilyPermission familyPermission = await progenyDbContext.FamilyPermissionsDb
                .AsNoTracking()
                .SingleOrDefaultAsync(pp => pp.FamilyId == familyId && pp.GroupId == userGroupId);

            return familyPermission;
        }

        /// <summary>
        /// Gets the highest permission level for a specific progeny and user by checking both direct user permissions and group permissions.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny.</param>
        /// <param name="userInfo">The information of the user whose permissions are being checked.</param>
        /// <returns></returns>
        public async Task<ProgenyPermission> GetProgenyPermissionForUser(int progenyId, UserInfo userInfo)
        {
            PermissionLevel highestPermission = PermissionLevel.None;
            ProgenyPermission resultPermission = new()
            {
                PermissionLevel = PermissionLevel.None
            };

            if (progenyId == Constants.DefaultChildId && userInfo.UserId != Constants.DefaultUserId)
            {
                ProgenyPermission defaultChildPermission = new()
                {
                    PermissionLevel = PermissionLevel.View,
                    ProgenyId = Constants.DefaultChildId
                };
                return defaultChildPermission;
            }
            
            // Check group permissions.
            List<ProgenyPermission> groupPermissions = await progenyDbContext.ProgenyPermissionsDb
                .AsNoTracking()
                .Where(pp => pp.GroupId > 0 && pp.ProgenyId == progenyId && pp.PermissionLevel < PermissionLevel.CreatorOnly)
                .ToListAsync();
            foreach (ProgenyPermission permission in groupPermissions)
            {
                bool isMember = await progenyDbContext.UserGroupMembersDb.AnyAsync(ug => ug.UserId == userInfo.UserId && ug.UserGroupId == permission.GroupId);
                if (!isMember || permission.PermissionLevel <= highestPermission) continue;

                highestPermission = permission.PermissionLevel;
                resultPermission = permission;
            }

            if (resultPermission.ProgenyPermissionId == 0)
            {
                Progeny progeny = await progenyDbContext.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == progenyId);
                if (progeny != null)
                {
                    if (progeny.IsInAdminList(userInfo.UserEmail))
                    {
                        // There is no explicit permission set, but the user is in the admin list, so give them admin permissions.
                        List<UserGroup> progenyGroups = await progenyDbContext.UserGroupsDb.AsNoTracking()
                            .Where(ug => ug.ProgenyId == progenyId).ToListAsync();
                        if (progenyGroups.Count == 0)
                        {
                            UserGroup adminGroup = new()
                            {
                                IsFamily = false,
                                Name = $"{progeny.Name} Admins",
                                Description = $"Administrators group for {progeny.NickName} created automatically.",
                                ProgenyId = progenyId,
                                FamilyId = 0,
                                CreatedBy = "System",
                                CreatedTime = DateTime.UtcNow,
                                ModifiedBy = "System",
                                ModifiedTime = DateTime.UtcNow
                            };

                            progenyDbContext.UserGroupsDb.Add(adminGroup);
                            await progenyDbContext.SaveChangesAsync();

                            UserGroupMember adminMember = new()
                            {
                                UserId = userInfo.UserId,
                                Email = userInfo.UserEmail,
                                UserGroupId = adminGroup.UserGroupId,
                                CreatedBy = "System",
                                CreatedTime = DateTime.UtcNow,
                                ModifiedBy = "System",
                                ModifiedTime = DateTime.UtcNow
                            };
                            progenyDbContext.UserGroupMembersDb.Add(adminMember);
                            await progenyDbContext.SaveChangesAsync();

                            ProgenyPermission adminPermission = new()
                            {
                                ProgenyId = progenyId,
                                GroupId = adminGroup.UserGroupId,
                                PermissionLevel = PermissionLevel.Admin,
                                CreatedBy = "System",
                                CreatedTime = DateTime.UtcNow,
                                ModifiedBy = "System",
                                ModifiedTime = DateTime.UtcNow
                            };
                            resultPermission = await GrantProgenyPermission(adminPermission, userInfo);
                        }
                    }
                }
            }

            return resultPermission;
        }

        /// <summary>
        /// Retrieves the highest permission level for a user within a specified family, considering both direct user
        /// permissions and group memberships.
        /// </summary>
        /// <remarks>This method first checks for direct permissions assigned to the user for the
        /// specified family. If no direct permissions are found, it evaluates the user's group memberships and their
        /// associated permissions. Group permissions with a <see cref="PermissionLevel"/> of <see
        /// cref="PermissionLevel.CreatorOnly"/> or higher are excluded from consideration.</remarks>
        /// <param name="familyId">The unique identifier of the family for which the permissions are being retrieved.</param>
        /// <param name="userInfo">An object containing information about the user whose permissions are being evaluated.</param>
        /// <returns>A <see cref="FamilyPermission"/> object representing the highest permission level the user has within the
        /// specified family. If the user has no permissions, the returned object will have a <see
        /// cref="PermissionLevel"/> of <see cref="PermissionLevel.None"/>.</returns>
        public async Task<FamilyPermission> GetFamilyPermissionForUser(int familyId, UserInfo userInfo)
        {
            PermissionLevel highestPermission = PermissionLevel.None;
            FamilyPermission resultPermission = new()
            {
                PermissionLevel = PermissionLevel.None
            };
            
            // Check group permissions.
            List<FamilyPermission> groupPermissions = await progenyDbContext.FamilyPermissionsDb
                .AsNoTracking()
                .Where(fp => fp.GroupId > 0 && fp.FamilyId == familyId && fp.PermissionLevel < PermissionLevel.CreatorOnly)
                .ToListAsync();
            foreach (FamilyPermission permission in groupPermissions)
            {
                bool isMember = await progenyDbContext.UserGroupMembersDb.AnyAsync(ug => ug.UserId == userInfo.UserId && ug.UserGroupId == permission.GroupId);
                if (isMember && permission.PermissionLevel > highestPermission)
                {
                    highestPermission = permission.PermissionLevel;
                    resultPermission = permission;
                }
            }

            return resultPermission;
        }

        /// <summary>
        /// Determines whether the specified user has administrative access to a resource based on the given permission
        /// type, entity ID, and optional timeline type.
        /// </summary>
        /// <remarks>This method evaluates the user's access by querying the resource permissions database
        /// for matching entries. The user must have an administrative permission level for the specified resource and
        /// permission type to be granted access.</remarks>
        /// <param name="currentUserInfo">The information of the current user whose access is being evaluated.</param>
        /// <param name="permissionType">The type of permission to evaluate. This determines the scope of the access check (e.g., timeline item,
        /// family member, or family).</param>
        /// <param name="entityId">The unique identifier of the resource entity for which admin access is being checked. (e.g., ProgenyId, FamilyMemberId, or FamilyId)</param>
        /// <returns><see langword="true"/> if the user has administrative access to the specified resource based on the provided
        /// parameters; otherwise, <see langword="false"/>.</returns>
        private async Task<bool> IsUserAccessManager(UserInfo currentUserInfo, PermissionType permissionType, int entityId)
        {
            if (permissionType == PermissionType.TimelineItem)
            {
                // For timeline items, we need to check if the user has admin rights for the Progeny associated with the timeline item.
                ProgenyPermission progenyPermission = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                    .SingleOrDefaultAsync(pp => pp.ProgenyId == entityId && pp.PermissionLevel == PermissionLevel.Admin);

                if (progenyPermission == null)
                {
                    return false; 
                }
                // Check if the user is a member of the admin group.
                List<UserGroupMember> userGroupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking()
                    .Where(ug => ug.UserId == currentUserInfo.UserId && ug.UserGroupId == progenyPermission.GroupId)
                    .ToListAsync();
                if (userGroupMembers.Count != 0)
                {
                    return true;
                }
            }

            if (permissionType == PermissionType.Progeny)
            {
                Progeny progeny = await progenyDbContext.ProgenyDb.AsNoTracking()
                    .SingleOrDefaultAsync(p => p.Id == entityId);
                if (progeny == null)
                {
                    return false;
                }

                if (progeny.IsInAdminList(currentUserInfo.UserEmail))
                {
                    return true;
                }

                // Check group permissions
                List<ProgenyPermission> groupPermissions = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking()
                    .Where(pp => pp.GroupId > 0 && pp.ProgenyId == entityId && pp.PermissionLevel == PermissionLevel.Admin)
                    .ToListAsync();
                foreach (ProgenyPermission permission in groupPermissions)
                {
                    List<UserGroupMember> userGroupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking()
                        .Where(ug => ug.UserId == currentUserInfo.UserId && ug.UserGroupId == permission.GroupId)
                        .ToListAsync();
                    if (userGroupMembers.Count != 0)
                    {
                        return true;
                    }
                }
            }

            if (permissionType == PermissionType.Family)
            {
                Family family = await progenyDbContext.FamiliesDb.AsNoTracking()
                    .SingleOrDefaultAsync(f => f.FamilyId == entityId);
                if (family == null)
                {
                    return false;
                }

                if (family.IsInAdminList(currentUserInfo.UserEmail))
                {
                    return true;
                }

                // Check group permissions
                List<FamilyPermission> groupPermissions = await progenyDbContext.FamilyPermissionsDb.AsNoTracking()
                    .Where(pp => pp.GroupId > 0 && pp.FamilyId == entityId && pp.PermissionLevel == PermissionLevel.Admin)
                    .ToListAsync();
                
                foreach (FamilyPermission permission in groupPermissions)
                {
                    List<UserGroupMember> userGroupMembers = await progenyDbContext.UserGroupMembersDb.AsNoTracking()
                        .Where(ug => ug.UserId == currentUserInfo.UserId && ug.UserGroupId == permission.GroupId)
                        .ToListAsync();
                    if (userGroupMembers.Count != 0)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Retrieves a list of progeny IDs that the specified user can access based on their permissions.
        /// </summary>
        /// <remarks>This method aggregates permissions directly assigned to the user as well as
        /// permissions granted through user groups.</remarks>
        /// <param name="userInfo">The user information, including the user's unique identifier.</param>
        /// <param name="permissionLevel">The required permission level to access the progeny. This parameter is currently unused but may be used in
        /// future implementations to filter results based on permission levels.</param>
        /// <returns>A list of integers representing the IDs of progenies the user has access to. The list contains distinct IDs
        /// and may be empty if the user has no access to any progeny.</returns>
        public async Task<List<int>> ProgeniesUserCanAccess(UserInfo userInfo, PermissionLevel permissionLevel)
        {
            List<int> progenies = [];
            
            // Get group permissions.
            List<UserGroupMember> userGroups = await progenyDbContext.UserGroupMembersDb.AsNoTracking().Where(ug => ug.UserId == userInfo.UserId).ToListAsync();
            foreach (UserGroupMember group in userGroups)
            {
                List<ProgenyPermission> groupPermissions = await progenyDbContext.ProgenyPermissionsDb.AsNoTracking().Where(pp => pp.GroupId == group.UserGroupId).ToListAsync();
                foreach (ProgenyPermission permission in groupPermissions)
                {
                    if (permission.PermissionLevel >= permissionLevel)
                    {
                        progenies.Add(permission.ProgenyId);
                    }
                }
            }

            List<Progeny> adminProgenies = await progenyDbContext.ProgenyDb.AsNoTracking().Where(p => p.Admins.ToLower().Contains(userInfo.UserEmail.ToLower())).ToListAsync();
            foreach (Progeny progeny in adminProgenies)
            {
                if (progeny.IsInAdminList(userInfo.UserEmail))
                {
                    progenies.Add(progeny.Id);
                }
            }

            List<int> resultProgenies = progenies.Distinct().ToList();
            return resultProgenies;
        }

        /// <summary>
        /// Retrieves a list of family IDs that the specified user can access based on the given permission level.
        /// </summary>
        /// <remarks>This method checks both user-specific permissions and permissions granted through
        /// user groups. If the user  has sufficient permissions for a family either directly or through a group, the
        /// family ID will be included  in the result.</remarks>
        /// <param name="userInfo">The user information, including the user's unique identifier.</param>
        /// <param name="permissionLevel">The minimum permission level required to access a family. Only families where the user has this level of
        /// access  or higher will be included in the result.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of distinct family IDs 
        /// that the user can access.</returns>
        public async Task<List<int>> FamiliesUserCanAccess(UserInfo userInfo, PermissionLevel permissionLevel)
        {
            List<int> families = [];
            
            // Get group permissions.
            List<UserGroupMember> userGroups = await progenyDbContext.UserGroupMembersDb.AsNoTracking().Where(ug => ug.UserId == userInfo.UserId).ToListAsync();
            foreach (UserGroupMember group in userGroups)
            {
                List<FamilyPermission> groupPermissions = await progenyDbContext.FamilyPermissionsDb.AsNoTracking().Where(pp => pp.GroupId == group.UserGroupId).ToListAsync();
                foreach (FamilyPermission permission in groupPermissions)
                {
                    if (permission.PermissionLevel >= permissionLevel)
                    {
                        families.Add(permission.FamilyId);
                    }
                }
            }

            List<int> resultFamilies = families.Distinct().ToList();
            return resultFamilies;
        }

        private async Task<Dictionary<int, PermissionLevel>> AllUsersItemPermissionsDictionary(UserInfo userInfo, KinaUnaTypes.TimeLineType type)
        {
            Dictionary<int, PermissionLevel> allPermissions = new();

            string cachedItemPermissions = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion +
                                                                     "allUsersItemPermissions" + "_userId_" + userInfo.UserId + "_type_" + (int)type);
            if (!string.IsNullOrEmpty(cachedItemPermissions))
            {
                ItemPermissionDictionaryCacheEntry cachedPermissions = JsonSerializer.Deserialize<ItemPermissionDictionaryCacheEntry>(cachedItemPermissions, JsonSerializerOptions.Web);

                // Check if user data has been modified since the cache was created.
                string userCacheEntryString = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userCacheEntry_" + userInfo.UserId);
                if (!string.IsNullOrEmpty(userCacheEntryString))
                {
                    UserUpdatedCacheEntry userCacheEntry = JsonSerializer.Deserialize<UserUpdatedCacheEntry>(userCacheEntryString, JsonSerializerOptions.Web);
                    if (userCacheEntry != null)
                    {
                        // If the user cache entry is found, check if the time stamp is newer than the cache for item permissions.
                        if (userCacheEntry.UpdateTime < cachedPermissions.UpdateTime)
                        {
                            return cachedPermissions.ItemPermissionDictionary;
                        }
                    }
                }
                else
                {
                    // No user cache entry found, return cached permissions.
                    return cachedPermissions.ItemPermissionDictionary;
                }
                
            }

            // Get user specific permissions.
            List<TimelineItemPermission> timelineItemPermissions = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking().Where(tp => tp.UserId == userInfo.UserId && tp.TimelineType == type).ToListAsync();
            // Get group permissions.
            IEnumerable<UserGroupMember> userGroups = progenyDbContext.UserGroupMembersDb.AsNoTracking().Where(ug => ug.UserId == userInfo.UserId);
            foreach (UserGroupMember group in userGroups)
            {
                IEnumerable<TimelineItemPermission> groupPermissions = progenyDbContext.TimelineItemPermissionsDb.AsNoTracking().Where(tp => tp.GroupId == group.UserGroupId && tp.TimelineType == type);
                timelineItemPermissions.AddRange(groupPermissions);
            }
            // Get inherited permissions.
            IEnumerable<int> progeniesUserCanAccess = await ProgeniesUserCanAccess(userInfo, PermissionLevel.View);
            foreach (int progenyId in progeniesUserCanAccess)
            {
                IEnumerable<TimelineItemPermission> inheritedPermissions = progenyDbContext.TimelineItemPermissionsDb.AsNoTracking()
                    .Where(tp => tp.ProgenyId == progenyId && tp.InheritPermissions && tp.TimelineType == type);
                if (inheritedPermissions.Any())
                {
                    ProgenyPermission progenyPermission = await GetProgenyPermissionForUser(progenyId, userInfo);
                    foreach (TimelineItemPermission timelineItemPermission in inheritedPermissions)
                    {
                        timelineItemPermission.PermissionLevel = progenyPermission.PermissionLevel;
                        timelineItemPermissions.Add(timelineItemPermission);
                    }
                }
            }

            IEnumerable<int> familiesUserCanAccess = await FamiliesUserCanAccess(userInfo, PermissionLevel.View);
            foreach (int familyId in familiesUserCanAccess)
            {
                
                IEnumerable<TimelineItemPermission> inheritedPermissions = progenyDbContext.TimelineItemPermissionsDb.AsNoTracking()
                    .Where(tp => tp.FamilyId == familyId && tp.InheritPermissions && tp.TimelineType == type);
                if (inheritedPermissions.Any())
                {
                    FamilyPermission familyPermission = await GetFamilyPermissionForUser(familyId, userInfo);
                    foreach (TimelineItemPermission timelineItemPermission in inheritedPermissions)
                    {
                        timelineItemPermission.PermissionLevel = familyPermission.PermissionLevel;
                        timelineItemPermissions.Add(timelineItemPermission);
                    }
                }
            }

            foreach (TimelineItemPermission timelineItemPermission in timelineItemPermissions)
            {
                allPermissions[timelineItemPermission.ItemId] = timelineItemPermission.PermissionLevel;
            }

            ItemPermissionDictionaryCacheEntry cacheEntry = new()
            {
                ItemPermissionDictionary = allPermissions,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSliding = new();
            cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(1, 0, 0, 0));
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "allUsersItemPermissions" + "_userId_" + userInfo.UserId + "_type_" + (int)type
                , JsonSerializer.Serialize(cacheEntry, JsonSerializerOptions.Web), cacheOptionsSliding);

            return allPermissions;
        }

        private async Task<List<TimelineItemPermission>> AllUsersTimelineItemPermissions(UserInfo userInfo, KinaUnaTypes.TimeLineType type)
        {
            string cachedItemPermissions = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion +
                                                                     "allUsersTimelineItemPermissions" + "_userId_" + userInfo.UserId + "_type_" + (int)type);
            if (!string.IsNullOrEmpty(cachedItemPermissions))
            {
                ItemPermissionListCacheEntry cachedPermissions = JsonSerializer.Deserialize<ItemPermissionListCacheEntry>(cachedItemPermissions, JsonSerializerOptions.Web);
                // Check if user data has been modified since the cache was created.
                string userCacheEntryString = await cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userCacheEntry_" + userInfo.UserId);
                if (!string.IsNullOrEmpty(userCacheEntryString))
                {
                    UserUpdatedCacheEntry userCacheEntry = JsonSerializer.Deserialize<UserUpdatedCacheEntry>(userCacheEntryString, JsonSerializerOptions.Web);
                    if (userCacheEntry != null)
                    {
                        // If the user cache entry is found, check if the time stamp is newer than the cache for item permissions.
                        if (userCacheEntry.UpdateTime < cachedPermissions.UpdateTime)
                        {
                            return cachedPermissions.TimelineItemPermissions;
                        }
                    }
                }
                else
                {
                    // No user cache entry found, return cached permissions.
                    return cachedPermissions.TimelineItemPermissions;
                    
                }
            }

            // Get user specific permissions.
            List<TimelineItemPermission> timelineItemPermissions = await progenyDbContext.TimelineItemPermissionsDb.AsNoTracking().Where(tp => tp.UserId == userInfo.UserId && tp.TimelineType == type).ToListAsync();
            // Get group permissions.
            IEnumerable<UserGroupMember> userGroups = progenyDbContext.UserGroupMembersDb.AsNoTracking().Where(ug => ug.UserId == userInfo.UserId);
            foreach (UserGroupMember group in userGroups)
            {
                IEnumerable<TimelineItemPermission> groupPermissions = progenyDbContext.TimelineItemPermissionsDb.AsNoTracking().Where(tp => tp.GroupId == group.UserGroupId && tp.TimelineType == type);
                timelineItemPermissions.AddRange(groupPermissions);
            }
            // Get inherited permissions.
            IEnumerable<int> progeniesUserCanAccess = await ProgeniesUserCanAccess(userInfo, PermissionLevel.View); 
            foreach (int progenyId in progeniesUserCanAccess)
            {
                IEnumerable<TimelineItemPermission> inheritedPermissions = progenyDbContext.TimelineItemPermissionsDb.AsNoTracking()
                    .Where(tp => tp.ProgenyId == progenyId && tp.InheritPermissions && tp.TimelineType == type);
                timelineItemPermissions.AddRange(inheritedPermissions);
            }

            IEnumerable<int> familiesUserCanAccess = await FamiliesUserCanAccess(userInfo, PermissionLevel.View);
            foreach (int familyId in familiesUserCanAccess)
            {
                IEnumerable<TimelineItemPermission> inheritedPermissions = progenyDbContext.TimelineItemPermissionsDb.AsNoTracking()
                    .Where(tp => tp.FamilyId == familyId && tp.InheritPermissions && tp.TimelineType == type);
                timelineItemPermissions.AddRange(inheritedPermissions);
            }

            ItemPermissionListCacheEntry cacheEntry = new()
            {
                TimelineItemPermissions = timelineItemPermissions,
                UpdateTime = DateTime.UtcNow
            };

            DistributedCacheEntryOptions cacheOptionsSliding = new();
            cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(1, 0, 0, 0));
            await cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "allUsersTimelineItemPermissions" + "_userId_" + userInfo.UserId + "_type_" + (int)type
                , JsonSerializer.Serialize(cacheEntry, JsonSerializerOptions.Web), cacheOptionsSliding);

            return timelineItemPermissions;
        }

        /// <summary>
        /// Updates the email address associated with a user's permissions across all relevant entities.
        /// </summary>
        /// <remarks>This method updates the email address for all permissions related to the specified
        /// user in the Progeny, Family, and TimelineItem permissions databases. The changes are persisted to the
        /// database upon successful completion of the operation.</remarks>
        /// <param name="userInfo">The user information containing the user's unique identifier.</param>
        /// <param name="newEmail">The new email address to associate with the user's permissions.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ChangeUsersEmailForPermissions(UserInfo userInfo, string newEmail)
        {
            List<TimelineItemPermission> timelineItemPermissions = await progenyDbContext.TimelineItemPermissionsDb.Where(tp => tp.UserId == userInfo.UserId).ToListAsync();
            foreach (TimelineItemPermission permission in timelineItemPermissions)
            {
                permission.Email = newEmail;
                progenyDbContext.TimelineItemPermissionsDb.Update(permission);
            }

            await progenyDbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Sets the user ID for all permissions associated with a new user based on their email address.
        /// </summary>
        /// <param name="userInfo">The user information containing the user's unique identifier and email address.</param>
        /// <returns></returns>
        public async Task UpdatePermissionsForNewUser(UserInfo userInfo)
        {
            List<TimelineItemPermission> timelineItemPermissions = await progenyDbContext.TimelineItemPermissionsDb.Where(tp => tp.Email.ToUpper() == userInfo.UserEmail.ToUpper()).ToListAsync();
            foreach (TimelineItemPermission permission in timelineItemPermissions)
            {
                permission.UserId = userInfo.UserId;
                progenyDbContext.TimelineItemPermissionsDb.Update(permission);
            }

            await progenyDbContext.SaveChangesAsync();
        }
    }
}
