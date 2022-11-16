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
    public class UserAccessService: IUserAccessService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public UserAccessService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<List<Progeny>> GetProgenyUserIsAdmin(string email)
        {
            List<Progeny> progenyList = await GetProgenyUserIsAdminFromCache(email);
            if (progenyList == null || progenyList.Count == 0)
            {
                progenyList = await SetProgenyUserIsAdminInCache(email);
            }
            
            return progenyList;
        }

        private async Task<List<Progeny>> GetProgenyUserIsAdminFromCache(string email)
        {
            List<Progeny> progenyList = new List<Progeny>();
            string cachedProgenyList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progenywhereadmin" + email);
            if (!string.IsNullOrEmpty(cachedProgenyList))
            {
                progenyList = JsonConvert.DeserializeObject<List<Progeny>>(cachedProgenyList);
            }

            return progenyList;
        }

        public async Task<List<Progeny>> SetProgenyUserIsAdminInCache(string email)
        {
            List<Progeny> progenyList = await _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(email)).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenywhereadmin" + email, JsonConvert.SerializeObject(progenyList), _cacheOptionsSliding);
            return progenyList;
        }
        
        public async Task<List<UserAccess>> GetProgenyUserAccessList(int progenyId)
        {
            List<UserAccess> accessList = await GetProgenyUserAccessListFromCache(progenyId);
            
            if (accessList == null || accessList.Count == 0)
            {
                accessList = await SetProgenyUserAccessListInCache(progenyId);
            }

            return accessList;
        }

        private async Task<List<UserAccess>> GetProgenyUserAccessListFromCache(int progenyId)
        {
            List<UserAccess> accessList = new List<UserAccess>();
            string cachedAccessList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "accessList" + progenyId);
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }

            return accessList;
        }
        
        public async Task<List<UserAccess>> SetProgenyUserAccessListInCache(int progenyId)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "accessList" + progenyId, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            
            return accessList;
        }

        public async Task<List<UserAccess>> GetUsersUserAccessList(string email)
        {
            List<UserAccess> accessList = await GetUsersUserAccessListFromCache(email);
            if (accessList == null || accessList.Count == 0)
            {
                accessList = await SetUsersUserAccessListInCache(email);
            }

            return accessList;
        }

        public async Task<List<UserAccess>> GetUsersUserAccessListFromCache(string email)
        {
            List<UserAccess> accessList = new List<UserAccess>();
            string cachedAccessList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "usersaccesslist" + email.ToUpper());
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }

            return accessList;
        }

        public async Task<List<UserAccess>> SetUsersUserAccessListInCache(string email)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == email.ToUpper()).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "usersaccesslist" + email.ToUpper(), JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            
            return accessList;
        }

        public async Task<UserAccess> GetUserAccess(int id)
        {
            UserAccess userAccess = await GetUserAccessFromCache(id);
            if (userAccess == null || userAccess.AccessId == 0)
            {
                userAccess = await SetUserAccessInCache(id);
            }

            return userAccess;
        }

        private async Task<UserAccess> GetUserAccessFromCache(int id)
        {
            UserAccess userAccess = new UserAccess();
            string cachedUserAccess = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }

            return userAccess;
        }

        public async Task<UserAccess> SetUserAccessInCache(int id)
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

        public async Task<UserAccess> AddUserAccess(UserAccess userAccess)
        {
            await _context.UserAccessDb.AddAsync(userAccess);
            await _context.SaveChangesAsync();

            await SetUserAccessInCache(userAccess.AccessId);
            await SetUsersUserAccessListInCache(userAccess.UserId);
            await SetProgenyUserAccessListInCache(userAccess.ProgenyId);
            await SetProgenyUserIsAdminInCache(userAccess.UserId);
            return userAccess;
        }

        public async Task<UserAccess> UpdateUserAccess(UserAccess userAccess)
        {
            UserAccess userAccessToUpdate = await _context.UserAccessDb.SingleOrDefaultAsync(ua => ua.AccessId == userAccess.AccessId);
            if (userAccessToUpdate != null)
            {
                userAccessToUpdate.UserId = userAccess.UserId;
                userAccessToUpdate.Progeny = userAccess.Progeny;
                userAccessToUpdate.AccessLevel = userAccess.AccessLevel;
                userAccessToUpdate.AccessLevelString = userAccess.AccessLevelString;
                userAccessToUpdate.CanContribute = userAccess.CanContribute;
                userAccessToUpdate.ProgenyId = userAccess.ProgenyId;
                userAccessToUpdate.User = userAccess.User;

                _context.UserAccessDb.Update(userAccessToUpdate);
                await _context.SaveChangesAsync();

                await SetUserAccessInCache(userAccessToUpdate.AccessId);
                await SetUsersUserAccessListInCache(userAccessToUpdate.UserId);
                await SetProgenyUserAccessListInCache(userAccessToUpdate.ProgenyId);
                await SetProgenyUserIsAdminInCache(userAccessToUpdate.UserId);
            }

            return userAccessToUpdate;
        }

        public async Task RemoveUserAccess(int id, int progenyId, string userId)
        {
            UserAccess deleteUserAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id && u.ProgenyId == progenyId);
            if (deleteUserAccess != null)
            {
                _context.UserAccessDb.Remove(deleteUserAccess);
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id);
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userId);
                await SetUsersUserAccessListInCache(userId);
                await SetProgenyUserAccessListInCache(progenyId);
                await SetProgenyUserIsAdminInCache(userId);
            }
        }

        public async Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail)
        {
            UserAccess userAccess = await GetProgenyUserAccessForUserFromCache(progenyId, userEmail);
            if (userAccess == null || userAccess.AccessId == 0)
            {
                userAccess = await SetProgenyUserAccessForUserInCache(progenyId, userEmail);
            }
            
            return userAccess;
        }

        private async Task<UserAccess> GetProgenyUserAccessForUserFromCache(int progenyId, string userEmail)
        {
            UserAccess userAccess = new UserAccess();
            string cachedUserAccess = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userEmail.ToUpper());
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }

            return userAccess;
        }

        private async Task<UserAccess> SetProgenyUserAccessForUserInCache(int progenyId, string userEmail)
        {
            UserAccess userAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userEmail.ToUpper(), JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);

            return userAccess;
        }

        public async Task UpdateProgenyAdmins(Progeny progeny)
        {
            Progeny existingProgeny = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progeny.Id);

            if (existingProgeny != null)
            {
                existingProgeny.Admins = progeny.Admins;
                _context.ProgenyDb.Update(existingProgeny);
                await _context.SaveChangesAsync();

                await SetProgenyInCache(existingProgeny);
            }
        }

        private async Task SetProgenyInCache(Progeny progeny)
        {
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + progeny.Id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
        }
    }
}
