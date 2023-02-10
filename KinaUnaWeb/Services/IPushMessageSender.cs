using KinaUna.Data.Models;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    public interface IPushMessageSender
    {
        Task SendMessage(string user, string title, string message, string link, string tag);
        Task<PushDevices> GetPushDeviceById(int id);
    }
}
