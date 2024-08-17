using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.HomeViewModels;
using Microsoft.AspNetCore.Http;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Service for managing languages, translations and page texts.
    /// </summary>
    public interface ILocaleManager
    {
        /// <summary>
        /// Generates a SetLanguageIdViewModel object with a list of available languages and the current language id.
        /// </summary>
        /// <param name="currentLanguageId">The Id of the currently selected language.</param>
        /// <returns>SetLanguageIdViewModel object.</returns>
        Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId);

        /// <summary>
        /// Gets a translation for a given word (or phrase) on a given page, in a given language.
        /// </summary>
        /// <param name="word">The word (or phrase) to get a translation for.</param>
        /// <param name="page">The page the word appears on.</param>
        /// <param name="languageId">The Id of the language to translate the word into.</param>
        /// <returns>String with the translated word.</returns>
        Task<string> GetTranslation(string word, string page, int languageId);

        /// <summary>
        /// Gets a KinaUnaText object for a given title on a given page, in a given language.
        /// </summary>
        /// <param name="title">The Title of the KinaUnaText.</param>
        /// <param name="page">The page the text appears on.</param>
        /// <param name="languageId">The Id of the language it should be translated into.</param>
        /// <returns>KinaUnaText object.</returns>
        Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId);

        /// <summary>
        /// Gets the current user's language Id.
        /// First tries to get the language Id from the cookie, if not found, it tries to get the language from the browser settings.
        /// If no language is found, it defaults to language id 1 = English.
        /// </summary>
        /// <param name="request">The current HttpRequest.</param>
        /// <returns>Integer for the language Id.</returns>
        int GetLanguageId(HttpRequest request);
    }
}
