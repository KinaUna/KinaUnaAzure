using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IUserAccessService
    {
        Task<List<Progeny>> GetProgenyUserIsAdmin(string email);
        Task<List<Progeny>> SetProgenyUserIsAdmin(string email);
        Task<List<UserAccess>> GetProgenyUserAccessList(int progenyId);
        Task<List<UserAccess>> SetProgenyUserAccessList(int progenyId);
        Task<List<UserAccess>> GetUsersUserAccessList(string email);
        Task<List<UserAccess>> SetUsersUserAccessList(string email);
        Task<UserAccess> GetUserAccess(int id);
        Task<UserAccess> SetUserAccess(int id);
        Task<UserAccess> AddUserAccess(UserAccess userAccess);
        Task<UserAccess> UpdateUserAccess(UserAccess userAccess);
        Task RemoveUserAccess(int id, int progenyId, string userId);
        Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail);
        Task UpdateProgenyAdmins(Progeny progeny);
    }
}
