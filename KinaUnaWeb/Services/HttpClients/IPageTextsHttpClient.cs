using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the PageTexts API.
    /// Contains the methods for adding, retrieving and updating data relevant to page text functions.
    /// </summary>
    public interface IPageTextsHttpClient
    {
        /// <summary>
        /// Gets the KinaUnaText with the given Title and Page, translated into the given language.
        /// If the text does not exist, a new KinaUnaText entry is created.
        /// </summary>
        /// <param name="title">The Title property of the KinaUnaText to get.</param>
        /// <param name="page">The name of the page the text appears on.</param>
        /// <param name="languageId">The language to get the text in.</param>
        /// <param name="updateCache">If false, attempts to get the KinaUnaText from the cache first. If true, force updates the cache.</param>
        /// <returns>KinaUnaText object.</returns>
        Task<KinaUnaText> GetPageTextByTitle(string title, string page, int languageId, bool updateCache = false);

        /// <summary>
        /// Gets the KinaUnaText with the given Id.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText to get.</param>
        /// <param name="updateCache">If False, attempts to get the item from cache first. If True, gets the item from the API and updates the cache.</param>
        /// <returns>The KinaUnaText object with the given Id.</returns>
        Task<KinaUnaText> GetPageTextById(int id, bool updateCache = false);

        /// <summary>
        /// Updates a KinaUnaText entry.
        /// </summary>
        /// <param name="kinaUnaText">The KinaUnaText object with the updated properties.</param>
        /// <returns>The updated KinaUnaText object.</returns>
        Task<KinaUnaText> UpdatePageText(KinaUnaText kinaUnaText);

        /// <summary>
        /// Gets the list of all KinaUnaTexts in a given language.
        /// </summary>
        /// <param name="languageId">The LanguageId of the KinaUnaTexts to get.</param>
        /// <param name="updateCache">If False, attempts to get the ist from cache first. If True, gets the list from the API and updates the cache.</param>
        /// <returns>List of KinaUnaText objects.</returns>
        Task<List<KinaUnaText>> GetAllKinaUnaTexts(int languageId = 0, bool updateCache = false);
    }
}
