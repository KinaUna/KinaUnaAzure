using KinaUna.Data.Models;
using KinaUnaWebBlazor.Models.HomeViewModels;

namespace KinaUnaWebBlazor.Services
{
    public interface ILocaleManager
    {
        Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId);
        Task<List<KinaUnaLanguage>?> GetAllLanguages();
        Task<string> GetTranslation(string word, string page, int languageId);
        Task<KinaUnaText?> GetPageTextByTitle(string title, string page, int languageId);
        int GetLanguageId(HttpRequest request);
    }
}
