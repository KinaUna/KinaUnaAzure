using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services.UserAccessService
{
    public class UserAccessService : IUserAccessService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public UserAccessService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
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
                progenyList = JsonConvert.DeserializeObject<List<Progeny>>(cachedProgenyList);
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
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenywhereadmin" + email, JsonConvert.SerializeObject(progenyList), _cacheOptionsSliding);
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
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
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
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "accessList" + progenyId, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);

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
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
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
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "usersaccesslist" + email.ToUpper(), JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);

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

            UserAccess userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
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
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + userAccess.ProgenyId + userAccess.UserId, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
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

            UserAccess userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
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
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userEmail.ToUpper(), JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);

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
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + progeny.Id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
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
                if (ua.UserId.Equals(userEmail, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    allowedAccess = true;
                }
            }

            return allowedAccess;
        }
    }
}
