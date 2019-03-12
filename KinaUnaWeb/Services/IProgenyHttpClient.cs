using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    public interface IProgenyHttpClient
    {
        Task<HttpClient> GetClient();

        Task<UserInfo> GetUserInfo(string email);
        Task<UserInfo> GetUserInfoByUserId(string userId);
        Task<Progeny> GetProgeny(int progenyId);
        Task<Progeny> AddProgeny(Progeny progeny);
        Task<Progeny> UpdateProgeny(Progeny progeny);
        Task<bool> DeleteProgeny(int progenyId);
        Task<List<Progeny>> GetProgenyAdminList(string email);
        Task<List<UserAccess>> GetProgenyAccessList(int progenyId);
        Task<List<UserAccess>> GetUserAccessList(string userEmail);
        Task<List<Location>> GetProgenyLocations(int progenyId, int accessLevel);
        Task<UserAccess> GetUserAccess(int userAccessId);
        Task<UserAccess> AddUserAccess(UserAccess userAccess);
        Task<UserAccess> UpdateUserAccess(UserAccess userAccess);
        Task<bool> DeleteUserAccess(int userAccessId);
        Task<UserInfo> UpdateUserInfo(UserInfo userinfo);
    }
}
