using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.IDP.Models.HomeViewModels;
using Microsoft.AspNetCore.Http;

namespace KinaUna.IDP.Services
{
    public interface ILocaleManager
    {
        Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId);
        Task<string> GetTranslation(string word, string page, int languageId, bool updateCache = false);
        Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId);
        int GetLanguageId(HttpRequest request);
    }
}
