using System.Threading.Tasks;
using KinaUnaWeb.Models;

namespace KinaUnaWeb.Services
{
    public interface IProgenyManager
    {
        Task<UserInfo> GetInfo(string userEmail);
        string GetImageUrl(string pictureLink, string pictureContainer);
    }
}
