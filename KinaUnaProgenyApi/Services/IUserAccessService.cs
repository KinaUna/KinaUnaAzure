using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IUserAccessService
    {
        Task<List<Progeny>> GetProgenyUserIsAdmin(string email);
        Task<List<Progeny>> SetProgenyUserIsAdminInCache(string email);
        Task<List<UserAccess>> GetProgenyUserAccessList(int progenyId);
        Task<List<UserAccess>> SetProgenyUserAccessListInCache(int progenyId);
        Task<List<UserAccess>> GetUsersUserAccessList(string email);
        Task<List<UserAccess>> GetUsersUserAdminAccessList(string email);
        Task<List<UserAccess>> SetUsersUserAccessListInCache(string email);
        Task<UserAccess> GetUserAccess(int id);
        Task<UserAccess> SetUserAccessInCache(int id);
        Task<UserAccess> AddUserAccess(UserAccess userAccess);
        Task<UserAccess> UpdateUserAccess(UserAccess userAccess);
        Task RemoveUserAccess(int id, int progenyId, string userId);
        Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail);
        Task UpdateProgenyAdmins(Progeny progeny);
    }
}
