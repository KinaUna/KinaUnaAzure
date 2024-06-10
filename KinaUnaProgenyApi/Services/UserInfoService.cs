using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

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

        public async Task<List<UserInfo>> GetAllUserInfos()
        {
            List<UserInfo> userinfo = await _context.UserInfoDb.ToListAsync();
            
            return userinfo;
        }

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

        private async Task<UserInfo> GetUserInfoByEmailFromCache(string userEmail)
        {
            UserInfo userinfo = new();
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + userEmail.ToUpper());
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }

            return userinfo;
        }

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

        private async Task<UserInfo> GetUserInfoByUserIdFromCache(string userId)
        {
            UserInfo userinfo = new();
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + userId);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }

            return userinfo;
        }

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

        private async Task<UserInfo> SetUserInfoById(int id)
        {
            UserInfo userinfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
            if (userinfo != null)
            {
                _ = await SetUserInfoByEmail(userinfo.UserEmail);
            }

            return userinfo;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "StringComparison seems to break Db queries.")]
        private async Task<UserInfo> SetUserInfoByUserId(string userId)
        {
            UserInfo userinfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId.ToUpper() == userId.ToUpper());
            if (userinfo != null)
            {
                _ = await SetUserInfoByEmail(userinfo.UserEmail);
            }

            return userinfo;
        }

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
            userInfoToUpdate.IsPivoqAdmin = userInfo.IsPivoqAdmin;
            userInfoToUpdate.IsKinaUnaUser = userInfo.IsKinaUnaUser;
            userInfoToUpdate.IsPivoqUser = userInfo.IsPivoqUser;
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

        public async Task RemoveUserInfoByEmail(string userEmail, string userId, int userInfoId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + userEmail.ToUpper());
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + userId);
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + userInfoId);
        }

        public async Task<UserInfo> GetUserInfoById(int id)
        {
            UserInfo userinfo = await GetUserInfoByIdFromCache(id);
            if (userinfo == null || userinfo.Id == 0)
            {
                userinfo = await SetUserInfoById(id);
            }

            return userinfo;
        }

        public async Task<UserInfo> GetUserInfoByUserId(string id)
        {
            UserInfo userinfo = await GetUserInfoByUserIdFromCache(id);
            if (userinfo == null || userinfo.Id == 0)
            {
                userinfo = await SetUserInfoByUserId(id);
            }

            return userinfo;
        }

        public async Task<List<UserInfo>> GetDeletedUserInfos()
        {
            List<UserInfo> deletedUserInfos = await _context.UserInfoDb.AsNoTracking().Where(u => u.Deleted).ToListAsync();
            return deletedUserInfos;
        }

        public async Task<bool> IsAdminUserId(string userId)
        {
            UserInfo userInfo = await _context.UserInfoDb.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userId);
            return userInfo != null && userInfo.IsKinaUnaAdmin;
        }
    }
}
