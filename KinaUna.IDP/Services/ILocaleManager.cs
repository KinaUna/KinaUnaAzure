using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.IDP.Models.HomeViewModels;
using Microsoft.AspNetCore.Http;

namespace KinaUna.IDP.Services
{
    /// <summary>
    /// Dependency injection interface for handling languages, translations, and page texts.
    /// </summary>
    public interface ILocaleManager
    {
        /// <summary>
        /// Gets a SetLanguageIdViewModel for displaying a select list with a list of all languages and the currently selected language.
        /// </summary>
        /// <param name="currentLanguageId">The LanguageId of the user's currently selected language.</param>
        /// <returns>SetLanguageIdViewModel</returns>
        Task<SetLanguageIdViewModel> GetLanguageModel(int currentLanguageId);

        /// <summary>
        /// Gets a translation for a word on a specific page in a specific language.
        /// If the translation is not found, adds it to the database.
        /// Gets the translations via the ProgenyApi.
        /// </summary>
        /// <param name="word">The word to translate.</param>
        /// <param name="page">The page the word appears on.</param>
        /// <param name="languageId">The LanguageId of the language the word should be translated to.</param>
        /// <param name="updateCache">If true gets the translation directly from the ProgenyApi and updates the cache, if false attempts to get it from the cache first.</param>
        /// <returns>String with the translation.</returns>
        Task<string> GetTranslation(string word, string page, int languageId, bool updateCache = false);

        /// <summary>
        /// Gets a KinaUnaText item by title and page in a specific language.
        /// </summary>
        /// <param name="title">The Title of the KinaUnaText to get.</param>
        /// <param name="page">The page the KinaUnaText appears on.</param>
        /// <param name="languageId">The LanguageId of the language to translate it to.</param>
        /// <returns>KinaUnaText</returns>
        Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId);

        /// <summary>
        /// Gets the LanguageId from the user's cookie.
        /// If not found, returns 1 (English).
        /// </summary>
        /// <param name="request">The HttpRequest of the current user.</param>
        /// <returns>Integer with the LanguageId.</returns>
        int GetLanguageId(HttpRequest request);
    }
}
