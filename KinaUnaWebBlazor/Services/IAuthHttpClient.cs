using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface IAuthHttpClient
    {
        Task<UserInfo?> CheckDeleteUser(UserInfo userInfo);
        Task<UserInfo?> RemoveDeleteUser(UserInfo userInfo);
        Task<bool> IsApplicationUserValid(string userId);
    }
}
