using System.Threading.Tasks;
using KinaUnaWeb.Models.HomeViewModels;

namespace KinaUnaWeb.Services
{
    public class LocaleManager:ILocaleManager
    {
        private readonly ILanguagesHttpClient _languagesHttpClient;
        private readonly ITranslationsHttpClient _translationsHttpClient;
        
        public LocaleManager(ILanguagesHttpClient languagesHttpClient, ITranslationsHttpClient translationsHttpClient)
        {
            _languagesHttpClient = languagesHttpClient;
            _translationsHttpClient = translationsHttpClient;
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
    }
}
