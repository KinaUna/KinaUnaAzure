using System.Threading.Tasks;
using KinaUna.Data.Models.Support;

namespace KinaUnaProgenyApi.Services.Support
{
    public interface IHelpContentService
    {
        Task<HelpContent> GetHelpContent(string page, string element, int languageId);
    }
}
