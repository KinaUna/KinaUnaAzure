using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ILanguageService
    {
        /// <summary>
        /// Gets a list of all KinaUnaLanguages.
        /// </summary>
        /// <returns>List of KinaUnaLanguage</returns>
        Task<List<KinaUnaLanguage>> GetAllLanguages();

        /// <summary>
        /// Gets a KinaUnaLanguage by Id.
        /// </summary>
        /// <param name="languageId">The Id of the KinaUnaLanguage.</param>
        /// <returns>KinaUnaLanguage with the given Id.</returns>
        Task<KinaUnaLanguage> GetLanguage(int languageId);

        /// <summary>
        /// Adds a new KinaUnaLanguage to the database.
        /// </summary>
        /// <param name="language">The KinaUnaLanguage to add.</param>
        /// <returns>The added KinaUnaLanguage.</returns>
        Task<KinaUnaLanguage> AddLanguage(KinaUnaLanguage language);

        /// <summary>
        /// Updates a KinaUnaLanguage in the database.
        /// </summary>
        /// <param name="language">The KinaUnaLanguage object with updated properties.</param>
        /// <returns>The updated KinaUnaLanguage object.</returns>
        Task<KinaUnaLanguage> UpdateLanguage(KinaUnaLanguage language);

        /// <summary>
        /// Deletes a KinaUnaLanguage from the database.
        /// </summary>
        /// <param name="languageId">The Id of the KinaUnaLanguage to delete.</param>
        /// <returns>The deleted KinaUnaLanguage.</returns>
        Task<KinaUnaLanguage> DeleteLanguage(int languageId);
    }
}
