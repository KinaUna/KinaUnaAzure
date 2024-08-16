using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Languages API.
    /// </summary>
    public interface ILanguagesHttpClient
    {
        /// <summary>
        /// Gets the list of all KinaUnaLanguages from the cache.
        /// If the list is not found in the cache, or if updateCache is true, the list is fetched from the API.
        /// </summary>
        /// <param name="updateCache">Get the list from the API first and update the cache.</param>
        /// <returns>List of KinaUnaLanguage objects.</returns>
        Task<List<KinaUnaLanguage>> GetAllLanguages(bool updateCache = false);

        /// <summary>
        /// Gets the KinaUnaLanguage with the given Id.
        /// </summary>
        /// <param name="languageId">The Id of the KinaUnaLanguage to get.</param>
        /// <param name="updateCache">Get the KinaUnaLanguage from the API first and update the cache.</param>
        /// <returns>KinaUnaLanguage object with the given Id.</returns>
        Task<KinaUnaLanguage> GetLanguage(int languageId, bool updateCache = false);

        /// <summary>
        /// Adds a new KinaUnaLanguage.
        /// Only KinaUnaAdmins can add new languages.
        /// </summary>
        /// <param name="language">The KinaUnaLanguage object to add.</param>
        /// <returns>The added KinaUnaLanguage object.</returns>
        Task<KinaUnaLanguage> AddLanguage(KinaUnaLanguage language);

        /// <summary>
        /// Updates a KinaUnaLanguage.
        /// Only KinaUnaAdmins can update languages.
        /// </summary>
        /// <param name="language">The KinaUnaLanguage object with the updated properties.</param>
        /// <returns>The updated KinaUnaLanguage object.</returns>
        Task<KinaUnaLanguage> UpdateLanguage(KinaUnaLanguage language);

        /// <summary>
        /// Deletes a KinaUnaLanguage and removes it from the cache.
        /// Also removes the list of all languages from the cache, forcing a cache update.
        /// Only KinaUnaAdmins can delete languages.
        /// </summary>
        /// <param name="language">The KinaUnaLanguage object to delete.</param>
        /// <returns>The deleted KinaUnaLanguage object.</returns>
        Task<KinaUnaLanguage> DeleteLanguage(KinaUnaLanguage language);
    }
}
