using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.HomeViewModels;
using Microsoft.AspNetCore.Http;

namespace KinaUnaWeb.Services
{
    public class LocaleManager:ILocaleManager
    {
        private readonly ILanguagesHttpClient _languagesHttpClient;
        private readonly ITranslationsHttpClient _translationsHttpClient;
        private readonly IPageTextsHttpClient _pageTextsHttpClient;
        public LocaleManager(ILanguagesHttpClient languagesHttpClient, ITranslationsHttpClient translationsHttpClient, IPageTextsHttpClient pageTextsHttpClient)
        {
            _languagesHttpClient = languagesHttpClient;
            _translationsHttpClient = translationsHttpClient;
            _pageTextsHttpClient = pageTextsHttpClient;
        }

        public async Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId)
        {
            SetLanguageIdViewModel languageIdModel = new SetLanguageIdViewModel();
            languageIdModel.LanguageList = await _languagesHttpClient.GetAllLanguages();
            languageIdModel.SelectedId = currentLanguageId;

            return languageIdModel;

        }

        
        public async Task<string> GetTranslation(string word, string page, int languageId)
        {
            string translation = await _translationsHttpClient.GetTranslation(word, page, languageId);
            return translation;
        }

        public async Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId)
        {
            KinaUnaText text = await _pageTextsHttpClient.GetPageTextByTitle(title, page, languageId);
            return text;
        }

        public int GetLanguageId(HttpRequest request)
        {
            return request.GetLanguageIdFromCookie();
        }
    }
}
