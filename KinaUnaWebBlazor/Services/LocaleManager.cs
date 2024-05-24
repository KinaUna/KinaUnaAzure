using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWebBlazor.Models.HomeViewModels;

namespace KinaUnaWebBlazor.Services
{
    public class LocaleManager(ILanguagesHttpClient languagesHttpClient, ITranslationsHttpClient translationsHttpClient, IPageTextsHttpClient pageTextsHttpClient)
        : ILocaleManager
    {
        public async Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId)
        {
            SetLanguageIdViewModel languageIdModel = new SetLanguageIdViewModel();
            languageIdModel.LanguageList = await languagesHttpClient.GetAllLanguages();
            languageIdModel.SelectedId = currentLanguageId;

            return languageIdModel;

        }

        public async Task<List<KinaUnaLanguage>?> GetAllLanguages()
        {
            return await languagesHttpClient.GetAllLanguages();
        }
        
        public async Task<string> GetTranslation(string word, string page, int languageId)
        {
            string translation = await translationsHttpClient.GetTranslation(word, page, languageId);
            return translation;
        }

        public async Task<KinaUnaText?> GetPageTextByTitle(string title, string page, int languageId)
        {
            KinaUnaText? text = await pageTextsHttpClient.GetPageTextByTitle(title, page, languageId);
            return text;
        }

        public int GetLanguageId(HttpRequest request)
        {
            return request.GetLanguageIdFromCookie();
        }
    }
}
