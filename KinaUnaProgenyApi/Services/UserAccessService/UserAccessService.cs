using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace KinaUnaProgenyApi.Services.UserAccessService
{
    public class UserAccessService : IUserAccessService
    {
        private readonly ProgenyDbContext _context;
        private readonly MediaDbContext _mediaContext;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public UserAccessService(ProgenyDbContext context, MediaDbContext mediaContext, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _mediaContext = mediaContext;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets the list of Progeny where the user is an admin.
        /// Gets the list from the cache if it exists, otherwise gets the list from the database and adds it to the cache.
        /// The reason email is used instead of UserId is that access for a user can be granted by email address, so that even if a user hasn't created an account yet, they can still be granted access as soon as they do.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>List of Progeny objects.</returns>
        public async Task<List<Progeny>> GetProgenyUserIsAdmin(string email)
        {
            List<Progeny> progenyList = await GetProgenyUserIsAdminFromCache(email);
            if (progenyList == null || progenyList.Count == 0)
            {
                progenyList = await SetProgenyUserIsAdminInCache(email);
            }

            return progenyList;
        }

        /// <summary>
        /// Gets the list of Progeny where the user is an admin from the cache.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>List of Progeny objects.</returns>
        private async Task<List<Progeny>> GetProgenyUserIsAdminFromCache(string email)
        {
            List<Progeny> progenyList = [];
            string cachedProgenyList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progenywhereadmin" + email);
            if (!string.IsNullOrEmpty(cachedProgenyList))
            {
                progenyList = JsonSerializer.Deserialize<List<Progeny>>(cachedProgenyList, JsonSerializerOptions.Web);
            }

            return progenyList;
        }

        /// <summary>
        /// Gets the list of Progeny where the user is an admin from the database and adds it to the cache.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>List of Progeny objects.</returns>
        private async Task<List<Progeny>> SetProgenyUserIsAdminInCache(string email)
        {
            List<Progeny> progenyList = await _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(email)).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenywhereadmin" + email, JsonSerializer.Serialize(progenyList, JsonSerializerOptions.Web), _cacheOptionsSliding);
            return progenyList;
        }

        /// <summary>
        /// Gets the list of all UserAccess entities that exist for a Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list of UserAccesses for.</param>
        /// <param name="currentUserEmail">The email address of the current user, to validate if the user should be allowed access. Constants.SystemAccountEmail overrides access checks.</param>
        /// <returns>List of UserAccess objects.</returns>
        public async Task<CustomResult<List<UserAccess>>> GetProgenyUserAccessList(int progenyId, string currentUserEmail)
        {
            List<UserAccess> accessList = await GetProgenyUserAccessListFromCache(progenyId);

            if (accessList == null || accessList.Count == 0)
            {
                accessList = await SetProgenyUserAccessListInCache(progenyId);
            }

            bool allowedAccess = IsUserInUserAccessList(accessList, currentUserEmail);

            if (!allowedAccess && progenyId != Constants.DefaultChildId) // DefaultChild is always allowed.
            {
                return CustomError.UnauthorizedError("GetProgenyUserAccessList: User is not authorized to access this progeny.");
            }

            return accessList;
        }

        /// <summary>
        /// Gets the list of all UserAccess entities that exist for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get the list of UserAccesses for.</param>
        /// <returns>List of UserAccess objects.</returns>
        private async Task<List<UserAccess>> GetProgenyUserAccessListFromCache(int progenyId)
        {
            List<UserAccess> accessList = [];
            string cachedAccessList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "accessList" + progenyId);
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonSerializer.Deserialize<List<UserAccess>>(cachedAccessList, JsonSerializerOptions.Web);
            }

            return accessList;
        }

        /// <summary>
        /// Gets the list of all UserAccess entities that exist for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set the list for.</param>
        /// <returns>List of UserAccess objects.</returns>
        private async Task<List<UserAccess>> SetProgenyUserAccessListInCache(int progenyId)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "accessList" + progenyId, JsonSerializer.Serialize(accessList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return accessList;
        }

        /// <summary>
        /// Gets the list of all UserAccess entities that exist for a user.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>List of UserAccess objects.</returns>
        public async Task<List<UserAccess>> GetUsersUserAccessList(string email)
        {
            List<UserAccess> accessList = await GetUsersUserAccessListFromCache(email);
            if (accessList == null || accessList.Count == 0)
            {
                accessList = await SetUsersUserAccessListInCache(email);
            }

            return accessList;
        }

        /// <summary>
        /// Gets the list of all UserAccess entities for a user where the user is admin of the Progeny.
        /// First checks the cache, if not found, gets the list from the database and adds it to the cache.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>List of UserAccess objects.</returns>
        public async Task<List<UserAccess>> GetUsersUserAdminAccessList(string email)
        {
            List<UserAccess> userAccessList = await GetUsersUserAccessList(email);
            userAccessList = [.. userAccessList.Where(u => u.AccessLevel == 0)];

            return userAccessList;
        }

        /// <summary>
        /// Gets the list of all UserAccess entities for a user from the cache.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>List of UserAccess objects.</returns>
        private async Task<List<UserAccess>> GetUsersUserAccessListFromCache(string email)
        {
            List<UserAccess> accessList = [];
            string cachedAccessList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "usersaccesslist" + email.ToUpper());
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonSerializer.Deserialize<List<UserAccess>>(cachedAccessList, JsonSerializerOptions.Web);
            }

            return accessList;
        }

        /// <summary>
        /// Gets the list of all UserAccess entities for a user from the database and adds it to the cache.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>List of UserAccess objects.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "StringComparison seems to break Db queries.")]
        private async Task<List<UserAccess>> SetUsersUserAccessListInCache(string email)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == email.ToUpper()).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "usersaccesslist" + email.ToUpper(), JsonSerializer.Serialize(accessList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return accessList;
        }

        /// <summary>
        /// Gets a UserAccess entity with the specified AccessId.
        /// First checks the cache, if not found, gets the UserAccess from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The AccessId of the UserAccess to get.</param>
        /// <returns>UserAccess object with the given AccessId. Null if the UserAccess doesn't exist.</returns>
        public async Task<UserAccess> GetUserAccess(int id)
        {
            UserAccess userAccess = await GetUserAccessFromCache(id);
            if (userAccess == null || userAccess.AccessId == 0)
            {
                userAccess = await SetUserAccessInCache(id);
            }

            return userAccess;
        }

        /// <summary>
        /// Gets a UserAccess entity with the specified AccessId from the cache.
        /// </summary>
        /// <param name="id">The AccessId of the UserAccess to get.</param>
        /// <returns>UserAccess object with the given AccessId. Null if the UserAccess item isn't found in the cache.</returns>
        private async Task<UserAccess> GetUserAccessFromCache(int id)
        {
            string cachedUserAccess = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id);
            if (string.IsNullOrEmpty(cachedUserAccess))
            {
                return null;
            }

            UserAccess userAccess = JsonSerializer.Deserialize<UserAccess>(cachedUserAccess, JsonSerializerOptions.Web);
            return userAccess;
        }

        /// <summary>
        /// Gets a UserAccess entity with the specified AccessId from the database and adds it to the cache.
        /// If the UserAccess isn't found in the database, it is removed from the cache to ensure no user will have access by accidentally leaving a cache entry.
        /// </summary>
        /// <param name="id">The AccessId of the UserAccess item to get and set.</param>
        /// <returns>The UserAccess with the given AccessId. Null if the UserAccess item doesn't exist.</returns>
        private async Task<UserAccess> SetUserAccessInCache(int id)
        {
            UserAccess userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.AccessId == id);
            if (userAccess != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id, JsonSerializer.Serialize(userAccess, JsonSerializerOptions.Web), _cacheOptionsSliding);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + userAccess.ProgenyId + userAccess.UserId, JsonSerializer.Serialize(userAccess, JsonSerializerOptions.Web), _cacheOptionsSliding);
            }
            else
            {
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id);
            }

            return userAccess;
        }

        /// <summary>
        /// Adds a new UserAccess entity to the database and adds it to the cache.
        /// Updates the related lists of UserAccesses in the cache too.
        /// If the AccessLevel is 0 or the user's email address is in the Admins property of the Progeny, the user is added to the Admins list.
        /// </summary>
        /// <param name="userAccess">The UserAccess object to add.</param>
        /// <returns>The added UserAccess object.</returns>
        public async Task<UserAccess> AddUserAccess(UserAccess userAccess)
        {
            // If a UserAccess entry with the same user and progeny exists, replace it.
            List<UserAccess> progenyAccessList = await GetUsersUserAccessList(userAccess.UserId);
            UserAccess oldUserAccess = progenyAccessList.SingleOrDefault(u => u.ProgenyId == userAccess.ProgenyId);
            if (oldUserAccess != null)
            {
                await RemoveUserAccess(oldUserAccess.AccessId, oldUserAccess.ProgenyId, oldUserAccess.UserId);
            }

            _ = _context.UserAccessDb.Add(userAccess);
            _ = await _context.SaveChangesAsync();

            _ = await SetUserAccessInCache(userAccess.AccessId);
            _ = await SetUsersUserAccessListInCache(userAccess.UserId);
            _ = await SetProgenyUserAccessListInCache(userAccess.ProgenyId);
            _ = await SetProgenyUserIsAdminInCache(userAccess.UserId);

            if (userAccess.AccessLevel != (int)AccessLevel.Private || userAccess.Progeny.IsInAdminList(userAccess.UserId)) return userAccess;

            userAccess.Progeny.Admins = userAccess.Progeny.Admins + ", " + userAccess.UserId.ToUpper();
            await UpdateProgenyAdmins(userAccess.Progeny);
            return userAccess;
        }

        /// <summary>
        /// Updates a UserAccess entity in the database and the cache.
        /// Updates the related lists of UserAccesses in the cache too.
        /// If the AccessLevel is 0 or the user's email address is in the Admins property of the Progeny, the user is added to the Admins list.
        /// </summary>
        /// <param name="userAccess">The UserAccess object with the updated properties.</param>
        /// <returns>The updated UserAccess object.</returns>
        public async Task<UserAccess> UpdateUserAccess(UserAccess userAccess)
        {
            UserAccess userAccessToUpdate = await _context.UserAccessDb.SingleOrDefaultAsync(ua => ua.AccessId == userAccess.AccessId);
            if (userAccessToUpdate == null) return null;

            userAccessToUpdate.CopyForUpdate(userAccess);

            _ = _context.UserAccessDb.Update(userAccessToUpdate);
            _ = await _context.SaveChangesAsync();

            _ = await SetUserAccessInCache(userAccessToUpdate.AccessId);
            _ = await SetUsersUserAccessListInCache(userAccessToUpdate.UserId);
            _ = await SetProgenyUserAccessListInCache(userAccessToUpdate.ProgenyId);
            _ = await SetProgenyUserIsAdminInCache(userAccessToUpdate.UserId);

            if (userAccess.AccessLevel != (int)AccessLevel.Private || userAccess.Progeny.IsInAdminList(userAccess.UserId)) return userAccessToUpdate;

            userAccess.Progeny.Admins = userAccess.Progeny.Admins + ", " + userAccess.UserId.ToUpper();
            await UpdateProgenyAdmins(userAccess.Progeny);

            return userAccessToUpdate;
        }

        /// <summary>
        /// Removes a UserAccess entity from the database and the cache.
        /// Also updates the related lists of UserAccesses in the cache.
        /// Only admins of a Progeny can remove UserAccess entities that belong to the Progeny.
        /// If the UserAccess entity is an admin of the Progeny, the user is removed from the Admins list.
        /// </summary>
        /// <param name="id">The AccessId of the UserAccess entity to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the UserAccess belongs to.</param>
        /// <param name="userId">The email address of the UserId for the UserAccess to delete.</param>
        /// <returns></returns>
        public async Task RemoveUserAccess(int id, int progenyId, string userId)
        {
            UserAccess deleteUserAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id && u.ProgenyId == progenyId);
            if (deleteUserAccess != null)
            {
                deleteUserAccess.Progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == deleteUserAccess.ProgenyId);
                if (deleteUserAccess.AccessLevel == (int)AccessLevel.Private && deleteUserAccess.Progeny.IsInAdminList(deleteUserAccess.UserId))
                {
                    deleteUserAccess.Progeny = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == deleteUserAccess.ProgenyId);
                    if (deleteUserAccess.Progeny != null)
                    {
                        string[] adminList = deleteUserAccess.Progeny.Admins.Split(',');
                        deleteUserAccess.Progeny.Admins = "";
                        foreach (string adminItem in adminList)
                        {
                            if (!adminItem.Trim().ToUpper().Equals(deleteUserAccess.UserId.Trim().ToUpper()))
                            {
                                deleteUserAccess.Progeny.Admins = deleteUserAccess.Progeny.Admins + ", " + deleteUserAccess.UserId.ToUpper();
                            }
                        }

                        deleteUserAccess.Progeny.Admins = deleteUserAccess.Progeny.Admins.Trim(',');
                        await UpdateProgenyAdmins(deleteUserAccess.Progeny);
                    }

                }

                _ = _context.UserAccessDb.Remove(deleteUserAccess);
                _ = await _context.SaveChangesAsync();

                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id);
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userId);

                _ = await SetUsersUserAccessListInCache(userId);
                _ = await SetProgenyUserAccessListInCache(progenyId);
                _ = await SetProgenyUserIsAdminInCache(userId);
            }
        }

        /// <summary>
        /// Gets the UserAccess entity for a specific User and a Progeny.
        /// First checks the cache, if not found, gets the UserAccess from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the UserAccess.</param>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>UserAccess with the given ProgenyId and email. Null if the UserAccess doesn't exist.</returns>
        public async Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail)
        {
            UserAccess userAccess = await GetProgenyUserAccessForUserFromCache(progenyId, userEmail);
            if (userAccess == null || userAccess.AccessId == 0)
            {
                userAccess = await SetProgenyUserAccessForUserInCache(progenyId, userEmail);
            }

            return userAccess;
        }

        /// <summary>
        /// Gets the validated access level for a user for a Progeny.
        /// </summary>
        /// <param name="progenyId">The Progeny's Id.</param>
        /// <param name="userEmail">The current user's email address.</param>
        /// <param name="itemAccessLevel">Optional access level required for a specific item.</param>
        /// <returns>Integer with the access level.</returns>
        public async Task<CustomResult<int>> GetValidatedAccessLevel(int progenyId, string userEmail, int? itemAccessLevel)
        {
            UserAccess userAccess = await GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId == Constants.DefaultChildId)
            {
                // Default child is always allowed.
                userAccess = await GetProgenyUserAccessForUser(Constants.DefaultChildId, userEmail);
            }

            if (userAccess == null || (itemAccessLevel != null && itemAccessLevel < userAccess.AccessLevel))
            {
                return CustomError.UnauthorizedError("User is not authorized to view this content");
            }
            
            return userAccess.AccessLevel;
        }

        /// <summary>
        /// Gets the UserAccess entity for a specific User and a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the UserAccess.</param>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>UserAccess with the given ProgenyId and email. Null if the UserAccess isn't found in the cache.</returns>
        private async Task<UserAccess> GetProgenyUserAccessForUserFromCache(int progenyId, string userEmail)
        {
            string cachedUserAccess = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userEmail.ToUpper());
            if (string.IsNullOrEmpty(cachedUserAccess))
            {
                return null;
            }

            UserAccess userAccess = JsonSerializer.Deserialize<UserAccess>(cachedUserAccess, JsonSerializerOptions.Web);
            return userAccess;
        }

        /// <summary>
        /// Gets the UserAccess entity for a specific User and a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the UserAccess.</param>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>UserAccess with the given ProgenyId and email. Null if the UserAccess doesn't exist.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "StringComparison seems to break Db queries.")]
        private async Task<UserAccess> SetProgenyUserAccessForUserInCache(int progenyId, string userEmail)
        {
            UserAccess userAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userEmail.ToUpper(), JsonSerializer.Serialize(userAccess, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return userAccess;
        }

        /// <summary>
        /// Updates the Admins property of a Progeny in the database and cache.
        /// </summary>
        /// <param name="progeny">Progeny object with the updated Admins property.</param>
        /// <returns></returns>
        public async Task UpdateProgenyAdmins(Progeny progeny)
        {
            Progeny existingProgeny = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progeny.Id);

            if (existingProgeny != null)
            {
                existingProgeny.Admins = progeny.Admins;
                _ = _context.ProgenyDb.Update(existingProgeny);
                _ = await _context.SaveChangesAsync();

                await SetProgenyInCache(existingProgeny);
            }
        }

        /// <summary>
        /// Updates a Progeny in the cache only.
        /// </summary>
        /// <param name="progeny"></param>
        /// <returns></returns>
        private async Task SetProgenyInCache(Progeny progeny)
        {
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + progeny.Id, JsonSerializer.Serialize(progeny, JsonSerializerOptions.Web), _cacheOptionsSliding);
        }

        /// <summary>
        /// Checks if a user with a given email is in a list of UserAccesses.
        /// </summary>
        /// <param name="accessList">The list of UserAccesses.</param>
        /// <param name="userEmail">The user's email address. Constants.SystemAccountEmail overrides access checks.</param>
        /// <returns>Boolean, true if the user has any kind of access.</returns>
        public bool IsUserInUserAccessList(List<UserAccess> accessList, string userEmail)
        {
            bool allowedAccess = false;
            if(userEmail == Constants.SystemAccountEmail) return true;

            foreach (UserAccess ua in accessList)
            {
                if (ua.UserId.Equals(userEmail, StringComparison.CurrentCultureIgnoreCase))
                {
                    allowedAccess = true;
                }
            }

            return allowedAccess;
        }

        public async Task ConvertUserAccessesToUserGroups()
        {
            // Get the list of progenies.
            List<Progeny> progenyList = await _context.ProgenyDb.AsNoTracking().ToListAsync();

            AccessLevelList accessLevels = new();
            // For each progeny, create a group for each access level, if it doesn't already exist.
            foreach (Progeny progeny in progenyList)
            {
                // For each access level, create a group if it doesn't already exist.
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    // Only allow admins, family, caretakes, and friends.
                    if (accessLevelValue > 3)
                    {
                        continue;
                    }

                    UserGroup userGroup = await _context.UserGroupsDb.SingleOrDefaultAsync(ug => ug.ProgenyId == progeny.Id && ug.Name == accessLevel.Text);
                    if (userGroup == null)
                    {
                        userGroup = new()
                        {
                            ProgenyId = progeny.Id,
                            Name = accessLevel.Text,
                            Description = $"Auto-generated group for access level {accessLevel.Text} for {progeny.NickName}",
                        };
                        _ = _context.UserGroupsDb.Add(userGroup);
                        _ = await _context.SaveChangesAsync();
                    }

                    // Ensure there is a ProgenyPermission entry for the group.
                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        1 => PermissionLevel.Add,
                        _ => PermissionLevel.View,
                    };

                    ProgenyPermission progenyPermission = await _context.ProgenyPermissionsDb.SingleOrDefaultAsync(pp => pp.ProgenyId == progeny.Id && pp.GroupId == userGroup.UserGroupId);
                    if (progenyPermission == null)
                    {
                        progenyPermission = new()
                        {
                            ProgenyId = progeny.Id,
                            GroupId = userGroup.UserGroupId,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow,
                            PermissionLevel = permissionLevel,
                        };
                        _ = _context.ProgenyPermissionsDb.Add(progenyPermission);
                        _ = await _context.SaveChangesAsync();
                    }

                    // Get the list of UserAccess entries for the progeny with the current access level.
                    List<UserAccess> userAccessList = await _context.UserAccessDb.Where(ua => ua.ProgenyId == progeny.Id && ua.AccessLevel == accessLevelValue).ToListAsync();
                    foreach (UserAccess userAccess in userAccessList)
                    {
                        UserInfo userInfo = await _context.UserInfoDb.SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == userAccess.UserId.ToUpper());
                        UserGroupMember existingGroupMember = await _context.UserGroupMembersDb.SingleOrDefaultAsync(ugm => ugm.UserGroupId == userGroup.UserGroupId && ugm.Email.ToUpper() == userAccess.UserId.ToUpper());
                        if (existingGroupMember == null)
                        {
                            UserGroupMember userGroupMember = new()
                            {
                                UserGroupId = userGroup.UserGroupId,
                                Email = userAccess.UserId,
                                UserId = userInfo?.UserId ?? string.Empty,
                                CreatedBy = "system",
                                CreatedTime = DateTime.UtcNow,
                                ModifiedBy = "system",
                                ModifiedTime = DateTime.UtcNow
                            };
                            _ = _context.UserGroupMembersDb.Add(userGroupMember);
                            _ = await _context.SaveChangesAsync();
                        }
                    }
                }
            }
        }

        public async Task<bool> ConvertItemAccessLevelToItemPermissionsForGroups(KinaUnaTypes.TimeLineType timeLineType, int count)
        {
            bool moreItemRemaining = false;
            // For each TimelineItem type, get all items, convert the AccessLevel to ItemPermissions, and save the changes.
            // If the item is AccessLevel 0 (Private), add ItemPermission for Admin group.
            // If the item is AccessLevel 1 (Family), add ItemPermission for Admin and Family groups.
            // If the item is AccessLevel 2 (Caretakers), add ItemPermission for Admin, Family, and Caretakers groups.
            // If the item is AccessLevel 3 (Friends), add ItemPermission for Admin, Family, Caretakers, and Friends groups.
            if (timeLineType == KinaUnaTypes.TimeLineType.Photo)
            {
                moreItemRemaining = await ConvertPicturesAccessLevels(count);
            }

            if (timeLineType == KinaUnaTypes.TimeLineType.Video)
            {
                moreItemRemaining = await ConvertVideosAccessLevels(count);
            }

            if (timeLineType == KinaUnaTypes.TimeLineType.Calendar)
            {
                moreItemRemaining = await ConvertCalendarItemsAccessLevels(count);
            }

            if (timeLineType == KinaUnaTypes.TimeLineType.Vocabulary)
            {
                moreItemRemaining = await ConvertVocabularyItemsAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.Skill)
            {
                moreItemRemaining = await ConvertSkillsAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.Friend)
            {
                moreItemRemaining = await ConvertFriendsAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.Measurement)
            {
                moreItemRemaining = await ConvertMeasurementsAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.Sleep)
            {
                moreItemRemaining = await ConvertSleepAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.Note)
            {
                moreItemRemaining = await ConvertNotesAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.Contact)
            {
                moreItemRemaining = await ConvertContactsAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.Vaccination)
            {
                moreItemRemaining = await ConvertVaccinationsAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.Location)
            {
                moreItemRemaining = await ConvertLocationsAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.TodoItem)
            {
                moreItemRemaining = await ConvertTodoItemsAccessLevels(count);
            }
            if (timeLineType == KinaUnaTypes.TimeLineType.KanbanBoard)
            {
                moreItemRemaining = await ConvertKanbanBoardsAccessLevels(count);
            }


            return moreItemRemaining;
        }

        private async Task<bool> ConvertPicturesAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Picture> items = await _mediaContext.PicturesDb.OrderBy(p => p.PictureId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Picture picture in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (picture.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == picture.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }
                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Photo && tip.ItemId == picture.PictureId && tip.ProgenyId == picture.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Photo,
                        ItemId = picture.PictureId,
                        ProgenyId = picture.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }


                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == picture.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == picture.PictureId && tp.TimelineType == KinaUnaTypes.TimeLineType.Photo && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Photo,
                            ItemId = picture.PictureId,
                            ProgenyId = picture.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                picture.AccessLevel = 99;
                _ = _mediaContext.PicturesDb.Update(picture);
                _ = await _context.SaveChangesAsync();
                _ = await _mediaContext.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertVideosAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Video> items = await _mediaContext.VideoDb.OrderBy(p => p.VideoId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Video video in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (video.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == video.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }
                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Video && tip.ItemId == video.VideoId && tip.ProgenyId == video.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Video,
                        ItemId = video.VideoId,
                        ProgenyId = video.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == video.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == video.VideoId && tp.TimelineType == KinaUnaTypes.TimeLineType.Video && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Video,
                            ItemId = video.VideoId,
                            ProgenyId = video.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                video.AccessLevel = 99;
                _ = _mediaContext.VideoDb.Update(video);
                _ = await _context.SaveChangesAsync();
                _ = await _mediaContext.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertCalendarItemsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<CalendarItem> items = await _context.CalendarDb.OrderBy(p => p.EventId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (CalendarItem calendarItem in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (calendarItem.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == calendarItem.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }
                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Calendar && tip.ItemId == calendarItem.EventId && tip.ProgenyId == calendarItem.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Calendar,
                        ItemId = calendarItem.EventId,
                        ProgenyId = calendarItem.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == calendarItem.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == calendarItem.EventId && tp.TimelineType == KinaUnaTypes.TimeLineType.Calendar && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Calendar,
                            ItemId = calendarItem.EventId,
                            ProgenyId = calendarItem.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                calendarItem.AccessLevel = 99;
                _ = _context.CalendarDb.Update(calendarItem);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertVocabularyItemsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<VocabularyItem> items = await _context.VocabularyDb.OrderBy(p => p.WordId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (VocabularyItem vocabularyItem in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (vocabularyItem.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == vocabularyItem.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Vocabulary && tip.ItemId == vocabularyItem.WordId && tip.ProgenyId == vocabularyItem.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Vocabulary,
                        ItemId = vocabularyItem.WordId,
                        ProgenyId = vocabularyItem.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == vocabularyItem.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == vocabularyItem.WordId && tp.TimelineType == KinaUnaTypes.TimeLineType.Vocabulary && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Vocabulary,
                            ItemId = vocabularyItem.WordId,
                            ProgenyId = vocabularyItem.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                vocabularyItem.AccessLevel = 99;
                _ = _context.VocabularyDb.Update(vocabularyItem);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertSkillsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Skill> items = await _context.SkillsDb.OrderBy(p => p.SkillId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Skill skill in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (skill.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == skill.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Skill && tip.ItemId == skill.SkillId && tip.ProgenyId == skill.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Skill,
                        ItemId = skill.SkillId,
                        ProgenyId = skill.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == skill.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == skill.SkillId && tp.TimelineType == KinaUnaTypes.TimeLineType.Skill && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Skill,
                            ItemId = skill.SkillId,
                            ProgenyId = skill.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                skill.AccessLevel = 99;
                _ = _context.SkillsDb.Update(skill);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertFriendsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Friend> items = await _context.FriendsDb.OrderBy(p => p.FriendId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Friend friend in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (friend.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == friend.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Friend && tip.ItemId == friend.FriendId && tip.ProgenyId == friend.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Friend,
                        ItemId = friend.FriendId,
                        ProgenyId = friend.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == friend.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == friend.FriendId && tp.TimelineType == KinaUnaTypes.TimeLineType.Friend && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Friend,
                            ItemId = friend.FriendId,
                            ProgenyId = friend.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                friend.AccessLevel = 99;
                _ = _context.FriendsDb.Update(friend);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertMeasurementsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Measurement> items = await _context.MeasurementsDb.OrderBy(p => p.MeasurementId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Measurement measurement in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (measurement.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == measurement.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Measurement && tip.ItemId == measurement.MeasurementId && tip.ProgenyId == measurement.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Measurement,
                        ItemId = measurement.MeasurementId,
                        ProgenyId = measurement.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == measurement.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == measurement.MeasurementId && tp.TimelineType == KinaUnaTypes.TimeLineType.Measurement && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Measurement,
                            ItemId = measurement.MeasurementId,
                            ProgenyId = measurement.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }
                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                measurement.AccessLevel = 99;
                _ = _context.MeasurementsDb.Update(measurement);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }
        

        private async Task<bool> ConvertSleepAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Sleep> items = await _context.SleepDb.OrderBy(p => p.SleepId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Sleep sleep in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (sleep.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == sleep.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Sleep && tip.ItemId == sleep.SleepId && tip.ProgenyId == sleep.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Sleep,
                        ItemId = sleep.SleepId,
                        ProgenyId = sleep.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == sleep.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == sleep.SleepId && tp.TimelineType == KinaUnaTypes.TimeLineType.Sleep && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Sleep,
                            ItemId = sleep.SleepId,
                            ProgenyId = sleep.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                sleep.AccessLevel = 99;
                _ = _context.SleepDb.Update(sleep);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        

        private async Task<bool> ConvertNotesAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Note> items = await _context.NotesDb.OrderBy(p => p.NoteId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Note note in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (note.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == note.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Note && tip.ItemId == note.NoteId && tip.ProgenyId == note.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };
                    
                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Note,
                        ItemId = note.NoteId,
                        ProgenyId = note.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == note.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == note.NoteId && tp.TimelineType == KinaUnaTypes.TimeLineType.Note && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Note,
                            ItemId = note.NoteId,
                            ProgenyId = note.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                note.AccessLevel = 99;
                _ = _context.NotesDb.Update(note);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }
        
        private async Task<bool> ConvertContactsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Contact> items = await _context.ContactsDb.OrderBy(p => p.ContactId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Contact contact in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (contact.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == contact.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Contact && tip.ItemId == contact.ContactId && tip.ProgenyId == contact.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Contact,
                        ItemId = contact.ContactId,
                        ProgenyId = contact.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == contact.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == contact.ContactId && tp.TimelineType == KinaUnaTypes.TimeLineType.Contact && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Contact,
                            ItemId = contact.ContactId,
                            ProgenyId = contact.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }
                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                contact.AccessLevel = 99;
                _ = _context.ContactsDb.Update(contact);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertVaccinationsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Vaccination> items = await _context.VaccinationsDb.OrderBy(p => p.VaccinationId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Vaccination vaccination in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (vaccination.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == vaccination.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Vaccination && tip.ItemId == vaccination.VaccinationId && tip.ProgenyId == vaccination.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Vaccination,
                        ItemId = vaccination.VaccinationId,
                        ProgenyId = vaccination.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == vaccination.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == vaccination.VaccinationId && tp.TimelineType == KinaUnaTypes.TimeLineType.Vaccination && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Vaccination,
                            ItemId = vaccination.VaccinationId,
                            ProgenyId = vaccination.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                vaccination.AccessLevel = 99;
                _ = _context.VaccinationsDb.Update(vaccination);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertLocationsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<Location> items = await _context.LocationsDb.OrderBy(p => p.LocationId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (Location location in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (location.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == location.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.Location && tip.ItemId == location.LocationId && tip.ProgenyId == location.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.Location,
                        ItemId = location.LocationId,
                        ProgenyId = location.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == location.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == location.LocationId && tp.TimelineType == KinaUnaTypes.TimeLineType.Location && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.Location,
                            ItemId = location.LocationId,
                            ProgenyId = location.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                location.AccessLevel = 99;
                _ = _context.LocationsDb.Update(location);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertTodoItemsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<TodoItem> items = await _context.TodoItemsDb.OrderBy(p => p.TodoItemId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (TodoItem todoItem in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (todoItem.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == todoItem.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.TodoItem && tip.ItemId == todoItem.TodoItemId && tip.ProgenyId == todoItem.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.TodoItem,
                        ItemId = todoItem.TodoItemId,
                        ProgenyId = todoItem.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == todoItem.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == todoItem.TodoItemId && tp.TimelineType == KinaUnaTypes.TimeLineType.TodoItem && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.TodoItem,
                            ItemId = todoItem.TodoItemId,
                            ProgenyId = todoItem.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                todoItem.AccessLevel = 99;
                _ = _context.TodoItemsDb.Update(todoItem);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }

        private async Task<bool> ConvertKanbanBoardsAccessLevels(int count)
        {
            AccessLevelList accessLevels = new();
            List<KanbanBoard> items = await _context.KanbanBoardsDb.OrderBy(p => p.KanbanBoardId).Where(p => p.AccessLevel < 10).Take(count).ToListAsync();
            if (items.Count == 0)
            {
                return false;
            }
            // For each item, create ItemPermissions for each access level up to and including the item's AccessLevel.
            foreach (KanbanBoard kanbanBoard in items)
            {
                foreach (SelectListItem accessLevel in accessLevels.AccessLevelListEn)
                {
                    if (!int.TryParse(accessLevel.Value, out int accessLevelValue))
                    {
                        continue;
                    }

                    if (kanbanBoard.AccessLevel < accessLevelValue)
                    {
                        continue;
                    }

                    string groupName = accessLevel.Text;
                    UserGroup userGroup = await _context.UserGroupsDb.AsNoTracking().SingleOrDefaultAsync(ug => ug.ProgenyId == kanbanBoard.ProgenyId && ug.Name == groupName);
                    if (userGroup == null)
                    {
                        continue;
                    }

                    TimelineItemPermission existingPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tip => tip.TimelineType == KinaUnaTypes.TimeLineType.KanbanBoard && tip.ItemId == kanbanBoard.KanbanBoardId && tip.ProgenyId == kanbanBoard.ProgenyId && tip.GroupId == userGroup.UserGroupId);

                    if (existingPermission != null)
                    {
                        continue;
                    }

                    PermissionLevel permissionLevel = accessLevelValue switch
                    {
                        0 => PermissionLevel.Admin,
                        _ => PermissionLevel.View,
                    };

                    TimelineItemPermission timelineItemPermission = new()
                    {
                        TimelineType = KinaUnaTypes.TimeLineType.KanbanBoard,
                        ItemId = kanbanBoard.KanbanBoardId,
                        ProgenyId = kanbanBoard.ProgenyId,
                        GroupId = userGroup.UserGroupId,
                        InheritPermissions = false,
                        PermissionLevel = permissionLevel,
                        CreatedBy = "system",
                        CreatedTime = DateTime.UtcNow,
                        ModifiedBy = "system",
                        ModifiedTime = DateTime.UtcNow
                    };
                    _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                }

                // For admins, create explicit user ItemPermissions, if they don't already exist.
                Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == kanbanBoard.ProgenyId);
                List<string> adminList = progeny.GetAdminsList();
                foreach (string adminEmail in adminList)
                {
                    UserInfo adminUserInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(ui => ui.UserEmail.ToUpper() == adminEmail.ToUpper());
                    TimelineItemPermission existingAdminPermission = await _context.TimelineItemPermissionsDb.AsNoTracking()
                        .SingleOrDefaultAsync(tp => tp.ProgenyId == progeny.Id && tp.ItemId == kanbanBoard.KanbanBoardId && tp.TimelineType == KinaUnaTypes.TimeLineType.KanbanBoard && tp.Email.ToUpper() == adminEmail.ToUpper());
                    if (existingAdminPermission == null)
                    {
                        TimelineItemPermission timelineItemPermission = new()
                        {
                            TimelineType = KinaUnaTypes.TimeLineType.KanbanBoard,
                            ItemId = kanbanBoard.KanbanBoardId,
                            ProgenyId = kanbanBoard.ProgenyId,
                            Email = adminEmail,
                            UserId = adminUserInfo?.UserId ?? string.Empty,
                            InheritPermissions = false,
                            PermissionLevel = PermissionLevel.Admin,
                            CreatedBy = "system",
                            CreatedTime = DateTime.UtcNow,
                            ModifiedBy = "system",
                            ModifiedTime = DateTime.UtcNow
                        };
                        _ = _context.TimelineItemPermissionsDb.Add(timelineItemPermission);
                    }
                }

                // Set the AccessLevel to 99 (Custom) to indicate that the item now has custom permissions.
                kanbanBoard.AccessLevel = 99;
                _ = _context.KanbanBoardsDb.Update(kanbanBoard);
                _ = await _context.SaveChangesAsync();

            }
            // Are there more items remaining to process?
            return items.Count >= count;
        }
    }
}
