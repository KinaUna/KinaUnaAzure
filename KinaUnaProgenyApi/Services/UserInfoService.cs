using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    public class UserInfoService : IUserInfoService
    {
        private readonly ProgenyDbContext _context;
        private readonly IImageStore _imageStore;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public UserInfoService(ProgenyDbContext context, IDistributedCache cache, IImageStore imageStore)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
            _imageStore = imageStore;
        }

        /// <summary>
        /// Gets a list of all UserInfos in the database.
        /// </summary>
        /// <returns>List of UserInfo objects.</returns>
        public async Task<List<UserInfo>> GetAllUserInfos()
        {
            List<UserInfo> userinfo = await _context.UserInfoDb.ToListAsync();
            
            return userinfo;
        }

        /// <summary>
        /// Gets a UserInfo object by email address.
        /// First checks the cache, if not found, gets the UserInfo from the database and adds it to the cache.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>UserInfo object with the given email address. Null if the UserInfo doesn't exist.</returns>
        public async Task<UserInfo> GetUserInfoByEmail(string userEmail)
        {
            userEmail = userEmail.Trim();

            UserInfo userinfo = await GetUserInfoByEmailFromCache(userEmail);
            if (userinfo == null || userinfo.Id == 0)
            {
                userinfo = await SetUserInfoByEmail(userEmail);
            }

            return userinfo;
        }

        /// <summary>
        /// Adds a new UserInfo to the database.
        /// </summary>
        /// <param name="userInfo">The UserInfo entity to add.</param>
        /// <returns>The added UserInfo object.</returns>
        public async Task<UserInfo> AddUserInfo(UserInfo userInfo)
        {
            if (string.IsNullOrEmpty(userInfo.FirstName))
            {
                userInfo.FirstName = "";
            }

            if (string.IsNullOrEmpty(userInfo.MiddleName))
            {
                userInfo.MiddleName = "";
            }

            if (string.IsNullOrEmpty(userInfo.LastName))
            {
                userInfo.LastName = "";
            }

            if (string.IsNullOrEmpty(userInfo.ProfilePicture))
            {
                userInfo.ProfilePicture = Constants.ProfilePictureUrl;
            }
            _ = _context.UserInfoDb.Add(userInfo);
            _ = await _context.SaveChangesAsync();
            _ = await SetUserInfoByEmail(userInfo.UserEmail);

            return userInfo;
        }

        /// <summary>
        /// Gets a UserInfo object from the cache by email address.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>The UserInfo object with the given email address. Null if the UserInfo item isn't found in the cache.</returns>
        private async Task<UserInfo> GetUserInfoByEmailFromCache(string userEmail)
        {
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + userEmail.ToUpper());
            if (string.IsNullOrEmpty(cachedUserInfo))
            {
                return null;
                
            }

            UserInfo userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            return userinfo;
        }

        /// <summary>
        /// Gets a UserInfo object from the cache by Id.
        /// First checks the cache, if not found, gets the UserInfo from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The Id of the UserInfo entity to get.</param>
        /// <returns>The UserInfo object with the given Id. Null if the UserInfo doesn't exist.</returns>
        private async Task<UserInfo> GetUserInfoByIdFromCache(int id)
        {
            UserInfo userinfo = new();
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }

            return userinfo;
        }

        /// <summary>
        /// Gets a UserInfo object from the cache by UserId.
        /// </summary>
        /// <param name="userId">The Id of the UserInfo item to get.</param>
        /// <returns>The UserInfo object with the given Id. Null if the UserInfo item isn't found in the cache.</returns>
        private async Task<UserInfo> GetUserInfoByUserIdFromCache(string userId)
        {
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + userId);
            if (string.IsNullOrEmpty(cachedUserInfo))
            {
                return null;
            }

            UserInfo userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            return userinfo;
        }

        /// <summary>
        /// Gets a UserInfo entity from the database by email address and adds it to the cache.
        /// Also updates the cache for UserInfoById and UserInfoByUserId.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <returns>The UserInfo with the given email address. Null if the UserInfo item doesn't exist.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "StringComparison seems to break Db queries.")]
        public async Task<UserInfo> SetUserInfoByEmail(string userEmail)
        {
            UserInfo userinfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            if (userinfo == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + userEmail.ToUpper(), JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + userinfo.UserId, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + userinfo.Id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);

            return userinfo;
        }

        /// <summary>
        /// Gets a UserInfo entity from the database by Id and adds it to the cache.
        /// </summary>
        /// <param name="id">The Id of the UserInfo item to get and set.</param>
        /// <returns>The UserInfo object with the given Id. Null if the UserInfo item doesn't exist.</returns>
        private async Task<UserInfo> SetUserInfoById(int id)
        {
            UserInfo userinfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
            if (userinfo != null)
            {
                _ = await SetUserInfoByEmail(userinfo.UserEmail);
            }

            return userinfo;
        }

        /// <summary>
        /// Gets a UserInfo entity from the database by UserId and adds it to the cache.
        /// The UserId is the User's Id from the Identity database.
        /// </summary>
        /// <param name="userId">The UserId of the UserInfo entity to get and set.</param>
        /// <returns>The UserInfo with the given UserId. Null if the UserInfo item doesn't exist.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "StringComparison seems to break Db queries.")]
        private async Task<UserInfo> SetUserInfoByUserId(string userId)
        {
            UserInfo userinfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId.ToUpper() == userId.ToUpper());
            if (userinfo == null)
            {
                return null;
            }

            _ = await SetUserInfoByEmail(userinfo.UserEmail);
            return userinfo;
        }

        /// <summary>
        /// Updates a UserInfo entity in the database and the cache.
        /// </summary>
        /// <param name="userInfo">The UserInfo object with the updated properties.</param>
        /// <returns>The updated UserInfo object.</returns>
        public async Task<UserInfo> UpdateUserInfo(UserInfo userInfo)
        {
            UserInfo userInfoToUpdate = await _context.UserInfoDb.SingleOrDefaultAsync(ui => ui.Id == userInfo.Id);
            if (userInfoToUpdate == null) return null;

            string oldPictureLink = userInfoToUpdate.ProfilePicture;
                
            userInfoToUpdate.UserEmail = userInfo.UserEmail;
            userInfoToUpdate.UserId = userInfo.UserId;
            userInfoToUpdate.UserName = userInfo.UserName;
            userInfoToUpdate.ViewChild = userInfo.ViewChild;
            userInfoToUpdate.FirstName = userInfo.FirstName ?? "";
            userInfoToUpdate.MiddleName = userInfo.MiddleName ?? "";
            userInfoToUpdate.LastName = userInfo.LastName ?? "";
            userInfoToUpdate.ProfilePicture = userInfo.ProfilePicture;
            userInfoToUpdate.Timezone = userInfo.Timezone;
            userInfoToUpdate.PhoneNumber = userInfo.PhoneNumber ?? "";
            userInfoToUpdate.CanUserAddItems = userInfo.CanUserAddItems;
            userInfoToUpdate.Deleted = userInfo.Deleted;
            userInfoToUpdate.DeletedTime = userInfo.DeletedTime;
            userInfoToUpdate.IsKinaUnaAdmin = userInfo.IsKinaUnaAdmin;
            userInfoToUpdate.UpdateIsAdmin = userInfo.UpdateIsAdmin;
            userInfoToUpdate.ProgenyList = userInfo.ProgenyList;
            userInfoToUpdate.AccessList = userInfo.AccessList;
            userInfoToUpdate.UpdatedTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(userInfo.ProfilePicture))
            {
                userInfo.ProfilePicture = Constants.ProfilePictureUrl;
            }
                
            _ = _context.UserInfoDb.Update(userInfoToUpdate);
            _ = await _context.SaveChangesAsync();

            if (oldPictureLink != userInfo.ProfilePicture)
            {
                await _imageStore.DeleteImage(oldPictureLink, BlobContainers.Profiles);
            }

            _ = await SetUserInfoByEmail(userInfo.UserEmail);


            return userInfoToUpdate;
        }

        /// <summary>
        /// Deletes a UserInfo entity from the database and the cache.
        /// This is a hard delete, to soft delete a UserInfo entity, use the UpdateUserInfo method and set the Deleted property to true.
        /// </summary>
        /// <param name="userInfo">The UserInfo object to delete.</param>
        /// <returns>The deleted UserInfo object.</returns>
        public async Task<UserInfo> DeleteUserInfo(UserInfo userInfo)
        {
            UserInfo userInfoToDelete = await _context.UserInfoDb.SingleOrDefaultAsync(ui => ui.Id == userInfo.Id);
            if (userInfoToDelete != null)
            {
                _context.UserInfoDb.Remove(userInfoToDelete);
                await _context.SaveChangesAsync();
            }

            await RemoveUserInfoByEmail(userInfo.UserEmail, userInfo.UserId, userInfo.Id);

            _ = await _imageStore.DeleteImage(userInfo.ProfilePicture, BlobContainers.Profiles);

            return userInfo;
        }

        /// <summary>
        /// Removes a UserInfo entity from UserInfoByEmail, UserInfoByUserId and UserInfoById caches.
        /// </summary>
        /// <param name="userEmail">The user's email address.</param>
        /// <param name="userId">The user's UserId</param>
        /// <param name="userInfoId">The UserInfo's Id.</param>
        /// <returns></returns>
        public async Task RemoveUserInfoByEmail(string userEmail, string userId, int userInfoId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + userEmail.ToUpper());
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + userId);
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + userInfoId);
        }

        /// <summary>
        /// Gets a UserInfo entity by Id.
        /// First checks the cache, if not found, gets the UserInfo from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The Id of the UserInfo item to get.</param>
        /// <returns>The UserInfo object with the given Id. Null if the UserInfo item doesn't exist.</returns>
        public async Task<UserInfo> GetUserInfoById(int id)
        {
            UserInfo userinfo = await GetUserInfoByIdFromCache(id);
            if (userinfo == null || userinfo.Id == 0)
            {
                userinfo = await SetUserInfoById(id);
            }

            return userinfo;
        }

        /// <summary>
        /// Gets a UserInfo entity by UserId.
        /// First checks the cache, if not found, gets the UserInfo from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The UserId of the UserInfo entity to get.</param>
        /// <returns>The UserInfo object with the given UserId. Null if the UserInfo item doesn't exist.</returns>
        public async Task<UserInfo> GetUserInfoByUserId(string id)
        {
            UserInfo userinfo = await GetUserInfoByUserIdFromCache(id);
            if (userinfo == null || userinfo.Id == 0)
            {
                userinfo = await SetUserInfoByUserId(id);
            }

            return userinfo;
        }

        /// <summary>
        /// Gets a list of all UserInfos that have been marked as deleted.
        /// </summary>
        /// <returns>List of UserInfo objects.</returns>
        public async Task<List<UserInfo>> GetDeletedUserInfos()
        {
            List<UserInfo> deletedUserInfos = await _context.UserInfoDb.AsNoTracking().Where(u => u.Deleted).ToListAsync();
            return deletedUserInfos;
        }


        public async Task<UserInfo> AddUserInfoToDeletedUserInfos(UserInfo userInfo)
        {
            UserInfo userInfoToAddToDelete = await _context.DeletedUsers.SingleOrDefaultAsync(u => u.UserId == userInfo.UserId);
            
            if (userInfoToAddToDelete != null && !string.IsNullOrEmpty(userInfoToAddToDelete.UserId))
            {
                userInfoToAddToDelete.UserName = userInfo.UserName;
                userInfoToAddToDelete.UserId = userInfo.UserId;
                userInfoToAddToDelete.UserEmail = userInfo.UserEmail;
                userInfoToAddToDelete.Deleted = false;
                userInfoToAddToDelete.DeletedTime = DateTime.UtcNow;
                userInfoToAddToDelete.UpdatedTime = DateTime.UtcNow;
                userInfoToAddToDelete.ProfilePicture = JsonConvert.SerializeObject(userInfo);
                _ = _context.DeletedUsers.Update(userInfoToAddToDelete);
            }
            else
            {
                userInfoToAddToDelete = new UserInfo
                {
                    UserName = userInfo.UserName,
                    UserId = userInfo.UserId,
                    UserEmail = userInfo.UserEmail,
                    Deleted = false,
                    DeletedTime = DateTime.UtcNow,
                    UpdatedTime = DateTime.UtcNow,
                    ProfilePicture = JsonConvert.SerializeObject(userInfo)
                };
                _ = _context.DeletedUsers.Add(userInfoToAddToDelete);
            }

            _ = await _context.SaveChangesAsync();

            return userInfoToAddToDelete;
        }

        /// <summary>
        /// Removes the specified user information from the collection of deleted user information.
        /// </summary>
        /// <remarks>This does not update the original userinfo entity. This method performs an asynchronous operation to remove the specified user
        /// information from the collection  of deleted user information. If the user information is not found, the
        /// method completes successfully and  returns <see langword="null"/>.</remarks>
        /// <param name="userInfo">The user information to be removed. This parameter cannot be <see langword="null"/>.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the  <see
        /// cref="UserInfo"/> object that was removed, or <see langword="null"/> if the specified user information  was
        /// not found in the collection.</returns>
        public async Task<UserInfo> RemoveUserInfoFromDeletedUserInfos(UserInfo userInfo)
        {
            UserInfo deletedUserInfo = _context.DeletedUsers.SingleOrDefault(u => u.UserId == userInfo.UserId);
            if (deletedUserInfo == null)
            {
                return null;
            }
            
            _ = _context.DeletedUsers.Remove(deletedUserInfo);
            _ = await _context.SaveChangesAsync();

            return deletedUserInfo;

        }

        public async Task<UserInfo> UpdateDeletedUserInfo(UserInfo userInfo)
        {
            UserInfo userInfoToUpdate = await _context.DeletedUsers.SingleOrDefaultAsync(ui => ui.Id == userInfo.Id);
            if (userInfoToUpdate == null) return null;

            userInfoToUpdate.Deleted = userInfo.Deleted;
            userInfoToUpdate.DeletedTime = userInfo.DeletedTime;
            userInfoToUpdate.UpdatedTime = userInfo.UpdatedTime;
            _ = _context.DeletedUsers.Update(userInfoToUpdate);
            _ = await _context.SaveChangesAsync();

            return userInfoToUpdate;
        }

        /// <summary>
        /// Checks if the user is a KinaUna admin.
        /// </summary>
        /// <param name="userId">The user's UserId.</param>
        /// <returns>Boolean, true if the user is a KinaUna admin.</returns>
        public async Task<bool> IsAdminUserId(string userId)
        {
            UserInfo userInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userId);
            return userInfo != null && userInfo.IsKinaUnaAdmin;
        }
    }
}
