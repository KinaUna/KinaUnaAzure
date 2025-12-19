using System.Threading.Tasks;
using KinaUna.Data.Models.Support;

namespace KinaUnaWeb.Services.HttpClients.Support
{
    public interface IHelpHttpClient
    {
        Task<HelpContent> GetHelpContent(string page, string element, int languageId);
    }
}
