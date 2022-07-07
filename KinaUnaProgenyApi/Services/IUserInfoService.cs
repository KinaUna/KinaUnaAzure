using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IUserInfoService
    {
        Task<List<UserInfo>> GetAllUserInfos();
        Task<UserInfo> GetUserInfoByEmail(string userEmail);
        Task<UserInfo> SetUserInfoByEmail(string userEmail);
        Task<UserInfo> AddUserInfo(UserInfo userInfo);
        Task<UserInfo> UpdateUserInfo(UserInfo userInfo);
        Task<UserInfo> DeleteUserInfo(UserInfo userInfo);
        Task RemoveUserInfoByEmail(string userEmail, string userId, int userInfoId);
        Task<UserInfo> GetUserInfoById(int id);
        Task<UserInfo> GetUserInfoByUserId(string id);
        Task<List<UserInfo>> GetDeletedUserInfos();
        Task<bool> IsAdminUserId(string userId);
    }
}
