using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    public interface IAuthHttpClient
    {
        Task<UserInfo> CheckDeleteUser(UserInfo userInfo);
        Task<UserInfo> RemoveDeleteUser(UserInfo userInfo);
        Task<bool> IsApplicationUserValid(string userId);
    }
}
