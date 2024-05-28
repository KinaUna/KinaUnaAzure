using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.HomeViewModels;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Http;

namespace KinaUnaWeb.Services
{
    public class LocaleManager(ILanguagesHttpClient languagesHttpClient, ITranslationsHttpClient translationsHttpClient, IPageTextsHttpClient pageTextsHttpClient)
        : ILocaleManager
    {
        public async Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId)
        {
            SetLanguageIdViewModel languageIdModel = new()
            {
                LanguageList = await languagesHttpClient.GetAllLanguages(),
                SelectedId = currentLanguageId
            };

            return languageIdModel;

        }
        
        public async Task<string> GetTranslation(string word, string page, int languageId)
        {
            string translation = await translationsHttpClient.GetTranslation(word, page, languageId);
            return translation;
        }

        public async Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId)
        {
            KinaUnaText text = await pageTextsHttpClient.GetPageTextByTitle(title, page, languageId);
            return text;
        }

        public int GetLanguageId(HttpRequest request)
        {
            return request.GetLanguageIdFromCookie();
        }
    }
}
