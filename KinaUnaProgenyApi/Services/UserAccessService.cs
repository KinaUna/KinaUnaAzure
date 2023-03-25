using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
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
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

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
            List<Progeny> progenyList = new();
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
            List<UserAccess> accessList = new();
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

        public async Task<List<UserAccess>> GetUsersUserAdminAccessList(string email)
        {
            List<UserAccess> userAccessList = await GetUsersUserAccessList(email);
            userAccessList = userAccessList.Where(u => u.AccessLevel == 0).ToList();

            return userAccessList;
        }

        private async Task<List<UserAccess>> GetUsersUserAccessListFromCache(string email)
        {
            List<UserAccess> accessList = new();
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
            UserAccess userAccess = new();
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
            // If a UserAccess entry with the same user and progeny exists, replace it.
            List<UserAccess> progenyAccessList = await GetUsersUserAccessList(userAccess.UserId);
            UserAccess oldUserAccess = progenyAccessList.SingleOrDefault(u => u.ProgenyId == userAccess.ProgenyId);
            if (oldUserAccess != null)
            {
                await RemoveUserAccess(oldUserAccess.AccessId, oldUserAccess.ProgenyId, oldUserAccess.UserId);
            }

            _ = await _context.UserAccessDb.AddAsync(userAccess);
            _ = await _context.SaveChangesAsync();

            _ = await SetUserAccessInCache(userAccess.AccessId);
            _ = await SetUsersUserAccessListInCache(userAccess.UserId);
            _ = await SetProgenyUserAccessListInCache(userAccess.ProgenyId);
            _ = await SetProgenyUserIsAdminInCache(userAccess.UserId);

            if (userAccess.AccessLevel == (int)AccessLevel.Private && !userAccess.Progeny.IsInAdminList(userAccess.UserId))
            {
                userAccess.Progeny.Admins = userAccess.Progeny.Admins + ", " + userAccess.UserId.ToUpper();
                await UpdateProgenyAdmins(userAccess.Progeny);
            }
            return userAccess;
        }

        public async Task<UserAccess> UpdateUserAccess(UserAccess userAccess)
        {
            UserAccess userAccessToUpdate = await _context.UserAccessDb.SingleOrDefaultAsync(ua => ua.AccessId == userAccess.AccessId);
            if (userAccessToUpdate != null)
            {
                userAccessToUpdate.CopyForUpdate(userAccess);

                _ = _context.UserAccessDb.Update(userAccessToUpdate);
                _ = await _context.SaveChangesAsync();

                _ = await SetUserAccessInCache(userAccessToUpdate.AccessId);
                _ = await SetUsersUserAccessListInCache(userAccessToUpdate.UserId);
                _ = await SetProgenyUserAccessListInCache(userAccessToUpdate.ProgenyId);
                _ = await SetProgenyUserIsAdminInCache(userAccessToUpdate.UserId);

                if (userAccess.AccessLevel == (int)AccessLevel.Private && !userAccess.Progeny.IsInAdminList(userAccess.UserId))
                {

                    userAccess.Progeny.Admins = userAccess.Progeny.Admins + ", " + userAccess.UserId.ToUpper();
                    await UpdateProgenyAdmins(userAccess.Progeny);
                }
            }

            return userAccessToUpdate;
        }

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
            UserAccess userAccess = new();
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
                _ = _context.ProgenyDb.Update(existingProgeny);
                _ = await _context.SaveChangesAsync();

                await SetProgenyInCache(existingProgeny);
            }
        }

        private async Task SetProgenyInCache(Progeny progeny)
        {
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + progeny.Id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
        }
    }
}
