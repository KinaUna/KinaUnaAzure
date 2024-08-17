using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.HomeViewModels;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Http;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Service for managing languages, translations and page texts.
    /// </summary>
    /// <param name="languagesHttpClient"></param>
    /// <param name="translationsHttpClient"></param>
    /// <param name="pageTextsHttpClient"></param>
    public class LocaleManager(ILanguagesHttpClient languagesHttpClient, ITranslationsHttpClient translationsHttpClient, IPageTextsHttpClient pageTextsHttpClient)
        : ILocaleManager
    {
        /// <summary>
        /// Generates a SetLanguageIdViewModel object with a list of available languages and the current language id.
        /// </summary>
        /// <param name="currentLanguageId">The Id of the currently selected language.</param>
        /// <returns>SetLanguageIdViewModel object.</returns>
        public async Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId)
        {
            SetLanguageIdViewModel languageIdModel = new()
            {
                LanguageList = await languagesHttpClient.GetAllLanguages(),
                SelectedId = currentLanguageId
            };

            return languageIdModel;

        }

        /// <summary>
        /// Gets a translation for a given word (or phrase) on a given page, in a given language.
        /// </summary>
        /// <param name="word">The word (or phrase) to get a translation for.</param>
        /// <param name="page">The page the word appears on.</param>
        /// <param name="languageId">The Id of the language to translate the word into.</param>
        /// <returns>String with the translated word.</returns>
        public async Task<string> GetTranslation(string word, string page, int languageId)
        {
            string translation = await translationsHttpClient.GetTranslation(word, page, languageId);
            return translation;
        }

        /// <summary>
        /// Gets a KinaUnaText object for a given title on a given page, in a given language.
        /// </summary>
        /// <param name="title">The Title of the KinaUnaText.</param>
        /// <param name="page">The page the text appears on.</param>
        /// <param name="languageId">The Id of the language it should be translated into.</param>
        /// <returns>KinaUnaText object.</returns>
        public async Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId)
        {
            KinaUnaText text = await pageTextsHttpClient.GetPageTextByTitle(title, page, languageId);
            return text;
        }

        /// <summary>
        /// Gets the current user's language Id.
        /// First tries to get the language Id from the cookie, if not found, it tries to get the language from the browser settings.
        /// If no language is found, it defaults to language id 1 = English.
        /// </summary>
        /// <param name="request">The current HttpRequest.</param>
        /// <returns>Integer for the language Id.</returns>
        public int GetLanguageId(HttpRequest request)
        {
            return request.GetLanguageIdFromCookie();
        }
    }
}
